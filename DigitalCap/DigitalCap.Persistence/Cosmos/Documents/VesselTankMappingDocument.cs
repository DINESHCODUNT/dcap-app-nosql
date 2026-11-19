using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class VesselTankMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public string? Id { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("ImoNumber")]
        public string? ImoNumber { get; set; }

        [JsonProperty("TankName")]
        public string? TankName { get; set; }

        [JsonProperty("TankTypeId")]
        public int? TankTypeId { get; set; }

        [JsonProperty("VesselType")]
        public string? VesselType { get; set; }

        [JsonProperty("Subheader")]
        public string? Subheader { get; set; }

        [JsonProperty("RequiredInReport")]
        public bool? RequiredInReport { get; set; }

        [JsonProperty("IsActive")]
        public bool? IsActive { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }

        [JsonProperty("CreatedDttm")]
        public DateTime? CreatedDttm { get; set; }

        [JsonProperty("UpdateDttm")]
        public DateTime? UpdateDttm { get; set; }

        [JsonProperty("UpdatedBy")]
        public string? UpdatedBy { get; set; }
    }
}

