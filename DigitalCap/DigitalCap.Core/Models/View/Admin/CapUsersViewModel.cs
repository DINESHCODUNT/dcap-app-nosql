using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.View.Admin
{
    public class CapUsersViewModel
    {
        public IEnumerable<CapUser> CapUsers { get; set; }
        public string Company { get; set; }
        public bool UserUpdated { get; set; }
        public bool UserCreated { get; set; }
        public bool IsClientUsers { get; set; }
        public Guid ClientId { get; set; }
    }
}
