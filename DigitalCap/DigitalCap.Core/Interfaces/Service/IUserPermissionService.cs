using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Permissions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IUserPermissionService
    {
        Task<ServiceResult<List<UserPermission>>> GetAllPermissionsAsync(bool isActive = false);
        Task<ServiceResult<UserPermission>> AddPermissionAsync(UserPermission permission);
        Task<ServiceResult<UserPermission>> UpdatePermissionAsync(UserPermission permission);
        Task<ServiceResult<bool>> DeletePermissionAsync(Guid id);
        Task<ServiceResult<bool>> DeleteRolePermissionAsync(Guid id);
        Task<ServiceResult<List<Role>>> GetUserRoles();
        Task<ServiceResult<List<UserRolePermissionViewModel>>> GetRolePermission(string roleId);
        Task<ServiceResult<List<UserRolePermissionViewModel>>> UpdatePermission(string Id, string RoleId, string PermissionId, string Key, bool Value);
        Task<ServiceResult<object>> GetUserMenuPermissions(string username);
    }
}
