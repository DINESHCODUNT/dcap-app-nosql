using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class ManagedUserResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public bool IsEnabled { get; set; }
        public string Role { get; set; }
    }

}
