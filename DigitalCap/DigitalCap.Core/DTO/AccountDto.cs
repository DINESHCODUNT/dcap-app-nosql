using System;
using System.Text.Json.Serialization;

namespace DigitalCap.Core.DTO
{
    public class AccountDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; }

        [JsonPropertyName("company")]
        public string Company { get; set; }

        [JsonPropertyName("displayRole")]
        public string DisplayRole { get; set; }

        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; }

        [JsonPropertyName("clientId")]
        public Guid? ClientId { get; set; }
    }
}