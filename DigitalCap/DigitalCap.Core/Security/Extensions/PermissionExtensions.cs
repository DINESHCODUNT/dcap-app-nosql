using DigitalCap.Core.Security;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace DigitalCap.Core.Security.Extensions
{
    public static class PermissionExtensions
    {
        public static IEnumerable<Permission> ToPermissions(this IEnumerable<Claim> source)
        {
            var claimTypes = source?.Select(x => x.Type);
            var result = claimTypes.ToPermissions();

            return result;
        }

        public static IEnumerable<Permission> ToPermissions(this IEnumerable<string> source)
        {
            if (!(source?.Any() ?? false))
                yield break;

            foreach (var currentPermission in source)
                if (Enum.TryParse<Permission>(currentPermission, out var permission))
                    yield return permission;
        }

        public static Claim ToClaim(this Permission source)
        {
            var result = new Claim(source.ToString(), string.Empty);

            return result;
        }
    }
}


    

