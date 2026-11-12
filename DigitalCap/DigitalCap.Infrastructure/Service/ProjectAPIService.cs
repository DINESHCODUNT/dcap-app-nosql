using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class ProjectAPIService : IProjectAPIService
    {
        private readonly IProjectService _projectService;

        public ProjectAPIService(IProjectService projectService)
        {
            _projectService = projectService;
        }

        public async Task<ServiceResult<ProjectSummary>> GetSummary(int projectId)
        {
            try
            {
                var result = await _projectService.GetSummary(projectId);
                return ServiceResult<ProjectSummary>.Success(result.Data);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectSummary>.Failure("Failed to get project summary");
            }
        }
    }
}
