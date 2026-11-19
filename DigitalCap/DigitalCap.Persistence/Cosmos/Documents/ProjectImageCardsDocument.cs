using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ProjectImageCardsDocument
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

        [JsonProperty("CardId")]
        public int? CardId { get; set; }

        [JsonProperty("DescriptionId")]
        public int? DescriptionId { get; set; }

        [JsonProperty("AdditionalDescription")]
        public string? AdditionalDescription { get; set; }

        [JsonProperty("CurrentCondition")]
        public int? CurrentCondition { get; set; }

        [JsonProperty("ImageId")]
        public int? ImageId { get; set; }

        [JsonProperty("CreatedDttm")]
        public DateTime? CreatedDttm { get; set; }

        [JsonProperty("UpdateDttm")]
        public DateTime? UpdateDttm { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
