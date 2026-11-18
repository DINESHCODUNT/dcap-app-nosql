using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ImageDescriptionDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
