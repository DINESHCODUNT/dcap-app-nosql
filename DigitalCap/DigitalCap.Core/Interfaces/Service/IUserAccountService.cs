using DigitalCap.Core.DTO;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.Security;
using DigitalCap.Core.ViewModels.AccountViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IUserAccountService
    {
        Task<ServiceResult<LoginResult>> LoginAsync(LoginViewModel model);
        Task<ServiceResult<string>> ConfirmEmailAsync(string userId, string code);
        Task<ServiceResult<string>> ForgotPasswordAsync(string email);
        Task<ServiceResult<string>> ResetPasswordAsync(string email, string token, string newPassword);

        // Repository passthroughs
        Task<UserAccountModel?> GetByAspNetIdAsync(string aspNetUserId);
        Task<UserAccountModel?> GetByAspNetIdIncludingDeletedAsync(string aspNetUserId);
        Task<IEnumerable<UserAccountModel>> GetUsersInRoleAsync(string roleName);
        Task<IEnumerable<UserAccountModel>> GetUsersInRoleForClientAsync(string roleName, Guid clientId);
        Task<IEnumerable<UserAccountModel>> GetUsersWithPermissionAsync(Permission permission);
        Task<IEnumerable<CapUser>> GetAbsUsersAsync();
        Task<IEnumerable<CapUser>> GetClientUsersAsync(string clientId);

        Task CommitAsync();
        Task RequestAccessAsync(string email);

        //ManageAbsUsers
        Task<ServiceResult<bool>> DeleteAsync(string aspNetUserId);
        Task<ServiceResult<CapUsersViewModel>> ManageAbsUsers(bool userUpdated = false, bool userCreated = false);

        //ManageClientUsers
        Task<ServiceResult<CapUsersViewModel>> ManageClientUsersAsync(bool userUpdated = false, bool userCreated = false);
    }
}
