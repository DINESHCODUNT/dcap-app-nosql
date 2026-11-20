using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosClientProfileRepository : CosmosRepositoryBase<ClientsDocument>, IClientProfileRepository
    {
        public CosmosClientProfileRepository(ICosmosContainerFactory containerFactory, ILogger<CosmosClientProfileRepository> logger)
            : base(containerFactory, CosmosContainers.CAP_Clients, logger)
        {
        }

        public async Task<IReadOnlyList<Client>> GetAllClientsAsync(CancellationToken cancellationToken = default)
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var documents = await QueryAsync(query, ct: cancellationToken);
            return documents.Select(d => new Client
            {
                Id = d.Id,
                Wcn = d.Wcn,
                CompanyName = d.CompanyName!,
                CompanyAddress = d.CompanyAddress!,
                CompanyBillingEmail = d.CompanyBillingEmail!,
                CompanyBillingSystemUrl = d.CompanyBillingSystemUrl!
            }).ToList();
        }

        public async Task<List<Client>> GetAllClients()
        {
            var query = new QueryDefinition("SELECT * FROM c");
            var documents = await QueryAsync(query);
            return documents.Select(d => new Client
            {
                Id = d.Id,
                Wcn = d.Wcn,
                CompanyName = d.CompanyName!,
                CompanyAddress = d.CompanyAddress!,
                CompanyBillingEmail = d.CompanyBillingEmail!,
                CompanyBillingSystemUrl = d.CompanyBillingSystemUrl!
            }).ToList();
        }

        public async Task<Client?> GetClientAsync(Guid Id)
        {
            var id = Id.ToString("D").ToLowerInvariant();
            var doc = await ReadAsync(id, new PartitionKey(id));
            if (doc == null)
            {
                return null;
            }

            return new Client
            {
                Id = doc.Id,
                Wcn = doc.Wcn,
                CompanyName = doc.CompanyName!,
                CompanyAddress = doc.CompanyAddress!,
                CompanyBillingEmail = doc.CompanyBillingEmail!,
                CompanyBillingSystemUrl = doc.CompanyBillingSystemUrl!
            };
        }

        private static void ValidateClient(Client client, bool isInsert)
        {
            var op = isInsert ? "INSERT" : "UPDATE";
            if (client.CompanyName == null)
            {
                throw new InvalidOperationException($"Cannot insert the value NULL into column 'CompanyName', table 'CAP.Clients'; column does not allow nulls. {op} fails.\r\nThe statement has been terminated.");
            }
            if (client.CompanyAddress == null)
            {
                throw new InvalidOperationException($"Cannot insert the value NULL into column 'CompanyAddress', table 'CAP.Clients'; column does not allow nulls. {op} fails.\r\nThe statement has been terminated.");
            }
            if (client.CompanyName.Length > 150 || client.CompanyAddress.Length > 1000 ||
                (client.Wcn != null && client.Wcn.Length > 8) ||
                (client.CompanyBillingEmail != null && client.CompanyBillingEmail.Length > 50) ||
                (client.CompanyBillingSystemUrl != null && client.CompanyBillingSystemUrl.Length > 150))
            {
                throw new InvalidOperationException("String or binary data would be truncated.\r\nThe statement has been terminated.");
            }
        }

        public async Task AddAsync(Client client)
        {
            ValidateClient(client, isInsert: true);
            var id = client.Id.ToString("D").ToLowerInvariant();
            var doc = new ClientsDocument
            {
                id = id,
                Id = client.Id,
                Wcn = client.Wcn,
                CompanyName = client.CompanyName,
                CompanyAddress = client.CompanyAddress,
                CompanyBillingEmail = client.CompanyBillingEmail,
                CompanyBillingSystemUrl = client.CompanyBillingSystemUrl
            };

            await CreateAsync(doc, new PartitionKey(id));
        }

        public async Task<Guid> SaveClientProfileAsync(Client client)
        {
            if (client.Id == Guid.Empty)
            {
                ValidateClient(client, isInsert: true);
                var newId = Guid.NewGuid();
                client.Id = newId;
                var id = newId.ToString("D").ToLowerInvariant();
                var doc = new ClientsDocument
                {
                    id = id,
                    Id = newId,
                    Wcn = client.Wcn,
                    CompanyName = client.CompanyName,
                    CompanyAddress = client.CompanyAddress,
                    CompanyBillingEmail = client.CompanyBillingEmail,
                    CompanyBillingSystemUrl = client.CompanyBillingSystemUrl
                };

                await CreateAsync(doc, new PartitionKey(id));
                return newId;
            }
            else
            {
                var id = client.Id.ToString("D").ToLowerInvariant();
                var existing = await ReadAsync(id, new PartitionKey(id));
                if (existing == null)
                {
                    throw new Exception("Update failed. No rows affected.");
                }

                ValidateClient(client, isInsert: false);

                var patchOperations = new List<PatchOperation>
                {
                    PatchOperation.Set("/CompanyName", client.CompanyName),
                    PatchOperation.Set("/CompanyAddress", client.CompanyAddress),
                    PatchOperation.Set("/CompanyBillingEmail", client.CompanyBillingEmail),
                    PatchOperation.Set("/CompanyBillingSystemUrl", client.CompanyBillingSystemUrl),
                    PatchOperation.Set("/Wcn", client.Wcn)
                };

                var patched = await PatchAsync(id, new PartitionKey(id), patchOperations);
                if (patched == null)
                {
                    throw new Exception("Update failed. No rows affected.");
                }

                return client.Id;
            }
        }

        public async Task DeleteClientAsync(Guid Id)
        {
            var id = Id.ToString("D").ToLowerInvariant();
            var deleted = await DeleteAsync(id, new PartitionKey(id));
            if (!deleted)
            {
                Logger.LogInformation("Client {Id} not found for delete", Id);
            }
        }

        public async Task<IEnumerable<ClientAdminDto>> GetClientAdminsAsync(Guid clientId)
        {
            // 1. dbo_UserAccounts with partition key = clientId (lowercase GUID): all accounts of that client. No Deleted filter.
            var userAccountsContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_UserAccounts);
            var pkString = clientId.ToString("D").ToLowerInvariant();
            var accountsQuery = new QueryDefinition("SELECT c.id, c.ClientId, c.AspNetUserId, c.FirstName, c.LastName FROM c");

            var accounts = new List<UserAccountDocument>();
            using var accountsFeed = userAccountsContainer.GetItemQueryIterator<UserAccountDocument>(
                accountsQuery,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(pkString) });

            while (accountsFeed.HasMoreResults)
            {
                var response = await accountsFeed.ReadNextAsync();
                accounts.AddRange(response);
            }

            if (accounts.Count == 0)
            {
                return Enumerable.Empty<ClientAdminDto>();
            }

            // 2. dbo_AspNetUsers where ARRAY_CONTAINS(@ids, c.id) for distinct AspNetUserIds.
            var distinctUserIds = accounts
                .Select(a => a.AspNetUserId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinctUserIds.Count == 0)
            {
                return Enumerable.Empty<ClientAdminDto>();
            }

            var lowerDistinctUserIds = distinctUserIds.Select(id => id.ToLowerInvariant()).ToList();
            var aspNetUsersContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetUsers);
            var usersQuery = new QueryDefinition("SELECT c.id, c.Email FROM c WHERE ARRAY_CONTAINS(@ids, LOWER(c.id))")
                .WithParameter("@ids", lowerDistinctUserIds);

            var users = new Dictionary<string, AspNetUserDocument>(StringComparer.OrdinalIgnoreCase);
            using var usersFeed = aspNetUsersContainer.GetItemQueryIterator<AspNetUserDocument>(usersQuery);
            while (usersFeed.HasMoreResults)
            {
                var response = await usersFeed.ReadNextAsync();
                foreach (var u in response)
                {
                    users[u.id] = u;
                }
            }

            // INNER JOIN: an account whose AspNet user is not found is dropped.
            var validAccounts = accounts
                .Where(a => !string.IsNullOrEmpty(a.AspNetUserId) && users.ContainsKey(a.AspNetUserId))
                .ToList();

            if (validAccounts.Count == 0)
            {
                return Enumerable.Empty<ClientAdminDto>();
            }

            // 3. dbo_AspNetUserRoles where ARRAY_CONTAINS(@userIds, c.UserId).
            var matchedUserIds = validAccounts
                .Select(a => a.AspNetUserId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var lowerMatchedUserIds = matchedUserIds.Select(id => id.ToLowerInvariant()).ToList();
            var userRolesContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetUserRoles);
            var urQuery = new QueryDefinition("SELECT c.id, c.UserId, c.RoleId FROM c WHERE ARRAY_CONTAINS(@userIds, LOWER(c.UserId))")
                .WithParameter("@userIds", lowerMatchedUserIds);

            var userRoles = new List<AspNetUserRoleDocument>();
            using var urFeed = userRolesContainer.GetItemQueryIterator<AspNetUserRoleDocument>(urQuery);
            while (urFeed.HasMoreResults)
            {
                var response = await urFeed.ReadNextAsync();
                userRoles.AddRange(response);
            }

            var rolesByUserId = userRoles
                .GroupBy(ur => ur.UserId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleId).ToList(), StringComparer.OrdinalIgnoreCase);

            // 4. dbo_AspNetRoles where ARRAY_CONTAINS(@roleIds, c.id).
            var distinctRoleIds = userRoles
                .Select(ur => ur.RoleId)
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var roles = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (distinctRoleIds.Count > 0)
            {
                var lowerDistinctRoleIds = distinctRoleIds.Select(id => id.ToLowerInvariant()).ToList();
                var aspNetRolesContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetRoles);
                var rolesQuery = new QueryDefinition("SELECT c.id, c.Name FROM c WHERE ARRAY_CONTAINS(@roleIds, LOWER(c.id))")
                    .WithParameter("@roleIds", lowerDistinctRoleIds);

                using var rolesFeed = aspNetRolesContainer.GetItemQueryIterator<AspNetRoleDocument>(rolesQuery);
                while (rolesFeed.HasMoreResults)
                {
                    var response = await rolesFeed.ReadNextAsync();
                    foreach (var r in response)
                    {
                        roles[r.id] = r.Name;
                    }
                }
            }

            // 5, 6, 7. Map and Order
            var resultList = new List<ClientAdminDto>();
            foreach (var account in validAccounts)
            {
                var user = users[account.AspNetUserId!];
                List<string?> roleNames;
                if (rolesByUserId.TryGetValue(account.AspNetUserId!, out var roleIds) && roleIds.Count > 0)
                {
                    // A link whose role is missing -> UserRole = null (LEFT JOIN)
                    roleNames = roleIds.Select(rId => roles.TryGetValue(rId, out var rName) ? rName : null).ToList();
                }
                else
                {
                    // LEFT JOIN: a user with no role gives one row with UserRole = null
                    roleNames = new List<string?> { null };
                }

                foreach (var roleName in roleNames)
                {
                    var dto = new ClientAdminDto();
                    if (account.FirstName != null) dto.FirstName = account.FirstName;
                    if (account.LastName != null) dto.LastName = account.LastName;
                    if (user.Email != null) dto.Email = user.Email;
                    if (roleName != null) dto.UserRole = roleName;
                    resultList.Add(dto);
                }
            }

            var stringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
            var nullsFirstComparer = Comparer<string?>.Create((x, y) =>
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                return stringComparer.Compare(x.TrimEnd(' '), y.TrimEnd(' '));
            });

            return resultList
                .OrderBy(x => x.FirstName, nullsFirstComparer)
                .ThenBy(x => x.LastName, nullsFirstComparer)
                .ToList();
        }
    }
}
