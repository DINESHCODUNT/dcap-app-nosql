using DigitalCap.Core.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class ProjectStatusViewModel
    {
        public int ProjectId { get; set; }
        public ProjectStatusType Type { get; set; }
        public double? CompletionPercentage { get; set; }
        public int? DaysWorked { get; set; }
        public int? ProjectPriority { get; set; }
    }
}
