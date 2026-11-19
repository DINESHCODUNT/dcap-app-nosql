using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class CurrentConditionDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("CurrentCondition")]
        public string? CurrentCondition { get; set; }
    }
}
