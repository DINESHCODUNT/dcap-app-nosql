using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class UserProfileResponse
    {
        public required UserAccountModel Account { get; set; }
    }
   
}
