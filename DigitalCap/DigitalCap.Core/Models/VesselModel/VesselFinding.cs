using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.VesselModel
{
    public class Failures
    {
        public string Type { get; set; }
        public string Descriptor { get; set; }
    }

    public class VesselFinding : CachedData
    {
        public ulong? Uid { get; set; }

        public string ServiceType { get; set; }

        public string ServiceGroup { get; set; }

        public string FindingNumber { get; set; }


        public string FindingType { get; set; }


        public Failures[] Failures { get; set; }


        public string Status { get; set; }

        public ulong? PartUid { get; set; }


        public string PartName { get; set; }
        public string Criticality { get; set; }
        public string ReportNumber { get; set; }
        public string AssociatedTask { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DueDate { get; set; }
        public string DueByTask { get; set; }
        public string Description { get; set; }
        public History[] History { get; set; }
        public int? CriticalityValue { get; set; }
    }
}
