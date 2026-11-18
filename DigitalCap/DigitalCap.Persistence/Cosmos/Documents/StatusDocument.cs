using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class StatusDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("ID")]
        public int ID { get; set; }

        [JsonProperty("Status")]
        public string? Status { get; set; }

        [JsonProperty("Type")]
        public string? Type { get; set; }
    }
}
