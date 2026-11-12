using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Survey
{
    public class ReportTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ProjectSections
    {
        public Guid Id { get; set; }
        public int TemplateId { get; set; }
        public int TankTypeId { get; set; }
    }

}
