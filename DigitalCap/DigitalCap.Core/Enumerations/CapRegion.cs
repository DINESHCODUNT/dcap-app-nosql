using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DigitalCap.Core.Enumerations
{
    public enum CapRegion
    {
        [Description("Americas / Europe")]
        AmericasEurope = 1,
        Greece,
        Singapore,
        [Description("Middle East")]
        MiddleEast
    }
}
