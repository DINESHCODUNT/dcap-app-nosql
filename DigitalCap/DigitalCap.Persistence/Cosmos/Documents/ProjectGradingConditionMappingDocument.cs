using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ProjectGradingConditionMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("TemplateId")]
        public int? TemplateId { get; set; }

        [JsonProperty("SectionId")]
        public string? SectionId { get; set; }

        [JsonProperty("GradingId")]
        public int? GradingId { get; set; }

        [JsonProperty("GradingConditionLabel")]
        public string? GradingConditionLabel { get; set; }

        [JsonProperty("GradingConditionValue")]
        public bool? GradingConditionValue { get; set; }

        [JsonProperty("CreatedDttm")]
        public DateTime? CreatedDttm { get; set; }

        [JsonProperty("UpdatedDttm")]
        public DateTime? UpdatedDttm { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
