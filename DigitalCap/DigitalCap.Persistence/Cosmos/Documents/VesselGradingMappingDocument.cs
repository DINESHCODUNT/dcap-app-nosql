using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class VesselGradingMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }

        [JsonProperty("UpdateDttm")]
        public DateTime? UpdateDttm { get; set; }
    }
}
