using DigitalCap.Core.DTO;
using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Models;
using DigitalCap.Core.ViewModels;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IActivityService
    {
        Task<ServiceResult<List<RecentActivity>>> GetRecentActivitiesAsync(int take);
        Task<ServiceResult<List<RecentActivityViewModel>>> GetRecentActivitiesAsync2(int page, int size);
        Task<ServiceResult<List<RecentActivityViewModel>>> GetRecentActivitiesByProjectIdAsync(int size, int projectId);
        
    }
}
