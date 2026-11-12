using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class AddClientAdminInitDto
    {
        public Guid ClientId { get; set; }
        public string Company { get; set; } = string.Empty;
        public string DefaultRole { get; set; } = DigitalCap.Core.Constants.AppConstants.Roles.ClientAdmin;
    }
}
