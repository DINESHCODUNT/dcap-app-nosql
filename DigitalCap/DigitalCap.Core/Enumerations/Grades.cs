using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DigitalCap.Core.Enumerations
{
    public enum Grades
    {
        [Description("A+")]
        APlus = 1,
        [Description("A")]
        A,
        [Description("A-")]
        AMinus,
        [Description("B+")]
        BPlus,
        [Description("B")]
        B,
        [Description("B-")]
        BMinus,
        [Description("C+")]
        CPlus,
        [Description("C")]
        C,
        [Description("C-")]
        CMinus,
        [Description("D")]
        D
    }

}
