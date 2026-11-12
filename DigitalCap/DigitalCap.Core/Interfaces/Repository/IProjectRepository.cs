using DigitalCap.Core.DTO;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IProjectRepository : IRepositoryBase<Project, int>
    {
        Task<int> CreateProject(Project project,string Name);
        Task<bool> CheckProjectExistsByImoNumber(int imoNumber);
        Task<List<Models.SelectListItem>> GetProjectListByIMO(int? imoNumber = null);
        Task<Project> GetProject(int id);
        Task<int> UpdatePercentComplete(int projectId, byte percentComplete, string currentUser);
        Task<bool> CheckProjectExistsByProjectName(string projectName, int imoNumber, int vesseltypeid);
        Task Update_Project(Project project,string currentUser);
        Task<string> GetProjectName(int id);
        Task UpdateProjectStatus(int id, string currentUserId, int projectStatus, string currentUser);
        Task DeleteProject(int id, string currentUserId, string currentUser);
        Task<string> GetIMONumberByProjectId(int projectId);
        Task<IEnumerable<ProjectDetailItem>> GetDetails(int id);
        Task<ProjectSummary> GetSummary(int id);
        Task UpdatePriority(int id, int? priority, string currentUser);
        Task<IEnumerable<AllProjectsGrid>> GetAllProjectsGridData();
        Task<List<AllProjectsGridExport>> GetAllProjectsGridExport();
        Task<string> GetProjectVesselType(int id);

        Task<IEnumerable<ProjectsByClientId>> GetProjectsByClientIdAsync(Guid clientId);
        Task<IEnumerable<ClientProfileDTO>> GetProjectCountGroupedByClientIdAsync(CancellationToken cancellationToken = default);
        Task<List<Project>> GetAllProjects(string user = null);

        Task<ProjectStats> GetProjectStats();
    }
}
