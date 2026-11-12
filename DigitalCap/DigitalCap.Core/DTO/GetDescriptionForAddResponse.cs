using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.Tank;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class GetDescriptionForAddResponse
    {
        public List<GradingTemplate> Templates { get; set; }
        public List<GradingSection> Sections { get; set; }
        public List<ShipType> VesselTypes { get; set; }
    }
}
