using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.ReportConfig
{
    public class ReportConfigDropDownDataList
    {
        public List<VesselTypes>? vesselTypes { get; set; } // To return all the Vessel Types to View
        public List<ReportTemplates>? reportTemplates { get; set; }
        public int SelectedVesselTypeId { get; set; }

    }

    public class VesselTypes
    {
        public int VesselTypeId { get; set; }
        public string VesselType { get; set; }
    }

    public class ReportTemplates
    {
        public int VesselTypeId { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; }
        public int SequenceNo { get; set; }
        public bool IsActive { get; set; }
        public bool IsEditable { get; set; }
    }

    public class PartSectionNamesList
    {
        public List<NormalSectionNames> NormalSections { get; set; }
        public List<SubSectionNames> SubSections { get; set; }
        public List<TankSectionNames> TanksSections { get; set; }
    }

    public class NormalSectionNames
    {
        public int PartId { get; set; }
        public string PartName { get; set; }
        public int VesselTypeId { get; set; }
        public Guid SectionId { get; set; }
        public string SectionName { get; set; }
        public string SubHeader { get; set; }
        public int TotalCards { get; set; }
        public int PlaceholderCount { get; set; }
        public int FileNameCount { get; set; }
        public bool IsActive { get; set; }

        public bool ReportRequired { get; set; }
    }
    public class SubSectionNames
    {
        public int PartId { get; set; }
        public string PartName { get; set; }
        public int VesselTypeId { get; set; }
        public Guid SubSectionId { get; set; }
        public Guid SectionId { get; set; }
        public string SectionName { get; set; }
        public int TotalCards { get; set; }
        public int PlaceholderCount { get; set; }
        public int FileNameCount { get; set; }
        public bool IsActive { get; set; }
    }
    public class TankSectionNames
    {
        public int PartId { get; set; }
        public string PartName { get; set; }
        public int VesselTypeId { get; set; }
        public int TankTypeId { get; set; }
        public string TankType { get; set; }
        public int TotalCards { get; set; }
        public int PlaceholderCount { get; set; }
        public int FileNameCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsMapped { get; set; }
    }

    public class VesselTypeMappingPart
    {
        public int vesselTypeId { get; set; }
        public int partId { get; set; }
        public string partName { get; set; }
        public int sequenceNo { get; set; }
        public bool isActive { get; set; }
        public bool isEditable { get; set; }
    }
}
