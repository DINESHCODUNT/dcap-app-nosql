using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.ViewModels.AccountViewModels
{
    public class RegisterViewModel
    {
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6,
            ErrorMessage = "Password must be at least {2} characters long.")]
        public string Password { get; set; }

        [Compare(nameof(Password), ErrorMessage = "Password and Confirm Password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [StringLength(64, MinimumLength = 2,
            ErrorMessage = "Organization name must be between {2} and {1} characters.")]
        public string Organization { get; set; }
    }
}

