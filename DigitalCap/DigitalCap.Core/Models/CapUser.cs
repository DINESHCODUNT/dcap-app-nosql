using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class CapUser
    {
        public Guid Id { get; set; }
        public string AspNetUserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; } = "";
        public string TimeZone { get; set; } = "";
        public string Company { get; set; }
        public string UserRole { get; set; }
    }
}




