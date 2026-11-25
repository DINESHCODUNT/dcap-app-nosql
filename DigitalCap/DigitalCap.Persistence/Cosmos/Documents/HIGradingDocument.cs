using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class HIGradingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("LabelName")]
        public string? LabelName { get; set; }

        [JsonProperty("GradingConditionGroupId")]
        public int? GradingConditionGroupId { get; set; }

        [JsonProperty("SpecialGasGrading")]
        public bool? SpecialGasGrading { get; set; }
    }
}
