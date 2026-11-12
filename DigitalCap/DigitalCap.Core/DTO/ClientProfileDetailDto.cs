using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DigitalCap.Core.DTO
{
    public class ClientProfileDetailDto
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

        [JsonPropertyName("projects")]
        public List<ProjectSummaryDto> Projects { get; set; } = new List<ProjectSummaryDto>();

        [JsonPropertyName("clientAdmins")]
        public List<ClientAdminDto> ClientAdmins { get; set; } = new();

        [JsonPropertyName("freshmeat")]
        public bool Freshmeat { get; set; }
    }
}