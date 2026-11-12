using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IReportPartRepository
    {
        Task<bool> CreateProjectReportTemplate(int vesselTypeId, string userName, int projectId, bool imoExists = false, int copyprojectId = 0);
        Task<ReportConfigDropDownDataList> GetVesselTypes();
        Task<ReportConfigDropDownDataList> GetReportPartsByVesselTypeAsync(int vesselTypeId);
        Task<bool> CreateReportPart(VesselTypeReportConfigList reportConfigList, int vesselTypeId,string modifiedBy);
        Task<PartSectionNamesList> GetSectionNamesByPartNameAsync(int vesselTypeId, string partName);
        Task<PartSectionNamesList> GetSectionNamesByPartId(int vesselTypeId, int partId);
        Task<List<ProjectCheckBox>> GetGradingCondition();
        Task<List<ProjectSubSection>> GetNormalSectionByProjectId(int projectId, int partId);
        Task<List<ProjectSubSection>> GetProjectSubsectionBySectionId(Guid sectionId);
        Task<List<ProjectCheckBox>> GetGradingConditionByProjectId(int projectId);
        Task<List<ProjectGradingUI>> GetGradingBySectionId(int projectId, Guid sectionId);
        Task<List<ImageDescriptionUI>> GetImageDescriptionsBySectionId(int projectId, Guid sectionId);
        Task<List<ImageDescriptionUI>> GetImageDescriptionsBySubSectionId(int projectId, Guid sectionId);
        Task<List<GenericImageCard>> GetGenericImageCard(int projectId, int templateId, Guid sectionId);
        Task<List<ProjectTankUI>> GetTankSectionByProjectId(int projectId, int partId, int imoNumber);
        Task<List<Core.Models.Survey.Grading>> GetH_IGrading(bool isGasCarrier);
        Task<List<ImageDescriptionUI>> GetImageDescriptionsByTankTypeId(int projectId, int tankTypeId);
        Task<List<ProjectGradingMapping>> GetH_IProjectGradingByProjectId(int projectId);
        Task<List<ProjectGradingConditionMapping>> GetHandIGradingConditionByProjectId(int projectId);
        Task<ProjectPart> GetProjectTemplateTitle(int partId, int projectId);
        Task<List<ExportPart>> GetVesselTypeExportPartConfiguration(int vesseltypeId);
        Task<List<ExportSections>> GetVesselTypeExportSectionConfiguration(int partId, int vesselTypeId);
        Task<List<ExportPart>> GetProjectExportPartConfiguration(int projectId);
        Task<List<ExportSections>> GetProjectExportSectionConfiguration(int partId, int projectId);
        Task<bool> CheckToShowSectionExportConfig(int projectId, int imoNumber, int partId);
    }
}
