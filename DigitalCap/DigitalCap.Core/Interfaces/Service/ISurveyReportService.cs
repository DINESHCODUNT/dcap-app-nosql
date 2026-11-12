using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.ViewModels;
using DigitalCap.WebApi.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface ISurveyReportService
    {
        Task<Report> GetReport(int projectId, int templateSectionId, List<string> sectionIds);
        Task<ServiceResult<SurveyReportViewModel>> GetIndexAsync(int projectId, string currentUserName, int? templateSectionId = null);
        Task<bool> DeleteReport(int projectId, int templateSectionId);
        Task<ServiceResult<Report>> IndexPartial(int projectId, int? templateSectionId = null);
        Task<ServiceResult<List<BulkUploadViewModel>>> GetBulkUploadSectionDropdown(int projectId);
        Task<ServiceResult<bool>> CreateReport(int projectId, int templateSectionId);
        /// <summary>Resolves IMO and section ids from the project (MVC GetReportBySectionId parity).</summary>
        Task<ServiceResult<Report>> GetReportBySectionId(int projectId, int templateSectionId, Guid sectionId);
        Task<ServiceResult<Report>> GetReportBySectionId(int projectId, int templateSectionId, Guid SectionId, string imoNumber, List<string> SectionIds);
        Task<int> GetUploadedImageCount(int projectId, int templateId, Guid sectionId);
        Task<ServiceResult<Report>> RefreshCertificate(int projectId);
        Task<ServiceResult<SurveyReportViewModel>> GetSectionIdsByProjectId(int projectId, int? templateId);
        Task<ServiceResult<string>> ExportAllValidation(int projectId);
        Task<ServiceResult<string>> ExportValidationPartial(int projectId, int sectionid);
        Task<ServiceResult<bool>> DeleteImage(string assignmentId, string fileId);
        Task<ServiceResult<bool>> DeleteCard(int projectId, string cardId, string sequenceId, string assignmentId, string applicationId);
        Task<ServiceResult<int>> DeleteBulkUploadImage(int imageId);
        Task<ServiceResult<bool>> UpdateGenericCurrentCondition(int templateId, int projectId, Guid sectionId, int cardId, int value, string cardName);
        Task<ServiceResult<UploadPhotoResultDto>> UploadGenericImage(IFormFile file, int templateId, int projectId, Guid sectionId, int cardId, int imageId, bool replaceImage = false, string cardName = null);
        Task<ServiceResult<bool>> DeleteGenericImage(int templateId, int projectId, Guid sectionId, int cardId, int imageId);
        Task<ServiceResult<bool>> UpdateGenericAdditionalDescription(int templateId, int projectId, Guid sectionId, int cardId, string value, string cardName);
        Task<ServiceResult<bool>> UpdateGenericImageDescriptionDropdownCard(int templateId, int projectId, Guid sectionId, int cardId, int value, string cardName);
        string ResizeImage(byte[] imageBytes);
        
        // New methods for converted APIs
        Task<ServiceResult<UploadPhotoResultDto>> UploadImage(IFormFile file, int templateId, int projectId, Guid sectionId, int cardId, int imageId, bool replaceImage = false);
        Task<ServiceResult<bool>> DeleteImage(int templateId, int projectId, Guid sectionId, int cardId, int imageId);
        Task<ServiceResult<bool>> UpdateImageDescriptionDropdownCard(int templateId, int projectId, Guid sectionId, int cardId, int value);
        Task<ServiceResult<bool>> UpdateAdditionalDescription(int templateId, int projectId, Guid sectionId, int cardId, string value);
        Task<ServiceResult<bool>> UpdateCurrentCondition(int templateId, int projectId, Guid sectionId, int cardId, int value);
        Task<ServiceResult<bool>> UpdateSectionNote(int templateId, int projectId, Guid sectionId, string value);
        Task<ServiceResult<bool>> UpdateGradingNotes(int templateId, int projectId, Guid sectionId, int gradingId, string value);
        Task<ServiceResult<bool>> UpdateGradingCondition(int templateId, int projectId, Guid sectionId, int gradingId, List<GradingChoiceUpdateDto> choices);
        Task<ServiceResult<bool>> UpdateH_IReport(Guid Id, int ProjectId, string CompanyName, string CertificateNumber, DateTime ExpiryDate, string ThicknessMeasurementA, string ThicknessMeasurementB, string ThicknessMeasurementC, string AdditionalSurveyorComments, string MaintanceExecuted, string Accommodation, string PhotoGraphs, string OperationalTestLocation, DateTime OperationalTestStart, DateTime OperationalTestFinish, string OptionalTestSatisFactory, string AssociatedReportNumber, string AssociatedReportDate, string Remarks, string LeadSurveyorName, string LeadSurveyorNumber, string SurveyorName, string SurveyorNumber, DateTime LastVisitDate, string IndividualA, string IndividualB, string IndividualC, string IndividualD, string IndividualE, string IndividualF, string ScouringOrPitting, string SubstantialCorrosionRemaining, string StatutoryDeficiencies, string FoundSatisfactory, string UnderRemarks, string BottomPlattingScouringOrPitting);
        Task<ServiceResult<string>> UpdateReport(int projectId, int templateId, int reportPart, string value, int imonumber, int hullNo = 1, string surveyDate = null);
        Task<ServiceResult<object>> GetPhotoPlacementDropdownData(int projectId, int templateId, Guid sectionId);
    }
}
