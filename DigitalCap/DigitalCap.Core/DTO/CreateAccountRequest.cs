using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class CreateAccountRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public ApplicationDto AspNetUser { get; set; }
        public UserAccountModel UserAccount { get; set; }
    }

}
