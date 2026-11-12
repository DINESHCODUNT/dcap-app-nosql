using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.ImageDescription
{
    public class ImageDescriptions
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }

        public int CategoryId { get; set; }

        public int ProjectId { get; set; }

        public Guid? SectionId { get; set; }

        public string SectionName { get; set; }

        public DateTime CreatedDttm { get; set; }

        public DateTime UpdatedDttm { get; set; }

        public int? TankTypeId { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public string VesselType { get; set; }
        public int TemplateId { get; set; }

        public string TemplateName { get; set; }
    }
    public class VesselTankMapping
    {
        public Guid Id { get; set; }
        public string VesselType { get; set; }
        public string Subheader { get; set; }
        public string ImoNumber { get; set; }
        public int TankTypeId { get; set; }
        public string TankName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public bool IsDeleted { get; set; }
        public bool RequiredInReport { get; set; }

        public int? ProjectId { get; set; }
    }

    public class ImageDescriptionsData
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public int ProjectId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdatedDttm { get; set; }
        public bool IsDeleted { get; set; }
        public string VesselType { get; set; }
    }
    public class VesselGradingMapping
    {
        public int Id { get; set; }
        public string VesselType { get; set; }
        public int TankTypeId { get; set; }
        public string GradingName { get; set; }
        public int GradingGroupId { get; set; }
        public int GradingConditionGroupId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public bool IsDeleted { get; set; }
        public int ProjectId { get; set; }
        public bool RequiredInReport { get; set; }
    }

    public class VesselTypeTankMapping
    {
        public Guid VesselTypeTankSectionMappingId { get; set; }
        public int? VesselTypePartMappingId { get; set; }
        public int? VesselTypeId { get; set; }
        public int? TankTypeId { get; set; }
        public int? TotalCards { get; set; }
        public int? PlaceholderCount { get; set; }
        public int? FileNameCount { get; set; }
        public bool? IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsDeleted { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }

}
