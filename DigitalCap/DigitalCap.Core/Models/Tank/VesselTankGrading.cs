using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Tank
{
    public class VesselTankGrading
    {
        public int Id { get; set; }
        public String VesselName { get; set; }
        public string VesselType { get; set; }
        public int TankTypeId { get; set; }
        public string GradingName { get; set; }
        public string GradingGroupId { get; set; }
        public string GradingConditionGroupId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public int TemplateId { get; set; }
        public int ProjectId { get; set; }
        public bool RequiredInReport { get; set; }
    }
}
