using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.ReportConfig
{
    public class ProjectReportViewModel
    {


        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public ProjectReport Report { get; set; }
        public List<ProjectPart> ReportPartGrid { get; set; }
        public List<UpskillImageData> UnplacedImages { get; set; }
        public bool? ReportExport { get; set; } = false;
        public bool? ReportExportAll { get; set; }
        public bool? ResetTemplate { get; set; }
        public IsSynchedOnline SynchedOnline { get; set; }
        public bool IsSynched { get; set; }
        public bool IsRleased { get; set; }
        public int? Imonumber { get; set; }
        public ProjectExportSettings ExportSettings { get; set; }
        public bool DisplayTitle { get; set; }

    }
    public class ProjectReport
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int? ReportTemplateSectionId { get; set; }
        public string ReportNo { get; set; }
        public string ReportPort { get; set; }
        public string ReportDate { get; set; }
        public DigitalCap.Core.Models.Survey.Vessel Vessel { get; set; }
        public SurveyStatus SurveyStatus { get; set; }
        public StatutoryCertificates StatutoryCertificatesExpirationDates { get; set; }
        public StatutoryCertificates StatutoryCertificatesIssuedDates { get; set; }
        public ProjectView template { get; set; }
        public ProjectTankReportView tankReportView { get; set; }
        public List<ProjectGradingSection> sections { get; set; }
        public int? ProjectStatus { get; set; }
        public bool IsSynched { get; set; }
        public string ProjectVesselType { get; set; }
        public List<Guid> sectionIds { get; set; }
        public int ImoNumber { get; set; }
        public ProjectExportSettings ExportSettings { get; set; }

        public bool ShowExportSettings { get; set; } = true;
        public int Order { get; set; }
    }


    public class ProjectPart
    {
        public int PartId { get; set; }
        public string PartName { get; set; }
        public int SequenceNo { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool IsDeleted { get; set; }
        public List<NormalProjectSections>? normalSections { get; set; }

        public List<Guid?> SectionIds { get; set; }
        public List<ProjectGradingSection> SectionsList { get; set; }
    }


    public class ProjectGradingUI
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool RequiredInReport { get; set; }
        public int GradingConditionGroupId { get; set; }
        public List<ProjectCheckBox> Checkbox { get; set; }
    }
    public class ProjectCheckBox
    {
        public Guid SectionId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Value { get; set; }
        public int GradingId { get; set; }
    }
    public class ProjectGradingSection
    {
        public Guid? SectionId { get; set; }
        public String SectionName { get; set; }
        public int Order { get; set; }
        public bool IsSubSection { get; set; }

        public int TankTypeId { get; set; }
    }



    public class ProjectView
    {

        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string TemplateTitle { get; set; }
        public HandIReport handireport { get; set; }
        public ProjectSpecificSections Sections { get; set; }
        public List<CurrentCondition> currentConditions { get; set; }

    }

    public class ProjectTankReportView
    {

        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public List<ProjectTankUI> Sections { get; set; }
        public int sectionStartCount { get; set; }
        public string VesselType { get; set; }


    }
    public class ProjectSpecificSections
    {
        public Guid Id { get; set; }
        public string SectionName { get; set; }
        public List<ProjectSubSection> subSections { get; set; }
    }


    public class ProjectSubSection
    {
        public Guid SectionMappingId { get; set; }
        public Guid Id { get; set; }
        public string SectionName { get; set; }
        public string SectionNotes { get; set; }
        public string SubHeader { get; set; }
        public int Order { get; set; }
        public int FileNameCount { get; set; }
        public int Totalcards { get; set; }
        public int PlaceholderCount { get; set; }

        public List<string> GradingLabelName { get; set; }
        public List<GenericImageCard> ImageCards { get; set; }
        public List<ProjectGradingUI> Grading { get; set; }
        public List<ImageDescriptionUI> imageDescriptions { get; set; }
        public List<ProjectCardMapping> projectCards { get; set; }
        public List<CurrentCondition> currentConditions { get; set; }
        public bool HasSubsection { get; set; }

        public bool IsSubSection { get; set; }

    }


    public class GenericImageCard
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }
        public int CardNumber { get; set; }
        public string CardName { get; set; }
        public int DescriptionId { get; set; }
        public string AdditionalDescription { get; set; }
        public int? CurrentCondition { get; set; }
        public int ImageId { get; set; }
        public string src { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdatedDttm { get; set; }
        public bool IsActive { get; set; }
        public bool IsSync { get; set; }
        public bool IsDeleted { get; set; }
    }
    class GenericCardComparer : IEqualityComparer<GenericImageCard>
    {
        public bool Equals(GenericImageCard x, GenericImageCard y)
        {
            return x.CardNumber == y.CardNumber && x.ImageId > 0;
        }
        public int GetHashCode(GenericImageCard obj)
        {
            return obj.CardNumber.GetHashCode();
        }
    }

    public class ProjectTankUI
    {
        public int OrderNumber { get; set; }
        public Guid Id { get; set; }
        public string TankName { get; set; }
        public int TankTypeId { get; set; }
        public Guid? SectionMappingId { get; set; }
        public List<string> GradingLabelName { get; set; }
        public List<TankGradingUI> Grading { get; set; }
        public List<TankImageCard> tankImageCards { get; set; }
        public string SectionNotes { get; set; }
        public List<ImageDescriptionUI> imageDescriptions { get; set; }
        public List<CurrentCondition> currentConditions { get; set; }
        public string Subheader { get; set; }
        public int FileNameCount { get; set; }
        public int Totalcards { get; set; }
        public int PlaceholderCount { get; set; }
    }
    public class NormalProjectSections
    {

        public string SectionName { get; set; }

    }

    public class ProjectExportSettings
    {

        public List<ExportPart> exportParts { get; set; }


    }
}
