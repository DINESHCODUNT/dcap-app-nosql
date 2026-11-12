using DigitalCap.Core.Models.View.Admin;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class GetDataByIMOResponse
    {
        public List<TankFilterModel> TankTypes { get; set; }
        public List<TankFilterModel> TankNames { get; set; }
        public List<TankFilterModel> VesselTypes { get; set; }
    }
}
