using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosUserPermissionRepository : CosmosRepositoryBase<RolePermissionsMappingDocument>, IUserPermissionRepository
    {
        public CosmosUserPermissionRepository(
            ICosmosContainerFactory containerFactory,
            ILogger<CosmosUserPermissionRepository> logger)
            : base(containerFactory, CosmosContainers.dbo_RolePermissionsMapping, logger)
        {
        }

        public void Commit() { }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetCurrentUserPermission].
        /// </summary>
        public async Task<PermissionViewModel> GetRolePermissionByUserName(string username, string viewname, int? projectId = null)
        {
            try
            {
                // Step 1: Role IDs
                var roleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (projectId.HasValue)
                {
                    var projectsContainer = ContainerFactory.GetContainer(CosmosContainers.CAP_Projects);
                    var projIdStr = projectId.Value.ToString();
                    ProjectSurveyorDocument? project = null;
                    try
                    {
                        var response = await projectsContainer.ReadItemAsync<ProjectSurveyorDocument>(
                            projIdStr,
                            new PartitionKey(projIdStr));
                        project = response.Resource;
                    }
                    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        project = null;
                    }

                    if (project != null)
                    {
                        // (a) LeadSurveyor = username (ignore case and trailing spaces)
                        if (!string.IsNullOrEmpty(project.LeadSurveyor) &&
                            string.Equals(project.LeadSurveyor.TrimEnd(), username.TrimEnd(), StringComparison.OrdinalIgnoreCase))
                        {
                            roleIds.Add("64cb13e1-f634-4ddd-ba73-90b8df56956f");
                        }

                        // (b) SecondSurveyor LIKE '%username%'
                        if (!string.IsNullOrEmpty(project.SecondSurveyor))
                        {
                            var regexPattern = LikeToRegex(username);
                            if (Regex.IsMatch(project.SecondSurveyor, regexPattern, RegexOptions.IgnoreCase))
                            {
                                roleIds.Add("7cb843e6-8b1d-4359-ab16-bad121f36db3");
                            }
                        }
                    }
                }

                // (c) AspNetRoles for AspNetUsers where UserName = username
                var aspNetUsersContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetUsers);
                var uQuery = new QueryDefinition("SELECT c.id FROM c WHERE STRINGEQUALS(c.UserName, @u, true)")
                    .WithParameter("@u", username);

                var userIds = new List<string>();
                using (var feed = aspNetUsersContainer.GetItemQueryIterator<AspNetUserDocument>(uQuery))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        userIds.AddRange(response.Select(u => u.id));
                    }
                }

                if (userIds.Count > 0)
                {
                    var lowerUserIds = userIds.Select(id => id.ToLowerInvariant()).ToList();
                    var userRolesContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetUserRoles);
                    var urQuery = new QueryDefinition("SELECT c.RoleId FROM c WHERE ARRAY_CONTAINS(@userIds, LOWER(c.UserId))")
                        .WithParameter("@userIds", lowerUserIds);

                    using var feed = userRolesContainer.GetItemQueryIterator<AspNetUserRoleDocument>(urQuery);
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var ur in response)
                        {
                            if (!string.IsNullOrWhiteSpace(ur.RoleId))
                            {
                                roleIds.Add(ur.RoleId);
                            }
                        }
                    }
                }

                if (roleIds.Count == 0)
                {
                    return null!;
                }

                // Step 2: Permissions matching viewname
                var userPermsContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_UserPermissions);
                var permQuery = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.PermissionName), @vn, true)")
                    .WithParameter("@vn", viewname.TrimEnd());

                var permissions = new List<UserPermissionsDocument>();
                using (var feed = userPermsContainer.GetItemQueryIterator<UserPermissionsDocument>(permQuery))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        permissions.AddRange(response);
                    }
                }

                if (permissions.Count == 0)
                {
                    return null!;
                }

                var permMap = permissions
                    .Where(p => p.Id.HasValue)
                    .ToDictionary(p => p.Id!.Value, p => p.PermissionName ?? string.Empty);

                var permGuids = permMap.Keys.ToList();

                // Step 3: RolePermissionsMapping
                var mapQuery = new QueryDefinition("SELECT * FROM c WHERE IS_DEFINED(c.RoleId) AND c.RoleId != null");
                var allMappings = await QueryAsync(mapQuery);

                var matchedMappings = allMappings.Where(m =>
                    m.RoleId.HasValue &&
                    roleIds.Contains(m.RoleId.Value.ToString("D").ToLowerInvariant()) &&
                    m.PermissionId.HasValue &&
                    permMap.ContainsKey(m.PermissionId.Value)
                ).ToList();

                if (matchedMappings.Count == 0)
                {
                    return null!;
                }

                // Step 4: Group by PermissionId, PermissionName
                var grouped = matchedMappings
                    .GroupBy(m => m.PermissionId!.Value)
                    .Select(g =>
                    {
                        var permId = g.Key;
                        var permName = permMap[permId];
                        return new PermissionViewModel
                        {
                            Read = g.Any(x => x.IsReadable == true),
                            Edit = g.Any(x => x.IsEditable == true),
                            Delete = g.Any(x => x.IsDeleted == true),
                            PermissionId = permId.ToString("D").ToUpperInvariant(),
                            PermissionName = permName.Replace(" ", string.Empty)
                        };
                    })
                    .OrderBy(x => x.PermissionId, StringComparer.Ordinal)
                    .FirstOrDefault();

                return grouped!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error retrieving permissions for user '{username}' and view '{viewname}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetCurrentUserProjects].
        /// </summary>
        public async Task<List<int>> GetCurrentUserProjects(string username)
        {
            var pattern = LikeToRegex(username);
            var query = new QueryDefinition(@"
                SELECT c.Id FROM c 
                WHERE (IS_STRING(c.LeadSurveyor) AND STRINGEQUALS(RTRIM(c.LeadSurveyor), @username, true))
                   OR (IS_STRING(c.SecondSurveyor) AND RegexMatch(c.SecondSurveyor, @pattern, 'i') AND c.IsDeleted = false)")
                .WithParameter("@username", username.TrimEnd())
                .WithParameter("@pattern", pattern);

            var projectsContainer = ContainerFactory.GetContainer(CosmosContainers.CAP_Projects);
            var result = new List<int>();
            using var feed = projectsContainer.GetItemQueryIterator<ProjectSurveyorDocument>(query);
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                result.AddRange(response.Select(p => p.Id));
            }
            return result;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetUserPermissions].
        /// </summary>
        public async Task<List<UserPermission>> GetAllPermissionsAsync()
        {
            var userPermsContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_UserPermissions);
            var query = new QueryDefinition("SELECT * FROM c");

            var items = new List<UserPermissionsDocument>();
            using (var feed = userPermsContainer.GetItemQueryIterator<UserPermissionsDocument>(query))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    items.AddRange(response);
                }
            }

            return items.Select(d => new UserPermission
            {
                Id = d.Id ?? Guid.Empty,
                PermissionName = d.PermissionName!,
                RoleName = null!
            }).ToList();
        }

        /// <summary>
        /// Insert_UserPermissions does not exist in the database and is not used.
        /// </summary>
        public Task<bool> AddPermissionAsync(UserPermission permission)
        {
            throw new NotSupportedException("Insert_UserPermissions does not exist in the database and is not used.");
        }

        /// <summary>
        /// Update_UserPermissions does not exist in the database and is not used.
        /// </summary>
        public Task<bool> UpdatePermissionAsync(UserPermission permission)
        {
            throw new NotSupportedException("Update_UserPermissions does not exist in the database and is not used.");
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[Delete_UserPermissions].
        /// </summary>
        public async Task<bool> DeletePermissionAsync(Guid id)
        {
            var userPermsContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_UserPermissions);
            var idStr = id.ToString("D").ToLowerInvariant();
            try
            {
                await userPermsContainer.DeleteItemAsync<UserPermissionsDocument>(idStr, new PartitionKey(idStr));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetRolePermissionById].
        /// </summary>
        public async Task<UserRolePermission> GetRolePermissionById(string id)
        {
            var g = Guid.Parse(id);
            var idStr = g.ToString("D").ToLowerInvariant();
            var doc = await ReadAsync(idStr, new PartitionKey(idStr));
            if (doc == null)
            {
                return null!;
            }

            return new UserRolePermission
            {
                Id = doc.Id ?? Guid.Empty,
                RoleId = doc.RoleId ?? Guid.Empty,
                PermissionId = doc.PermissionId ?? Guid.Empty,
                IsReadable = doc.IsReadable ?? false,
                IsEditable = doc.IsEditable ?? false,
                IsDeleted = doc.IsDeleted ?? false,
                ReadExist = doc.ReadExist ?? false,
                EditExist = doc.EditExist ?? false,
                DeleteExist = doc.DeleteExist ?? false
            };
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[Update_RolePermissionMapping].
        /// </summary>
        public async Task<bool> UpdateRolePermissionAsync(UserRolePermission rolepermission)
        {
            if (rolepermission == null)
            {
                throw new ArgumentNullException(nameof(rolepermission));
            }

            var idStr = rolepermission.Id.ToString("D").ToLowerInvariant();
            var patchOperations = new List<PatchOperation>
            {
                PatchOperation.Set("/PermissionId", rolepermission.PermissionId.ToString("D").ToLowerInvariant()),
                PatchOperation.Set("/RoleId", rolepermission.RoleId.ToString("D").ToLowerInvariant()),
                PatchOperation.Set("/IsReadable", rolepermission.IsReadable),
                PatchOperation.Set("/IsEditable", rolepermission.IsEditable),
                PatchOperation.Set("/IsDeleted", rolepermission.IsDeleted)
            };

            var patched = await PatchAsync(idStr, new PartitionKey(idStr), patchOperations);
            return patched != null;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[Insert_RolePermissionMapping].
        /// </summary>
        public async Task<bool> AddRolePermissionAsync(UserRolePermission rolepermission)
        {
            if (rolepermission == null)
            {
                throw new ArgumentNullException(nameof(rolepermission));
            }

            var permIdStr = rolepermission.PermissionId.ToString("D").ToLowerInvariant();
            var templateQuery = new QueryDefinition(
                "SELECT * FROM c WHERE STRINGEQUALS(c.PermissionId, @permId, true) AND (NOT IS_DEFINED(c.RoleId) OR IS_NULL(c.RoleId))")
                .WithParameter("@permId", permIdStr);

            var templates = await QueryAsync(templateQuery);
            if (templates.Count == 0)
            {
                return false;
            }
            if (templates.Count > 1)
            {
                throw new InvalidOperationException($"{templates.Count} template rows found for PermissionId '{rolepermission.PermissionId}'.");
            }

            var template = templates[0];
            var newDoc = new RolePermissionsMappingDocument
            {
                id = rolepermission.Id.ToString("D").ToLowerInvariant(),
                Id = rolepermission.Id,
                RoleId = rolepermission.RoleId,
                PermissionId = rolepermission.PermissionId,
                IsReadable = rolepermission.IsReadable,
                IsEditable = rolepermission.IsEditable,
                IsDeleted = rolepermission.IsDeleted,
                ReadExist = template.ReadExist,
                EditExist = template.EditExist,
                DeleteExist = template.DeleteExist
            };

            await CreateAsync(newDoc, new PartitionKey(newDoc.id));
            return true;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[Delete_RolePermissionMapping].
        /// </summary>
        public async Task<bool> DeleteRolePermissionAsync(Guid id)
        {
            var roleIdStr = id.ToString("D").ToLowerInvariant();
            var query = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(c.RoleId, @roleId, true)")
                .WithParameter("@roleId", roleIdStr);

            var items = await QueryAsync(query);
            if (items.Count == 0)
            {
                return false;
            }

            int deletedCount = 0;
            foreach (var item in items)
            {
                try
                {
                    var ok = await DeleteAsync(item.id, new PartitionKey(item.id));
                    if (ok)
                    {
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error deleting RolePermissionsMapping items for RoleId '{id}'. {deletedCount} of {items.Count} were deleted before failure: {ex.Message}", ex);
                }
            }

            return deletedCount > 0;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetAspNetUserRoles].
        /// </summary>
        public async Task<List<Role>> GetAllRoles()
        {
            var aspNetRolesContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_AspNetRoles);
            var query = new QueryDefinition("SELECT c.id, c.Name FROM c");

            var result = new List<Role>();
            using var feed = aspNetRolesContainer.GetItemQueryIterator<AspNetRoleDocument>(query);
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                result.AddRange(response.Select(r => new Role
                {
                    Id = r.id,
                    Name = r.Name!
                }));
            }
            return result;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetUserRolePermission].
        /// </summary>
        public async Task<List<UserRolePermissionViewModel>> GetRolePermission(string id)
        {
            var roleGuid = Guid.Parse(id);
            var roleIdStr = roleGuid.ToString("D").ToLowerInvariant();

            var query = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(c.RoleId, @roleId, true)")
                .WithParameter("@roleId", roleIdStr);

            var R = await QueryAsync(query);
            if (R.Count == 0)
            {
                return new List<UserRolePermissionViewModel>();
            }

            var permIds = R
                .Where(r => r.PermissionId.HasValue)
                .Select(r => r.PermissionId!.Value)
                .Distinct()
                .ToList();

            var permsByGuid = new Dictionary<Guid, List<string?>>();
            if (permIds.Count > 0)
            {
                var permIdStrings = permIds.Select(p => p.ToString("D").ToLowerInvariant()).ToList();
                var userPermsContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_UserPermissions);
                var pQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@ids, c.id)")
                    .WithParameter("@ids", permIdStrings);

                using var feed = userPermsContainer.GetItemQueryIterator<UserPermissionsDocument>(pQuery);
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (doc.Id.HasValue)
                        {
                            if (!permsByGuid.TryGetValue(doc.Id.Value, out var list))
                            {
                                list = new List<string?>();
                                permsByGuid[doc.Id.Value] = list;
                            }
                            list.Add(doc.PermissionName);
                        }
                    }
                }
            }

            var output = new List<UserRolePermissionViewModel>();
            foreach (var r in R)
            {
                var m = r.PermissionId.HasValue
                    ? R.Where(x => x.PermissionId.HasValue && x.PermissionId.Value == r.PermissionId.Value).ToList()
                    : new List<RolePermissionsMappingDocument>();

                List<string?> permNames;
                if (r.PermissionId.HasValue && permsByGuid.TryGetValue(r.PermissionId.Value, out var names) && names.Count > 0)
                {
                    permNames = names;
                }
                else
                {
                    permNames = new List<string?> { null };
                }

                if (m.Count == 0)
                {
                    foreach (var pName in permNames)
                    {
                        output.Add(new UserRolePermissionViewModel
                        {
                            Id = Guid.Empty,
                            RoleId = r.RoleId ?? Guid.Empty,
                            PermissionId = r.PermissionId ?? Guid.Empty,
                            IsReadable = r.IsReadable ?? false,
                            IsEditable = r.IsEditable ?? false,
                            IsDeleted = r.IsDeleted ?? false,
                            ReadExist = r.ReadExist ?? false,
                            EditExist = r.EditExist ?? false,
                            DeleteExist = r.DeleteExist ?? false,
                            PermissionName = pName!,
                            RoleName = null!
                        });
                    }
                }
                else
                {
                    foreach (var sub in m)
                    {
                        var isSubIdNotNull = sub.Id != null;
                        var target = isSubIdNotNull ? sub : r;
                        var rowId = isSubIdNotNull ? sub.Id!.Value : Guid.Empty;

                        foreach (var pName in permNames)
                        {
                            output.Add(new UserRolePermissionViewModel
                            {
                                Id = rowId,
                                RoleId = target.RoleId ?? Guid.Empty,
                                PermissionId = target.PermissionId ?? Guid.Empty,
                                IsReadable = target.IsReadable ?? false,
                                IsEditable = target.IsEditable ?? false,
                                IsDeleted = target.IsDeleted ?? false,
                                ReadExist = target.ReadExist ?? false,
                                EditExist = target.EditExist ?? false,
                                DeleteExist = target.DeleteExist ?? false,
                                PermissionName = pName!,
                                RoleName = null!
                            });
                        }
                    }
                }
            }

            return output;
        }

        #region Helpers
        private static string LikeToRegex(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return ".*";
            }

            var sb = new StringBuilder();
            foreach (var ch in input)
            {
                if (ch == '%' || ch == '_')
                {
                    sb.Append(ch);
                }
                else if (@"\^$.|?*+()[{".Contains(ch))
                {
                    sb.Append('\\').Append(ch);
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return ".*" + sb.ToString().Replace("%", ".*").Replace("_", ".") + ".*";
        }
        #endregion
    }
}
