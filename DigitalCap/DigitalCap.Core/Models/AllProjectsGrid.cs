using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class AllProjectsGrid
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string StatusName { get; set; }
        public string StatusColor { get; set; }
        public string CapRegion { get; set; }
        public string IMO { get; set; }
        public string VesselName { get; set; }
        public string PIDNumber { get; set; }
        public string ProjectPriority { get; set; }
        public string CompanyName { get; set; }
        public string WCN { get; set; }
        public DateTime? PotentialDrydockDate { get; set; }
        public string DisplayDrydockDate
        {
            get
            {
                return PotentialDrydockDate != null && PotentialDrydockDate != DateTime.MinValue ? Convert.ToDateTime(PotentialDrydockDate).ToString("MMM yyyy") : "";
            }
        }
        public string MostRecentComment { get; set; }
        public string CommentCreatedBy { get; set; }
        public DateTimeOffset? CommentLastModifiedDate { get; set; }
        public DateTime? ProjectLastUpdatedDate { get; set; }
        public byte? PercentComplete { get; set; }
        public string IsDownload { get; set; }
        public string ProjectStatusName { get; set; }
        public string ProjectStatusColor { get; set; }
        public int TotalCount { get; set; }
    }
    public class DropdownListProjects
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Imo { get; set; }
    }
}
