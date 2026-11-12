using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Grading
{
    public class Grading
    {
        public Guid SectionId { get; set; }
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public int TanktypeId { get; set; }
        public string SectionName { get; set; }
        public int GradingId { get; set; }
        public string GradingName { get; set; }
        public string VesselType { get; set; }
        public string? ProjectName { get; set; }
        public int? ProjectId { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; } = false;
        public bool RequiredInReport { get; set; }
        public int TotalCount { get; set; }
    }

    public class DeleteGradingRequest
    {
        public int GradingId { get; set; }
        public int TankId { get; set; }
    }
    public class CreateGradingRequest
    {
        public Guid SectionId { get; set; }
        public int GradingId { get; set; }
        public int TanktypeId { get; set; }
        public string GradingName { get; set; }
        public string SectionName { get; set; }
        public string VesselType { get; set; }
        public string? ProjectName { get; set; }
        public string TemplateName { get; set; }
        public int? ProjectId { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public bool? IsDeleted { get; set; } = false;
        public bool RequiredInReport { get; set; }
    }

    public class GradingResponse
    {
        public List<Grading> Data { get; set; }
        public int TotalCount { get; set; }
    }
}
