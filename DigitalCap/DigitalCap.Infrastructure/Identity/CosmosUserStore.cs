using DigitalCap.Infrastructure.DbContext;
using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Identity
{
    public class CosmosUserStore : UserStore<ApplicationUser, IdentityRole, ApplicationDbContext, string>
    {
        public CosmosUserStore(ApplicationDbContext context, IdentityErrorDescriber? describer = null)
            : base(context, describer)
        {
        }

        public override async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);

            var userId = user.Id;
            var roleIds = await Context.Set<IdentityUserRole<string>>()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync(cancellationToken);

            if (roleIds.Count == 0)
            {
                return new List<string>();
            }

            return await Context.Set<IdentityRole>()
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Name!)
                .ToListAsync(cancellationToken);
        }

        public override async Task<bool> IsInRoleAsync(ApplicationUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(user);
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(normalizedRoleName));
            }

            var role = await Context.Set<IdentityRole>()
                .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);

            if (role == null)
            {
                return false;
            }

            var userId = user.Id;
            var roleId = role.Id;

            return await Context.Set<IdentityUserRole<string>>()
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        }

        public override async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(normalizedRoleName));
            }

            var role = await Context.Set<IdentityRole>()
                .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);

            if (role == null)
            {
                return new List<ApplicationUser>();
            }

            var roleId = role.Id;
            var userIds = await Context.Set<IdentityUserRole<string>>()
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .ToListAsync(cancellationToken);

            if (userIds.Count == 0)
            {
                return new List<ApplicationUser>();
            }

            return await Context.Set<ApplicationUser>()
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync(cancellationToken);
        }
    }
}
