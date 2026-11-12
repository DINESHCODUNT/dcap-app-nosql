using DapperExtensions.Mapper;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalCap.Core.Models
{
    [Table("UserAccounts")]
    public class UserAccountModel : AuditableEntity<Guid>
    {
        public string AspNetUserId { get; set; }
        public Guid ClientId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public bool IsAgreeToPrivacy { get; set; }
        public string DisplayName => string.Join(" ", FirstName, LastName);
        public string ProfileImageUrl { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Title { get; set; }
        public string Phone { get; set; }
        public string TimeZone { get; set; } = "";
    }

    public class UserAccountModelMapper : ClassMapper<UserAccountModel>
    {
        public UserAccountModelMapper()
        {
            Table("[dbo].[UserAccounts]");
            
            // Ignore computed property
            Map(x => x.DisplayName).Ignore();
            
            // Ignore properties that don't exist in the database table
            Map(x => x.ProfileImageUrl).Ignore();
            Map(x => x.Email).Ignore();
            Map(x => x.Title).Ignore();
            
            AutoMap();
        }
    }
}