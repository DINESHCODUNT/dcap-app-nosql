using Newtonsoft.Json;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ReportVesselMainDataDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("projectId")]
        public int projectId { get; set; }

        [JsonProperty("reportNo")]
        public string? reportNo { get; set; }

        [JsonProperty("portDate")]
        public string? portDate { get; set; }

        [JsonProperty("actualReportStartDate")]
        public string? actualReportStartDate { get; set; }

        [JsonProperty("vesselName")]
        public string? vesselName { get; set; }

        [JsonProperty("vesselID")]
        public int? vesselID { get; set; }

        [JsonProperty("imoNumber")]
        public int? imoNumber { get; set; }

        [JsonProperty("vesselType")]
        public string? vesselType { get; set; }

        [JsonProperty("yearBuilt")]
        public string? yearBuilt { get; set; }

        [JsonProperty("builtBy")]
        public string? builtBy { get; set; }

        [JsonProperty("hullNo")]
        public string? hullNo { get; set; }

        [JsonProperty("flagName")]
        public string? flagName { get; set; }

        [JsonProperty("homeport")]
        public string? homeport { get; set; }

        [JsonProperty("officalNumber")]
        public string? officalNumber { get; set; }

        [JsonProperty("callSign")]
        public string? callSign { get; set; }

        [JsonProperty("previousName")]
        public string? previousName { get; set; }

        [JsonProperty("ownerName")]
        public string? ownerName { get; set; }

        [JsonProperty("manager")]
        public string? manager { get; set; }

        [JsonProperty("length")]
        public string? length { get; set; }

        [JsonProperty("breadth")]
        public string? breadth { get; set; }

        [JsonProperty("depth")]
        public string? depth { get; set; }

        [JsonProperty("draft")]
        public string? draft { get; set; }

        [JsonProperty("deadweight")]
        public string? deadweight { get; set; }

        [JsonProperty("grossTons")]
        public string? grossTons { get; set; }

        [JsonProperty("netTons")]
        public string? netTons { get; set; }

        [JsonProperty("propulsionType")]
        public string? propulsionType { get; set; }

        [JsonProperty("kw")]
        public string? kw { get; set; }

        [JsonProperty("shaftRPM")]
        public string? shaftRPM { get; set; }

        [JsonProperty("propulsionManufacturer")]
        public string? propulsionManufacturer { get; set; }

        [JsonProperty("gearManufacturer")]
        public string? gearManufacturer { get; set; }

        [JsonProperty("classed")]
        public string? classed { get; set; }

        [JsonProperty("classNo")]
        public string? classNo { get; set; }

        [JsonProperty("classNotation")]
        public string? classNotation { get; set; }

        [JsonProperty("machinery")]
        public string? machinery { get; set; }

        [JsonProperty("other")]
        public string? other { get; set; }

        [JsonProperty("environment")]
        public string? environment { get; set; }

        [JsonProperty("firstVisitDate")]
        public string? firstVisitDate { get; set; }

        [JsonProperty("lastVisitDate")]
        public string? lastVisitDate { get; set; }

        [JsonProperty("SelectedPrefix")]
        public int? SelectedPrefix { get; set; }

        [JsonProperty("reportPort")]
        public string? reportPort { get; set; }
    }
}
