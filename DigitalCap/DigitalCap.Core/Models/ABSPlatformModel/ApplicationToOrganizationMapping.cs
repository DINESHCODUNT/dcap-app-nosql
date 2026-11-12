using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.Models.ABSPlatformModel
{
    public class ApplicationToOrganizationMapping
    {
        public string Id { get; set; }

        [Display(Name = "Organization")]
        public string Organization { get; set; }

        [Display(Name = "Application")]
        public string Application { get; set; }
    }
}
