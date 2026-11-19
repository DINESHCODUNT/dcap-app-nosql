using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class VesselTypePartMappingDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("VesselTypePartMappingId")]
        public int VesselTypePartMappingId { get; set; }

        [JsonProperty("VesselTypeId")]
        public int? VesselTypeId { get; set; }

        [JsonProperty("PartName")]
        public string? PartName { get; set; }

        [JsonProperty("SequenceNo")]
        public int? SequenceNo { get; set; }

        [JsonProperty("RequiredInReport")]
        public bool? RequiredInReport { get; set; }

        [JsonProperty("CreatedBy")]
        public string? CreatedBy { get; set; }

        [JsonProperty("CreatedOn")]
        public DateTime? CreatedOn { get; set; }

        [JsonProperty("ModifiedBy")]
        public string? ModifiedBy { get; set; }

        [JsonProperty("ModifiedOn")]
        public DateTime? ModifiedOn { get; set; }

        [JsonProperty("IsActive")]
        public bool? IsActive { get; set; }

        [JsonProperty("IsDeleted")]
        public bool? IsDeleted { get; set; }
    }
}
