using DigitalCap.Core.Models.Survey;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class SurveyReportViewModel
    {
        public int? Imonumber { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public Report Report { get; set; }
        public List<ReportPart> ReportPartGrid { get; set; }
        public List<UpskillImageData> UnplacedImages { get; set; }
        public bool? ReportExport { get; set; } = false;
        public bool? ReportExportAll { get; set; }
        public bool? ResetTemplate { get; set; }
        public IsSynchedOnline SynchedOnline { get; set; }
        public bool IsSynched { get; set; }
        public bool IsRleased { get; set; }
        public bool IsConfiguredProject { get; set; }
    }


    public class MergeReportResultViewModel
    {
        public string Base64 { get; set; }
        public string FileName { get; set; }
    }

    public class ValidateReportResponse
    {
        public string ResultSession { get; set; }
    }
    public class UploadPhotoResultDto
    {
        public string FileId { get; set; }
        public string Image { get; set; }
    }

    public class PlaceImageResultDto
    {
        public bool IsSuccess { get; set; }
        public string ImageData { get; set; }
    }

    public class UpdateGradingDto
    {
        public int TemplateId { get; set; }
        public int ProjectId { get; set; }
        public Guid SectionId { get; set; }
        public int GradingId { get; set; }
        public List<GradingChoiceUpdateDto> Choices { get; set; }
    }

    public class GradingChoiceUpdateDto
    {
        public string ChoiceName { get; set; }
        public bool Value { get; set; }
    }
    public class UpdateNotesDto
    {
        public int TemplateId { get; set; }
        public int ProjectId { get; set; }
        public Guid SectionId { get; set; }
        public int GradingId { get; set; }
        public string Value { get; set; }
    }

    public class UploadImageDto
    {
        public IFormFile File { get; set; }

        public int TemplateId { get; set; }
        public int ProjectId { get; set; }
        public Guid SectionId { get; set; }
        public int CardId { get; set; }
        public int ImageId { get; set; }
        public bool ReplaceImage { get; set; }
    }

    public class ValidateGradingDto
    {
        public int ProjectId { get; set; }
        public int TemplateSectionId { get; set; }
    }

    public class ExportReportDto
    {
        public int ProjectId { get; set; }
        public int? TemplateId { get; set; }
        public string SectionId { get; set; }
        public bool AllTemplate { get; set; }
    }

    public class UpdateImageDescriptionDto
    {
        public int TemplateId { get; set; }
        public int ProjectId { get; set; }
        public Guid SectionId { get; set; }
        public int CardId { get; set; }
        public int Value { get; set; }
    }
    public class UpdateAdditionalDescriptionDto
    {
        public int TemplateId { get; set; }
        public int ProjectId { get; set; }
        public Guid SectionId { get; set; }
        public int CardId { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public class UpdateCurrentConditionDto
    {
        public int TemplateId { get; set; }
        public int ProjectId { get; set; }
        public Guid SectionId { get; set; }
        public int CardId { get; set; }
        public int Value { get; set; }
    }

    public class UpdateReportRequest
    {
        public int ProjectId { get; set; }

        public int TemplateId { get; set; }

        public int ReportPart { get; set; }

        public string Value { get; set; }

        public int Imonumber { get; set; }

        public int HullNo { get; set; } = 1;

        public string? SurveyDate { get; set; }
    }

    public class UpdateHIReportDto
    {
        public Guid Id { get; set; }

        public int ProjectId { get; set; }

        public string CompanyName { get; set; }

        public string CertificateNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public string ThicknessMeasurementA { get; set; }

        public string ThicknessMeasurementB { get; set; }

        public string ThicknessMeasurementC { get; set; }

        public string AdditionalSurveyorComments { get; set; }

        public string MaintanceExecuted { get; set; }

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

        public string ScouringOrPitting { get; set; }

        public string SubstantialCorrosionRemaining { get; set; }

        public string StatutoryDeficiencies { get; set; }

        public string FoundSatisfactory { get; set; }

        public string UnderRemarks { get; set; }

        public string BottomPlattingScouringOrPitting { get; set; }
    }
}
