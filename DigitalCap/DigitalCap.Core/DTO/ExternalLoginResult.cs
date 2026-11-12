using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class ExternalLoginResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? RedirectUrl { get; set; }
    }

}
