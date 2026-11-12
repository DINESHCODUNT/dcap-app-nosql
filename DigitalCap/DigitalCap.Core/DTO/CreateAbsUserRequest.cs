using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class CreateAbsUserRequest
    {
        public string Email { get; set; }
        public string Company { get; set; }
        public string UpdatedRole { get; set; }
        public bool AbsUser { get; set; }
        public bool ClientUser { get; set; }
        public Guid? ClientId { get; set; }

        public UserAccountModel UserAccount { get; set; }
    }
}
