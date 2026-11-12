using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DigitalCap.Core.Helpers.Constants
{
    public enum CapTaskStatus
    {
        Completed = 1,
        [Description("In Progress")]
        InProgress = 2,
        [Description("Not Started")]
        NotStarted = 3,
        [Description("Not Applicable")]
        NotApplicable = 4,
        Overdue = 5,
        [Description("On Hold")]
        OnHold = 12
    }
}
