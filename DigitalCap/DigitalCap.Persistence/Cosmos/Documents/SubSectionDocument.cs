using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class SubSectionDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("SectionName")]
        public string? SectionName { get; set; }

        [JsonProperty("SectionId")]
        public string? SectionId { get; set; }
    }
}
