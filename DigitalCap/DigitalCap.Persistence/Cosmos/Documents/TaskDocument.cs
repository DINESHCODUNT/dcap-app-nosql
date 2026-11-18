using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class TaskDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("Category")]
        public string? Category { get; set; }

        [JsonProperty("Type")]
        public string? Type { get; set; }

        [JsonProperty("Cap2Name")]
        public string? Cap2Name { get; set; }

        [JsonProperty("Cap2Type")]
        public string? Cap2Type { get; set; }

        [JsonProperty("IsMilestone")]
        public bool? IsMilestone { get; set; }

        [JsonProperty("PercentageComplete")]
        public byte? PercentageComplete { get; set; }

        [JsonProperty("InitialStatus")]
        public int? InitialStatus { get; set; }
    }
}
