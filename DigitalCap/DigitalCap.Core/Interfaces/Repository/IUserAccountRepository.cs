using DigitalCap.Core.DTO;
using DigitalCap.Core.Models;
using DigitalCap.Core.Security;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IUserAccountRepository : IRepositoryBase<UserAccountModel, Guid>
    {
        Task<UserAccountModel> GetByAspNetId(string id);
        // Single user lookups
        Task<UserAccountModel> GetByAspNetIdAsync(string aspNetUserId);

        Task<UserAccountModel> GetByAspNetIdIncludingDeletedAsync(string id);

        

        // Role-based queries
        Task<IEnumerable<UserAccountModel>> GetUsersInRoleAsync(string roleName);

        Task<IEnumerable<UserAccountModel>> GetUsersInRoleForClientAsync(string roleName,Guid clientId);

        // Permission-based queries
        Task<IEnumerable<UserAccountModel>> GetUsersWithPermissionAsync(Permission permission);

        // ABS / Client users
        Task<IEnumerable<CapUser>> GetAbsUsersAsync();

        Task<IEnumerable<CapUser>> GetClientUsersAsync(string clientId);

        //Manage Abs User
        Task<IEnumerable<UserAccountModel>> GetAbsUsers();

        Task<List<AccountDto>> GetClientAdminsAsync(Guid clientId, CancellationToken cancellationToken = default);

        Task<UserAccountModel> GetByAspNetId_ActiveOrDeleted(string id);
    }
}



