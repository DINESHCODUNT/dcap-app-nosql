using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.ReportConfig;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IExportRepository
    {
        Task<List<ExportPart>> GetVesselTypeExportPartConfiguration(int vesselTypeId);
        Task<List<ExportSections>> GetVesselTypeExportSectionConfiguration(int partId, int vesselTypeId);

        Task<List<ExportPart>> GetProjectExportPartConfiguration(int projectId);
        Task<List<ExportSections>> GetProjectExportSectionConfiguration(int partId, int projectId);

        Task<bool> UpdatePartRequiredInReportAsync(bool requiredInReport, int partId, int projectId, int vesselTypeId, string updatedBy);
        Task<bool> UpdateTankSectionRequiredInReportAsync(bool requiredInReport, Guid sectionId, int projectId, string updatedBy);
        Task<bool> UpdateSectionRequiredInReportAsync(bool requiredInReport, Guid sectionId, int projectId, int vesselTypeId, string updatedBy);
    }


}
