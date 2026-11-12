using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Survey
{
    public class SurveyGrading
    {
        public Id1 id1 { get; set; } = new Id1();
        public Id2 id2 { get; set; } = new Id2();
        public Id3 id3 { get; set; } = new Id3();
        public Id4 id4 { get; set; } = new Id4();
        public Id5 id5 { get; set; } = new Id5();
        public Id6 id6 { get; set; } = new Id6();
    }

    public class Id1
    {
        public string label { get; set; } = "Grade 1 - Very Good";
        public double position { get; set; } = 1.0;
        public bool selected { get; set; } = false;
        public object action { get; set; }
    }

    public class Id2
    {
        public string label { get; set; } = "Grade 2 - Good";
        public double position { get; set; } = 2.0;
        public bool selected { get; set; } = false;
        public object action { get; set; }
    }

    public class Id3
    {
        public string label { get; set; } = "Grade 3 - Satisfactory";
        public double position { get; set; } = 3.0;
        public bool selected { get; set; } = false;
        public object action { get; set; }
    }

    public class Id4
    {
        public string label { get; set; } = "Grade 4 - Unsatisfactory";
        public double position { get; set; } = 4.0;
        public bool selected { get; set; } = false;
        public object action { get; set; }
    }

    public class Id5
    {
        public string label { get; set; } = "Grade 5 - Unexamined";
        public double position { get; set; } = 5.0;
        public bool selected { get; set; } = false;
        public object action { get; set; }
    }

    public class Id6
    {
        public string label { get; set; } = "Not Applicable";
        public double position { get; set; } = 6.0;
        public bool selected { get; set; } = false;
        public object action { get; set; }
    }
}
