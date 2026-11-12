using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface ITransferDataOnlinetoOfflineRepository : IRepositoryBase<UserAccountModel, Guid>
    {
        Task<IsSynchedOnline> GetDownloadOfflineProjects(int projectId);
        Task<bool> ReleaseReadOnlyOnline(int projectId, string userName);
    }
}
