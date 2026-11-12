using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Persistence.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DigitalCap.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserPermissionController : ApiBaseController
    {
        private readonly IUserPermissionService _userPermissionService;

        public UserPermissionController(IUserPermissionService userPermissionService)
        {
            _userPermissionService = userPermissionService;
        }

        #region Permission Management

        #endregion

        #region Role Permission Management

        [HttpGet("[action]")]
        public async Task<IActionResult> GetRolePermission([FromQuery] string roleId)
        {
            if (string.IsNullOrEmpty(roleId))
                return BadRequest("RoleId is required");

            var response = await _userPermissionService.GetRolePermission(roleId);

            if (!response.IsSuccess)
                return NotFound(response);

            return Ok(response.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetUserRoles()
        {
            var response = await _userPermissionService.GetUserRoles();

            if (!response.IsSuccess)
                return StatusCode(500, response);

            return Ok(response.Data);
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> UpdatePermission(string Id, string RoleId, string PermissionId, string Key, bool Value)
        {
            var result =await _userPermissionService.UpdatePermission(Id, RoleId, PermissionId, Key, Value);

            return Ok(result);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetUserMenuPermissions()
        {
            var username = CurrentUsername;

            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            try
            {
                var result = await _userPermissionService.GetUserMenuPermissions(username);

                if (!result.IsSuccess)
                    return BadRequest(new { message = result.Message });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error fetching menu permissions",
                    error = ex.Message
                });
            }
        }
        #endregion
    }
}
