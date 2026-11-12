using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.ViewModels.AccountViewModels
{
    public class ResetPasswordViewModel
    {
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The password must be at least {2} characters long.", MinimumLength = 6)]
        public string Password { get; set; }

        [Compare(nameof(Password), ErrorMessage = "Password and Confirm Password do not match")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }
}

