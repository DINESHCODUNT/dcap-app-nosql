using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class FileDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Projectid")]
        public int? Projectid { get; set; }

        [JsonIgnore]
        public int? ProjectId
        {
            get => Projectid;
            set => Projectid = value;
        }

        [JsonProperty("TaskId")]
        public int? TaskId { get; set; }

        [JsonProperty("DataType")]
        public int? DataType { get; set; }

        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("Extension")]
        public string? Extension { get; set; }

        [JsonProperty("MimeType")]
        public string? MimeType { get; set; }

        [JsonProperty("StorageKey")]
        public string? StorageKey { get; set; }

        [JsonProperty("UploadedBy")]
        public string? UploadedBy { get; set; }

        [JsonProperty("LastModifiedBy")]
        public string? LastModifiedBy { get; set; }

        [JsonProperty("LastModifiedDate")]
        public DateTime? LastModifiedDate { get; set; }

        [JsonProperty("AvailableToClient")]
        public bool? AvailableToClient { get; set; }

        [JsonProperty("SurveyImage")]
        public bool? SurveyImage { get; set; }
    }
}
