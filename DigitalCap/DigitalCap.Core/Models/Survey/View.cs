using DigitalCap.Core.Models.Tank;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Survey
{
    public class View
    {

        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string TemplateTitle { get; set; }
        public HandIReport handireport { get; set; }
        public List<Sections> Sections { get; set; }
        public List<CurrentCondition> currentConditions { get; set; }
        public TankReportView tankReportView { get; set; }

    }

    public class Sections
    {
        public Guid Id { get; set; }
        public string SectionName { get; set; }
        public List<SubSection> subSections { get; set; }
        public string SpecialSectionName { get; set; }


    }
    public class SubSection
    {
        public int OrderNumber { get; set; }
        public Guid Id { get; set; }
        public string SubSectionName { get; set; }
        public List<string> GradingLabelName { get; set; }
        public List<Grading> Grading { get; set; }
        public List<ImageCards> ImageCards { get; set; }

        public List<ImageDescriptionUI> imageDescriptions { get; set; }
        public List<ProjectCardMapping> projectCards { get; set; }
        public string SectionNotes { get; set; }
        public string SpecialSectionName { get; set; }
    }
    public class Grading
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public List<CheckBox> Checkbox { get; set; }
    }

    public class ImageCards
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public bool CurrentCondition { get; set; }
        public List<CurrentCondition> CurrentConditionOptions { get; set; }
        public string AdditionalDescription { get; set; }
        public string FileName { get; set; }
        public int ImageId { get; set; }
        public List<Description> DescriptionOptions { get; set; }
        public int descriptionvalue { get; set; }
        public int currentconditionvalue { get; set; }
        public string src { get; set; }

    }

    public class Description
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CheckBox
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Value { get; set; }
        public int GradingId { get; set; }
    }

    public class CurrentCondition
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class HandIReport
    {
        public Guid Id { get; set; }
        public int ProjectId { get; set; }
        public Guid SectionId { get; set; }
        public string CompanyName { get; set; }
        public string CertificateNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string ThicknessMeasurementA { get; set; }
        public string ThicknessMeasurementB { get; set; }
        public string ThicknessMeasurementC { get; set; }
        public string AdditionalSurveyorComments { get; set; }
        public string MaintenanceExecuted { get; set; }
        public string Accommodation { get; set; }
        public string PhotoGraphs { get; set; }
        public string OperationalTestLocation { get; set; }
        public DateTime OperationalTestStart { get; set; }
        public DateTime OperationalTestFinish { get; set; }
        public string OptionalTestSatisFactory { get; set; }
        public string AssociatedReportNumber { get; set; }
        public string AssociatedReportDate { get; set; }
        public string Remarks { get; set; }
        public string LeadSurveyorName { get; set; }
        public string LeadSurveyorNumber { get; set; }
        public string SurveyorName { get; set; }
        public string SurveyorNumber { get; set; }
        public DateTime LastVisitDate { get; set; }
        public string IndividualA { get; set; }
        public string IndividualB { get; set; }
        public string IndividualC { get; set; }
        public string IndividualD { get; set; }
        public string IndividualE { get; set; }
        public string IndividualF { get; set; }
        public string SubstantialCorrosionRemaining { get; set; } = null;
        public string StatutoryDeficiencies { get; set; } = null;
        public string FoundSatisfactory { get; set; } = null;
        public string UnderRemarks { get; set; } = null;
        public string ScouringOrPitting { get; set; } = null;
        public string BottomPlattingScouringOrPitting { get; set; } = null;
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdatedDttm { get; set; }
        public List<Options> maintanenceOptions { get; set; }
        public List<Options> sectionOptions { get; set; }
        public List<string> GradingLabelName { get; set; }
        public List<Grading> Grading { get; set; }
    }

    public class Options
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }

}
