using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class ProjectCountGroupedByClientId
    {
        public Guid ClientId { get; set; }

        public int ProjectCount { get; set; }
    }
}
