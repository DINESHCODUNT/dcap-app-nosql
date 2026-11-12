using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.ViewModels;
using DigitalCap.Infrastructure.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectConfigController : ApiBaseController
    {
        private readonly IGradingService _gradingService;
        private readonly ITankService _tankService;
        private readonly IProjectService _projectService;
        private readonly ISecurityService _securityService;
        public ProjectConfigController(IGradingService gradingService, ITankService tankService, IProjectService projectService, ISecurityService securityService)
        {
            _gradingService = gradingService;
            _tankService = tankService;
            _projectService = projectService;
            _securityService = securityService;
        }
        #region ManageGrading

        [HttpPost("[action]")]
        public async Task<IActionResult> AddNewGrading([FromBody] CreateGradingRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _gradingService.CreateGradingAsync(model);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(true);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> EditGrading([FromBody] CreateGradingRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _gradingService.UpdateGradingAsync(model);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(new { message = "Grading updated successfully", filter = 1, searchValue = 1 });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> DeleteGrading([FromBody] DeleteGradingRequest request)
        {
            try
            {
                var result = await _gradingService.DeleteGradingAsync(
                    request.GradingId,
                    request.TankId
                );

                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok(result.Data);
            }
            catch
            {
                return BadRequest();
            }
        }

        #endregion

        #region Manage Tank

        [HttpGet("[action]")]
        public async Task<IActionResult> ManageTank(bool isActive = false, string imo = null, string projectId = null, int tankRestoreFilter = 0, int searchRestoreFilter = 0)
        {
            var username = CurrentUsername;

            var result = await _tankService.ManageTankAsync(username, isActive, imo, projectId, tankRestoreFilter, searchRestoreFilter);

            if (!result.IsSuccess)
            {
                if (result.Message == "AccessDenied")
                    return Forbid();

                return BadRequest(result.Message);
            }

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AddNewTank([FromBody] CreateTankRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = CurrentUsername;

            var result = await _tankService.CreateTankAsync(model, username);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(new { message = "Tank created successfully" });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> EditTank([FromBody] CreateTankRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var username = CurrentUsername ?? "";

            var result = await _tankService.UpdateTankAsync(model, username);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(new { message = "Tank updated successfully", filter = 1, searchValue = 1 });
        }

        #endregion

        [HttpGet("[action]")]
        public async Task<IActionResult> GetProjectListByIMO(int? imoNumber = null)
        {
            var projectnamelist = await _projectService.GetProjectListByIMO(imoNumber);
            return Ok(projectnamelist);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetVesselTankbyImo(string imoNumber = null)
        {
            var username = CurrentUsername;
            var projectnamelist = await _tankService.GetVesselTankbyImo(username,imoNumber);
            return Ok(projectnamelist.Data);
        }
    }
}
