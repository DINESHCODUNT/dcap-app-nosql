using DigitalCap.Core.Models;
using System;
using System.Text.Json.Serialization;

namespace DigitalCap.Core.DTO
{
    public class ProjectSummaryDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("pidNumber")]
        public string PIDNumber { get; set; }

        [JsonPropertyName("statusName")]
        public string StatusName { get; set; }

        [JsonPropertyName("visitDates")]
        public string VisitDates { get; set; }

        // Constructor to convert from ProjectsByClientId
        public ProjectSummaryDto() { }

        public ProjectSummaryDto(ProjectsByClientId project)
        {
            Name = project.Name;
            PIDNumber = project.PIDNumber;
            StatusName = project.StatusName;

            if (project.SurveyFirstVisit == null && project.SurveyLastVisit == null)
            {
                VisitDates = "N/A";
            }
            else
            {
                try
                {
                    var list = new[] { project.SurveyFirstVisit, project.SurveyLastVisit }
                        .Where(x => x != null)
                        .Select(x => x.Value.ToString("dd-MMM-yyyy"));
                    VisitDates = string.Join(", ", list);
                }
                catch
                {
                    VisitDates = "N/A";
                }
            }
        }
    }
}