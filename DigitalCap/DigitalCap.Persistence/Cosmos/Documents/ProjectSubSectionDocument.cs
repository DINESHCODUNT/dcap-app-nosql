using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ProjectSubSectionDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public string? Id { get; set; }

        [JsonProperty("ProjectSectionId")]
        public string? ProjectSectionId { get; set; }

        [JsonProperty("SectionName")]
        public string? SectionName { get; set; }

        [JsonProperty("OrderNumber")]
        public int? OrderNumber { get; set; }

        [JsonProperty("Totalcards")]
        public int? Totalcards { get; set; }

        [JsonProperty("PlaceholderCount")]
        public int? PlaceholderCount { get; set; }

        [JsonProperty("FileNameCount")]
        public int? FileNameCount { get; set; }

        [JsonProperty("CreatedBy")]
        public string? CreatedBy { get; set; }

        [JsonProperty("CreatedOn")]
        public DateTime? CreatedOn { get; set; }

        [JsonProperty("ModifiedBy")]
        public string? ModifiedBy { get; set; }

        [JsonProperty("ModifiedOn")]
        public DateTime? ModifiedOn { get; set; }

        [JsonProperty("IsActive")]
        public bool? IsActive { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
