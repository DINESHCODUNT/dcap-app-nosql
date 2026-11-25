using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class VesselTypeNormalSectionMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("SectionMappingId")]
        public string? SectionMappingId { get; set; }

        [JsonProperty("VesselTypeId")]
        public int? VesselTypeId { get; set; }

        [JsonProperty("VesselTypePartMappingId")]
        public int? VesselTypePartMappingId { get; set; }

        [JsonProperty("SectionName")]
        public string? SectionName { get; set; }

        [JsonProperty("SubHeader")]
        public string? SubHeader { get; set; }

        [JsonProperty("RequiredInReport")]
        public bool? RequiredInReport { get; set; }

        [JsonProperty("Order")]
        public int? Order { get; set; }

        [JsonProperty("HasSubSection")]
        public bool? HasSubSection { get; set; }

        [JsonProperty("TotalCards")]
        public int? TotalCards { get; set; }

        [JsonProperty("PlaceholderCount")]
        public int? PlaceholderCount { get; set; }

        [JsonProperty("FileNameCount")]
        public int? FileNameCount { get; set; }

        [JsonProperty("CreatedBy")]
        public string? CreatedBy { get; set; }

        [JsonProperty("CreatedOn")]
        public DateTime? CreatedOn { get; set; }

        [JsonProperty("ModifiedBy")]
        public string? ModifiedBy { get; set; }

        [JsonProperty("ModifiedOn")]
        public DateTime? ModifiedOn { get; set; }

        [JsonProperty("IsActive")]
        public bool? IsActive { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
