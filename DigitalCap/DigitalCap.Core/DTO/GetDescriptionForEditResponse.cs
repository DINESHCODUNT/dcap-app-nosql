using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.Tank;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class GetDescriptionForEditResponse
    {
        public int Id { get; set; }
        public string TemplateName { get; set; }
        public string VesselType { get; set; }
        public Guid? SectionId { get; set; }
        public string SectionName { get; set; }
        public string DescriptionName { get; set; }
        public int TankTypeId { get; set; }
        public bool Status { get; set; }

        public List<GradingTemplate> Templates { get; set; }
        public List<GradingSection> Sections { get; set; }
        public List<ShipType> VesselTypes { get; set; }
    }
}
