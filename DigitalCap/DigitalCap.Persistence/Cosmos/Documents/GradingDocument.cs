using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class GradingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("LabelName")]
        public string? LabelName { get; set; }

        [JsonProperty("GradingGroupId")]
        public int? GradingGroupId { get; set; }

        [JsonProperty("GradingConditionGroupId")]
        public int? GradingConditionGroupId { get; set; }

        [JsonProperty("IsActive")]
        public bool? IsActive { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }

        [JsonProperty("VesselType")]
        public string? VesselType { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("RequiredInReport")]
        public bool? RequiredInReport { get; set; }
    }
}
