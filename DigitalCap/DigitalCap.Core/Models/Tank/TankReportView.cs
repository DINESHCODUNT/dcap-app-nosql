using DigitalCap.Core.Models.Survey;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Tank
{
    public class TankReportView
    {

        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public List<TankUI> Sections { get; set; }
        public int sectionStartCount { get; set; }
        public int TankTypeId { get; set; }
        public string VesselType { get; set; }


    }
    public class TankUI
    {
        public int OrderNumber { get; set; }
        public Guid Id { get; set; }
        public string TankName { get; set; }
        public int TankTypeId { get; set; }
        public List<string> GradingLabelName { get; set; }
        public List<TankGradingUI> Grading { get; set; }
        public List<TankImageCard> tankImageCards { get; set; }
        public string SectionNotes { get; set; }
        public List<ImageDescriptionUI> imageDescriptions { get; set; }
        public List<CurrentCondition> currentConditions { get; set; }
        public int CurrentconditionPlaceholders { get; set; }
        public string Subheader { get; set; }

    }



    public class TankGradingUI
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public int GradingConditionGroupId { get; set; }

        public bool RequiredInReport { get; set; }
        public List<TankCheckBox> Checkbox { get; set; }
        public int TankTypeId { get; set; }

        public Guid SectionId { get; set; }
    }



    public class TankCheckBox
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Value { get; set; }
    }

    public class TankListTemplate
    {
        public int TankListId { get; set; }
    }

}
