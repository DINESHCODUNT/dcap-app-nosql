using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.ViewModels;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.Models.Permissions;


namespace DigitalCap.Infrastructure.Service
{
    public class ActivityService : IActivityService
    {
        private readonly IActivityRepository _activityRepository;
        public ActivityService(IActivityRepository activityRepository)
        {
            _activityRepository = activityRepository;
        }
        public async Task<ServiceResult<List<RecentActivity>>> GetRecentActivitiesAsync(int take)
        {
            try
            {
                var activities = await _activityRepository.GetRecentActivitiesAsync(take);

                if (activities == null || !activities.Any())
                {
                    return ServiceResult<List<RecentActivity>>
                        .Failure("No recent activities found.");
                }

                return ServiceResult<List<RecentActivity>>.SuccessResult(activities, "Recent activities fetched successfully.");
            }
            catch (Exception ex)
            {
                // log exception here
                return ServiceResult<List<RecentActivity>>.Failure($"Error occurred: {ex.Message}");
            }
        }

       
        public async Task<ServiceResult<List<RecentActivityViewModel>>>GetRecentActivitiesAsync2(int page, int size)
        {
            try
            {
                var activities = await _activityRepository.GetRecentActivitiesAsync2(page, size);

                var result = activities.Select(a => new RecentActivityViewModel(a)).ToList();

                return ServiceResult<List<RecentActivityViewModel>>
                    .SuccessResult(result);
            }
            catch (Exception ex)
            {
                    return ServiceResult<List<RecentActivityViewModel>>
                    .Failure("An error occurred while fetching activities.");
            }
        }

        public async Task<ServiceResult<List<RecentActivityViewModel>>> GetRecentActivitiesByProjectIdAsync(int size, int projectId)
        {
            try
            {
                var results = await _activityRepository.GetRecentActivitiesByProjectIdAsync(size, projectId);

                var model = results
                .Select(x => new RecentActivityViewModel(x)).ToList();

                return ServiceResult<List<RecentActivityViewModel>>.Success(model);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<RecentActivityViewModel>>
                    .Failure(ex.Message);
            }
        }

        
    }
}
