using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DigitalCap.Core.Helpers.Constants
{
    public enum ProjectStatus
    {
        Contracted = 7,
        Open = 8,
        Active = 9,
        [Description("Ready for Closure")]
        ToBeClosed = 10,
        Closed = 11,
        Cancelled = 13,
        Deleted = 14
    }
}
