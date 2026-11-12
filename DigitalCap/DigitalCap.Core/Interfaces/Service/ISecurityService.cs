using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface ISecurityService
    {
        Task<ApplicationDto?> GetUser(Guid userId);

        Task<bool> ToggleUserIsEnabled(Guid id, bool isEnabled);
        Task<ServiceResult<ApplicationDto>> GetCurrentUserAsync();
        IEnumerable<string> Roles { get; }
        Task<bool> UpdateRolesForUser(ApplicationDto userId, params string[] roles);
    }
}
