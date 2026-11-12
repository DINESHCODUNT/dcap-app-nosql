using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Tank
{
    public class VesselTank : EntityBase<Guid>
    {
        public Guid Id { get; set; }
        public string VesselName { get; set; }
        public string VesselType { get; set; }
        public string TankName { get; set; }
        public string Subheader { get; set; }
        public int TankTypeId { get; set; }
        public string ImoNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public int TemplateId { get; set; }

        public bool RequiredInReport { get; set; }
        public int? ProjectId { get; set; }
    }
}
