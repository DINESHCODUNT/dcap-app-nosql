using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ReportSurveyDetailsDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("projectId")]
        public int projectId { get; set; }

        [JsonProperty("imoNum")]
        public DateTime? imoNum { get; set; }

        [JsonProperty("classSurveyDate")]
        public DateTime? classSurveyDate { get; set; }

        [JsonProperty("dryDockDate")]
        public DateTime? dryDockDate { get; set; }

        [JsonProperty("specialContinuousMachineryDate")]
        public DateTime? specialContinuousMachineryDate { get; set; }

        [JsonProperty("boilerDate")]
        public DateTime? boilerDate { get; set; }

        [JsonProperty("tailshaftDate")]
        public DateTime? tailshaftDate { get; set; }

        [JsonProperty("specialContinuousHull1")]
        public DateTime? specialContinuousHull1 { get; set; }

        [JsonProperty("specialContinuousHull2")]
        public DateTime? specialContinuousHull2 { get; set; }

        [JsonProperty("specialContinuousHull3")]
        public DateTime? specialContinuousHull3 { get; set; }

        [JsonProperty("specialContinuousHull4")]
        public DateTime? specialContinuousHull4 { get; set; }

        [JsonProperty("specialContinuousHull5")]
        public DateTime? specialContinuousHull5 { get; set; }

        [JsonProperty("specialContinuousHull6")]
        public DateTime? specialContinuousHull6 { get; set; }

        [JsonProperty("specialContinuousHull7")]
        public DateTime? specialContinuousHull7 { get; set; }

        [JsonProperty("specialContinuousHull8")]
        public DateTime? specialContinuousHull8 { get; set; }

        [JsonProperty("specialContinuousHull9")]
        public DateTime? specialContinuousHull9 { get; set; }

        [JsonProperty("specialContinuousHull10")]
        public DateTime? specialContinuousHull10 { get; set; }

        [JsonProperty("specialContinuousHull11")]
        public DateTime? specialContinuousHull11 { get; set; }

        [JsonProperty("specialContinuousHull12")]
        public DateTime? specialContinuousHull12 { get; set; }

        [JsonProperty("specialContinuousHull13")]
        public DateTime? specialContinuousHull13 { get; set; }

        [JsonProperty("specialContinuousHull14")]
        public DateTime? specialContinuousHull14 { get; set; }

        [JsonProperty("specialContinuousHull15")]
        public DateTime? specialContinuousHull15 { get; set; }

        [JsonProperty("specialHullNo")]
        public int? specialHullNo { get; set; }
    }
}
