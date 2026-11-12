using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
                   Inherited = true,
                   AllowMultiple = true)]
    public class PermissionAuthorizeAttribute : Attribute, IFilterFactory
    {
        private readonly Permission[] _permissions;
        private readonly bool _areAllPermissionRequired;

        public bool IsReusable => true;

        public PermissionAuthorizeAttribute(params Permission[] permissions)
            : this(true, permissions)
        { }

        public PermissionAuthorizeAttribute(bool areAllPermissionsRequired, params Permission[] permissions)
        {
            _permissions = permissions;
            _areAllPermissionRequired = areAllPermissionsRequired;
        }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var result = serviceProvider.GetService<PermissionAuthorizationFilter>();
            
            if (result == null)
            {
                throw new InvalidOperationException(
                    $"Unable to resolve {nameof(PermissionAuthorizationFilter)} from dependency injection. " +
                    "Please ensure it is registered in the service collection.");
            }
            
            result.Permissions = _permissions;
            result.AreAllPermissionsRequired = _areAllPermissionRequired;

            return result;
        }
    }

}
