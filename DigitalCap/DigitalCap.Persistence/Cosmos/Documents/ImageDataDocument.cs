using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ImageDataDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("ImageId")]
        public int? ImageId { get; set; }

        [JsonProperty("Base64String")]
        public string? Base64String { get; set; }

        [JsonProperty("CreateDttm")]
        public DateTime? CreateDttm { get; set; }

        [JsonProperty("UpdateDttm")]
        public DateTime? UpdateDttm { get; set; }
    }
}
