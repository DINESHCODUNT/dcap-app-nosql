using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class TaskStatusGroupDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Category")]
        public string? Category { get; set; }

        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("StartedTaskId")]
        public int StartedTaskId { get; set; }

        [JsonProperty("CompletedTaskId")]
        public int CompletedTaskId { get; set; }
    }
}
