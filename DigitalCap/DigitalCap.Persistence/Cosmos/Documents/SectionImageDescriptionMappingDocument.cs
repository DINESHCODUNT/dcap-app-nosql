using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class SectionImageDescriptionMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("SectionId")]
        public string? SectionId { get; set; }

        [JsonProperty("CategoryId")]
        public long? CategoryId { get; set; }
    }
}
