using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ReportConfig;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IReportPartService
    {
        Task<ServiceResult<ProjectReport>> GetReportBySectionId(int projectId, int partId, Guid? SectionId, int imoNumber, List<string> SectionIds);

        Task<ServiceResult<ReportConfigDropDownDataList>> GetReportPartsByVesselType(int vesselTypeId);

        Task<ServiceResult<PartSectionNamesList>> GetSectionNamesByPartNameAsync(int vesselTypeId, string partName);
        Task<ServiceResult<PartSectionNamesList>> GetSectionNamesByPartIdAsync(int vesselTypeId, int partId);
        Task<ServiceResult<bool>> CreateReportPart(VesselTypeReportConfigList reportConfigList, int vesselTypeId,string modifiedBy);
        Task<ServiceResult<ReportConfigDropDownDataList>> GetReportPartsByVesselTypeAsync(int vesselTypeId);
    }
}
