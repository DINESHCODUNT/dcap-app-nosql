using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class UserPermissionsDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public Guid? Id { get; set; }

        [JsonProperty("PermissionName")]
        public string? PermissionName { get; set; }
    }
}
