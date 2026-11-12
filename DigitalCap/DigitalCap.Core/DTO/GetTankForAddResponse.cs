using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.Models.VesselModel;
using DigitalCap.Core.Models.View.Admin;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class GetTankForAddResponse
    {
        public string IMO { get; set; }
        public string ProjectId { get; set; }
        public bool ProjectTanktype { get; set; }
        public string VesselName { get; set; }
        public string ProjectName { get; set; }
        public List<VesselDetails> VesselList { get; set; }
        public List<IMOTankFilterModel> Projects { get; set; }
    }
}
