using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class GradingSectionMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("SectionId")]
        public string SectionId { get; set; } = string.Empty;

        [JsonProperty("GradingGroupId")]
        public int GradingGroupId { get; set; }
    }
}
