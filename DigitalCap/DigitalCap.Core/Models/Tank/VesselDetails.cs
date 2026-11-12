using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Tank
{
    
    public class VesselDetails
    {
        public string VesselName { get; set; }
        public string IMO { get; set; }
        public int Type { get; set; }
        public string[] ClassSociety { get; set; }
        public string AbsClassID { get; set; }
        public string Builder { get; set; }
        public string HullNumber { get; set; }
        public double LengthOverall { get; set; }
        public int? MonthBuilt { get; set; }
        public int? YearBuilt { get; set; }
        public DateTime PotentialDrydockDate { get; set; }
        public string DrydockLocation { get; set; }
    }
}
