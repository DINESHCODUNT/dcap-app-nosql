using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.WebApi.Core;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace DigitalCap.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> _roleManager;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IPlatformUserRepository _platformUserRepository;
        public UserRepository(IConfiguration config, UserManager<ApplicationUser> userManager
            , RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> roleManager
            , IHttpContextAccessor httpContextAccessor, IPlatformUserRepository platformUserRepository)
        {         
            _userManager = userManager;
            _roleManager = roleManager;
            _httpContext = httpContextAccessor;
            _platformUserRepository = platformUserRepository;
        }
        public async Task<IEnumerable<UserType>> GetUserType(string objectId)
        {
            var roles = await _platformUserRepository.GetUserType(objectId);

            var er_roles = roles.Where(x => Enum.GetName(typeof(UserType), x).Contains("ER_")).ToList();                    
            
            return er_roles;
        }
        public async Task<List<ApplicationDto>> GetAllUsers()
        {
            return _userManager.Users
                .Select(u => new ApplicationDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    UserName = u.UserName
                })
                .ToList();
        }

        public async Task<string> GetLoggedInUserName()
        {
            ClaimsPrincipal principal = _httpContext.HttpContext.User as ClaimsPrincipal;
            var userId = _userManager.GetUserId(principal);
            if (userId == null) return string.Empty;
            var user = await _userManager.FindByIdAsync(userId);
            return user.UserName;
        }
    }
}
