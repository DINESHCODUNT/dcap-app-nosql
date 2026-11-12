using DigitalCap.Core.Interfaces.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Security.Extensions
{
    public static class UserExtensions
    {
        public static bool IsABSAdministrator(this IUser source)
        {
            var result = source?.HasAllPermissions(Permission.ManageAbsUsers)
                            ?? false;

            return result;
        }
    }
}
