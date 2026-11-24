using DigitalCap.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DigitalCap.Core.Models.ABSPlatformModel;
using DigitalCap.WebApi.Core;



namespace DigitalCap.Infrastructure.DbContext
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>()
                .Ignore(x => x.Roles)
                .Ignore(x => x.Permissions)
                .Ignore(x => x.UserAccount)
                .Ignore(x => x.IsEnabled);

            base.OnModelCreating(builder);

            if (Database.IsCosmos())
            {
                builder.Ignore<UserRequest>();
                builder.Ignore<ApplicationToOrganizationMapping>();

                builder.Entity<ApplicationUser>(b =>
                {
                    b.ToContainer("dbo_AspNetUsers");
                    b.HasPartitionKey(u => u.Id);
                    b.Property(u => u.Id).ToJsonProperty("id");
                    b.HasNoDiscriminator();
                    b.UseETagConcurrency();
                    b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken(false);
                });

                builder.Entity<IdentityRole>(b =>
                {
                    b.ToContainer("dbo_AspNetRoles");
                    b.HasPartitionKey(r => r.Id);
                    b.Property(r => r.Id).ToJsonProperty("id");
                    b.HasNoDiscriminator();
                    b.UseETagConcurrency();
                    b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken(false);
                });

                builder.Entity<IdentityUserRole<string>>(b =>
                {
                    b.ToContainer("dbo_AspNetUserRoles");
                    b.HasPartitionKey(x => x.UserId);
                    b.HasNoDiscriminator();
                });

                builder.Entity<IdentityUserClaim<string>>(b =>
                {
                    b.ToContainer("dbo_AspNetUserClaims");
                    b.HasPartitionKey(x => x.UserId);
                    b.HasNoDiscriminator();
                });

                builder.Entity<IdentityUserLogin<string>>(b =>
                {
                    b.ToContainer("dbo_AspNetUserLogins");
                    b.HasPartitionKey(x => x.UserId);
                    b.HasNoDiscriminator();
                });

                builder.Entity<IdentityUserToken<string>>(b =>
                {
                    b.ToContainer("dbo_AspNetUserTokens");
                    b.HasPartitionKey(x => x.UserId);
                    b.HasNoDiscriminator();
                });

                builder.Entity<IdentityRoleClaim<string>>(b =>
                {
                    b.ToContainer("dbo_AspNetRoleClaims");
                    b.HasPartitionKey(x => x.RoleId);
                    b.HasNoDiscriminator();
                });

                // Cosmos DB provider does not support index definitions: remove the indexes IdentityDbContext adds
                // (EmailIndex, UserNameIndex, RoleNameIndex). Lookups by NormalizedEmail/NormalizedUserName still work as queries.
                foreach (var entityType in builder.Model.GetEntityTypes())
                {
                    foreach (var index in entityType.GetIndexes().ToList())
                    {
                        entityType.RemoveIndex(index);
                    }
                }
            }
        }

        public DbSet<ApplicationToOrganizationMapping> ApplicationToOrganizationMappings { get; set; }
        public DbSet<UserRequest> UserRequests { get; set; }



    }
}
