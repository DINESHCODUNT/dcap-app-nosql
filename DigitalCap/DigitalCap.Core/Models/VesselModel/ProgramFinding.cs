using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.VesselModel
{
    public class ProgramFinding
    {
        public string ID { get; set; }
        public string TicketNumber { get; set; }
        public int? VesselId { get; set; }
        public string CRITICALITY { get; set; }
        public string FindingService { get; set; }
        public string AssetType { get; set; }
        public string AssetLabelName { get; set; }
        public string FailureType { get; set; }
        public string Category { get; set; }
        public string Group_Name { get; set; }
        public string Item_Type { get; set; }
    }
}
