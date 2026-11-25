using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ProjectTemplateMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public string? Id { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("TemplateId")]
        public int? TemplateId { get; set; }

        [JsonProperty("SectionId")]
        public string? SectionId { get; set; }

        [JsonProperty("Notes")]
        public string? Notes { get; set; }

        [JsonProperty("CreatedDttm")]
        public DateTime? CreatedDttm { get; set; }

        [JsonProperty("UpdatedDttm")]
        public DateTime? UpdatedDttm { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
