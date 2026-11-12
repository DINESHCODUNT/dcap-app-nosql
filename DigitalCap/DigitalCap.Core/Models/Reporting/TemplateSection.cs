using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Reporting
{
    public class TemplateSection
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public string Name { get; set; }
        public int? Sequence { get; set; }
        public int? ParentId { get; set; }
        public bool IsFirstChild { get; set; }
        public TemplateSection Parent { get; set; }
        public List<TemplateSection> Sections { get; set; }
        public int? FileId { get; set; }
        public ReportFile File { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string LastModifiedBy { get; set; }
    }
}
