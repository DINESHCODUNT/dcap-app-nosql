using DigitalCap.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using DigitalCap.Core.Models.ABSPlatformModel;
using DigitalCap.WebApi.Core;



namespace DigitalCap.Infrastructure.DbContext
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly string _connectionString;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>()
                .Ignore(x => x.Roles)
                .Ignore(x => x.Permissions)
                .Ignore(x => x.UserAccount)
                .Ignore(x => x.IsEnabled);

            base.OnModelCreating(builder);
        }

        public DbSet<ApplicationToOrganizationMapping> ApplicationToOrganizationMappings { get; set; }
        public DbSet<UserRequest> UserRequests { get; set; }



    }
}
