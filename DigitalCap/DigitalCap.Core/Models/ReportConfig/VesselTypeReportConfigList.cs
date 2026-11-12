using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.ReportConfig
{
    public class VesselTypeReportConfigList
    {
        public List<VesselTypePartMapping>? reportParts { get; set; }
        public List<VesselTypeSectionMapping>? normalSectionMappings { get; set; }
        public List<VesselTypeTankSectionMapping>? tankSectionMappings { get; set; }
        public List<VesselTypeSubSectionMapping>? normalSubSectionMappings { get; set; }
    }

    public class VesselTypePartMapping
    {
        public int vesselTypePartMappingId { get; set; }
        public int VesselTypeId { get; set; }
        public string PartName { get; set; }
        public int SequenceNo { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public bool? IsDeleted { get; set; } = false;
    }


    public class VesselTypeSectionMapping
    {
        public Guid? VesselTypeNormalSectionMappingId { get; set; } = new Guid();
        public int? VesselTypePartMappingId { get; set; }
        public int? VesselTypeId { get; set; }
        public string SectionName { get; set; }
        public string SubHeader { get; set; }

        public int? Order { get; set; }
        public int? TotalCards { get; set; }
        public int? PlaceholderCount { get; set; }
        public int? FileNameCount { get; set; }
        public bool? IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public string? PartName { get; set; }
        public bool? IsDeleted { get; set; } = false;
    }


    public class VesselTypeTankSectionMapping
    {
        public Guid? VessselTypeTankSectionMappingId { get; set; } = new Guid();
        public int? VesselTypePartMappingId { get; set; }
        public int? VesselTypeId { get; set; }
        public string? PartName { get; set; }
        public int? TankTypeId { get; set; }

        public int? TotalCards { get; set; }

        public int? PlaceholderCount { get; set; }

        public int? FileNameCount { get; set; }

        public bool? IsActive { get; set; }
        public string? CreatedBy { get; set; }

        public bool? IsDeleted { get; set; } = false;
    }

    public class VesselTypeSubSectionMapping
    {
        public Guid SubSectionId { get; set; }
        public Guid SectionId { get; set; }
        public string SectionName { get; set; }
        public int TotalCards { get; set; }
        public int PlaceholderCount { get; set; }
        public int FileNameCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }

}
