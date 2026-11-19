using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ReportTemplateSectionDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public string? Id { get; set; }

        [JsonProperty("SectionName")]
        public string? SectionName { get; set; }

        [JsonProperty("HasSubSection")]
        public bool? HasSubSection { get; set; }

        [JsonProperty("SpecialSectionName")]
        public string? SpecialSectionName { get; set; }
    }
}
