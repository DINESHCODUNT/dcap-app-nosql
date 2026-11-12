using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivityController : ApiBaseController
    {
        private readonly IActivityService _activityService;
        public ActivityController(IActivityService activityService)
        {
            _activityService = activityService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetRecentActivities([FromQuery] int take = 10)
        {
            try
            {
                var serviceResult = await _activityService.GetRecentActivitiesAsync(take);
                
                if (!serviceResult.IsSuccess)
                {
                    return BadRequest(new { message = serviceResult.ErrorMessage });
                }

                var activities = serviceResult.Data ?? new List<RecentActivity>();
                
                
                return Ok(serviceResult.Data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving recent activities", error = ex.Message });
            }
        }

        private ActivityLogDto FormatActivitiesForReact(List<RecentActivity> activities)
        {
            var activityLog = new ActivityLogDto();
            var now = DateTime.UtcNow;
            var today = now.Date;
            var yesterday = today.AddDays(-1);

            // Group activities by date
            var groupedByDate = activities
                .OrderByDescending(a => a.CreateDttm)
                .GroupBy(a => a.CreateDttm.Date)
                .ToList();

            foreach (var dateGroup in groupedByDate)
            {
                var groupDate = dateGroup.Key;
                var dateLabel = GetDateLabel(groupDate, today, yesterday);
                var dateText = GetDateText(groupDate, today, yesterday);

                var activityGroup = new ActivityGroupDto
                {
                    DateLabel = dateLabel,
                    DateText = dateText,
                    Activities = dateGroup.Select(a => MapToActivityItem(a)).ToList()
                };

                activityLog.Groups.Add(activityGroup);
            }

            return activityLog;
        }

        private string GetDateLabel(DateTime date, DateTime today, DateTime yesterday)
        {
            if (date == today)
                return "Today";
            if (date == yesterday)
                return "Yesterday";
            
            return date.ToString("MMM dd", CultureInfo.InvariantCulture);
        }

        private string GetDateText(DateTime date, DateTime today, DateTime yesterday)
        {
            if (date == today)
                return $"Today � {date:MMM dd}";
            if (date == yesterday)
                return $"Yesterday � {date:MMM dd}";
            
            return date.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
        }

        private ActivityItemDto MapToActivityItem(RecentActivity activity)
        {
            var activityType = DetermineActivityType(activity.Text);
            var tag = DetermineTag(activity.Text);
            var entity = ExtractEntity(activity);
            var user = MapUser(activity.CreateBy);

            return new ActivityItemDto
            {
                Id = activity.ID,
                Type = activityType,
                Text = FormatActivityText(activity.Text, entity),
                Time = FormatTime(activity.CreateDttm),
                Tag = tag,
                User = user,
                Entity = entity,
                Metadata = ExtractMetadata(activity)
            };
        }

        private string DetermineActivityType(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "modify";

            var lowerText = text.ToLowerInvariant();
            
            if (lowerText.Contains("created") || lowerText.Contains("new"))
                return "create";
            if (lowerText.Contains("assigned") || lowerText.Contains("assign"))
                return "assigned";
            if (lowerText.Contains("completed") || lowerText.Contains("complete") || lowerText.Contains("finished"))
                return "complete";
            if (lowerText.Contains("uploaded") || lowerText.Contains("upload") || lowerText.Contains("file"))
                return "status";
            if (lowerText.Contains("alert") || lowerText.Contains("warning"))
                return "modify"; // Use modify with alert-icon class
            
            return "modify";
        }

        private string DetermineTag(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "Updated";

            var lowerText = text.ToLowerInvariant();
            
            if (lowerText.Contains("created") || lowerText.Contains("new"))
                return "Created";
            if (lowerText.Contains("assigned") || lowerText.Contains("assign"))
                return "Assigned";
            if (lowerText.Contains("completed") || lowerText.Contains("complete"))
                return "Completed";
            if (lowerText.Contains("uploaded") || lowerText.Contains("upload"))
                return "Upload";
            if (lowerText.Contains("deleted") || lowerText.Contains("removed"))
                return "Deleted";
            if (lowerText.Contains("alert") || lowerText.Contains("warning"))
                return "Alert";
            
            return "Updated";
        }

        private string ExtractEntity(RecentActivity activity)
        {
            // Try to extract project ID or reference from text
            if (activity.PIDNumber > 0)
                return $"PID-{activity.PIDNumber}";
            
            // Try to find pattern like "SRV-2026-006" or similar
            if (!string.IsNullOrWhiteSpace(activity.Text))
            {
                var match = Regex.Match(activity.Text, @"\b[A-Z]{2,4}-\d{4}-\d{3}\b");
                if (match.Success)
                    return match.Value;
            }
            
            return activity.VesselName ?? string.Empty;
        }

        private string FormatActivityText(string text, string entity)
        {
            // Return plain text - React component will handle entity highlighting
            return text ?? string.Empty;
        }

        private string FormatTime(DateTime dateTime)
        {
            var localTime = dateTime.ToLocalTime();
            return localTime.ToString("h:mm tt", CultureInfo.InvariantCulture);
        }

        private ActivityUserDto MapUser(string createBy)
        {
            if (string.IsNullOrWhiteSpace(createBy) || createBy.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                return new ActivityUserDto
                {
                    Name = "System",
                    Initials = "SY",
                    AvatarColor = "#6b7280"
                };
            }

            // Extract initials from name
            var parts = createBy.Split(new[] { ' ', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var initials = parts.Length >= 2 
                ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
                : createBy.Length >= 2 
                    ? createBy.Substring(0, 2).ToUpperInvariant()
                    : createBy.ToUpperInvariant();

            // Generate consistent color based on name
            var avatarColor = GenerateAvatarColor(createBy);

            return new ActivityUserDto
            {
                Name = createBy,
                Initials = initials,
                AvatarColor = avatarColor
            };
        }

        private string GenerateAvatarColor(string name)
        {
            // Generate a consistent color based on the name hash
            var hash = name.GetHashCode();
            var colors = new[]
            {
                "#3b82f6", // blue
                "#8b5cf6", // purple
                "#ec4899", // pink
                "#f59e0b", // amber
                "#10b981", // green
                "#ef4444", // red
                "#06b6d4", // cyan
                "#f97316"  // orange
            };
            
            return colors[Math.Abs(hash) % colors.Length];
        }

        private List<ActivityMetaDto> ExtractMetadata(RecentActivity activity)
        {
            var metadata = new List<ActivityMetaDto>();

            // Add location if available (CapRegion)
            // Note: CapRegion might not be in RecentActivity model, so this is a placeholder
            // You may need to join with Project table to get this information

            // Add vessel name if available
            if (!string.IsNullOrWhiteSpace(activity.VesselName))
            {
                metadata.Add(new ActivityMetaDto
                {
                    Type = "location",
                    Value = activity.VesselName,
                    Icon = "location"
                });
            }

            return metadata;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetRecentActivities2([FromQuery] int page = 0,[FromQuery] int size = 200)
        {
            var result = await _activityService.GetRecentActivitiesAsync2(page, size);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult>GetRecentActivities2ByProjectId(
           int projectId,[FromQuery] int size = 10)
        {
            var result = await _activityService.GetRecentActivitiesByProjectIdAsync(size, projectId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult>GetAllActivities2ByProjectId(int projectId, [FromQuery] int size = 200)
        {
            var result = await _activityService.GetRecentActivitiesByProjectIdAsync(size, projectId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result.Data);
        }

    }
}
