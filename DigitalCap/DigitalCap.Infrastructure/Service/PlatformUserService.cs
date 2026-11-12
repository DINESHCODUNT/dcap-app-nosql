using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Service
{
    public class PlatformUserService : IPlatformUserService
    {
        private readonly IPlatformUserRepository _platformUserrepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContext;

        public PlatformUserService(
            IPlatformUserRepository platformUserrepository,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _platformUserrepository = platformUserrepository;
            _userManager = userManager;
            _httpContext = httpContextAccessor;
        }

        private async Task<string> GetLoggedInUserName()
        {
            var principal = _httpContext.HttpContext.User as ClaimsPrincipal;
            var userId = _userManager.GetUserId(principal);
            if (userId == null) return string.Empty;

            var user = await _userManager.FindByIdAsync(userId);
            return user.UserName;
        }

        private string GetLoggedInUserId()
        {
            return _userManager.GetUserId(_httpContext.HttpContext.User);
        }

        public async Task<List<UserWCNMappingModel>> GetWCNGrid()
        {
            var email = await GetLoggedInUserName();
            return (List<UserWCNMappingModel>)await _platformUserrepository.GetWCNGridAsync(email);
        }

        public async Task<string> GetTermsOfUse(int applicationId)
        {
            return await _platformUserrepository.GetTermsOfUseTextAsync(applicationId);
        }

        public async Task<IEnumerable<UserType>> GetUserType()
        {
            var userId = GetLoggedInUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<UserType>();

            var roles = await _userManager.GetRolesAsync(user);
            return roles.Select(x => (UserType)Enum.Parse(typeof(UserType), x.ToUpper())).ToList();
        }

        public async Task<bool> AddNewWCNAsync(WCNModel model)
        {
            var email = await GetLoggedInUserName();
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return await _platformUserrepository.AddNewWCNAsync(
             email, model);
        }
    }
}
