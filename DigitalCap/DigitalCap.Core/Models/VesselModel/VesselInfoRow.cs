using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.VesselModel
{
    public class VesselInfoRow
    {
        public int VesselId { get; set; }
        public string VesselName { get; set; }
        public int FleetId { get; set; }
        public string FleetName { get; set; }

        public int VesselDWIdentifier { get; set; }
        public string AbsClassNumber { get; set; }
        public string ImoNumber { get; set; }
        public string ExtensionOrMonitoring { get; set; }
        public string IsOutlier { get; set; }
        public string DashboardVM { get; set; }
        public DateTime FatigueDate { get; set; }
        public string VesselGuid { get; set; }

        public string FleetsLookup { get; set; }
    }
}
