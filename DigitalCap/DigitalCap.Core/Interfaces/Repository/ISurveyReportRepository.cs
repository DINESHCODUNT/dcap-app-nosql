using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface ISurveyReportRepository
    {
        Task<SurveyStatus> MapSurveyStatus(string classNumber, int imo);
        Task<Report> MapStatuatoryCertificates(string classNumber, Report report);
        Task<List<ReportTemplateSection>> GetTemplateSections(int templateId);
        Task<List<ReportTemplate>> GetReportTemplateList();
        Task<List<int>> GetExistingReportPartsSectionIds(int projectId);
        Task<ReportTemplateUI> GetTemplateTitle(int templateId);
        Task<List<CurrentCondition>> GetCurrentCondition();
        Task<List<ProjectReportMapping>> GetProjectReportTemplate(int projectId, int templateId);
        Task<List<ProjectGradingMapping>> GetProjectGrading(int projectId, int templateId, Guid sectionId);
        Task<List<ProjectGradingConditionMapping>> GetProjectGradingConditionMapping(int projectId, int templateId, Guid sectionId, int gradingId);
        Task<List<GradingUI>> GetTemplatGradings(int templateId, int projectId);
        Task<List<ImageCardUI>> GetImageCard(int templateId);
        Task<List<GradingConditionUI>> GetGradingCondition(int templateId, int projectid);
        Task<List<ProjectCardMapping>> GetProjectImageCard(int projectId, int templateId, Guid sectionId);
        Task<HandIReport> GetHIReport(int projectId);
        Task<ProjectReportMapping> GetProjectReportTemplate(int projectId, int templateId, Guid sectionId);
        Task<List<ReportTemplateSection>> GetTemplatSections(int templateId);
        Task<List<UpskillImageData>> GetBulkUploadUnplacedImages();
        Task<bool> CreateProjectTemplate(ProjectReportMapping model);
        Task<bool> DeleteProjectTemplate(int projectId, int templateId);
        Task<int> DeleteBulkUploadImage(int imageId);
        Task<bool> AddEditImageNew(int imgId, string imageBase64);
        Task<bool> DeleteImage(string assignmentId, string fileId);
        Task<bool> DeleteCard(int projectId, string cardId, string sequenceId, string assignmentId, string appId);
        Task<int> UploadImage(string assignmentId, string fileId);
        Task<ProjectCardMapping> GetProjectImagCardById(int projectId, int templateId, Guid sectionId, int cardId);
        Task<bool> CreateProjectImageCard(ProjectCardMapping model);
        Task<bool> UpdateProjectImageCard(ProjectCardMapping model);
        Task<ProjectGradingMapping> GetProjectGradingById(int projectId, int templateId, Guid sectionId, int gradingId);
        Task<bool> UpdateProjectGrading(ProjectGradingMapping model);
        Task<bool> CreateProjectGrading(ProjectGradingMapping model);
        Task<bool> UpdateProjectGradingConditionMapping(ProjectGradingConditionMapping model);
        Task<bool> CreateProjectGradingConditionMapping(ProjectGradingConditionMapping model);
        Task<bool> UpdateProjectTemplate(ProjectReportMapping model);
        Task<bool> UpdateHIReport(HandIReport model);
        Task<bool> CreateHIReport(HandIReport model);
    }
}
