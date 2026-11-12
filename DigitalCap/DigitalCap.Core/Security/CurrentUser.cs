using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DigitalCap.Core.Security
{
    public class CurrentUser : IUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;

        public string UserName =>
            User?.Identity?.Name ?? string.Empty;

        public string Email =>
            User?.FindFirst(ClaimTypes.Email)?.Value
            ?? User?.FindFirst("preferred_username")?.Value
            ?? string.Empty;

        public bool IsEnabled => true;

        public IReadOnlyCollection<string> Roles =>
            User?.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
                .Select(c => c.Value)
                .ToList()
            ?? new List<string>();

        public IReadOnlyCollection<Permission> Permissions =>
            new List<Permission>(); // permissions loaded later from DB/service

        public bool HasAllPermissions(params Permission[] permissions)
        {
            return false;
        }

        public bool HasAnyPermissions(params Permission[] permissions)
        {
            return false;
        }

        public UserAccountModel? UserAccount => null;
    }
}
