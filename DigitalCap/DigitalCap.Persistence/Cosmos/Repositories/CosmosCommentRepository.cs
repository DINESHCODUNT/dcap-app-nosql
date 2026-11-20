using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosCommentRepository : CosmosRepositoryBase<CommentDocument>, ICommentRepository
    {
        private readonly Container _loginsContainer;
        private readonly Container _usersContainer;
        private readonly CosmosActivityWriter _activityWriter;
        private readonly ISequenceService _sequenceService;

        public CosmosCommentRepository(
            ICosmosContainerFactory containerFactory,
            CosmosActivityWriter activityWriter,
            ISequenceService sequenceService,
            ILogger<CosmosCommentRepository> logger)
            : base(containerFactory, CosmosContainers.CAP_Comments, logger)
        {
            _loginsContainer = containerFactory.GetContainer(CosmosContainers.dbo_AspNetUserLogins);
            _usersContainer = containerFactory.GetContainer(CosmosContainers.dbo_AspNetUsers);
            _activityWriter = activityWriter;
            _sequenceService = sequenceService;
        }

        public void Commit() { }

        public async Task<IEnumerable<CommentModel>> GetCommentsByProjectIdAsync(int projectId)
        {
            var pk = new PartitionKey((double)projectId);
            var query = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND (IS_DEFINED(c.ProjectTaskId) = false OR IS_NULL(c.ProjectTaskId) = true)")
                .WithParameter("@projectId", projectId);

            var comments = await QueryAsync(query, pk);
            return await JoinWithLoginsAsync(comments);
        }

        public async Task<IEnumerable<CommentModel>> GetCommentsByProjectTaskIdAsync(int projectTaskId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.ProjectTaskId = @projectTaskId")
                .WithParameter("@projectTaskId", projectTaskId);

            var comments = await QueryAsync(query);
            return await JoinWithLoginsAsync(comments);
        }

        public async Task<IEnumerable<CommentModel>> CreateCommentAsync(int projectId, int? projectTaskId, string userId, string comment)
        {
            if (comment == null || userId == null)
            {
                // SQL Create_Comment: the INSERT fails (NOT NULL), but the SP has no transaction/TRY,
                // so CAP.Create_Activity still runs (Activity = @Comment, CreatedBy = looked-up user name),
                // then the error reaches the caller.
                var failText = comment == null ? null : (comment.Length > 4000 ? comment[..4000] : comment);
                var failUser = await ResolveUserNameAsync(userId);
                await _activityWriter.CreateActivityAsync(projectId, failText, failUser);
                var nullCol = comment == null ? "Comment" : "CreatedBy";
                throw new InvalidOperationException($"Cannot insert the value NULL into column '{nullCol}', table 'CAP.Comments'; column does not allow nulls. INSERT fails.\r\nThe statement has been terminated.");
            }

            var cutComment = comment.Length > 4000 ? comment[..4000] : comment;
            var cutCreatedBy = userId.Length > 450 ? userId[..450] : userId;

            var newId = await _sequenceService.GetNextAsync(CosmosContainers.CAP_Comments);

            var doc = new CommentDocument
            {
                id = newId.ToString(),
                Id = newId,
                ProjectId = projectId,
                ProjectTaskId = projectTaskId,
                CreatedBy = cutCreatedBy,
                Comment = cutComment,
                LastModifiedDate = DateTimeOffset.UtcNow,
                IsDeleted = null
            };

            var pk = new PartitionKey((double)projectId);
            await CreateAsync(doc, pk);

            var resolvedUserName = await ResolveUserNameAsync(userId);
            await _activityWriter.CreateActivityAsync(projectId, cutComment, resolvedUserName);

            if (projectTaskId.HasValue)
            {
                return await GetCommentsByProjectTaskIdAsync(projectTaskId.Value);
            }

            return await GetCommentsByProjectIdAsync(projectId);
        }

        public async Task<IEnumerable<CommentModel>> UpdateCommentAsync(int commentId, int projectId, int? projectTaskId, string comment, string user)
        {
            var findQuery = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", commentId.ToString());
            var items = await QueryAsync(findQuery);

            if (items.Count > 0)
            {
                if (comment == null)
                {
                    // SQL Update_Comment: the UPDATE fails (NOT NULL), CAP.Create_Activity still runs (Activity = NULL), then the error reaches the caller.
                    var failUser = await ResolveUserNameAsync(user);
                    await _activityWriter.CreateActivityAsync(projectId, null, failUser);
                    throw new InvalidOperationException("Cannot insert the value NULL into column 'Comment', table 'CAP.Comments'; column does not allow nulls. UPDATE fails.\r\nThe statement has been terminated.");
                }

                var doc = items[0];
                var pk = new PartitionKey((double)doc.ProjectId);
                var cutComment = comment.Length > 4000 ? comment[..4000] : comment;
                doc.Comment = cutComment;
                doc.LastModifiedDate = DateTimeOffset.UtcNow;
                await ReplaceAsync(doc, doc.id, pk);
            }

            var effectiveComment = comment != null && comment.Length > 4000 ? comment[..4000] : comment;
            var resolvedUser = await ResolveUserNameAsync(user);
            await _activityWriter.CreateActivityAsync(projectId, effectiveComment, resolvedUser);

            if (projectTaskId.HasValue)
            {
                return await GetCommentsByProjectTaskIdAsync(projectTaskId.Value);
            }

            return await GetCommentsByProjectIdAsync(projectId);
        }

        public async Task<IEnumerable<CommentModel>> DeleteCommentAsync(int commentId, int projectId, string comment, string user)
        {
            var findQuery = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", commentId.ToString());
            var items = await QueryAsync(findQuery);

            if (items.Count > 0)
            {
                var doc = items[0];
                var pk = new PartitionKey((double)doc.ProjectId);
                await DeleteAsync(doc.id, pk);
            }

            var userName = await ResolveUserNameAsync(user);
            string? activityComment = comment != null ? "Comment Deleted: " + comment : null;
            await _activityWriter.CreateActivityAsync(projectId, activityComment, userName);

            return new List<CommentModel>();
        }

        private async Task<List<CommentModel>> JoinWithLoginsAsync(List<CommentDocument> comments)
        {
            if (comments.Count == 0)
            {
                return new List<CommentModel>();
            }

            var userIds = comments
                .Where(c => c.CreatedBy != null)
                .Select(c => c.CreatedBy!.ToLowerInvariant())
                .Distinct()
                .ToList();

            var loginsByUserId = new Dictionary<string, List<AspNetUserLoginDocument>>(StringComparer.OrdinalIgnoreCase);
            if (userIds.Count > 0)
            {
                var loginsQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@userIds, LOWER(c.UserId))")
                    .WithParameter("@userIds", userIds);

                using var feed = _loginsContainer.GetItemQueryIterator<AspNetUserLoginDocument>(loginsQuery);
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var login in response)
                    {
                        if (!loginsByUserId.TryGetValue(login.UserId, out var list))
                        {
                            list = new List<AspNetUserLoginDocument>();
                            loginsByUserId[login.UserId] = list;
                        }
                        list.Add(login);
                    }
                }
            }

            var result = new List<CommentModel>();
            foreach (var c in comments)
            {
                List<AspNetUserLoginDocument>? matchingLogins = null;
                if (c.CreatedBy != null)
                {
                    loginsByUserId.TryGetValue(c.CreatedBy, out matchingLogins);
                }

                if (matchingLogins != null && matchingLogins.Count > 0)
                {
                    // One row per login
                    foreach (var login in matchingLogins)
                    {
                        var model = new CommentModel
                        {
                            Id = c.Id,
                            ProjectId = c.ProjectId,
                            ProjectTaskId = c.ProjectTaskId,
                            Comment = c.Comment!,
                            UserId = login.UserId,
                            CreatedBy = login.ProviderDisplayName!,
                            LastModifiedDate = c.LastModifiedDate ?? default
                        };
                        model.UserId ??= model.CreatedBy;
                        model.CreatedBy ??= model.UserId;
                        result.Add(model);
                    }
                }
                else
                {
                    // No matching login -> UserId and CreatedBy null
                    var model = new CommentModel
                    {
                        Id = c.Id,
                        ProjectId = c.ProjectId,
                        ProjectTaskId = c.ProjectTaskId,
                        Comment = c.Comment!,
                        UserId = null!,
                        CreatedBy = null!,
                        LastModifiedDate = c.LastModifiedDate ?? default
                    };
                    model.UserId ??= model.CreatedBy;
                    model.CreatedBy ??= model.UserId;
                    result.Add(model);
                }
            }

            return result;
        }

        private async Task<string?> ResolveUserNameAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var query = new QueryDefinition("SELECT TOP 1 c.UserName FROM c WHERE STRINGEQUALS(c.id, @userId, true)")
                .WithParameter("@userId", userId);

            using var feed = _usersContainer.GetItemQueryIterator<AspNetUserDocument>(query);
            string? foundUserName = null;
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                var user = response.FirstOrDefault();
                if (user?.UserName != null && foundUserName == null)
                {
                    foundUserName = user.UserName;
                }
            }

            return foundUserName;
        }
    }
}
