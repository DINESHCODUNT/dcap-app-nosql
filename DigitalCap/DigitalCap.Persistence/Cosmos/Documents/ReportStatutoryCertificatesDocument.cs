using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ReportStatutoryCertificatesDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("projectId")]
        public int projectId { get; set; }

        [JsonProperty("llliIssueDate")]
        public DateTime? llliIssueDate { get; set; }

        [JsonProperty("llliExpiryDate")]
        public DateTime? llliExpiryDate { get; set; }

        [JsonProperty("safetyConIssueDate")]
        public DateTime? safetyConIssueDate { get; set; }

        [JsonProperty("safetyConExpiryDate")]
        public DateTime? safetyConExpiryDate { get; set; }

        [JsonProperty("safetyRadioIssueDate")]
        public DateTime? safetyRadioIssueDate { get; set; }

        [JsonProperty("safetyRadioExpiryDate")]
        public DateTime? safetyRadioExpiryDate { get; set; }

        [JsonProperty("safetyEquipmentIssueDate")]
        public DateTime? safetyEquipmentIssueDate { get; set; }

        [JsonProperty("safetyEquipmentExpiryDate")]
        public DateTime? safetyEquipmentExpiryDate { get; set; }

        [JsonProperty("ioppIssueDate")]
        public DateTime? ioppIssueDate { get; set; }

        [JsonProperty("ioppExpiryDate")]
        public DateTime? ioppExpiryDate { get; set; }

        [JsonProperty("iapIssueDate")]
        public DateTime? iapIssueDate { get; set; }

        [JsonProperty("iapExpiryDate")]
        public DateTime? iapExpiryDate { get; set; }

        [JsonProperty("iafsIsuueDate")]
        public DateTime? iafsIsuueDate { get; set; }

        [JsonProperty("iafsExpiryDate")]
        public DateTime? iafsExpiryDate { get; set; }

        [JsonProperty("ismIssueDate")]
        public DateTime? ismIssueDate { get; set; }

        [JsonProperty("ismExpiryDate")]
        public DateTime? ismExpiryDate { get; set; }

        [JsonProperty("isExpiryDate")]
        public DateTime? isExpiryDate { get; set; }

        [JsonProperty("ispsIssueDate")]
        public DateTime? ispsIssueDate { get; set; }

        [JsonProperty("ispsExpiryDate")]
        public DateTime? ispsExpiryDate { get; set; }

        [JsonProperty("cargoGearReTestIssueDate")]
        public DateTime? cargoGearReTestIssueDate { get; set; }

        [JsonProperty("cargoGearReTestExpiryDate")]
        public DateTime? cargoGearReTestExpiryDate { get; set; }

        [JsonProperty("cargoGearAnnualIssueCertificate")]
        public DateTime? cargoGearAnnualIssueCertificate { get; set; }

        [JsonProperty("cargoGearAnnualExpiryCertificate")]
        public DateTime? cargoGearAnnualExpiryCertificate { get; set; }

        [JsonProperty("ibwmIssueDate")]
        public DateTime? ibwmIssueDate { get; set; }

        [JsonProperty("ibwmExpiryDate")]
        public DateTime? ibwmExpiryDate { get; set; }

        [JsonProperty("isppSewageIssueDate")]
        public DateTime? isppSewageIssueDate { get; set; }

        [JsonProperty("isppSewageExpiryDate")]
        public DateTime? isppSewageExpiryDate { get; set; }

        [JsonProperty("ieeIssueDate")]
        public DateTime? ieeIssueDate { get; set; }

        [JsonProperty("ieeExpiryDate")]
        public DateTime? ieeExpiryDate { get; set; }

        [JsonProperty("maritimeLabourIssueDate")]
        public DateTime? maritimeLabourIssueDate { get; set; }

        [JsonProperty("maritimeLabourExpiryDate")]
        public DateTime? maritimeLabourExpiryDate { get; set; }

        [JsonProperty("fitnessGasCertificateIssueDate")]
        public DateTime? fitnessGasCertificateIssueDate { get; set; }

        [JsonProperty("fitnessGasCertificateExpiryDate")]
        public DateTime? fitnessGasCertificateExpiryDate { get; set; }
    }
}
