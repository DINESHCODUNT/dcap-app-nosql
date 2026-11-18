using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    public class ClientsDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public Guid Id { get; set; }

        [JsonProperty("Wcn")]
        public string? Wcn { get; set; }

        [JsonProperty("CompanyName")]
        public string? CompanyName { get; set; }

        [JsonProperty("CompanyAddress")]
        public string? CompanyAddress { get; set; }

        [JsonProperty("CompanyBillingEmail")]
        public string? CompanyBillingEmail { get; set; }

        [JsonProperty("CompanyBillingSystemUrl")]
        public string? CompanyBillingSystemUrl { get; set; }
    }
}
