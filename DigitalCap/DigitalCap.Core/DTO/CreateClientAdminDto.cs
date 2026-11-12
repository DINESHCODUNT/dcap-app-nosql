using DigitalCap.Core.Models;
using DigitalCap.Core.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class CreateClientAdminDto
    {
        public Guid ClientId { get; set; }
        public string Company { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string TimeZone { get; set; } = string.Empty;
        public List<string> UpdatedRoles { get; set; }
        public UserAccountModel UserAccount { get; set; }
        public ApplicationDto AspNetUser { get; set; }

        public string UpdatedRole { get; set; }
        
    }
}
