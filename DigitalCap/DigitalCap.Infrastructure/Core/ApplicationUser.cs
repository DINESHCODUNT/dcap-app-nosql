using DigitalCap.Core.Models;
using DigitalCap.Core.Security;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DigitalCap.WebApi.Core
{
    public class ApplicationUser : IdentityUser
    {
        public static readonly ApplicationUser Anonymous = new ApplicationUser();
        [Required]
        [EmailAddress(ErrorMessage = "The Email field is not a valid e-mail address.")]
        public override string Email { get; set; }

        public IReadOnlyList<string> Roles { get; set; }
        public HashSet<Permission> Permissions { get; set; }
        public UserAccountModel UserAccount { get; set; }
        public string DisplayName => UserAccount?.DisplayName ?? UserName;
        public string DisplayRole => Roles?.Any() ?? false
                                        ? string.Join(", ", Roles)
                                        : null;
        public bool IsEnabled { get; set; }

        public bool HasAnyPermissions(params Permission[] permissions)
        {
            if (Permissions == null || permissions == null)
                return false;

            return permissions.Any(p => Permissions.Contains(p));
        }

        public bool HasAllPermissions(params Permission[] permissions)
        {

            if (!(permissions?.Any() ?? false))
                return true;

            if (!(Permissions?.Any() ?? false))
                return false;

            var result = permissions.All(Permissions.Contains);

            return result;
        }

    }

}
