using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class RecentActivityViewModel
    {        
            public int ID { get; }
            public int ProjectId { get; set; }
            public string ChangedBy { get; }
            public DateTime ChangeTime { get; }
            public string VesselProject { get; } = string.Empty;
            public string Text { get; } = string.Empty;

            public RecentActivityViewModel() { }

            public RecentActivityViewModel(RecentActivity activity)
            {
                ID = activity.ID;
                ProjectId = activity.ProjectId;
                Text = activity.Text;
                ChangedBy = activity.CreateBy;
                ChangeTime = new DateTime(activity.CreateDttm.Ticks, DateTimeKind.Utc);
                VesselProject = FormatVesselProject(activity);
            }

            private string FormatVesselProject(RecentActivity activity)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(activity.VesselName))
                {
                    parts.Add(activity.VesselName);
                }
                if (activity.PIDNumber > 0)
                {
                    parts.Add($"Project ID #{activity.PIDNumber}");
                }
                return string.Join(", ", parts);
            }
    }
}
