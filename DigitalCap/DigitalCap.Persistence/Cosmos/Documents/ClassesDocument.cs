using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ClassesDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("ID")]
        public int ID { get; set; }

        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("Abbreviation")]
        public string? Abbreviation { get; set; }
    }
}
