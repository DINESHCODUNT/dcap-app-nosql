using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class UserModel
    {
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public required string UserEmail { get; set; }
        public bool TermsAccepted { get; set; }
        public DateTime? TermsAcceptedDttm { get; set; }
        public required string TermsOfUse { get; set; }
    }
    
}
