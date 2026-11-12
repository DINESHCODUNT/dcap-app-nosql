using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class UserWCNMappingModel
    {
        public int WCN { get; set; }
        public required string CompanyName { get; set; }
        public required string UserEmail { get; set; }
        public bool IsMapped { get; set; }
    }
}
