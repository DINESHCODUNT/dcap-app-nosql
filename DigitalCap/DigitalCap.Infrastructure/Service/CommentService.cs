using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Service
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly ILogger<CommentService> _logger;
        public CommentService(ICommentRepository commentRepository, ILogger<CommentService> logger)
        {
            _commentRepository = commentRepository;
            _logger = logger;
        }

        // MVC pattern methods (matching original)
        public async Task<IEnumerable<CommentModel>> GetComments(int projectId, int? projectTaskId)
        {
            if (projectTaskId != null && projectTaskId != 0)
            {
                return await _commentRepository.GetCommentsByProjectTaskIdAsync(projectTaskId.Value);
            }
            else
            {
                return await _commentRepository.GetCommentsByProjectIdAsync(projectId);
            }
        }

        public async Task<IEnumerable<CommentModel>> TransferExistingNotesToComment(int projectId, int? projectTaskId, string currentUser, string notes)
        {
            await CreateComment(projectId, projectTaskId, currentUser, notes);
            return await GetComments(projectId, projectTaskId);
        }

        public IEnumerable<CommentModel> SetCommentsEditableBasedOnUser(IEnumerable<CommentModel> comments, string currentUser)
        {
            foreach (var comment in comments)
            {
                var owner = comment.UserId ?? comment.CreatedBy;
                comment.Editable = !string.IsNullOrWhiteSpace(owner)
                    && string.Equals(owner.Trim(), currentUser.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            return comments;
        }

        public async Task<IEnumerable<CommentModel>> UpdateComment(int id, int projectId, string comment, string user)
        {
            var updatedComments = await _commentRepository.UpdateCommentAsync(id, projectId, null, comment, user);
            return updatedComments;
        }

        public async Task<IEnumerable<CommentModel>> CreateComment(int projectId, int? projectTaskId, string user, string comment)
        {
            var comments = await _commentRepository.CreateCommentAsync(projectId, projectTaskId, user, comment);
            return comments;
        }

        public async Task<IEnumerable<CommentModel>> DeleteComment(int id, int projectId, string comment, string user)
        {
            var updatedComments = await _commentRepository.DeleteCommentAsync(id, projectId, comment, user);
            return updatedComments;
        }
    }
}
