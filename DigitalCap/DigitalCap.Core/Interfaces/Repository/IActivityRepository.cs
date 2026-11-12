using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IActivityRepository
    {
        Task<List<RecentActivity>> GetRecentActivitiesAsync(int take);
        Task<List<RecentActivity>> GetRecentActivitiesAsync2(int page, int size);
        Task<List<RecentActivity>> GetRecentActivitiesByProjectIdAsync(int size, int projectId);
    }
}
