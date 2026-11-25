using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class H_IReportTemplateDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public string? Id { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonProperty("CertificateNumber")]
        public string? CertificateNumber { get; set; }

        [JsonProperty("ExpiryDate")]
        public DateTime? ExpiryDate { get; set; }

        [JsonProperty("ThicknessMeasurementA")]
        public string? ThicknessMeasurementA { get; set; }

        [JsonProperty("ThicknessMeasurementB")]
        public string? ThicknessMeasurementB { get; set; }

        [JsonProperty("ThicknessMeasurementC")]
        public string? ThicknessMeasurementC { get; set; }

        [JsonProperty("AdditionalSurveyorComments")]
        public string? AdditionalSurveyorComments { get; set; }

        [JsonProperty("MaintenanceExecuted")]
        public string? MaintenanceExecuted { get; set; }

        [JsonProperty("PhotoGraphs")]
        public string? PhotoGraphs { get; set; }

        [JsonProperty("OperationalTestLocation")]
        public string? OperationalTestLocation { get; set; }

        [JsonProperty("OperationalTestStart")]
        public DateTime? OperationalTestStart { get; set; }

        [JsonProperty("OperationalTestFinish")]
        public DateTime? OperationalTestFinish { get; set; }

        [JsonProperty("OptionalTestSatisFactory")]
        public string? OptionalTestSatisFactory { get; set; }

        [JsonProperty("AssociatedReportNumber")]
        public string? AssociatedReportNumber { get; set; }

        [JsonProperty("AssociatedReportDate")]
        public string? AssociatedReportDate { get; set; }

        [JsonProperty("Remarks")]
        public string? Remarks { get; set; }

        [JsonProperty("LeadSurveyorName")]
        public string? LeadSurveyorName { get; set; }

        [JsonProperty("LeadSurveyorNumber")]
        public string? LeadSurveyorNumber { get; set; }

        [JsonProperty("SurveyorName")]
        public string? SurveyorName { get; set; }

        [JsonProperty("SurveyorNumber")]
        public string? SurveyorNumber { get; set; }

        [JsonProperty("LastVisitDate")]
        public DateTime? LastVisitDate { get; set; }

        [JsonProperty("IndividualA")]
        public string? IndividualA { get; set; }

        [JsonProperty("IndividualB")]
        public string? IndividualB { get; set; }

        [JsonProperty("IndividualC")]
        public string? IndividualC { get; set; }

        [JsonProperty("IndividualD")]
        public string? IndividualD { get; set; }

        [JsonProperty("IndividualE")]
        public string? IndividualE { get; set; }

        [JsonProperty("IndividualF")]
        public string? IndividualF { get; set; }

        [JsonProperty("SubstantialCorrosionRemaining")]
        public string? SubstantialCorrosionRemaining { get; set; }

        [JsonProperty("StatutoryDeficiencies")]
        public string? StatutoryDeficiencies { get; set; }

        [JsonProperty("FoundSatisfactory")]
        public string? FoundSatisfactory { get; set; }

        [JsonProperty("UnderRemarks")]
        public string? UnderRemarks { get; set; }

        [JsonProperty("ScouringOrPitting")]
        public string? ScouringOrPitting { get; set; }

        [JsonProperty("BottomPlattingScouringOrPitting")]
        public string? BottomPlattingScouringOrPitting { get; set; }

        [JsonProperty("IsActive")]
        public bool? IsActive { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }

        [JsonProperty("CreatedDttm")]
        public DateTime? CreatedDttm { get; set; }

        [JsonProperty("UpdatedDttm")]
        public DateTime? UpdatedDttm { get; set; }

        [JsonProperty("Accommodation")]
        public string? Accommodation { get; set; }

        [JsonProperty("OthersDescription")]
        public string? OthersDescription { get; set; }
    }
}
