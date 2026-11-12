using DigitalCap.Core.DTO;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
namespace DigitalCap.Core.DTO
{
    public class ClientProfileDTO
    {
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }
        public uint ProjectCount { get; set; }
        public ClientProfileDTO() { }
        public ClientProfileDTO(Client client, Dictionary<Guid, uint> projectCounts)
        {
            ClientId = client.Id;
            ClientName = client.CompanyName;
            ProjectCount = projectCounts.TryGetValue(client.Id, out var count)
                ? count
                : 0;
        }
        public class ClientSummaryDto
        {
            public Guid Id { get; set; }

            public string CompanyName { get; set; }

            public string? Wcn { get; set; }
            public string CompanyAddress { get; set; }
            public string? CompanyBillingEmail { get; set; }
            public string? CompanyBillingSystemUrl { get; set; }

            public int ProjectCount { get; set; }

            public int AdminCount { get; set; }

            public bool IsActive { get; set; }
            public string Email { get; set; } = string.Empty;
        }

        public class ClientProfileResultDto
        {
            public Guid ClientId { get; set; }
            public bool IsDuplicateWcn { get; set; }
        }
        
    }
}



    



