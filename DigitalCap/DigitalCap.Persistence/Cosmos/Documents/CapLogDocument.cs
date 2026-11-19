using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class CapLogDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("AbsProjectId")]
        public string? AbsProjectId { get; set; }

        [JsonProperty("AppId")]
        public string AppId { get; set; } = string.Empty;

        [JsonProperty("AppName")]
        public string? AppName { get; set; }

        [JsonProperty("IntegrationId")]
        public string IntegrationId { get; set; } = string.Empty;

        [JsonProperty("AssignmentId")]
        public string AssignmentId { get; set; } = string.Empty;

        [JsonProperty("SequenceId")]
        public string SequenceId { get; set; } = string.Empty;

        [JsonProperty("CardId")]
        public int CardId { get; set; }

        [JsonProperty("ObjLabel")]
        public string? ObjLabel { get; set; }

        [JsonProperty("ObjValue")]
        public string? ObjValue { get; set; }

        [JsonProperty("UserEmail")]
        public string? UserEmail { get; set; }

        [JsonProperty("AdditionalData")]
        public string? AdditionalData { get; set; }

        [JsonProperty("Level0")]
        public string? Level0 { get; set; }

        [JsonProperty("Level1")]
        public string? Level1 { get; set; }

        [JsonProperty("Level2")]
        public string? Level2 { get; set; }

        [JsonProperty("Level3")]
        public string? Level3 { get; set; }

        [JsonProperty("Level4")]
        public string? Level4 { get; set; }

        [JsonProperty("Level5")]
        public string? Level5 { get; set; }
    }
}
