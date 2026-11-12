using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class AssignUserRolesAndPermissions
    {
        public Guid? UserId { get; set; }
        public string[] Roles { get; set; }
        public string[] Permissions { get; set; }
    }
}
