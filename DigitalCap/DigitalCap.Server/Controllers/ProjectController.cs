using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Core.Security;
using DigitalCap.Core.ViewModels;
using DigitalCap.Infrastructure.Service;
using DigitalCap.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ApiBaseController
    {
        private readonly IProjectService _projectService;
        private readonly IConfiguration _configData;

        public ProjectController(IProjectService projectService, IConfiguration configData)
        {
            _projectService = projectService;
            _configData = configData;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Create([FromBody] Project model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUser = CurrentUsername.ToLower();

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));

                var result = await _projectService.CreateProject(model, currentUser);

                if (!result.IsSuccess)
                    return BadRequest(new
                    {
                        ErrorMessage = result.ErrorMessage,
                        ErrorCode = result.ErrorCode
                    });

                return Ok(new { ProjectId = result.Data });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(504, new { message = "Request timed out. Please try again." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error creating project",
                    error = ex.Message
                });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Update_Project(Project model)
        {
            var currentUser = CurrentUsername.ToLower();

            var result = await _projectService.Update_Project(model, currentUser);
            if (!result.IsSuccess)
                return BadRequest(new { result.ErrorMessage, result.ErrorCode });

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CloseProject(int id)
        {
            var currentUser = CurrentUsername.ToLower();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized(new { message = "User identifier not found" });

            var result = await _projectService.UpdateProjectStatus(id, currentUserId, (int)ProjectStatus.Closed, currentUser);
            if (!result.IsSuccess)
                return BadRequest(new { message = result.ErrorMessage });

            return Ok(new { success = true, projectId = id });
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var currentUser = CurrentUsername.ToLower();
            var currentUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
            var result = await _projectService.DeleteProject(id, currentUserId, currentUser);

            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task UpdatePriority(int id, int? priority)
        {
            var currentUser = CurrentUsername.ToLower();
            await _projectService.UpdatePriority(id, priority, currentUser);
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _projectService.GetAllUsers();
            return Ok(result);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CancelProject(int id)
        {
            var currentUser = CurrentUsername.ToLower();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _projectService.UpdateProjectStatus(id, userId, (int)ProjectStatus.Cancelled, currentUser);
            return NoContent();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> List()
        {
            var isElectron = Convert.ToBoolean(_configData["IsElectron"]);
            var currentUser = CurrentUsername;

            var result = await _projectService.GetList(currentUser, isElectron);

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> GetAllProjectsGridData([FromBody] DataSourceRequest request)
        {
            try
            {
                var Iselectron = Convert.ToBoolean(_configData["IsElectron"]);
                var currentUser = CurrentUsername.ToLower();

                if (string.IsNullOrEmpty(currentUser))
                    return Unauthorized("User not authenticated");

                var refreshStale = request?.RefreshStalePercentComplete ?? true;
                var result = await _projectService.GetAllProjectsGridData(Iselectron, currentUser, refreshStale);

                if (!result.IsSuccess)
                    return BadRequest(result.ErrorMessage);

                // Format response to match UI expectations: { data: [...], total: number }
                var projects = result.Data ?? new List<AllProjectsGrid>();

                return Ok(new
                {
                    data = projects,
                    total = projects.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving projects grid data", error = ex.Message });
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllClients(bool wcnList = false)
        {
            var clients = await _projectService.GetAllClients(wcnList);
            return Ok(clients);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllSurveyors(bool wcnList = false)
        {
            var users = await _projectService.GetAllSurveyors(wcnList);
            return Ok(users.Data);
        }

        [HttpGet("[action]")]
        [PermissionAuthorize(DigitalCap.Core.Security.Permission.ViewWorkflowAndTasks)]
        public async Task<IActionResult> Workflow(int? id)
        {
            if (!id.HasValue)
                return BadRequest("Parameter(s) null or empty.");

            var model =await _projectService.Workflow(id);

            return Ok(model.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Landing(int? id)
        {
            if (!id.HasValue)
            {
                return BadRequest("Parameter(s) null or empty.");
            }

            var currentuser = CurrentUsername.ToLower();

            var result = await _projectService.Landing(id, currentuser);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Message);
            }

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> SaveReportDetails([FromBody] ReportDetailsViewModel reportDetails)
        {
            if (reportDetails == null)
                return BadRequest("Invalid request");

            var result = await _projectService.SaveReportDetails(reportDetails);

            return Ok(true);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetProjectStats()
        {
            try
            {
                var stats = await _projectService.GetProjectStats();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ReleaseOfflineDownload(int projectId)
        {
            try
            {
                // In a real scenario, you'd check roles using a policy or User.IsInRole(Constants.Admin_Cap_HQ)
                var currentUser = CurrentUsername.ToLower();
                var result = await _projectService.ReleaseOfflineDownload(projectId, currentUser);
                if (result.IsSuccess)
                {
                    return Ok(result.Data);
                }
                return BadRequest(result.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
