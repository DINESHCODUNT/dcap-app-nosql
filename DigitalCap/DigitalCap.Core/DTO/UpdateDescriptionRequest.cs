using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class UpdateDescriptionRequest
    {
            public int Id { get; set; }
            public string Description { get; set; }
            public bool IsActive { get; set; }
            public string VesselType { get; set; }
            public string TemplateName { get; set; }
            public Guid SectionId { get; set; }
            public string DescriptionName { get; set; }
            public bool Status { get; set; }
    }
}
