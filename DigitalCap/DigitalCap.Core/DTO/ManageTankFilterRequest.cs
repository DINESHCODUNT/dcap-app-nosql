using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class ManageTankFilterRequest
    {
        public int Filter { get; set; }
        public int SearchValue { get; set; }
        public string IMO { get; set; }
        public string ProjectId { get; set; }
    }
}
