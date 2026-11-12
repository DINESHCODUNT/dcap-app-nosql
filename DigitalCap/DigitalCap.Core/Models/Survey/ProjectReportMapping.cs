using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Survey
{
    public class ProjectReportMapping
    {
        public Guid Id { get; set; }
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }
        public string Notes { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsSync { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public DateTime UpdatedDttm { get; set; }
        public bool RequiredInReport { get; set; }
    }
    public class Images
    {
        public int Id { get; set; }
        public string AssignmentId { get; set; }
        public string FileId { get; set; }
        public DateTime InsertDate { get; set; }
        public int ProjectId { get; set; }
    }
    public class LoginData
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public string ProviderKey { get; set; }
    }
    public class CapActivity
    {
        public int ID { get; set; }
        public string LinkText { get; set; }
        public string LinkHref { get; set; }
        public string Text { get; set; }
        public int? Icon { get; set; }
        public bool? IsDeleted { get; set; }
        public bool? IsRead { get; set; }
        public string ReadBy { get; set; }
        public DateTime? ReadDttm { get; set; }
        public string CreateBy { get; set; }
        public DateTime? CreateDttm { get; set; }
        public int? ProjectId { get; set; }
    }
    public class TransferDataModel
    {
        public int ProjectId { get; set; }
        public ProjectDataTransfer Project { get; set; }
        public List<CapActivity> Activities { get; set; }
        public List<GetReportVesselMainData> VesselMainData { get; set; }
        public List<ReportSurveyDetails> SurveyDetails { get; set; }
        public List<ReportStatutoryCertificates> StatutoryCertificates { get; set; }
        public List<Comment> comments { get; set; }
        public List<CapProjectGrade> projectGrades { get; set; }
        public List<GradingData> gradingData { get; set; }
        public HIReportTemplate HIReport { get; set; }
        public List<ImageDescriptionsData> imageDescriptions { get; set; }
        public List<VesselTankMapping> vesselTankMapping { get; set; }
        public List<VesselGradingMapping> gradingMapping { get; set; }
        public List<ProjectGradingMapping> gradingGradingMapping { get; set; }
        public List<ProjectGradingConditionMapping> projectGradingConditionMappings { get; set; }
        public List<ProjectReportMapping> reportMapping { get; set; }
        public List<ProjectCardMapping> projectCardMappings { get; set; }
        public List<TankImageCard> tankImageCards { get; set; }
        public List<Images> images { get; set; }
        public List<UpskillImageData> upskillImageData { get; set; }
    }

    public class HIReportTemplate
    {
        public Guid Id { get; set; }
        public int? ProjectId { get; set; }
        public string CompanyName { get; set; }
        public string CertificateNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string ThicknessMeasurementA { get; set; }
        public string ThicknessMeasurementB { get; set; }
        public string ThicknessMeasurementC { get; set; }
        public string AdditionalSurveyorComments { get; set; }
        public string MaintenanceExecuted { get; set; }
        public string Accommodation { get; set; }
        public string PhotoGraphs { get; set; }
        public string OperationalTestLocation { get; set; }
        public DateTime? OperationalTestStart { get; set; }
        public DateTime? OperationalTestFinish { get; set; }
        public string OptionalTestSatisFactory { get; set; }
        public string AssociatedReportNumber { get; set; }
        public string AssociatedReportDate { get; set; }
        public string Remarks { get; set; }
        public string LeadSurveyorName { get; set; }
        public string LeadSurveyorNumber { get; set; }
        public string SurveyorName { get; set; }
        public string SurveyorNumber { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public string IndividualA { get; set; }
        public string IndividualB { get; set; }
        public string IndividualC { get; set; }
        public string IndividualD { get; set; }
        public string IndividualE { get; set; }
        public string IndividualF { get; set; }
        public string SubstantialCorrosionRemaining { get; set; }
        public string StatutoryDeficiencies { get; set; }
        public string FoundSatisfactory { get; set; }
        public string UnderRemarks { get; set; }
        public string ScouringOrPitting { get; set; }
        public string BottomPlattingScouringOrPitting { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedDttm { get; set; }
        public DateTime? UpdatedDttm { get; set; }
    }

    public class ProjectCardMapping
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }
        public int CardId { get; set; }
        public int DescriptionId { get; set; }
        public string AdditionalDescription { get; set; }
        public int? CurrentCondition { get; set; }
        public int ImageId { get; set; }
        public string Src { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public bool IsSync { get; set; }



    }
    public class Comment
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int ProjectTaskId { get; set; }
        public string CommentText { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedDate { get; set; }
    }
    public class DropdownList
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Imo { get; set; }
    }
    public class ProjectDataTransfer
    {
        public int ID { get; set; }
        public int PIDNumber { get; set; }
        public string CapRegion { get; set; }
        public string CapScope { get; set; }
        public int DrydockLocation { get; set; }
        public DateTime DryDockStart { get; set; }
        public DateTime DryDockEnd { get; set; }
        public int ExpenseMarkup { get; set; }
        public decimal SurveyDayRateClient { get; set; }
        public decimal SurveyDayRateAbs { get; set; }
        public decimal SedRate1 { get; set; }
        public decimal SedRate2 { get; set; }
        public string AgreementNumber { get; set; }
        public decimal CapContractValue { get; set; }
        public string AgreementOwner { get; set; }
        public DateTime AgreementDate { get; set; }
        public DateTime AgreementSignedDate { get; set; }
        public DateTime SedEvaluationGrade { get; set; }
        public string CertificateGrade { get; set; }
        public string ProjectComments { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyBillingEmail { get; set; }
        public string CompanyBillingSystemUrl { get; set; }
        public bool BillSameAsCapClient { get; set; }
        public string BillToCompanyName { get; set; }
        public string BillToCompanyAddress { get; set; }
        public string BillToCompanyEmail { get; set; }
        public string BillToCompanyBillingUrl { get; set; }
        public string PocName { get; set; }
        public string PocEmail { get; set; }
        public string PocPhone { get; set; }
        public int IMO { get; set; }
        public string VesselName { get; set; }
        public string WCN { get; set; }
        public string ClassSociety { get; set; }
        public string AbsClassID { get; set; }
        public string SisterVesselImoNumber { get; set; }
        public string SisterVesselName { get; set; }
        public string Builder { get; set; }
        public int MonthBuilt { get; set; }
        public int YearBuilt { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string LastModifiedBy { get; set; }
        public int ProjectStatus { get; set; }
        public int ProjectPriority { get; set; }
        public int ShipType { get; set; }
        public string HullNumber { get; set; }
        public decimal LengthOverall { get; set; }
        public bool SisterVessel { get; set; }
        public bool SpecialHull { get; set; }
        public bool FatigueAnalysis { get; set; }
        public int CapType { get; set; }
        public int ProposalRev { get; set; }
        public string CRMOpportunityNumber { get; set; }
        public string Name { get; set; }
        public DateTime PotentialDrydockDate { get; set; }
        public DateTime SurveyFirstVisit { get; set; }
        public DateTime SurveyLastVisit { get; set; }
        public string BillToWCN { get; set; }
        public string LeadSurveyor { get; set; }
        public string LeadSurveyorEID { get; set; }
        public string SecondSurveyor { get; set; }
        public string SecondSurveyorEID { get; set; }
        public string ThirdSurveyor { get; set; }
        public string ThirdSurveyorEID { get; set; }
        public string FourthSurveyor { get; set; }
        public string FourthSurveyorEID { get; set; }
        public bool AdditionalSurveyors { get; set; }
        public string CapScopeOther { get; set; }
        public string ClassSocietyOther { get; set; }
        public Guid ClientProfileId { get; set; }
        public Guid BillToClientProfileId { get; set; }
        public string CreatedBy { get; set; }
        public string StatusChangedBy { get; set; }
        public DateTimeOffset StatusChangedDate { get; set; }
        public byte PercentComplete { get; set; }
        public string VesselType { get; set; }
        public bool IsDefaultTank { get; set; }
        public int CopyingVesselID { get; set; }
        public string TankVesselType { get; set; }
        public int TankVesselIMO { get; set; }
    }
    public class CapProjectGrade
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int CAPCertificateNumber { get; set; }
        public DateTime? CAPCertificateIssuanceDate { get; set; }
        public int ClassReportNumber { get; set; }
        public DateTime? ClassReportDate { get; set; }
        public int StructuralReportNumber { get; set; }
        public DateTime? StructuralReportDate { get; set; }
        public int FinalGrade { get; set; }
        public int StructuralGrade { get; set; }
        public int FatigueGrade { get; set; }
        public int RenewalGrade { get; set; }
        public int MaterialGrade { get; set; }
        public int GaugingGrade { get; set; }
        public int HullGirderStrength { get; set; }
        public DateTime? CAP1CertificateIssuanceDate { get; set; }
        public string CAP1FinalGrade { get; set; }
    }
    public class ReportSurveyDetails
    {

        public int ProjectId { get; set; }

        public string ImoNum { get; set; }

        public DateTime? ClassSurveyDate { get; set; }

        public DateTime? DryDockDate { get; set; }

        public DateTime? SpecialContinuousMachineryDate { get; set; }

        public DateTime? BoilerDate { get; set; }

        public DateTime? TailshaftDate { get; set; }

        public DateTime? SpecialContinuousHull1 { get; set; }

        public DateTime? SpecialContinuousHull2 { get; set; }

        public DateTime? SpecialContinuousHull3 { get; set; }

        public DateTime? SpecialContinuousHull4 { get; set; }

        public DateTime? SpecialContinuousHull5 { get; set; }

        public DateTime? SpecialContinuousHull6 { get; set; }

        public DateTime? SpecialContinuousHull7 { get; set; }

        public DateTime? SpecialContinuousHull8 { get; set; }

        public DateTime? SpecialContinuousHull9 { get; set; }

        public DateTime? SpecialContinuousHull10 { get; set; }

        public DateTime? SpecialContinuousHull11 { get; set; }

        public DateTime? SpecialContinuousHull12 { get; set; }

        public DateTime? SpecialContinuousHull13 { get; set; }

        public DateTime? SpecialContinuousHull14 { get; set; }

        public DateTime? SpecialContinuousHull15 { get; set; }

        public int SpecialHullNo { get; set; }
    }

    public class GetReportVesselMainData
    {

        public int ProjectId { get; set; }

        public string ReportNo { get; set; }

        public string PortDate { get; set; }

        public string ActualReportStartDate { get; set; }

        public string VesselName { get; set; }

        public int VesselID { get; set; }

        public int ImoNumber { get; set; }

        public string VesselType { get; set; }

        public string YearBuilt { get; set; }

        public string BuiltBy { get; set; }

        public string HullNo { get; set; }

        public string FlagName { get; set; }

        public string Homeport { get; set; }

        public string OfficalNumber { get; set; }

        public string CallSign { get; set; }

        public string PreviousName { get; set; }

        public string OwnerName { get; set; }

        public string Manager { get; set; }

        public string Length { get; set; }

        public string Breadth { get; set; }

        public string Depth { get; set; }

        public string Draft { get; set; }

        public string Deadweight { get; set; }

        public string GrossTons { get; set; }

        public string NetTons { get; set; }

        public string PropulsionType { get; set; }

        public string KW { get; set; }

        public string ShaftRPM { get; set; }

        public string PropulsionManufacturer { get; set; }

        public string GearManufacturer { get; set; }

        public string Classed { get; set; }

        public string ClassNo { get; set; }

        public string ClassNotation { get; set; }

        public string Machinery { get; set; }

        public string Other { get; set; }

        public string Environment { get; set; }

        public string ReportPort { get; set; }

        public string FirstVisitDate { get; set; }

        public string LastVisitDate { get; set; }

        public int SelectedPrefix { get; set; }
    }
    public class IsSynchedOnline
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ProjectId { get; set; }
        public DateTime DownloadOfflineDate { get; set; }
        public string UserName { get; set; }
        public string EmailId { get; set; }
        public DateTime SynchedDate { get; set; }
        public bool IsSynched { get; set; }
        public string Name { get; set; }

    }

    public class GradingData
    {
        public int Id { get; set; }
        public string LabelName { get; set; }
        public int GradingGroupId { get; set; }
        public int GradingConditionGroupId { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public string VesselType { get; set; }
        public int ProjectId { get; set; }
        public bool RequiredInReport { get; set; }
    }
    public class HandIProjectGrading
    {
        public int Id { get; set; }
        public int? ProjectID { get; set; }
        public int? GradingId { get; set; }
        public string Value { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public string CreatedBY { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBY { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool IsSync { get; set; }
    }
    public class HandIProjectGradingConditions
    {
        public int Id { get; set; }
        public int? GradingID { get; set; }
        public int? ProjectId { get; set; }
        public string ConditionLabel { get; set; }
        public bool? Value { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public string CreatedBY { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBY { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool IsSync { get; set; }
    }
    public class AllSections
    {
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }
    }


    public class ProjectGradingMapping
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }
        public int GradingId { get; set; }
        public string Value { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public DateTime UpdatedDttm { get; set; }
        public bool IsSync { get; set; }
    }
    public class ProjectGradingConditionMapping
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }
        public int GradingId { get; set; }
        public string GradingConditionLabel { get; set; }
        public bool GradingConditionValue { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public DateTime UpdatedDttm { get; set; }
        public bool IsSync { get; set; }

    }


}
public class CapActivity
{
    public int ID { get; set; }
    public string LinkText { get; set; }
    public string LinkHref { get; set; }
    public string Text { get; set; }
    public int? Icon { get; set; }
    public bool? IsDeleted { get; set; }
    public bool? IsRead { get; set; }
    public string ReadBy { get; set; }
    public DateTime? ReadDttm { get; set; }
    public string CreateBy { get; set; }
    public DateTime? CreateDttm { get; set; }
    public int? ProjectId { get; set; }
}

public class TransferDataModel
{
    public int ProjectId { get; set; }

    public ProjectDataTransfer Project { get; set; }
    public List<GetReportVesselMainData> VesselMainData { get; set; }
    public List<CapActivity> Activities { get; set; }
    public List<ReportSurveyDetails> SurveyDetails { get; set; }
    public List<DigitalCap.Core.Models.DataTransfer.TransferReportStatutoryCertificates> StatutoryCertificates { get; set; }
    public List<HandIProjectGrading> handIProjectGradings { get; set; }
    public List<HandIProjectGradingConditions> handIProjectGradingConditions { get; set; }
    public List<Comment> comments { get; set; }
    public List<DigitalCap.Core.Models.DataTransfer.TransferReportPart> ReportPart { get; set; }
    public List<ProjectTankSectionMapping> ProjectTankSection { get; set; }
    public List<ProjectSectionMapping> ProjectSectionMapping { get; set; }
    public List<DigitalCap.Core.Models.ProjectSubSection> ProjectSubSection { get; set; }
    public List<ProjectSectionGradingMapping> ProjectSectionGrading { get; set; }
    public List<ProjectSectionDescriptionMapping> ProjectSectionDescription { get; set; }
    public List<ProjectSubSectionDescriptionMapping> ProjectSubSectionDescription { get; set; }
    public List<H_I_Grading> H_I_Grading { get; set; }
    public List<CapProjectGrade> projectGrades { get; set; }
    public List<GradingData> gradingData { get; set; }
    public HIReportTemplate HIReport { get; set; }
    public List<ImageDescriptionsData> imageDescriptions { get; set; }
    public List<VesselTankMapping> vesselTankMapping { get; set; }
    public List<VesselGradingMapping> gradingMapping { get; set; }
    public List<ProjectGradingMapping> gradingGradingMapping { get; set; }
    public List<ProjectGradingConditionMapping> projectGradingConditionMappings { get; set; }
    public List<ProjectReportMapping> reportMapping { get; set; }
    public List<ProjectCardMapping> projectCardMappings { get; set; }
    public List<GenericImageCard> genericImageCard { get; set; }
    public List<TankImageCard> tankImageCards { get; set; }
    public List<Images> images { get; set; }
    public List<UpskillImageData> upskillImageData { get; set; }

    public List<VesselTypeTankMapping> VesselTypeTankMapping { get; set; }

    public List<DigitalCap.Core.Models.SingleTask>? ProjectTasks { get; set; }
    public DigitalCap.Core.Models.DataTransfer.OfflineUserSecurity? UserSecurity { get; set; }
}
