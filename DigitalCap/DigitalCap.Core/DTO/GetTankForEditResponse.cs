using DigitalCap.Core.Models.View.Admin;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class GetTankForEditResponse
    {
        public Guid TankId { get; set; }
        public string TankName { get; set; }
        public int VesselId { get; set; }
        public string VesselName { get; set; }
        public string VesselType { get; set; }
        public string IMONumber { get; set; }
        public string TankType { get; set; }
        public int TaknTypeId { get; set; }
        public string Subheader { get; set; }
        public bool Status { get; set; }
        public string ProjectName { get; set; }
        public int ProjectId { get; set; }
    }
}
