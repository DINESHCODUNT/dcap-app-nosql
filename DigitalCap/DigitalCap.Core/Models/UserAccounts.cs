using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class UserAccounts : AuditableEntity<Guid>
    {
        public string AspNetUserId { get; set; }
        public Guid ClientId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public bool IsAgreeToPrivacy { get; set; } = false;
        public bool Deleted { get; set; } = false;
        public string hm3d_userid { get; set; }
        public string Phone { get; set; }
        public string TimeZone { get; set; }
    }
}
