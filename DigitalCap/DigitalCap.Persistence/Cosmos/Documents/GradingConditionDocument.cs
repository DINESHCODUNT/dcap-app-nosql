using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class GradingConditionDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Condition")]
        public string Condition { get; set; } = string.Empty;

        [JsonProperty("GradingConditionGroupId")]
        public int GradingConditionGroupId { get; set; }
    }
}
