using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.Security;
using DigitalCap.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using TimeZoneNames;
using static DigitalCap.Core.DTO.ClientProfileDTO;
using DigitalCap.Core.Models;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Constants;
using DigitalCap.WebApi.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AbsAdminController : ApiBaseController
    {
        private readonly IClientProfileService _clientProfileService;
        private readonly IUserAccountService _userAccountService;
        private readonly IManageAbsAdminService _manageAbsAdminService;
        private readonly ITankService _tankService;
        private readonly IGradingService _gradingService;
        private readonly IDescriptionService _descriptionService;
        private object _logger;

        public AbsAdminController(
            IClientProfileService clientProfileService,
            IUserAccountService userAccountService,
            IManageAbsAdminService manageAbsAdminService,
            ITankService tankService,
            IGradingService gradingService,
            IDescriptionService descriptionService)
        {
            _clientProfileService = clientProfileService;
            _userAccountService = userAccountService;
            _manageAbsAdminService = manageAbsAdminService;
            _tankService = tankService;
            _gradingService = gradingService;
            _descriptionService = descriptionService;
        }

        #region Client Profiles

        [HttpGet("[action]")]
        [PermissionAuthorize(Permission.ManageClientProfiles)]
        public async Task<IActionResult> GetClientProfiles()
        {
            var result = await _clientProfileService.GetClientSummariesAsync();

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        [PermissionAuthorize(Permission.ManageClientProfiles)]
        public async Task<IActionResult> CreateClientProfile([FromBody] ClientProfileViewModel model)
        {
            var result = await _clientProfileService.CreateClientProfileAsync(model);
            return Ok(result);
        }

        [HttpPut("[action]")]
        [PermissionAuthorize(Permission.ManageClientProfiles)]
        public async Task<IActionResult> EditClientProfile([FromBody] ClientProfileViewModel model)
        {
            if (model == null || model.Id == Guid.Empty)
                return BadRequest("Invalid data");

            var result = await _clientProfileService.EditClientProfileAsync(model);

            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return Ok(new
            {
                message = model.Id == Guid.Empty
                    ? "Client created successfully"
                    : "Client updated successfully",
                clientId = result.Data
            });
        }

        [HttpGet("[action]")]
        [PermissionAuthorize(Permission.ManageClientProfiles)]
        public async Task<ClientProfileDetailDto> GetClientProfileWithDetails(Guid clientId, bool freshmeat = false)
        {
            var result = await _clientProfileService.GetClientProfileWithDetailsAsync(clientId, freshmeat);
            return result.Data;
        }

        [HttpPost("[action]")]
        [PermissionAuthorize(Permission.ManageClientProfiles)]
        public async Task<IActionResult> DeleteClientProfile([FromBody] DeleteClientProfileRequest request)
        {
            if (request?.Id == null || request.Id == Guid.Empty)
                return BadRequest("Invalid client ID");

            await _clientProfileService.DeleteClientProfileAsync(request.Id);
            return Ok(new { message = "Client profile deleted successfully" });
        }

        [HttpGet("[action]")]
        public ActionResult<TimeZoneResponseDto> GetTimeZone([FromQuery] string clientTz)
        {
            if (string.IsNullOrWhiteSpace(clientTz))
                return BadRequest("Client time zone is required.");

            try
            {
                var timezone = TZNames.GetNamesForTimeZone(clientTz, "en-US")?.Standard;

                if (string.IsNullOrEmpty(timezone))
                    return NotFound("Invalid time zone.");

                return Ok(new TimeZoneResponseDto
                {
                    TimeZone = timezone
                });
            }
            catch (TimeZoneNotFoundException)
            {
                return NotFound("Invalid time zone.");
            }
        }

        #endregion

        #region Client Admins

        #endregion

        #region ABS Users

        [HttpGet("[action]")]
        [PermissionAuthorize(Permission.ViewAbsUsers)]
        public async Task<IActionResult> ManageAbsUsers(
            [FromQuery] bool userUpdated = false,
            [FromQuery] bool userCreated = false)
        {
            try
            {
                var result = await _userAccountService.ManageAbsUsers(userUpdated, userCreated);
                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving manage ABS users", error = ex.Message });
            }
        }

        [HttpGet("[action]")]
        [PermissionAuthorize(Permission.ManageAbsUsers)]
        public IActionResult AddAbsUser([FromQuery] string company)
        {
            return Ok(new AddClientAdminInitDto
            {
                Company = company ?? AppConstants.ABS_Company_Name,
                DefaultRole = AppConstants.Roles.ABSAdmin
            });
        }

        [HttpPost("[action]")]
        [PermissionAuthorize(Permission.ManageAbsUsers)]
        public async Task<IActionResult> AddAbsUser([FromBody] AccountViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _manageAbsAdminService.StoreAbsUserAsync(model);

                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok(new { message = "ABS user created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating ABS user", error = ex.Message });
            }
        }

        [HttpPost("[action]")]
        [PermissionAuthorize(Permission.ManageAbsUsers)]
        public async Task<IActionResult> EditAbsUser([FromBody] AccountViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _manageAbsAdminService.StoreAbsUserAsync(model);
                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok(new { message = "ABS user updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating ABS user",
                    error = ex.Message
                });
            }
        }

        [HttpPost("[action]")]
        [PermissionAuthorize(Permission.ManageAbsUsers)]
        public async Task<IActionResult> DeleteAbsUser(string aspNetUserId)
        {
            try
            {
                var result = await _userAccountService.DeleteAsync(aspNetUserId);

                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting ABS user", error = ex.Message });
            }
        }
        #endregion

        #region Client Users

        [HttpGet("[action]")]
        [PermissionAuthorize(Permission.ViewClientUsers)]
        public async Task<IActionResult> ManageClientUsers(
            [FromQuery] bool userUpdated = false,
            [FromQuery] bool userCreated = false)
        {
            try
            {
                var result = await _userAccountService.ManageClientUsersAsync(userUpdated, userCreated);
                if (!result.IsSuccess)
                    return BadRequest(result.Message);
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving client users", error = ex.Message });
            }
        }

        [HttpGet("[action]")]
        [PermissionAuthorize(Permission.ManageClientUsers)]
        public IActionResult AddClientUser([FromQuery] Guid clientId, [FromQuery] string company)
        {
            return Ok(new AddClientAdminInitDto
            {
                ClientId = clientId,
                Company = company,
                DefaultRole = AppConstants.Roles.Client
            });
        }

        [HttpGet("[action]")]
        [PermissionAuthorize(Permission.ManageClientUsers)]
        public async Task<IActionResult> EditClientUser([FromQuery] Guid userId)
        {
            try
            {
                var result = await _manageAbsAdminService.GetAbsUserAsync(userId);
                if (!result.IsSuccess)
                    return BadRequest(result.Message);
                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving client user", error = ex.Message });
            }
        }

        [HttpPost("[action]")]
        [PermissionAuthorize(Permission.ManageClientUsers)]
        public async Task<IActionResult> AddClientUser([FromBody] AccountViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _manageAbsAdminService.StoreAbsUserAsync(model);
                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok(new { message = "Client user created successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating client user", error = ex.Message });
            }
        }

        [HttpPost("[action]")]
        [PermissionAuthorize(Permission.ManageClientUsers)]
        public async Task<IActionResult> EditClientUser([FromBody] AccountViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _manageAbsAdminService.StoreAbsUserAsync(model);
                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok(new { message = "Client user updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating client user", error = ex.Message });
            }
        }

        [HttpPost("[action]")]
        [PermissionAuthorize(Permission.ManageClientUsers)]
        public async Task<IActionResult> DeleteClientUser([FromQuery] DataSourceRequest request, [FromBody] CapUser user)
        {
            try
            {
                var result = await _userAccountService.DeleteAsync(user.Id.ToString());
                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok(new[] { user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting client user", error = ex.Message });
            }
        }

        #endregion

        #region Tanks, Grading, Description

        #endregion
    }
}
