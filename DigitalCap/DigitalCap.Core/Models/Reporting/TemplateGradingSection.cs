using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Reporting
{
    public class TemplateGradingSection
    {
        public TemplateSection TemplateSection { get; set; }
        public string AdditionalNotesDefaultValue { get; set; }
        public List<TemplateGradingSectionSurveyItem> SurveyItems { get; set; }
        public List<string> PhotoDescriptions { get; set; }
    }
}
