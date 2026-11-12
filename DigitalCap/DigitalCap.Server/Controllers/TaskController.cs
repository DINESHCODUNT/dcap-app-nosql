using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Core.Security;
using DigitalCap.Core.ViewModels;
using DigitalCap.Infrastructure.Service;
using DigitalCap.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ApiBaseController
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly ISecurityService _securityService;
        private readonly IUserPermissionRepository _userPermissionRepository;

        public TaskController(
            ITaskService taskService,
            IProjectService projectService,
            ISecurityService securityService,
            IUserPermissionRepository userPermissionRepository)
        {
            _taskService = taskService;
            _projectService = projectService;
            _securityService = securityService;
            _userPermissionRepository = userPermissionRepository;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetStatuses()
        {
            try
            {
                var result = await _taskService.GetStatuses();
                if (!result.IsSuccess)
                    return BadRequest(new { message = result.ErrorMessage });

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving statuses", error = ex.Message });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> SaveTask([FromBody] SaveTaskRequest? request)
        {
            try
            {
                var currentUser = CurrentUsername.ToLower();
                if (request == null || !ModelState.IsValid)
                    return BadRequest(ModelState);

                var taskResult = await _taskService.GetCap2Task(request.Id);
                if (!taskResult.IsSuccess)
                    return BadRequest(new { message = taskResult.ErrorMessage });

                var task = taskResult.Data;
                task.StatusId = request.Status;

                if (request.StatusDate.HasValue)
                    task.StatusDate = request.StatusDate.Value;

                if (!string.IsNullOrWhiteSpace(request.AssignedTo) && task.AssignedTo != request.AssignedTo)
                {
                    task.AssignedDate = DateTime.Now.Date;
                    task.AssignedTo = request.AssignedTo;
                }

                if (request.DueDate.HasValue)
                    task.DueDate = request.DueDate.Value;

                if (request.Value != null)
                    task.Value = request.Value;

                if (request.SurveyDays != null)
                    task.SurveyDays = request.SurveyDays;

                var updateResult = await _taskService.Update(task, currentUser);
                if (!updateResult.IsSuccess)
                    return BadRequest(new { message = updateResult.ErrorMessage });

                // Refresh task and update project percent complete
                var refreshedTaskResult = await _taskService.GetCap2Task(request.Id);
                if (refreshedTaskResult.IsSuccess && refreshedTaskResult.Data?.ProjectId.HasValue == true)
                {
                    var percentCompleteResult = await _taskService.UpdateProjectPercentComplete(refreshedTaskResult.Data.ProjectId.Value);
                    if (percentCompleteResult.IsSuccess)
                    {
                        await _projectService.UpdateProjectPercentComplete(refreshedTaskResult.Data.ProjectId.Value, currentUser);
                    }
                }

                var finalTaskResult = await _taskService.GetCap2Task(request.Id);
                if (!finalTaskResult.IsSuccess)
                    return BadRequest(new { message = finalTaskResult.ErrorMessage });

                return Ok(finalTaskResult.Data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving task", error = ex.Message });
            }
        }
    }
}
