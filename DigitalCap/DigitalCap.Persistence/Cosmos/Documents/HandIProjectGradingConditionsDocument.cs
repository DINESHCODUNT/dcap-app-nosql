using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class HandIProjectGradingConditionsDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("GradingID")]
        public int? GradingID { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("ConditionLabel")]
        public string? ConditionLabel { get; set; }

        [JsonProperty("Value")]
        public bool? Value { get; set; }

        [JsonProperty("IsActive")]
        public bool? IsActive { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }

        [JsonProperty("CreatedBY")]
        public string? CreatedBY { get; set; }

        [JsonProperty("CreatedOn")]
        public DateTime? CreatedOn { get; set; }

        [JsonProperty("ModifiedBY")]
        public string? ModifiedBY { get; set; }

        [JsonProperty("ModifiedOn")]
        public DateTime? ModifiedOn { get; set; }
    }
}
