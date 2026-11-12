using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Reporting
{
    public class TemplateGradingSectionSurveyItem
    {
        public string Name { get; set; }
        public bool IsGradable { get; set; }
        public int Depth { get; set; }
    }
}
