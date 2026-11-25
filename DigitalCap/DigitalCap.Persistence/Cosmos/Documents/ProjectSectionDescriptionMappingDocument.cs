using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ProjectSectionDescriptionMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("SectionId")]
        public string? SectionId { get; set; }

        [JsonProperty("CategoryId")]
        public int CategoryId { get; set; }

        [JsonProperty("IsActive")]
        public bool? IsActive { get; set; }

        [JsonProperty("CreatedBy")]
        public string? CreatedBy { get; set; }

        [JsonProperty("CreatedOn")]
        public DateTime? CreatedOn { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }

        [JsonProperty("ModifiedOn")]
        public DateTime? ModifiedOn { get; set; }

        [JsonProperty("ModifiedBy")]
        public string? ModifiedBy { get; set; }
    }
}
