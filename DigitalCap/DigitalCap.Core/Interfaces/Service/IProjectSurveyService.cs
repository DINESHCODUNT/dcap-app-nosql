using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.ViewModels;
using System.Collections.Generic;

namespace DigitalCap.Core.Interfaces.Service
{
    /// <summary>
    /// Survey (project-template) flow migrated from MVC <c>SurveyController</c> — uses <see cref="ProjectReport"/> / <see cref="ProjectReportViewModel"/>.
    /// </summary>
    public interface IProjectSurveyService
    {
        Task<ServiceResult<ProjectReportViewModel>> GetSurveyIndexAsync(int projectId, string? currentUserName, int? templateSectionId = null);
        Task<ServiceResult<ProjectReportViewModel>> IndexPartialAsync(int projectId, int? templateSectionId, int imo);
        Task<ServiceResult<ProjectExportSettings>> PrepareProjectExportSettingsAsync(int projectId, int? templateId, int imo);
        Task<ServiceResult<bool>> CheckShowExportSettingsAsync(int projectId, int imo, int templateId);
        Task<ServiceResult<bool>> CheckShowExportAllSettingsAsync(int projectId, int imo);
        Task<ServiceResult<List<ExportSections>>> GetAllSectionsAsync(int projectId, int templateId);
        Task<ServiceResult<List<ExportPart>>> GetAllPartsAsync(int projectId, int imo);
        Task<ServiceResult<bool>> CreateProjectReportAsync(int projectId, int templateSectionId);

        Task<ServiceResult<string>> ValidateProjectGradingAsync(int projectId, int templateSectionId, int imo, List<ExportSections>? settings);

        Task<ServiceResult<string>> ExportAllProjectValidationAsync(int projectId, int imo, List<ExportPart> settings);
    }
}
