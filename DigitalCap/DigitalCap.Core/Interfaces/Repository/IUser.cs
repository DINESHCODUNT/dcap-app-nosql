using DigitalCap.Core.Models;
using DigitalCap.Core.Security;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IUser
    {
        string UserName { get; }
        string Email { get; }
        bool IsEnabled { get; }
        IReadOnlyCollection<string> Roles { get; }
        IReadOnlyCollection<Permission> Permissions { get; }
        bool HasAllPermissions(params Permission[] permissions);
        bool HasAnyPermissions(params Permission[] permissions);
        UserAccountModel? UserAccount { get; }
    }
}
