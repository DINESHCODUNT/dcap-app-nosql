using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ProjectGradeDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("ProjectId")]
        public int ProjectId { get; set; }

        [JsonProperty("CAPCertificateNumber")]
        public int? CAPCertificateNumber { get; set; }

        [JsonProperty("CAPCertificateIssuanceDate")]
        public DateTime? CAPCertificateIssuanceDate { get; set; }

        [JsonProperty("ClassReportNumber")]
        public int? ClassReportNumber { get; set; }

        [JsonProperty("ClassReportDate")]
        public DateTime? ClassReportDate { get; set; }

        [JsonProperty("StructuralReportNumber")]
        public int? StructuralReportNumber { get; set; }

        [JsonProperty("StructuralReportDate")]
        public DateTime? StructuralReportDate { get; set; }

        [JsonProperty("FinalGrade")]
        public int? FinalGrade { get; set; }

        [JsonProperty("StructuralGrade")]
        public int? StructuralGrade { get; set; }

        [JsonProperty("FatigueGrade")]
        public int? FatigueGrade { get; set; }

        [JsonProperty("RenewalGrade")]
        public int? RenewalGrade { get; set; }

        [JsonProperty("MaterialGrade")]
        public int? MaterialGrade { get; set; }

        [JsonProperty("GaugingGrade")]
        public int? GaugingGrade { get; set; }

        [JsonProperty("HullGirderStrength")]
        public int? HullGirderStrength { get; set; }

        [JsonProperty("CAP1CertificateIssuanceDate")]
        public DateTime? CAP1CertificateIssuanceDate { get; set; }

        [JsonProperty("CAP1FinalGrade")]
        public string? CAP1FinalGrade { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
