using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class PortOfCallDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("PortId")]
        public int PortId { get; set; }

        [JsonProperty("PortName")]
        public string? PortName { get; set; }

        [JsonProperty("PortCode")]
        public string? PortCode { get; set; }

        [JsonProperty("IsEUPort")]
        public bool? IsEUPort { get; set; }

        [JsonProperty("CountryCode")]
        public string? CountryCode { get; set; }

        [JsonProperty("AdditionalData")]
        public string? AdditionalData { get; set; }

        [JsonProperty("UpdateUser")]
        public string? UpdateUser { get; set; }

        [JsonProperty("UpdateDttm")]
        public DateTime? UpdateDttm { get; set; }

        [JsonProperty("CreateUser")]
        public string? CreateUser { get; set; }

        [JsonProperty("CreateDttm")]
        public DateTime? CreateDttm { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
