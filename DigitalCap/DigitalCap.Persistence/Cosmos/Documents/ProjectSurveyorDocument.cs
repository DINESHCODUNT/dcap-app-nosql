using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ProjectSurveyorDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("LeadSurveyor")]
        public string? LeadSurveyor { get; set; }

        [JsonProperty("SecondSurveyor")]
        public string? SecondSurveyor { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
