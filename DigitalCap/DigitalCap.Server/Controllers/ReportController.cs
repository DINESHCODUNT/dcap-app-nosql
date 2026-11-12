using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models.Survey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ApiBaseController
    {
        private static readonly ConcurrentDictionary<string, ProjectsExportJobStatus> _projectsExportJobs = new();
        private readonly IProjectService _projectService;
        private readonly IReportService _reportService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReportController> _logger;

        public ReportController(
            IProjectService projectService,
            IReportService reportService,
            IServiceScopeFactory scopeFactory,
            ILogger<ReportController> logger)
        {
            _projectService = projectService;
            _reportService = reportService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> StartExportAllProjects()
        {
            var securityService = HttpContext.RequestServices.GetRequiredService<ISecurityService>();
            var currentUserResult = await securityService.GetCurrentUserAsync();
            if (!currentUserResult.IsSuccess || currentUserResult.Data == null)
            {
                return Unauthorized();
            }

            var userName = currentUserResult.Data.UserName;
            var jobId = Guid.NewGuid().ToString();
            _projectsExportJobs[jobId] = new ProjectsExportJobStatus();

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                try
                {
                    var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
                    var rows = await reportService.ExportAllProjectsForUserAsync(userName);
                    _projectsExportJobs[jobId] = new ProjectsExportJobStatus { Status = "Completed", Rows = rows };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to export all projects for user {UserName}", userName);
                    _projectsExportJobs[jobId] = new ProjectsExportJobStatus { Status = "Failed", Error = "Unable to export projects. Please try again later." };
                }
            });

            return Ok(new { jobId, status = "Processing" });
        }

        [HttpGet("[action]")]
        public IActionResult GetExportAllProjectsStatus(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId) || !_projectsExportJobs.TryGetValue(jobId, out var job))
                return Ok(new { status = "NotFound" });
            if (job.Status == "Completed")
            {
                _projectsExportJobs.TryRemove(jobId, out _);
                return Ok(new { status = "Completed", rows = job.Rows });
            }
            if (job.Status == "Failed")
            {
                _projectsExportJobs.TryRemove(jobId, out _);
                return Ok(new { status = "Failed", error = job.Error });
            }
            return Ok(new { status = "Processing" });
        }
    }
}
