using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class VesselTypeDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("VesselType")]
        public string? VesselType { get; set; }
    }
}
