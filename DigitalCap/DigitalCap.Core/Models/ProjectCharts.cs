using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class ProjectCharts
    {
        public string Category { get; set; }
        public int? Value { get; set; } = 0;
        public string Color { get; set; }
    }
}
