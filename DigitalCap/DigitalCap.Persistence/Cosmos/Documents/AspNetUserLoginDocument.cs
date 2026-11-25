using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class AspNetUserLoginDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("LoginProvider")]
        public string LoginProvider { get; set; } = string.Empty;

        [JsonProperty("ProviderKey")]
        public string ProviderKey { get; set; } = string.Empty;

        [JsonProperty("ProviderDisplayName")]
        public string? ProviderDisplayName { get; set; }

        [JsonProperty("UserId")]
        public string UserId { get; set; } = string.Empty;
    }
}
