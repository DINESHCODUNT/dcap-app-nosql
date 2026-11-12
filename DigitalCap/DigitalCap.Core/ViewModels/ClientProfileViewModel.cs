using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DigitalCap.Core.ViewModels
{
    public class ClientProfileViewModel
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("wcn")]
        public string Wcn { get; set; }

        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; }

        [JsonPropertyName("companyAddress")]
        public string CompanyAddress { get; set; }

        [JsonPropertyName("companyBillingEmail")]
        public string? CompanyBillingEmail { get; set; }

        [JsonPropertyName("companyBillingSystemUrl")]
        public string? CompanyBillingSystemUrl { get; set; }

        [JsonPropertyName("projectCount")]
        public int? ProjectCount { get; set; }


    }
}
