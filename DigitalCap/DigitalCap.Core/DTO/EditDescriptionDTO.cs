using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class EditDescriptionDTO
    {
        public int Id { get; set; }
        public string VesselType { get; set; }
        public string TemplateName { get; set; }
        public string SectionName { get; set; }
        public string DescriptionName { get; set; }
        public bool Status { get; set; }

    }
}
