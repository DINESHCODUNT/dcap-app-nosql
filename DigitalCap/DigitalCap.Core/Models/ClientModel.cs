using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using DigitalCap.Core.Models;


namespace DigitalCap.Core.Models
{
    [Table("Clients")]
    public class ClientModel : AuditableEntity<Guid>
    {
        public string Name { get; set; }
        public string ClientId { get; set; }
        public string WCN { get; set; }
        public string CompanyWebsite { get; set; }
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string ContactFirstName { get; set; }
        public string ContactLastName { get; set; }
        public string ContactEmail { get; set; }
        public ClientModel()
        {
            Id = Guid.NewGuid();

        }
        public bool EmailIsEnabled { get; set; }
    }
}
