using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class TemplateSectionMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("TemplateId")]
        public int? TemplateId { get; set; }

        [JsonProperty("SectionId")]
        public string? SectionId { get; set; }
    }
}
