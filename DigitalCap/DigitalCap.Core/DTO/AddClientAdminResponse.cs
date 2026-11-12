using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class AddClientAdminResponse
    {
        public Guid ClientId { get; set; }
        public string Company { get; set; }
        public string DefaultRole { get; set; }

    }
}
