using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.DataTransfer;
using DigitalCap.Core.Models.Survey;
using DigitalCap.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DataTransferDevAPIController : ApiBaseController
    {
        private readonly IDataTransferDevRepository _repository;
        private readonly ILogger<DataTransferDevAPIController> _logger;

        public DataTransferDevAPIController(IDataTransferDevRepository repository, ILogger<DataTransferDevAPIController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllDropDownProjects([FromQuery] string? id, [FromQuery] string? username, CancellationToken cancellationToken)
        {
            try
            {
                var user = string.IsNullOrWhiteSpace(username) ? CurrentUsername : username;
                var list = await _repository.GetAllDropDownProjectsAsync(id, user, cancellationToken);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllDropDownProjects");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetDownloadedProject([FromQuery] int projectId, CancellationToken cancellationToken)
        {
            try
            {
                var marker = await _repository.GetDownloadedProjectAsync(projectId, cancellationToken);
                return Ok(marker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDownloadedProject");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> InsertDownloadedProject([FromQuery] int projectId, [FromQuery] string? username, [FromQuery] string? id, CancellationToken cancellationToken)
        {
            try
            {
                var user = string.IsNullOrWhiteSpace(username) ? CurrentUsername : username;
                var userId = string.IsNullOrWhiteSpace(id) ? (User.FindFirst("oid")?.Value ?? string.Empty) : id;
                var success = await _repository.AddDownloadedProjectAsync(projectId, userId, user ?? string.Empty, cancellationToken);
                return Ok(success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InsertDownloadedProject");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetSectionsById([FromQuery] int projectId, CancellationToken cancellationToken)
        {
            try
            {
                var sections = await _repository.GetAllSectionsAsync(projectId, cancellationToken);
                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSectionsById");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetDataByTemplateId([FromQuery] int projectId, [FromQuery] int templateId, [FromQuery] Guid sectionId, CancellationToken cancellationToken)
        {
            try
            {
                var sectionData = await _repository.GetSectionDataAsync(projectId, templateId, sectionId, cancellationToken);
                return Ok(sectionData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDataByTemplateId");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetUnChangedDataByTemplateId([FromQuery] int projectId, CancellationToken cancellationToken)
        {
            try
            {
                var data = await _repository.GetProjectDataAsync(projectId, cancellationToken);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUnChangedDataByTemplateId");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        [HttpPost("[action]")]
        [DisableRequestSizeLimit]
        [RequestSizeLimit(167773180)]
        [RequestFormLimits(MultipartBodyLengthLimit = 167773180, ValueLengthLimit = 167773180)]
        public async Task<IActionResult> InsertSyncData([FromBody] System.Text.Json.JsonElement body, CancellationToken cancellationToken)
        {
            var jsonOptions = HttpContext.RequestServices
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()
                .Value.JsonSerializerOptions;
            TransferDataModel? model;
            try
            {
                model = body.Deserialize<TransferDataModel>(jsonOptions);
            }
            catch (System.Text.Json.JsonException ex)
            {
                return BadRequest("Invalid sync data: " + ex.Message);
            }
            if (model == null)
            {
                return BadRequest("Request body is empty or invalid JSON.");
            }

            try
            {
                await _repository.InsertSyncDataAsync(model, cancellationToken);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InsertSyncData");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        [HttpPost("[action]")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> MoveToOnlineTable([FromQuery] int ProjectId, [FromQuery] int Keep, CancellationToken cancellationToken)
        {
            try
            {
                await _repository.MoveToOnlineTableAsync(ProjectId, Keep, cancellationToken);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MoveToOnlineTable");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> DeleteSyncData([FromQuery] int ProjectId, CancellationToken cancellationToken)
        {
            try
            {
                await _repository.DeleteSyncDataAsync(ProjectId, cancellationToken);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteSyncData");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ReleasedAPIStoppedData([FromQuery] int ProjectId, CancellationToken cancellationToken)
        {
            try
            {
                await _repository.ReleasedAPIStoppedDataAsync(ProjectId, cancellationToken);
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReleasedAPIStoppedData");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        // --- Additional endpoints needed by desktop app ---

        [HttpGet("[action]")]
        public async Task<IActionResult> GetProjectTasks([FromQuery] int projectId, CancellationToken cancellationToken)
        {
            try
            {
                var tasks = await _repository.GetProjectTasksAsync(projectId, cancellationToken);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProjectTasks");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetUserSecurity([FromQuery] string? userName, CancellationToken cancellationToken)
        {
            try
            {
                var user = string.IsNullOrWhiteSpace(userName) ? CurrentUsername : userName;
                if (string.IsNullOrWhiteSpace(user))
                    return BadRequest("User name is required.");
                var security = await _repository.GetUserSecurityAsync(user, cancellationToken);
                return Ok(security);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserSecurity");
                return StatusCode(500, "The server could not complete the request. Please try again or contact support.");
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetProjectData([FromQuery] int projectId, CancellationToken cancellationToken)
        {
            return await GetUnChangedDataByTemplateId(projectId, cancellationToken);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ReleaseSyncData([FromQuery] int projectId, CancellationToken cancellationToken)
        {
            return await DeleteSyncData(projectId, cancellationToken);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ReleaseErrorSyncData([FromQuery] int projectId, CancellationToken cancellationToken)
        {
            return await ReleasedAPIStoppedData(projectId, cancellationToken);
        }
    }
}
