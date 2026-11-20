using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class VesselDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Vessel_ID")]
        public int Vessel_ID { get; set; }

        [JsonProperty("Source")]
        public string? Source { get; set; }

        [JsonProperty("Beam")]
        public double? Beam { get; set; }

        [JsonProperty("Breadth")]
        public double? Breadth { get; set; }

        [JsonProperty("ContractDate")]
        public DateTime? ContractDate { get; set; }

        [JsonProperty("Depth")]
        public double? Depth { get; set; }

        [JsonProperty("Draft")]
        public double? Draft { get; set; }

        [JsonProperty("HullNum")]
        public string? HullNum { get; set; }

        [JsonProperty("IMONum")]
        public string? IMONum { get; set; }

        [JsonProperty("Length")]
        public double? Length { get; set; }

        [JsonProperty("MarketSegment")]
        public string? MarketSegment { get; set; }

        [JsonProperty("VesselName")]
        public string? VesselName { get; set; }

        [JsonProperty("MarketSubSegment")]
        public string? MarketSubSegment { get; set; }

        [JsonProperty("CallSign")]
        public string? CallSign { get; set; }

        [JsonProperty("NetTonnage")]
        public double? NetTonnage { get; set; }

        [JsonProperty("ClassNotation")]
        public string? ClassNotation { get; set; }

        [JsonProperty("ShipTypeGrouping")]
        public string? ShipTypeGrouping { get; set; }

        [JsonProperty("Statcode5")]
        public string? Statcode5 { get; set; }

        [JsonProperty("Liquid")]
        public int? Liquid { get; set; }

        [JsonProperty("RegisteredOwner")]
        public string? RegisteredOwner { get; set; }

        [JsonProperty("ProjectName")]
        public string? ProjectName { get; set; }

        [JsonProperty("Manager")]
        public string? Manager { get; set; }

        [JsonProperty("Operator")]
        public string? Operator { get; set; }

        [JsonProperty("Technical_Manager")]
        public string? Technical_Manager { get; set; }

        [JsonProperty("Registered_Owner_ID")]
        public int? Registered_Owner_ID { get; set; }

        [JsonProperty("Manager_ID")]
        public int? Manager_ID { get; set; }

        [JsonProperty("Operator_ID")]
        public int? Operator_ID { get; set; }

        [JsonProperty("Technical_Manager_ID")]
        public int? Technical_Manager_ID { get; set; }

        [JsonProperty("BuildDate")]
        public DateTime? BuildDate { get; set; }

        [JsonProperty("Builder")]
        public string? Builder { get; set; }

        [JsonProperty("BuilderName")]
        public string? BuilderName { get; set; }

        [JsonProperty("BuilderNum")]
        public int? BuilderNum { get; set; }

        [JsonProperty("ClassNum")]
        public string? ClassNum { get; set; }

        [JsonProperty("ClassedDate")]
        public DateTime? ClassedDate { get; set; }

        [JsonProperty("Country")]
        public string? Country { get; set; }

        [JsonProperty("DeliveryDate")]
        public DateTime? DeliveryDate { get; set; }

        [JsonProperty("FlagName")]
        public string? FlagName { get; set; }

        [JsonProperty("KeelLayingDate")]
        public DateTime? KeelLayingDate { get; set; }

        [JsonProperty("LaunchingDate")]
        public DateTime? LaunchingDate { get; set; }

        [JsonProperty("OwnerCountry")]
        public string? OwnerCountry { get; set; }

        [JsonProperty("OwnerID")]
        public int? OwnerID { get; set; }

        [JsonProperty("OwnerName")]
        public string? OwnerName { get; set; }

        [JsonProperty("VesselDescription")]
        public string? VesselDescription { get; set; }

        [JsonProperty("CommercialCategory")]
        public string? CommercialCategory { get; set; }

        [JsonProperty("ClassedBy")]
        public string? ClassedBy { get; set; }

        [JsonProperty("ClassedBy2")]
        public string? ClassedBy2 { get; set; }

        [JsonProperty("SubStatus")]
        public string? SubStatus { get; set; }

        [JsonProperty("DWT")]
        public double? DWT { get; set; }

        [JsonProperty("GT")]
        public double? GT { get; set; }

        [JsonProperty("VesselType")]
        public string? VesselType { get; set; }
    }
}
