using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ProjectTankSectionMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("TankSectionMappingId")]
        public string? TankSectionMappingId { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("PartId")]
        public int? PartId { get; set; }

        [JsonProperty("TankTypeId")]
        public int? TankTypeId { get; set; }

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

        [JsonProperty("TotalCards")]
        public int? TotalCards { get; set; }

        [JsonProperty("PlaceholderCount")]
        public int? PlaceholderCount { get; set; }

        [JsonProperty("FileNameCount")]
        public int? FileNameCount { get; set; }
    }
}
