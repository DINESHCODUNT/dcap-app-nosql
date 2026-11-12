using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Security;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class LoginData
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public string ProviderKey { get; set; }
    }

    public class ApplicationDto : IUser
    {
        public static readonly ApplicationDto Anonymous = new();

        public string Id { get; set; }
        public string UserName { get; set; }
        public string? Email { get; set; }
        public bool IsEnabled { get; set; }

        // MUST match interface exactly
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
        public IReadOnlyCollection<Permission> Permissions { get; set; } = Array.Empty<Permission>();

        public UserAccountModel? UserAccount { get; set; }

        public string DisplayName => UserAccount?.DisplayName ?? UserName;
        public virtual bool EmailConfirmed { get; set; }
        public string? DisplayRole =>
            Roles.Any() ? string.Join(", ", Roles) : null;

        public bool HasAllPermissions(params Permission[] permissions)
            => permissions.All(p => Permissions.Contains(p));

        public bool HasAnyPermissions(params Permission[] permissions)
            => permissions.Any(p => Permissions.Contains(p));

    }



}

