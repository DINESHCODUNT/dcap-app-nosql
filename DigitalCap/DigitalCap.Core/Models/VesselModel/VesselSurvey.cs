using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.VesselModel
{
    public class VesselSurvey : CachedData
    {
       
        public ulong Uid { get; set; }       
        public string Type { get; set; }      
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? RangeFromDate { get; set; }
        public DateTime? RangeToDate { get; set; }
        public DateTime? DoneDate { get; set; }
        public string PlaceHeld { get; set; }
        public DateTime? ExtendedDueDate { get; set; }
        public string Status { get; set; }
        public override string ToString()
        {
            return Description;
        }
    }
}
