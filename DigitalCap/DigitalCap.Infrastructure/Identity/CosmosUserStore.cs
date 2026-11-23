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

        public override async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(normalizedUserName);

            return await Users.FirstOrDefaultAsync(
                u => string.Equals(u.NormalizedUserName, normalizedUserName, StringComparison.OrdinalIgnoreCase),
                cancellationToken);
        }

        public override async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(normalizedEmail);

            return await Users.SingleOrDefaultAsync(
                u => string.Equals(u.NormalizedEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase),
                cancellationToken);
        }

        protected override async Task<IdentityRole?> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            return await Context.Set<IdentityRole>().SingleOrDefaultAsync(
                r => string.Equals(r.NormalizedName, normalizedRoleName, StringComparison.OrdinalIgnoreCase),
                cancellationToken);
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

            // EF Core Cosmos returns no rows for roleIds.Contains(r.Id) when Id is the partition key (the same query sent
            // directly to Cosmos returns the row). Roles are few: load them and filter in memory (SQL join is case-insensitive).
            var allRoles = await Context.Set<IdentityRole>().ToListAsync(cancellationToken);
            return allRoles
                .Where(r => roleIds.Contains(r.Id, StringComparer.OrdinalIgnoreCase))
                .Select(r => r.Name!)
                .ToList();
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
                .FirstOrDefaultAsync(r => string.Equals(r.NormalizedName, normalizedRoleName, StringComparison.OrdinalIgnoreCase), cancellationToken);

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
                .FirstOrDefaultAsync(r => string.Equals(r.NormalizedName, normalizedRoleName, StringComparison.OrdinalIgnoreCase), cancellationToken);

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

            // Same EF Core Cosmos limitation as GetRolesAsync: filter in memory.
            var userIdSet = new HashSet<string>(userIds, StringComparer.OrdinalIgnoreCase);
            var allUsers = await Context.Set<ApplicationUser>().ToListAsync(cancellationToken);
            return allUsers
                .Where(u => userIdSet.Contains(u.Id))
                .ToList();
        }
    }
}
