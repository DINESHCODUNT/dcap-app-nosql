using DigitalCap.Core.Models.View.Admin;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class ManageTankResponse
    {
        public bool IsActive { get; set; }
        public bool Editable { get; set; }
        public bool CanDelete { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsDownload { get; set; }
        public string IMO { get; set; }
        public string ProjectId { get; set; }

        public int TankRestoreFilter { get; set; }
        public int SearchRestoreFilter { get; set; }

        public List<Tank> Tanks { get; set; }
        public List<TankFilterModel> IMONumberOptions { get; set; }
        public List<TankFilterModel> TankTypeOptions { get; set; }
        public List<TankFilterModel> TankNameOptions { get; set; }
        public List<TankFilterModel> TankStatusOptions { get; set; }
        public List<TankFilterModel> VesselTypeOptions { get; set; }
        public List<TankFilterModel> VesselNameOptions { get; set; }
    }

    public class ManageTankStatusRequest
    {
        public List<Guid> Data { get; set; }
        public bool Status { get; set; }
        public string? IMO { get; set; }
    }

}
