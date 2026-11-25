using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ImagesDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("AssignmentId")]
        public string? AssignmentId { get; set; }

        [JsonProperty("FileId")]
        public string? FileId { get; set; }

        [JsonProperty("InsertDate")]
        public DateTime? InsertDate { get; set; }
    }
}
