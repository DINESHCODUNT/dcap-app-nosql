using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.VesselModel
{
    public class VesselTankDetails
    {
        public int ID { get; set; }
        public string IMO { get; set; }
        public string Name { get; set; }
        public string VesselType { get; set; }
        public string CombinedDescription { get; set; } 
    }
}
