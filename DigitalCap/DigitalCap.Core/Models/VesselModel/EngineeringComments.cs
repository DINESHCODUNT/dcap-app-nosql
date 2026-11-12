using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.VesselModel
{
    public class EngineeringComments : CachedData
    {
        [JsonProperty("projectType")]
        public string ProjectType { get; set; }

        [JsonProperty("projectNumber")]
        public string ProjectNumber { get; set; }

        [JsonProperty("commentNumber")]
        public string CommentNumber { get; set; }

        [JsonProperty("commentStatus")]
        public string CommentStatus { get; set; }

        [JsonProperty("commentType")]
        public string CommentType { get; set; }

        [JsonProperty("engineeringOffice")]
        public string EngineeringOffice { get; set; }

        [JsonProperty("reviewActivity")]
        public string ReviewActivity { get; set; }

        [JsonProperty("assetNum")]
        public string AssetNum { get; set; }

        [JsonProperty("raLabel")]
        public string RaLabel { get; set; }

        [JsonProperty("submitterName")]
        public string SubmitterName { get; set; }

        [JsonProperty("issueDate")]
        public string IssueDate { get; set; }
    }
}
