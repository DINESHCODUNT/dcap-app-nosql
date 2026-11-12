using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Survey
{
    public class UpskillTemplateModel
    {
        public string ApplicationId { get; set; }
        public List<Template> Template { get; set; }
        public string RootSequence { get; set; }
        public string Name { get; set; }
    }

    public class Template
    {
        public string ApplicationId { get; set; }
        public string SequenceId { get; set; }
        public List<UpSkillCard> Cards { get; set; }
    }

    public class JsonTemplate
    {
        public string ApplicationId { get; set; }
        public string Template { get; set; }
    }
}
