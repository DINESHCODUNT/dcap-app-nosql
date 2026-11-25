using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ImageCardDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("SectionId")]
        public string SectionId { get; set; } = string.Empty;

        [JsonProperty("FileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonProperty("CurrentCondition")]
        public bool CurrentCondition { get; set; }
    }
}
