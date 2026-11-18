using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Security;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosUserAccountRepository : CosmosRepositoryBase<UserAccountDocument>, IUserAccountRepository
    {
        public CosmosUserAccountRepository(
            ICosmosContainerFactory containerFactory,
            ILogger<CosmosUserAccountRepository> logger)
            : base(containerFactory, CosmosContainers.dbo_UserAccounts, logger)
        {
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[sp_UserAccounts_GetByAspNetId].
        /// </summary>
        public async Task<UserAccountModel> GetByAspNetId(string id)
        {
            return await GetByAspNetIdAsync(id) ?? null!;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[sp_UserAccounts_GetByAspNetId].
        /// </summary>
        public async Task<UserAccountModel?> GetByAspNetIdAsync(string aspNetUserId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(c.AspNetUserId, @id, true) AND c.Deleted = false")
                .WithParameter("@id", aspNetUserId);

            var items = await QueryAsync(query);
            if (items.Count == 0)
            {
                return null;
            }
            if (items.Count > 1)
            {
                throw new InvalidOperationException("Sequence contains more than one element");
            }

            return MapToUserAccountModel(items[0]);
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[sp_UserAccounts_GetByAspNetId_ActiveOrDeleted].
        /// </summary>
        public async Task<UserAccountModel?> GetByAspNetIdIncludingDeletedAsync(string id)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(c.AspNetUserId, @id, true)")
                .WithParameter("@id", id);

            var items = await QueryAsync(query);
            if (items.Count == 0)
            {
                return null;
            }
            if (items.Count > 1)
            {
                throw new InvalidOperationException("Sequence contains more than one element");
            }

            return MapToUserAccountModel(items[0]);
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[sp_UserAccounts_GetByAspNetId_ActiveOrDeleted].
        /// </summary>
        public async Task<UserAccountModel> GetByAspNetId_ActiveOrDeleted(string id)
        {
            return await GetByAspNetIdIncludingDeletedAsync(id) ?? null!;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[sp_UserAccounts_GetUsersInRole].
        /// </summary>
        public async Task<IEnumerable<UserAccountModel>> GetUsersInRoleAsync(string roleName)
        {
            // 1. Roles matching roleName
            var rolesContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetRoles);
            var roleQuery = new QueryDefinition("SELECT c.id FROM c WHERE STRINGEQUALS(c.Name, @roleName, true)")
                .WithParameter("@roleName", roleName);

            var roleIds = new List<string>();
            using (var feed = rolesContainer.GetItemQueryIterator<AspNetRoleDocument>(roleQuery))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    roleIds.AddRange(response.Select(r => r.id));
                }
            }

            if (roleIds.Count == 0)
            {
                return Enumerable.Empty<UserAccountModel>();
            }

            // 2. UserRole links matching roleIds
            var lowerRoleIds = roleIds.Select(r => r.ToLowerInvariant()).ToList();
            var userRolesContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetUserRoles);
            var urQuery = new QueryDefinition("SELECT c.UserId, c.RoleId FROM c WHERE ARRAY_CONTAINS(@roleIds, LOWER(c.RoleId))")
                .WithParameter("@roleIds", lowerRoleIds);

            var userRoles = new List<AspNetUserRoleDocument>();
            using (var feed = userRolesContainer.GetItemQueryIterator<AspNetUserRoleDocument>(urQuery))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    userRoles.AddRange(response);
                }
            }

            if (userRoles.Count == 0)
            {
                return Enumerable.Empty<UserAccountModel>();
            }

            var distinctUserIds = userRoles.Select(ur => ur.UserId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // 3. User accounts matching distinctUserIds and Deleted = false
            var lowerUserIds = distinctUserIds.Select(u => u.ToLowerInvariant()).ToList();
            var accQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@userIds, LOWER(c.AspNetUserId)) AND c.Deleted = false")
                .WithParameter("@userIds", lowerUserIds);

            var accounts = await QueryAsync(accQuery);
            var accountsByUserId = accounts
                .GroupBy(a => a.AspNetUserId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // INNER JOIN: one row per matching (account, link, role); no order (no Phone/TimeZone in SP)
            var result = new List<UserAccountModel>();
            foreach (var ur in userRoles)
            {
                if (accountsByUserId.TryGetValue(ur.UserId, out var matchedAccounts))
                {
                    foreach (var a in matchedAccounts)
                    {
                        result.Add(new UserAccountModel
                        {
                            Id = a.Id,
                            AspNetUserId = a.AspNetUserId!,
                            ClientId = a.ClientId,
                            FirstName = a.FirstName!,
                            LastName = a.LastName!,
                            Company = a.Company!,
                            IsAgreeToPrivacy = a.IsAgreeToPrivacy ?? false,
                            CreatedAt = a.CreatedAt ?? default,
                            CreatedBy = a.CreatedBy!,
                            UpdatedAt = a.UpdatedAt ?? default,
                            UpdatedBy = a.UpdatedBy!,
                            Deleted = a.Deleted ?? false
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[sp_UserAccounts_GetUsersInRoleForClient].
        /// </summary>
        public async Task<IEnumerable<UserAccountModel>> GetUsersInRoleForClientAsync(string roleName, Guid clientId)
        {
            // 1. Roles matching roleName
            var rolesContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetRoles);
            var roleQuery = new QueryDefinition("SELECT c.id FROM c WHERE STRINGEQUALS(c.Name, @roleName, true)")
                .WithParameter("@roleName", roleName);

            var roleIds = new List<string>();
            using (var feed = rolesContainer.GetItemQueryIterator<AspNetRoleDocument>(roleQuery))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    roleIds.AddRange(response.Select(r => r.id));
                }
            }

            if (roleIds.Count == 0)
            {
                return Enumerable.Empty<UserAccountModel>();
            }

            // 2. UserRole links matching roleIds
            var lowerRoleIds = roleIds.Select(r => r.ToLowerInvariant()).ToList();
            var userRolesContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetUserRoles);
            var urQuery = new QueryDefinition("SELECT c.UserId, c.RoleId FROM c WHERE ARRAY_CONTAINS(@roleIds, LOWER(c.RoleId))")
                .WithParameter("@roleIds", lowerRoleIds);

            var userRoles = new List<AspNetUserRoleDocument>();
            using (var feed = userRolesContainer.GetItemQueryIterator<AspNetUserRoleDocument>(urQuery))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    userRoles.AddRange(response);
                }
            }

            if (userRoles.Count == 0)
            {
                return Enumerable.Empty<UserAccountModel>();
            }

            var distinctUserIds = userRoles.Select(ur => ur.UserId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // 3. User accounts matching distinctUserIds and partition key = clientId
            var lowerUserIds = distinctUserIds.Select(u => u.ToLowerInvariant()).ToList();
            var accQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@userIds, LOWER(c.AspNetUserId)) AND c.Deleted = false")
                .WithParameter("@userIds", lowerUserIds);

            var pk = new PartitionKey(clientId.ToString("D").ToLowerInvariant());
            var accounts = await QueryAsync(accQuery, pk);
            var accountsByUserId = accounts
                .GroupBy(a => a.AspNetUserId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // INNER JOIN: one row per matching (account, link, role); includes Phone & TimeZone
            var result = new List<UserAccountModel>();
            foreach (var ur in userRoles)
            {
                if (accountsByUserId.TryGetValue(ur.UserId, out var matchedAccounts))
                {
                    foreach (var a in matchedAccounts)
                    {
                        result.Add(new UserAccountModel
                        {
                            Id = a.Id,
                            AspNetUserId = a.AspNetUserId!,
                            ClientId = a.ClientId,
                            FirstName = a.FirstName!,
                            LastName = a.LastName!,
                            Company = a.Company!,
                            IsAgreeToPrivacy = a.IsAgreeToPrivacy ?? false,
                            CreatedAt = a.CreatedAt ?? default,
                            CreatedBy = a.CreatedBy!,
                            UpdatedAt = a.UpdatedAt ?? default,
                            UpdatedBy = a.UpdatedBy!,
                            Deleted = a.Deleted ?? false,
                            Phone = a.Phone!,
                            TimeZone = a.TimeZone ?? ""
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// sp_UserAccounts_GetUsersWithPermission does not exist in the database and is not used.
        /// </summary>
        public Task<IEnumerable<UserAccountModel>> GetUsersWithPermissionAsync(DigitalCap.Core.Security.Permission permission)
        {
            throw new NotSupportedException("sp_UserAccounts_GetUsersWithPermission does not exist in the database and is not used.");
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[sp_UserAccounts_GetAbsUsers].
        /// </summary>
        public async Task<IEnumerable<CapUser>> GetAbsUsersAsync()
        {
            var accQuery = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.Company), 'ABS', true) AND c.Deleted = false");
            var accounts = await QueryAsync(accQuery);

            return await BuildCapUsersAsync(accounts, customCompanyName: null);
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[sp_UserAccounts_GetAbsUsers].
        /// </summary>
        public async Task<IEnumerable<UserAccountModel>> GetAbsUsers()
        {
            var accQuery = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.Company), 'ABS', true) AND c.Deleted = false");
            var accounts = await QueryAsync(accQuery);

            if (accounts.Count == 0)
            {
                return Enumerable.Empty<UserAccountModel>();
            }

            var userIds = accounts
                .Select(a => a.AspNetUserId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var users = new Dictionary<string, AspNetUserDocument>(StringComparer.OrdinalIgnoreCase);
            if (userIds.Count > 0)
            {
                var lowerUserIds = userIds.Select(id => id.ToLowerInvariant()).ToList();
                var aspNetUsersContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetUsers);
                var uQuery = new QueryDefinition("SELECT c.id, c.Email FROM c WHERE ARRAY_CONTAINS(@ids, LOWER(c.id))")
                    .WithParameter("@ids", lowerUserIds);

                using var feed = aspNetUsersContainer.GetItemQueryIterator<AspNetUserDocument>(uQuery);
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var u in response)
                    {
                        users[u.id] = u;
                    }
                }
            }

            // UserRoles and AspNetRoles for LEFT JOIN
            var userRoles = await GetUserRolesForUsersAsync(userIds);
            var roleIds = userRoles.Select(ur => ur.RoleId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var roles = await GetRolesByIdsAsync(roleIds);

            var rolesByUserId = userRoles
                .GroupBy(ur => ur.UserId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleId).ToList(), StringComparer.OrdinalIgnoreCase);

            var result = new List<UserAccountModel>();
            foreach (var a in accounts)
            {
                users.TryGetValue(a.AspNetUserId ?? string.Empty, out var user);
                var email = user?.Email;

                List<string?> userRoleIds;
                if (!string.IsNullOrEmpty(a.AspNetUserId) && rolesByUserId.TryGetValue(a.AspNetUserId, out var rIds) && rIds.Count > 0)
                {
                    userRoleIds = rIds.Cast<string?>().ToList();
                }
                else
                {
                    userRoleIds = new List<string?> { null };
                }

                foreach (var _ in userRoleIds)
                {
                    result.Add(new UserAccountModel
                    {
                        Id = a.Id,
                        AspNetUserId = a.AspNetUserId!,
                        FirstName = a.FirstName!,
                        LastName = a.LastName!,
                        Email = email!,
                        Phone = a.Phone!,
                        TimeZone = a.TimeZone!,
                        Company = a.Company!
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[sp_UserAccounts_GetClientUsers].
        /// </summary>
        public async Task<IEnumerable<CapUser>> GetClientUsersAsync(string clientId)
        {
            var clientGuid = Guid.Parse(clientId);
            var clientPk = clientGuid.ToString("D").ToLowerInvariant();

            var accQuery = new QueryDefinition("SELECT * FROM c WHERE c.Deleted = false");
            var accounts = await QueryAsync(accQuery, new PartitionKey(clientPk));

            // Point-read CompanyName from CAP_Clients
            var clientsContainer = ContainerFactory.GetContainer(CosmosContainers.CAP_Clients);
            string? companyName = null;
            try
            {
                var response = await clientsContainer.ReadItemAsync<ClientsDocument>(clientPk, new PartitionKey(clientPk));
                companyName = response.Resource?.CompanyName;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                companyName = null;
            }

            return await BuildCapUsersAsync(accounts, companyName);
        }

        /// <summary>
        /// Inline query: client admins ordered by LastName, FirstName.
        /// </summary>
        public async Task<List<AccountDto>> GetClientAdminsAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            try
            {
                var pkString = clientId.ToString("D").ToLowerInvariant();
                var accQuery = new QueryDefinition("SELECT * FROM c");
                var accounts = await QueryAsync(accQuery, new PartitionKey(pkString), cancellationToken);

                if (accounts.Count == 0)
                {
                    Logger.LogInformation("Retrieved 0 admins for client {ClientId}", clientId);
                    return new List<AccountDto>();
                }

                var distinctUserIds = accounts
                    .Select(a => a.AspNetUserId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (distinctUserIds.Count == 0)
                {
                    Logger.LogInformation("Retrieved 0 admins for client {ClientId}", clientId);
                    return new List<AccountDto>();
                }

                var lowerDistinctUserIds = distinctUserIds.Select(id => id.ToLowerInvariant()).ToList();
                var aspNetUsersContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetUsers);
                var uQuery = new QueryDefinition("SELECT c.id, c.Email FROM c WHERE ARRAY_CONTAINS(@ids, LOWER(c.id))")
                    .WithParameter("@ids", lowerDistinctUserIds);

                var users = new Dictionary<string, AspNetUserDocument>(StringComparer.OrdinalIgnoreCase);
                using (var feed = aspNetUsersContainer.GetItemQueryIterator<AspNetUserDocument>(uQuery))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync(cancellationToken);
                        foreach (var u in response)
                        {
                            users[u.id] = u;
                        }
                    }
                }

                var result = new List<AccountDto>();
                foreach (var ua in accounts)
                {
                    if (!string.IsNullOrEmpty(ua.AspNetUserId) && users.TryGetValue(ua.AspNetUserId, out var u))
                    {
                        result.Add(new AccountDto
                        {
                            Id = u.id,
                            FirstName = ua.FirstName!,
                            LastName = ua.LastName!,
                            Email = u.Email!,
                            Phone = ua.Phone!,
                            TimeZone = ua.TimeZone!,
                            ClientId = ua.ClientId
                        });
                    }
                }

                var stringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
                var nullsFirstComparer = Comparer<string?>.Create((x, y) =>
                {
                    if (x == null && y == null) return 0;
                    if (x == null) return -1;
                    if (y == null) return 1;
                    return stringComparer.Compare(x, y);
                });

                var ordered = result
                    .OrderBy(x => x.LastName, nullsFirstComparer)
                    .ThenBy(x => x.FirstName, nullsFirstComparer)
                    .ToList();

                Logger.LogInformation("Retrieved {Count} admins for client {ClientId}", ordered.Count, clientId);
                return ordered;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving client admins for client {ClientId}", clientId);
                throw;
            }
        }

        #region IRepositoryBase<UserAccountModel, Guid>
        public async Task<Guid> InsertAsync(UserAccountModel entity)
        {
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }
            if (!entity.CreatedAt.HasValue || entity.CreatedAt.Value == DateTime.MinValue)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }

            var doc = new UserAccountDocument
            {
                id = entity.Id.ToString("D").ToLowerInvariant(),
                Id = entity.Id,
                AspNetUserId = entity.AspNetUserId,
                ClientId = entity.ClientId,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Company = entity.Company,
                IsAgreeToPrivacy = entity.IsAgreeToPrivacy,
                CreatedAt = entity.CreatedAt,
                CreatedBy = entity.CreatedBy,
                UpdatedAt = entity.UpdatedAt,
                UpdatedBy = entity.UpdatedBy,
                Deleted = entity.Deleted,
                hm3d_userid = null,
                Phone = entity.Phone,
                TimeZone = entity.TimeZone
            };

            await CreateAsync(doc, new PartitionKey(entity.ClientId.ToString("D").ToLowerInvariant()));
            return entity.Id;
        }

        public Guid Insert(UserAccountModel entity) => InsertAsync(entity).GetAwaiter().GetResult();

        public async Task<bool> UpdateAsync(UserAccountModel entity)
        {
            if (entity.Id == Guid.Empty)
            {
                throw new ArgumentException("Entity Id cannot be null or empty for update operation", nameof(entity));
            }

            var idString = entity.Id.ToString("D").ToLowerInvariant();
            var findQuery = new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", idString);
            var items = await QueryAsync(findQuery);
            if (items.Count == 0)
            {
                return false;
            }

            var existingDoc = items[0];
            entity.UpdatedAt = DateTime.UtcNow;

            var updatedDoc = new UserAccountDocument
            {
                id = existingDoc.id,
                Id = entity.Id,
                AspNetUserId = entity.AspNetUserId,
                ClientId = entity.ClientId,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Company = entity.Company,
                IsAgreeToPrivacy = entity.IsAgreeToPrivacy,
                CreatedAt = entity.CreatedAt,
                CreatedBy = entity.CreatedBy,
                UpdatedAt = entity.UpdatedAt,
                UpdatedBy = entity.UpdatedBy,
                Deleted = entity.Deleted,
                hm3d_userid = existingDoc.hm3d_userid,
                Phone = entity.Phone,
                TimeZone = entity.TimeZone
            };

            if (entity.ClientId == existingDoc.ClientId)
            {
                await ReplaceAsync(updatedDoc, updatedDoc.id, new PartitionKey(entity.ClientId.ToString("D").ToLowerInvariant()));
                return true;
            }
            else
            {
                // Partition key changed: create in new partition, delete from old
                await CreateAsync(updatedDoc, new PartitionKey(entity.ClientId.ToString("D").ToLowerInvariant()));
                try
                {
                    await DeleteAsync(existingDoc.id, new PartitionKey(existingDoc.ClientId.ToString("D").ToLowerInvariant()));
                }
                catch
                {
                    // Rollback creation in new partition if deletion failed
                    try
                    {
                        await DeleteAsync(updatedDoc.id, new PartitionKey(updatedDoc.ClientId.ToString("D").ToLowerInvariant()));
                    }
                    catch
                    {
                    }
                    throw;
                }
                return true;
            }
        }

        public bool Update(UserAccountModel entity) => UpdateAsync(entity).GetAwaiter().GetResult();

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await GetAsync(id);
            if (existing == null)
            {
                return false;
            }

            existing.Deleted = true;
            return await UpdateAsync(existing);
        }

        public bool Delete(Guid id) => throw new NotImplementedException();

        public async Task<UserAccountModel?> GetAsync(Guid id, bool includeDeleted = false, bool useTransaction = true)
        {
            var idString = id.ToString("D").ToLowerInvariant();
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", idString);
            var items = await QueryAsync(query);
            if (items.Count == 0)
            {
                return null;
            }

            var doc = items[0];
            if (!includeDeleted && doc.Deleted == true)
            {
                return null;
            }

            return MapToUserAccountModel(doc);
        }

        public UserAccountModel Get(Guid id, bool includeDeleted = false, bool useTransaction = true)
            => GetAsync(id, includeDeleted, useTransaction).GetAwaiter().GetResult()!;

        public void Commit() { }
        public void Rollback() { }

        public Task<IQueryable<UserAccountModel>> GetAllAsync(bool useTransaction = true)
            => throw new NotSupportedException("UserAccountRepository.GetAllAsync is not used by the app and is not converted to Cosmos.");

        public IQueryable<UserAccountModel> GetFiltered(Expression<Func<UserAccountModel, bool>> filter)
            => throw new NotSupportedException("UserAccountRepository.GetFiltered is not used by the app and is not converted to Cosmos.");

        public UserAccountModel FirstOrDefault(Expression<Func<UserAccountModel, bool>> filter)
            => throw new NotSupportedException("UserAccountRepository.FirstOrDefault is not used by the app and is not converted to Cosmos.");

        public UserAccountModel First(Expression<Func<UserAccountModel, bool>> filter)
            => throw new NotSupportedException("UserAccountRepository.First is not used by the app and is not converted to Cosmos.");

        public IQueryable<UserAccountModel> ExecuteQuery(string sql)
            => throw new NotSupportedException("UserAccountRepository.ExecuteQuery is not used by the app and is not converted to Cosmos.");

        public IQueryable<UserAccountModel> ExecuteQuery(string sql, object[] parameters)
            => throw new NotSupportedException("UserAccountRepository.ExecuteQuery is not used by the app and is not converted to Cosmos.");
        #endregion

        #region Helpers
        private static UserAccountModel MapToUserAccountModel(UserAccountDocument doc)
        {
            return new UserAccountModel
            {
                Id = doc.Id,
                AspNetUserId = doc.AspNetUserId!,
                ClientId = doc.ClientId,
                FirstName = doc.FirstName!,
                LastName = doc.LastName!,
                Company = doc.Company!,
                IsAgreeToPrivacy = doc.IsAgreeToPrivacy ?? false,
                CreatedAt = doc.CreatedAt ?? default,
                CreatedBy = doc.CreatedBy!,
                UpdatedAt = doc.UpdatedAt ?? default,
                UpdatedBy = doc.UpdatedBy!,
                Deleted = doc.Deleted ?? false,
                Phone = doc.Phone!,
                TimeZone = doc.TimeZone ?? ""
            };
        }

        private async Task<List<AspNetUserRoleDocument>> GetUserRolesForUsersAsync(List<string> userIds)
        {
            if (userIds.Count == 0)
            {
                return new List<AspNetUserRoleDocument>();
            }

            var lowerUserIds = userIds.Select(id => id.ToLowerInvariant()).ToList();
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetUserRoles);
            var query = new QueryDefinition("SELECT c.UserId, c.RoleId FROM c WHERE ARRAY_CONTAINS(@userIds, LOWER(c.UserId))")
                .WithParameter("@userIds", lowerUserIds);

            var items = new List<AspNetUserRoleDocument>();
            using var feed = container.GetItemQueryIterator<AspNetUserRoleDocument>(query);
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                items.AddRange(response);
            }
            return items;
        }

        private async Task<Dictionary<string, string?>> GetRolesByIdsAsync(List<string> roleIds)
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (roleIds.Count == 0)
            {
                return dict;
            }

            var lowerRoleIds = roleIds.Select(id => id.ToLowerInvariant()).ToList();
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetRoles);
            var query = new QueryDefinition("SELECT c.id, c.NormalizedName FROM c WHERE ARRAY_CONTAINS(@roleIds, LOWER(c.id))")
                .WithParameter("@roleIds", lowerRoleIds);

            using var feed = container.GetItemQueryIterator<AspNetRoleDocument>(query);
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                foreach (var r in response)
                {
                    dict[r.id] = r.NormalizedName;
                }
            }
            return dict;
        }

        private async Task<IEnumerable<CapUser>> BuildCapUsersAsync(List<UserAccountDocument> accounts, string? customCompanyName)
        {
            if (accounts.Count == 0)
            {
                return Enumerable.Empty<CapUser>();
            }

            var userIds = accounts
                .Select(a => a.AspNetUserId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var users = new Dictionary<string, AspNetUserDocument>(StringComparer.OrdinalIgnoreCase);
            if (userIds.Count > 0)
            {
                var lowerUserIds = userIds.Select(id => id.ToLowerInvariant()).ToList();
                var aspNetUsersContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetUsers);
                var uQuery = new QueryDefinition("SELECT c.id, c.Email FROM c WHERE ARRAY_CONTAINS(@ids, LOWER(c.id))")
                    .WithParameter("@ids", lowerUserIds);

                using var feed = aspNetUsersContainer.GetItemQueryIterator<AspNetUserDocument>(uQuery);
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var u in response)
                    {
                        users[u.id] = u;
                    }
                }
            }

            var userRoles = await GetUserRolesForUsersAsync(userIds);
            var roleIds = userRoles.Select(ur => ur.RoleId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var roles = await GetRolesByIdsAsync(roleIds);

            var rolesByUserId = userRoles
                .GroupBy(ur => ur.UserId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleId).ToList(), StringComparer.OrdinalIgnoreCase);

            var result = new List<CapUser>();
            foreach (var a in accounts)
            {
                users.TryGetValue(a.AspNetUserId ?? string.Empty, out var user);
                var email = user?.Email;

                List<string?> roleNames;
                if (!string.IsNullOrEmpty(a.AspNetUserId) && rolesByUserId.TryGetValue(a.AspNetUserId, out var rIds) && rIds.Count > 0)
                {
                    roleNames = rIds.Select(rId => roles.TryGetValue(rId, out var rName) ? rName : null).ToList();
                }
                else
                {
                    roleNames = new List<string?> { null };
                }

                foreach (var roleName in roleNames)
                {
                    result.Add(new CapUser
                    {
                        Id = a.Id,
                        AspNetUserId = a.AspNetUserId!,
                        FirstName = a.FirstName!,
                        LastName = a.LastName!,
                        Email = email!,
                        Phone = a.Phone,
                        TimeZone = a.TimeZone!,
                        Company = customCompanyName ?? a.Company!,
                        UserRole = roleName!
                    });
                }
            }

            return result;
        }
        #endregion
    }
}
