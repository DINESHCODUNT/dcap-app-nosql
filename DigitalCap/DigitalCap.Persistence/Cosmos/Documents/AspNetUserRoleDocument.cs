using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class AspNetUserRoleDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("UserId")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty("RoleId")]
        public string RoleId { get; set; } = string.Empty;
    }
}
