using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Grading
{
    public class GradingSection
    {
        public Guid SectionId { get; set; }
        public int TanktypeId { get; set; }
        public String SectionName { get; set; }
        public int TemplateId { get; set; }
    }
}
