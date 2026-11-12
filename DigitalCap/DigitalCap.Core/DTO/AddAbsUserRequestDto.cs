using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DigitalCap.Core.DTO
{
    public class AddAbsUserRequestDto
    {
      
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }

        public string TimeZone { get; set; }
        public string Company { get; set; }
        public Guid? ClientId { get; set; }
        public string Role { get; set; }

        public List<string> UpdatedRoles { get; set; }
    }
}
