using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.Models.VesselModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Survey
{
    public class Card
    {
        public int CardId { get; set; }        
        public string ObjLabel { get; set; }
        public string ObjValue { get; set; }
        public string AdditionalData { get; set; }
        public int ControlType { get; set; }
    }
    public class Section
    {        
        public string IntegrationId { get; set; }        
        public string SequenceId { get; set; }
        public List<Card> Cards { get; set; }
        public int DisplayOrder { get; set; }
    }
    public class SectionByTemplateId
    {
        public int TemplateId { get; set; }

        public Guid SectionId { get; set; }
    }
    public class Assignment
    {       
        public string AssignmentId { get; set; }
        public List<Section> Sections { get; set; }
        public List<AssignmentImage> AssignmentImages { get; set; }
    }
    public class AssignmentImage
    {
        public int Id { get; set; }
        public string AssignmentId { get; set; }
        public string FileId { get; set; }
        public string ImageBase64 { get; set; }
    }
    public class App
    {
        public string AppId { get; set; }
        public string AppName { get; set; }
        public int TemplateDataId { get; set; }
        public List<Assignment> Assignments { get; set; }
    }
    public class ReportSection
    {
        public string Label { get; set; }
        public string AppId { get; set; }
        public string SequenceId { get; set; }
        public string CardId { get; set; }
        public string CombinedAppAndSequenceIds {
            get
            {
                if (!string.IsNullOrEmpty(AppId) && !string.IsNullOrEmpty(SequenceId))
                {
                    return $"{AppId},{CardId}";
                }
                return "";
            }
        }
    }
    public class Report
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int? ReportTemplateSectionId { get; set; }
        public string ReportNo { get; set; }
        public string ReportPort { get; set; }
        public string ReportDate { get; set; }
        public Vessel Vessel { get; set; }
        public SurveyStatus SurveyStatus { get; set; }
        public StatutoryCertificates StatutoryCertificatesExpirationDates { get; set; }
        public StatutoryCertificates StatutoryCertificatesIssuedDates { get; set; }       
        public View template { get; set; }
        public TankReportView tankReportView { get; set; }
        public List<GradingSection> sections { get; set; }
        public string AssignmentId { get; set; }
        public int? ProjectStatus { get; set; }
        public bool IsSynched { get; set; }

    }

    public class ReportPart
    {
        public int TemplateId { get; set; }
        public string Name { get; set; }
        public bool ReportPartExists { get; set; }
        public List<Guid> SectionIds { get; set; }
        public List<GradingSection> SectionsList { get; set; }
    }




    public enum ControlType
    {
        TextBox = 1,
        Checkbox = 2,
        PictureBox = 3,
        VideoBox = 4
    }


    public class ReportVesselMainData
    {
        public int projectId { get; set; }
        public string reportNo { get; set; }
        public string portDate { get; set; }
        public string actualReportStartDate { get; set; }
        public string vesselName { get; set; }
        public int vesselID { get; set; }
        public int imoNumber { get; set; }
        public string vesselType { get; set; }
        public string yearBuilt { get; set; }
        public string builtBy { get; set; }
        public string hullNo { get; set; }
        public string flagName { get; set; }
        public string homeport { get; set; }
        public string officalNumber { get; set; }
        public string callSign { get; set; }
        public string previousName { get; set; }
        public string ownerName { get; set; }
        public string manager { get; set; }
        public string length { get; set; }
        public string breadth { get; set; }
        public string depth { get; set; }
        public string draft { get; set; }
        public string deadweight { get; set; }
        public string grossTons { get; set; }
        public string netTons { get; set; }
        public string propulsionType { get; set; }
        public string kw { get; set; }
        public string shaftRPM { get; set; }
        public string propulsionManufacturer { get; set; }
        public string gearManufacturer { get; set; }
        public string classed { get; set; }
        public string classNo { get; set; }
        public string classNotation { get; set; }
        public string machinery { get; set; }
        public string other { get; set; }
        public string environment { get; set; }
        public string reportPort { get; set; }
        public string firstVisitDate { get; set; }
        public string lastVisitDate { get; set; }
        public int selectedPrefix { get; set; }
    }
    public class TemplateGrading
    {
        public int Id { get; set; }
        public string Label_Name { get; set; }
        public int GradingConditionGroupId { get; set; }


    }
    public class GradingCondition
    {
        public int Id { get; set; }
        public string Condition { get; set; }
        public int GradingGroupId { get; set; }
        public int GradingId { get; set; }

    }
    public class ImageCard
    {
        public int Id { get; set; }
        public string SectionId { get; set; }
        public string FileName { get; set; }
        public bool IsDescDropdown { get; set; }
        public bool CurrentCondition { get; set; }
        public string Description { get; set; }

    }
    public class ImageDescription
    {
        public int Id { get; set; }
        public string Description { get; set; }

    }
    public class ReportTemplateUI
    {
        public int Id { get; set; }
        public string SectionName { get; set; }

    }
    public class ReportTemplateSection
    {
        public Guid Id { get; set; }
        public Guid SectionId { get; set; }
        public string SectionName { get; set; }
        public bool HasSubSection { get; set; }
        public string SubSectionName { get; set; }
        public string SpecialSectionName { get; set; }

    }
    public class ImageCardUI
    {
        public int Id { get; set; }
        public Guid SectionId { get; set; }
        public string FileName { get; set; }
        public bool IsDescDropdown { get; set; }
        public bool CurrentCondition { get; set; }
        public string Description { get; set; }
    }
    public class GradingUI
    {
        public int Id { get; set; }
        public Guid SectionId { get; set; }
        public string LabelName { get; set; }


    }
    public class GradingConditionUI
    {
        public int Id { get; set; }
        public string Condition { get; set; }
        public Guid SectionId { get; set; }
        public int GradingId { get; set; }

    }

    public class ImageDescriptionUI
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public int value { get; set; }
        public int TankTypeId { get; set; }

    }


    public class Sectiondata
    {
        public bool isTankData { get; set; }
        public SubSection sectionData { get; set; }
        public TankUI tankSectionData { get; set; }
    }

}
