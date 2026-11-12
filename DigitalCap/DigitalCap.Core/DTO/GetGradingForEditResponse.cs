using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class GetGradingForEditResponse
    {
        public int Id { get; set; }
        public string TemplateName { get; set; }
        public string VesselType { get; set; }
        public string SectionName { get; set; }
        public string GradingName { get; set; }
        public bool Status { get; set; }
        public bool RequiredInReport { get; set; }
        public int TanktypeId { get; set; }
        public Guid SectionId { get; set; }

        public List<GradingTemplate> Templates { get; set; }
        public List<GradingSection> Sections { get; set; }
        public List<ShipType> VesselTypes { get; set; }
    }
}
