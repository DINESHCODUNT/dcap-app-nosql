using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.Models.View.Admin
{
    public class AccountViewModel
    {
        public UserAccountModel UserAccount { get; set; }
        public string AspNetUser { get; set; }
        public List<ClientModel> Clients { get; set; }

        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

        public string Id { get; set; }
               
        public string FirstName => UserAccount?.FirstName;
        public string LastName => UserAccount?.LastName;

        public string Email { get; set; }
        public bool IsEnabled { get; set; }
        public string DisplayRole { get; set; }
        public string Phone => UserAccount?.Phone;

        public string TimeZone => UserAccount?.TimeZone;

        public string Company { get; set; }

        public Guid? ClientId => UserAccount?.ClientId;
        public List<BreadCrumb> Breadcrumbs { get; set; } = new List<BreadCrumb>();
        public string Controller { get; set; }
        public string PostAction { get; set; }
        public string Title { get; set; }
        public string CancelUrl { get; set; }
        public string SaveText { get; set; }
        public bool AbsUser { get; set; }
        public bool ClientUser { get; set; }
        public bool NewUser { get; set; }

        [Display(Name = "User Role")]
        [Required]
        public string UpdatedRole { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AbsUser)
            {
                if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@eagle.org"))
                {
                    yield return new ValidationResult(
                        "An ABS User must use an email address with domain eagle.org.",
                        new[] { nameof(Email) }
                    );
                }
            }
        }

    }

    public class RequestAccessDto
    {
        public string Email { get; set; }
    }

    public class AbsUserModel
    {
        public UserAccountModel UserAccount { get; set; }
        public string AspNetUser { get; set; }
        public List<ClientModel> Clients { get; set; }

        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

        public string Id { get; set; }

        public string FirstName => UserAccount?.FirstName;
        public string LastName => UserAccount?.LastName;

        public string Email { get; set; }
        public bool IsEnabled { get; set; }
        public string DisplayRole { get; set; }
        public string Phone => UserAccount?.Phone;

        public string TimeZone => UserAccount?.TimeZone;

        public string Company { get; set; }

        public Guid? ClientId => UserAccount?.ClientId;
        public List<BreadCrumb> Breadcrumbs { get; set; } = new List<BreadCrumb>();
        public string Controller { get; set; }
        public string PostAction { get; set; }
        public string Title { get; set; }
        public string CancelUrl { get; set; }
        public string SaveText { get; set; }
        public bool AbsUser { get; set; }
        public bool ClientUser { get; set; }
        public bool NewUser { get; set; }

        [Display(Name = "User Role")]
        [Required]
        public string UpdatedRole { get; set; }

       
    }
   
}
