using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.VesselModel
{
    public class History
    {
        [JsonProperty("uid")]
        public ulong? Uid { get; set; }

        [JsonProperty("dateCreated")]
        public DateTime? DateCreated { get; set; }

        [JsonProperty("reportNumber")]
        public long ReportNumber { get; set; }

        [JsonProperty("reportLocation")]
        public string ReportLocation { get; set; }

        [JsonProperty("criticality")]
        public string Criticality { get; set; }

        [JsonProperty("findingType")]
        public string FindingType { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
