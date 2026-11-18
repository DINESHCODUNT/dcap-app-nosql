using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class IsSynchedOnlineDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("IsSynched")]
        public bool? IsSynched { get; set; }

        [JsonProperty("IsReadOnly")]
        public bool? IsReadOnly { get; set; }

        [JsonProperty("UpdateDttm")]
        public DateTime? UpdateDttm { get; set; }
    }
}
