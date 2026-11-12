using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class ImageDescriptionDTO

    {
        public Guid? SectionId { get; set; }
        public int ? TankTypeId { get; set; }
        public int? ProjectId { get; set; }
        public int? CategoryId { get; set; }
        public string Description { get; set; }
        public string TemplateName { get; set; }
        public string VesselType { get; set; }
        public string SectionName { get; set; }
        public DateTime? CreatedDttm { get; set; }
        public DateTime? UpdatedDttm { get; set; }
        public bool ? IsActive { get; set; }
        public bool ? IsDeleted { get; set; }   




    }
}
