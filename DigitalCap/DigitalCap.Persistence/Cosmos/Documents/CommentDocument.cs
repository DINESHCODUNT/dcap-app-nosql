using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class CommentDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("ProjectId")]
        public int ProjectId { get; set; }

        [JsonProperty("ProjectTaskId")]
        public int? ProjectTaskId { get; set; }

        [JsonProperty("CreatedBy")]
        public string? CreatedBy { get; set; }

        [JsonProperty("Comment")]
        public string? Comment { get; set; }

        [JsonProperty("LastModifiedDate")]
        public DateTimeOffset? LastModifiedDate { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
