using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class ManageAbsUsersResponseDto
    {
        public IEnumerable<UserAccountModel> CapUsers { get; set; }
        public bool UserUpdated { get; set; }
        public bool UserCreated { get; set; }
        public string Company { get; set; }
        public bool IsClientUsers { get; set; }
        public Guid ClientId { get; set; }
    }

}
