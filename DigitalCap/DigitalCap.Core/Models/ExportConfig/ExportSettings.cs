using DigitalCap.Core.Models.ReportConfig;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.ExportConfig
{
    public class ExportSettings
    {
        public int SelectedImoNumber { get; set; }
        public string VesselType { get; set; }
        public List<VesselTypes> VesselTypes { get; set; }
        public List<ProjectImoNumber> imoNumbers { get; set; }
        public List<string> ImoNumbers { get; set; }
        public int SelectedVesselTypeId { get; set; }
        public List<ExportPart> exportParts { get; set; }


    }
    public class ExportSettingsRequest
    {
        public int SelectedVesselTypeId { get; set; }
        public int SelectedImoNumber { get; set; }
        public List<ExportPart> exportParts { get; set; }
    }
    public class ProjectImoNumber
    {
        public int Id { get; set; }
        public int IMO { get; set; }
        public string Name { get; set; }
    }

    public class ExportPart
    {
        public int PartId { get; set; }
        public string PartName { get; set; }
        public bool RequiredInReport { get; set; }
        public bool IncludeInReport { get; set; } = true;

        public List<ExportSections> sections { get; set; }
    }

    public class ExportSections
    {
        public Guid SectionId { get; set; }
        public string SectionName { get; set; }
        public bool RequiredInReport { get; set; }
        public bool IncludeInReport { get; set; } = true;
        public int TankTypeId { get; set; } = 0;
        public bool IsSubSection { get; set; }
        public Guid ParentId { get; set; }
        public string TankType { get; set; }

    }

    public class ExportRequest
    {
        public List<ExportPart> Data { get; set; }
    }
}
