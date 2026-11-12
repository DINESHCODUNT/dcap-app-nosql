using DigitalCap.Core.Models;
using DigitalCap.Core.Models.DataTransfer;
using DigitalCap.Core.Models.Survey;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IDataTransferDevRepository
    {
        Task<List<DropdownListProjects>> GetAllDropDownProjectsAsync(string? id, string? username, CancellationToken cancellationToken = default);
        Task<IsSynchedOnline?> GetDownloadedProjectAsync(int projectId, CancellationToken cancellationToken = default);
        Task<bool> AddDownloadedProjectAsync(int projectId, string userId, string userName, CancellationToken cancellationToken = default);
        Task<TransferDataModel?> GetProjectDataAsync(int projectId, CancellationToken cancellationToken = default);
        Task<List<SingleTask>> GetProjectTasksAsync(int projectId, CancellationToken cancellationToken = default);
        Task<OfflineUserSecurity> GetUserSecurityAsync(string userName, CancellationToken cancellationToken = default);
        Task<List<AllSections>> GetAllSectionsAsync(int projectId, CancellationToken cancellationToken = default);
        Task<TransferDataModel> GetSectionDataAsync(int projectId, int templateId, Guid sectionId, CancellationToken cancellationToken = default);
        Task InsertSyncDataAsync(TransferDataModel model, CancellationToken cancellationToken = default);
        Task MoveToOnlineTableAsync(int projectId, int keep, CancellationToken cancellationToken = default);
        Task DeleteSyncDataAsync(int projectId, CancellationToken cancellationToken = default);
        Task ReleasedAPIStoppedDataAsync(int projectId, CancellationToken cancellationToken = default);
        Task<byte> UpdateProjectPercentCompleteAsync(int projectId);
    }
}
