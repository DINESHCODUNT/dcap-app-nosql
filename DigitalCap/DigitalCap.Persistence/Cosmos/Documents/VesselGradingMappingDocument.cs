using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class VesselGradingMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("VesselType")]
        public string? VesselType { get; set; }

        [JsonProperty("TankTypeId")]
        public int? TankTypeId { get; set; }

        [JsonProperty("GradingName")]
        public string? GradingName { get; set; }

        [JsonProperty("GradingGroupId")]
        public int? GradingGroupId { get; set; }

        [JsonProperty("GradingConditionGroupId")]
        public int? GradingConditionGroupId { get; set; }

        [JsonProperty("IsActive")]
        public bool? IsActive { get; set; }

        [JsonProperty("CreatedDttm")]
        public DateTime? CreatedDttm { get; set; }

        [JsonProperty("UpdateDttm")]
        public DateTime? UpdateDttm { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("RequiredInReport")]
        public bool? RequiredInReport { get; set; }
    }
}
