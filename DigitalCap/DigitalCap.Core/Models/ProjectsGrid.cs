using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class ProjectsGrid
    {
        public int? ID { get; set; }
        public string Name { get; set; }
        public string ProjectStatusName { get; set; }
        public string CapRegion { get; set; }
        public int? IMO { get; set; }
        public string VesselName { get; set; }
        public int? PIDNumber { get; set; }
        public int? ProjectPriority { get; set; }
        public string CompanyName { get; set; }
        public string WCN { get; set; }
    }
}
