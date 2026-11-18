using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ProjectTaskStatusDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("ID")]
        public int ID { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("TaskId")]
        public int? TaskId { get; set; }

        [JsonProperty("StatusId")]
        public int? StatusId { get; set; }

        [JsonProperty("Notes")]
        public string? Notes { get; set; }

        [JsonProperty("StatusDate")]
        public DateTime? StatusDate { get; set; }

        [JsonProperty("Value")]
        public decimal? Value { get; set; }

        [JsonProperty("LastModifiedDate")]
        public DateTime? LastModifiedDate { get; set; }

        [JsonProperty("LastModifiedBy")]
        public string? LastModifiedBy { get; set; }

        [JsonProperty("DueDate")]
        public DateTime? DueDate { get; set; }

        [JsonProperty("AssignedTo")]
        public string? AssignedTo { get; set; }

        [JsonProperty("AssignedDate")]
        public DateTime? AssignedDate { get; set; }

        [JsonProperty("SurveyDays")]
        public int? SurveyDays { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
