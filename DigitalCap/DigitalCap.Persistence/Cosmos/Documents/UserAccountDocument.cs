using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class UserAccountDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public Guid Id { get; set; }

        [JsonProperty("AspNetUserId")]
        public string? AspNetUserId { get; set; }

        [JsonProperty("ClientId")]
        public Guid ClientId { get; set; }

        [JsonProperty("FirstName")]
        public string? FirstName { get; set; }

        [JsonProperty("LastName")]
        public string? LastName { get; set; }

        [JsonProperty("Company")]
        public string? Company { get; set; }

        [JsonProperty("IsAgreeToPrivacy")]
        public bool? IsAgreeToPrivacy { get; set; }

        [JsonProperty("CreatedAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("CreatedBy")]
        public string? CreatedBy { get; set; }

        [JsonProperty("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("UpdatedBy")]
        public string? UpdatedBy { get; set; }

        [JsonProperty("Deleted")]
        public bool? Deleted { get; set; }

        [JsonProperty("hm3d_userid")]
        public string? hm3d_userid { get; set; }

        [JsonProperty("Phone")]
        public string? Phone { get; set; }

        [JsonProperty("TimeZone")]
        public string? TimeZone { get; set; }
    }
}
