using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class AspNetRoleDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public string? Id { get; set; }

        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("NormalizedName")]
        public string? NormalizedName { get; set; }
    }
}
