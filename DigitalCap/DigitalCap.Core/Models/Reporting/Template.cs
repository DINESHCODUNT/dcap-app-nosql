using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Reporting
{
    public class Template
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<TemplateSection> Sections { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string LastModifiedBy { get; set; }
    }
}
