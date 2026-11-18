using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ActivityDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("ID")]
        public int ID { get; set; }

        [JsonProperty("Text")]
        public string? Text { get; set; }

        [JsonProperty("CreateBy")]
        public string? CreateBy { get; set; }

        [JsonProperty("CreateDttm")]
        public DateTime? CreateDttm { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("LinkText")]
        public string? LinkText { get; set; }

        [JsonProperty("LinkHref")]
        public string? LinkHref { get; set; }

        [JsonProperty("Icon")]
        public string? Icon { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }

        [JsonProperty("IsRead")]
        public bool? IsRead { get; set; }

        [JsonProperty("ReadBy")]
        public string? ReadBy { get; set; }

        [JsonProperty("ReadDttm")]
        public DateTime? ReadDttm { get; set; }
    }
}
