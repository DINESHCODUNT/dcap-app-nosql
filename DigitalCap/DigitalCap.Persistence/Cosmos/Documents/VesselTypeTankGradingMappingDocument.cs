using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class VesselTypeTankGradingMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("TankTypeId")]
        public int? TankTypeId { get; set; }

        [JsonProperty("CategoryId")]
        public int? CategoryId { get; set; }
    }
}
