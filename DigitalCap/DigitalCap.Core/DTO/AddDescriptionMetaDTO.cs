using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Models.Grading;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class AddDescriptionMetaDTO
    {
        public IEnumerable<GradingTemplate> Templates { get; set; }
        public IEnumerable<GradingSection> Sections { get; set; }
        public IEnumerable<ShipType> VesselTypes { get; set; }
    }
}
