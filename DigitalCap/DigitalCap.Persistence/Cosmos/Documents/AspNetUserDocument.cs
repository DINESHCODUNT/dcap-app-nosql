using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class AspNetUserDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public string? Id { get; set; }

        [JsonProperty("UserName")]
        public string? UserName { get; set; }

        [JsonProperty("Email")]
        public string? Email { get; set; }
    }
}
