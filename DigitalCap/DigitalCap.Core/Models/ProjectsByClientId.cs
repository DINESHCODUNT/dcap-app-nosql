using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class ProjectsByClientId
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string PIDNumber { get; set; }
        public DateTime? SurveyFirstVisit { get; set; }
        public DateTime? SurveyLastVisit { get; set; }
        public string StatusName { get; set; }
    }
}
