using DigitalCap.Infrastructure.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Identity
{
    public class CosmosRoleStore : RoleStore<IdentityRole, ApplicationDbContext, string>
    {
        public CosmosRoleStore(ApplicationDbContext context, IdentityErrorDescriber? describer = null)
            : base(context, describer)
        {
        }

        public override async Task<IdentityRole?> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(normalizedName);

            return await Roles.FirstOrDefaultAsync(
                r => string.Equals(r.NormalizedName, normalizedName, StringComparison.OrdinalIgnoreCase),
                cancellationToken);
        }
    }
}
