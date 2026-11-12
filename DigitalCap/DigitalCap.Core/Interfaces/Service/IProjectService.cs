using DigitalCap.Core.DTO;
using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Models;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IProjectService
    {
        Task<ServiceResult<int>> CreateProject(Project model, string currentUser);
        Task<ServiceResult<int>> Update_Project(Project model, string currentUser);
        Task<ServiceResult<int>> DeleteProject(int id, string currentUserId, string currentUser);

        Task UpdatePriority(int projectId, int? priority, string currentUser);
        Task<ServiceResult<ProjectSummary>> GetSummary(int id);
        Task<ServiceResult<object>> GetList(string currentUser, bool isElectron);

        /// <param name="refreshStalePercentComplete">
        /// When true, projects with null percent complete are recalculated and persisted (expensive for large lists).
        /// Set false for bulk export/report to avoid N+1 calls and timeouts.
        /// </param>
        Task<ServiceResult<List<AllProjectsGrid>>> GetAllProjectsGridData(bool isElectron, string userName, bool refreshStalePercentComplete = true);

        Task<ServiceResult<List<AllProjectsGridExport>>> GetAllProjectsGridExport();

        Task<ServiceResult<byte>> UpdateProjectPercentComplete(int projectId, string currentUser);

        Task<ServiceResult<List<Client>>> GetAllClients(bool wcnList);

        Task<ServiceResult<object>> GetAllUsers();
        Task<ServiceResult<List<string>>> GetAllSurveyors(bool wcnList);
        Task<ServiceResult<WorkflowViewModel>> Workflow(int? id);

        Task<ServiceResult<ProjectLandingViewModel>> Landing(int? id, string currentUser);
        Task<ServiceResult<bool>> SaveReportDetails(ReportDetailsViewModel reportDetails);
        Task<ServiceResult<string>> GetProjectName(int id);
        Task<ServiceResult<IEnumerable<ProjectDetailItem>>> GetDetails(int id);

        //ClientProfiles
        Task<List<SelectListItem>> GetProjectListByIMO(int? imoNumber = null);
        Task<IReadOnlyList<ClientProfileDTO>> GetProjectCountGroupedByClientIdAsync();
        Task<ServiceResult<int>> UpdateProjectStatus(int id, string currentUserId, int ProjectStatus, string currentUser);

        Task<ProjectStats> GetProjectStats();
        Task<ServiceResult<bool>> ReleaseOfflineDownload(int projectId, string userName);
    }

}
