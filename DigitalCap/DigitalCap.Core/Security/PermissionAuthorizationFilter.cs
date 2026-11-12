using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class PermissionAuthorizationFilter : IAuthorizationFilter
{
    private readonly IUser _user;

    public Permission[] Permissions { get; set; }
    public bool AreAllPermissionsRequired { get; set; }

    public PermissionAuthorizationFilter(IUser user)
    {
        _user = user;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var httpUser = context.HttpContext.User;

        if (httpUser == null || !httpUser.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (_user.Permissions.Contains(Permission.SystemAdministrator))
            return;

        var hasRequiredPermissions = AreAllPermissionsRequired
            ? _user.HasAllPermissions(Permissions)
            : _user.HasAnyPermissions(Permissions);

        if (!hasRequiredPermissions)
            context.Result = new ForbidResult();
    }
}
