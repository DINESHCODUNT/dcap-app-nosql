using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ExportConfig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Claims;

namespace DigitalCap.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyController : ControllerBase
    {
        private readonly IProjectSurveyService _projectSurveyService;

        public SurveyController(IProjectSurveyService projectSurveyService)
        {
            _projectSurveyService = projectSurveyService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Index(int projectId, int? templateSectionId = null)
        {
            var user = User.Identity?.Name;

            var currentUserName = User.FindFirstValue(ClaimTypes.Upn)
               ?? User.FindFirstValue(ClaimTypes.Name);

            var result = await _projectSurveyService.GetSurveyIndexAsync(projectId, currentUserName, templateSectionId);
            if (!result.IsSuccess)
                return StatusCode(403, new { result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> IndexPartial(int projectId, int? templateSectionId = null, int imo = 0)
        {
            var result = await _projectSurveyService.IndexPartialAsync(projectId, templateSectionId, imo);
            if (!result.IsSuccess)
                return BadRequest(new { result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> CheckShowExportAllSettings(int projectId, int imo = 0)
        {
            var result = await _projectSurveyService.CheckShowExportAllSettingsAsync(projectId, imo);
            if (!result.IsSuccess)
                return BadRequest(new { result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> CheckShowExportSettings(int projectId, int? templateId = null, int imo = 0)
        {
            if (templateId == null)
                return BadRequest("templateId is required");
            var result = await _projectSurveyService.CheckShowExportSettingsAsync(projectId, imo, templateId.Value);
            if (!result.IsSuccess)
                return BadRequest(new { result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllSections(int projectId, int? templateId = null)
        {
            if (templateId == null)
                return BadRequest("templateId is required");
            var result = await _projectSurveyService.GetAllSectionsAsync(projectId, templateId.Value);
            if (!result.IsSuccess)
                return BadRequest(new { result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllParts(int projectId, int imo = 0)
        {
            var result = await _projectSurveyService.GetAllPartsAsync(projectId, imo);
            if (!result.IsSuccess)
                return BadRequest(new { result.ErrorMessage });
            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ValidateProjectGrading([FromQuery] int projectId, [FromQuery] int templateSectionId, [FromQuery] int imo, [FromBody] List<ExportSections>? settings)
        {
            var result = await _projectSurveyService.ValidateProjectGradingAsync(projectId, templateSectionId, imo, settings);
            if (!result.IsSuccess)
                return BadRequest(new { result.ErrorMessage });
            return Content(JsonConvert.SerializeObject(result.Data), "application/json");
        }
    }
}
