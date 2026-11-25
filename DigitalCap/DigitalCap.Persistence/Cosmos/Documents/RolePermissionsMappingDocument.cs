using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class RolePermissionsMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public Guid? Id { get; set; }

        [JsonProperty("RoleId")]
        public Guid? RoleId { get; set; }

        [JsonProperty("PermissionId")]
        public Guid? PermissionId { get; set; }

        [JsonProperty("IsReadable")]
        public bool? IsReadable { get; set; }

        [JsonProperty("IsEditable")]
        public bool? IsEditable { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }

        [JsonProperty("ReadExist")]
        public bool? ReadExist { get; set; }

        [JsonProperty("EditExist")]
        public bool? EditExist { get; set; }

        [JsonProperty("DeleteExist")]
        public bool? DeleteExist { get; set; }
    }
}
