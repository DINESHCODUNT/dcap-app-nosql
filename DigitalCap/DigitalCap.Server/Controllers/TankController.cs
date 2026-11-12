using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TankController : ApiBaseController
    {
        private readonly ITankService _tankService;
        private readonly IUserPermissionRepository _userPermissionRepository;

        public TankController(ITankService tankService, IUserPermissionRepository userPermissionRepository)
        {
            _tankService = tankService;
            _userPermissionRepository = userPermissionRepository;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateTankImageDescriptionDropdownCard([FromForm] int templateId, [FromForm] int projectId, [FromForm] Guid sectionId, [FromForm] int cardNumber, [FromForm] int value, [FromForm] string cardName)
        {
            var result = await _tankService.UpdateTankImageDescriptionDropdownCard(templateId, projectId, sectionId, cardNumber, value, cardName);

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateTankCardAdditionalDescription([FromForm] int templateId, [FromForm] int projectId, [FromForm] Guid sectionId, [FromForm] int cardNumber, [FromForm] string value, [FromForm] string cardName)
        {
            var result = await _tankService.UpdateTankCardAdditionalDescription(templateId, projectId, sectionId, cardNumber, value, cardName);

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateTankCardCurrentCondition([FromForm] int templateId, [FromForm] int projectId, [FromForm] Guid sectionId, [FromForm] int cardNumber, [FromForm] int value, [FromForm] string cardName)
        {
            var result = await _tankService.UpdateTankCardCurrentCondition(templateId, projectId, sectionId, cardNumber, value, cardName);

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> UploadTankCardImage(IFormFile tankfile, [FromForm] int templateId, [FromForm] int projectId, [FromForm] Guid sectionId, [FromForm] int cardNumber, [FromForm] int imageId, [FromForm] bool replaceImage = false, [FromForm] string cardName = null)
        {
            var result = await _tankService.UploadTankCardImage(tankfile, templateId, projectId, sectionId, cardNumber, imageId, replaceImage, cardName);

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> DeleteTankCardImage([FromForm] int templateId, [FromForm] int projectId, [FromForm] Guid sectionId, [FromForm] int cardNumber, [FromForm] int imageId)
        {
            var result = await _tankService.DeleteTankCardImage(templateId, projectId, sectionId, cardNumber, imageId);
            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetMappedTankTypes(int projectId, string? vesseltype)
        {
            var result = await _tankService.GetMappedTankTypes(projectId, vesseltype);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetVesselDetails()
        {
            var result = await _tankService.GetVesselDetails();
            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetShipType()
        {
            var result = await _tankService.GetShipType();
            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetVesselTypeList()
        {
            var result = await _tankService.GetVesselTypeList();
            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> FilterMenuCustomization_Read([FromQuery] DataSourceRequest request)
        {
            try
            {
                var serviceResult = await _tankService.FilterMenuCustomization_Read();

                if (!serviceResult.IsSuccess)
                {
                    return BadRequest(serviceResult.Message);
                }

                var allData = serviceResult.Data;
                var pagedData = allData;

                // Apply pagination
                if (request != null)
                {
                    if (request.Skip.HasValue && request.Take.HasValue)
                    {
                        pagedData = allData.Skip(request.Skip.Value).Take(request.Take.Value).ToList();
                    }
                    else if (request.Page.HasValue && request.PageSize.HasValue)
                    {
                        var skip = (request.Page.Value - 1) * request.PageSize.Value;
                        pagedData = allData.Skip(skip).Take(request.PageSize.Value).ToList();
                    }
                }

                bool editable = false;
                if (!string.IsNullOrEmpty(CurrentUsername))
                {
                    var permission = await _userPermissionRepository.GetRolePermissionByUserName(
                        CurrentUsername,
                        EnumExtensions.GetDescription(ManagePages.ManageTank));
                    editable = Convert.ToBoolean(permission?.Edit);
                }

                // Return data with editable permission
                return Ok(new { data = pagedData, editable = editable });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tanks", error = ex.Message });
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ManageTankFilter_TankName(string IMO)
        {
            var result = await _tankService.ManageTankFilter_TankName(IMO);
            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ManageTankFilter_TankType(string IMO)
        {
            var result = await _tankService.ManageTankFilter_TankType(IMO);
            return Ok(result.Data);
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> ManageTankFilter_Project(string IMO)
        {
            var projectnames = _tankService.ManageTankFilter_Project(IMO).Result;
            return Ok(projectnames.Data);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ManageTankActiveCheckBox([FromBody] ManageTankStatusRequest request)
        {
            bool editable = false;
            if (!string.IsNullOrEmpty(CurrentUsername))
            {
                var permission = await _userPermissionRepository.GetRolePermissionByUserName(
                    CurrentUsername,
                    EnumExtensions.GetDescription(ManagePages.ManageTank));
                editable = Convert.ToBoolean(permission?.Edit);
            }

            if (!editable)
            {
                return StatusCode(403, new { message = "You do not have permission to change tank status." });
            }

            var result = await _tankService.ManageTankActiveCheckBox(request.Data, request.Status, request.IMO);

            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> DeleteTanks(Guid tankId, string? IMO = null, int ProjectId = 0)
        {
            var result = await _tankService.DeleteTanks(tankId, IMO, ProjectId);
            return Ok(result.Data);
        }
    }
}
