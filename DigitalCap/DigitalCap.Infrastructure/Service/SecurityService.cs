using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Security;
using DigitalCap.Core.Security.Extensions;
using DigitalCap.WebApi.Core;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;


namespace DigitalCap.Infrastructure.Service
{
    public class SecurityService : ISecurityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> _roleManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserAccountRepository _userAccountRepository;

        public SecurityService(
        UserManager<ApplicationUser> userManager, RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> roleManager
            , IHttpContextAccessor httpContextAccessor, IUserAccountRepository userAccountRepository
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _httpContextAccessor = httpContextAccessor;
            _userAccountRepository = userAccountRepository;
        }

        public async Task<ServiceResult<ApplicationDto>> GetCurrentUserAsync()
        {
            try
            {
                var claimsPrincipal = _httpContextAccessor.HttpContext?.User;

                if (claimsPrincipal == null)
                    return ServiceResult<ApplicationDto>.Failure("No user context");

                // Azure AD v2 tokens use preferred_username; v1 tokens use unique_name.
                // claimsPrincipal.Identity.Name is null for v2 tokens without explicit NameClaimType mapping,
                // so we resolve the email/UPN from claims directly.
                var email = claimsPrincipal.FindFirstValue("preferred_username")
                    ?? claimsPrincipal.FindFirstValue("unique_name")
                    ?? claimsPrincipal.FindFirstValue("upn")
                    ?? claimsPrincipal.FindFirstValue(ClaimTypes.Email)
                    ?? claimsPrincipal.FindFirstValue(ClaimTypes.Name)
                    ?? claimsPrincipal.Identity?.Name;

                if (string.IsNullOrEmpty(email))
                    return ServiceResult<ApplicationDto>.Failure("No user identity in token");

                // 1. Try exact email match
                var user = await _userManager.FindByEmailAsync(email);

                // 2. Try by username (some systems store email as username)
                if (user == null)
                    user = await _userManager.FindByNameAsync(email);

                // 3. Try by the username prefix before '@' (handles domain mismatch between AD and DB)
                if (user == null && email.Contains('@'))
                {
                    var usernamePart = email.Split('@')[0];
                    user = await _userManager.FindByNameAsync(usernamePart);
                    // Also try email-style lookup with just the prefix
                    if (user == null)
                        user = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.UserName != null && u.UserName.StartsWith(usernamePart));
                }

                if (user == null)
                    return ServiceResult<ApplicationDto>.Failure($"User not found: {email}");

                await MapUserDetails(user);

                var dto = new ApplicationDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    IsEnabled = user.IsEnabled,

                    Roles = user.Roles ?? Array.Empty<string>(),
                    Permissions = user.Permissions,

                    UserAccount = user.UserAccount
                };


                return ServiceResult<ApplicationDto>.Success(dto);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static class ErrorCodes
        {
            public const string UserAlreadyInRole = nameof(UserAlreadyInRole);
        }
        private async Task MapUserDetails(ApplicationUser user)
        {
            await MapIsEnabled(user);
            await MapRolesAndClaimsToUser(user);
            await MapUserAccountToUser(user);
        }


        private async Task MapIsEnabled(ApplicationUser user)
        {
            if (user == null)
                return;

            var lockoutEndDate = await _userManager.GetLockoutEndDateAsync(user);
            user.IsEnabled = !lockoutEndDate.HasValue;
        }

        private async Task MapUserAccountToUser(ApplicationUser user)
        {
            if (user == null)
                return;

            user.UserAccount = await _userAccountRepository.GetByAspNetId(user.Id);
        }

        private async Task MapRolesAndClaimsToUser(ApplicationUser user)
        {
            if (user == null)
                return;

            user.Roles = (await _userManager.GetRolesAsync(user)).ToList();
            var userRoles = _roleManager.Roles.Where(x => user.Roles.Contains(x.Name)).ToList();
            var permissions = new List<Permission>();

            foreach (var currentRole in userRoles)
            {
                var currentClaims = await _roleManager.GetClaimsAsync(currentRole);
                var currentPermissions = currentClaims.ToPermissions();
                permissions.AddRange(currentPermissions);
            }

            var userClaims = await _userManager.GetClaimsAsync(user);
            var userPermissions = userClaims.ToPermissions();
            permissions.AddRange(userPermissions);
            var distinctPermissions = permissions.Distinct().ToList();

            user.Permissions = new HashSet<Permission>(distinctPermissions);
        }
        public IEnumerable<string> Roles
    => _roleManager.Roles.Select(r => r.Name).ToList();

        public async Task<ApplicationDto?> GetUser(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return null;

            await MapUserDetails(user);

            return new ApplicationDto
            {
                Id = user.Id, // ✅ NO conversion needed
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                IsEnabled = user.IsEnabled,
                Roles = user.Roles ?? Array.Empty<string>(),
                Permissions = user.Permissions,
                UserAccount = user.UserAccount
            };
        }


        public async Task<bool> UpdateRolesForUser(ApplicationDto userId, params string[] roles)
        {
            if (userId == null)
                return false;

            var user = await _userManager.FindByIdAsync(userId.Id.ToString());
            if (user == null)
                return false;

            // Get existing roles
            var existingRoles = await _userManager.GetRolesAsync(user);

            // Remove existing roles
            if (existingRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, existingRoles);
                if (!removeResult.Succeeded)
                    return false;
            }

            // If no new roles, we are done
            if (!(roles?.Any() ?? false))
                return true;

            // Validate roles against RoleManager
            var allValidRoles = await _roleManager.Roles
                .Select(r => r.Name)
                .ToListAsync();

            var validRoles = roles
                .Where(r => allValidRoles.Contains(r))
                .Distinct()
                .ToArray();

            if (!validRoles.Any())
                return true;

            // Add new roles
            var addResult = await _userManager.AddToRolesAsync(user, validRoles);

            return addResult.Succeeded
                || addResult.Errors.All(e => e.Code == ErrorCodes.UserAlreadyInRole);
        }

        public async Task<bool> ToggleUserIsEnabled(Guid id, bool isEnabled)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
                return false;

            var lockoutEndDate = isEnabled
                                    ? (DateTimeOffset?)null
                                    : DateTimeOffset.MaxValue;
            var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEndDate);
            return result.Succeeded;
        }
    }
}
