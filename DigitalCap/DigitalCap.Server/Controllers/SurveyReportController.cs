using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.Skylight;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.Security;
using DigitalCap.Core.ViewModels;
using DigitalCap.Infrastructure.Service;
using DigitalCap.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DigitalCap.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyReportController : ControllerBase
    {
        private readonly ISurveyReportService _surveyReportService;
        private readonly IConfiguration _configData;
        private readonly IConfiguration _configuration;
        private readonly IAsposePdfService _asposePdfService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SurveyReportController> _logger;
        private static readonly ConcurrentDictionary<string, ValidationJobStatus> _validationJobs = new();
        private static readonly ConcurrentDictionary<string, PdfJobStatus> _pdfJobs = new();
        public SurveyReportController(ISurveyReportService surveyReportService, IConfiguration configData, IConfiguration configuration, IAsposePdfService asposePdfService, IWebHostEnvironment webHostEnvironment, IServiceScopeFactory scopeFactory, ILogger<SurveyReportController> logger)
        {
            _surveyReportService = surveyReportService;
            _configData = configData;
            _configuration = configuration;
            _asposePdfService = asposePdfService;
            _webHostEnvironment = webHostEnvironment;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> SurveyReportIndex(int projectId, int? templateSectionId = null)
        {
            var currentUserName = User.FindFirstValue(ClaimTypes.Upn)
                ?? User.FindFirstValue(ClaimTypes.Name);

            var result = await _surveyReportService.GetIndexAsync(projectId, currentUserName, templateSectionId);

            if (!result.IsSuccess && result.ErrorMessage == "REDIRECT_TO_SURVEY")
            {
                return RedirectToAction("Index", "Survey", new { projectId = projectId });
            }

            if (!result.IsSuccess)
                return BadRequest(new { result.ErrorMessage, result.ErrorMessages, result.ErrorCode });
            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> IndexPartial(int projectId, int? templateSectionId = null)
        {
            var result = await _surveyReportService.IndexPartial(projectId, templateSectionId);

            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetIsElectron()
        {
            var isElectron = Convert.ToBoolean(_configData["IsElectron"]);
            var defaultProjectId = _configuration.GetValue<int>("DefaultTemplateProjectId");
            return Ok(new
            {
                isElectron = isElectron,
                defaultProjectId = defaultProjectId
            });
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetBulkUploadSectionDropdown(int projectId)
        {
            var result = await _surveyReportService.GetBulkUploadSectionDropdown(projectId);

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> DeleteReport(int projectId, int templateSectionId)
        {
            var result = await _surveyReportService.DeleteReport(projectId, templateSectionId);

            return Ok(result);
        }

        [HttpGet("[action]")]
        public async Task<int> GetUploadedImageCount(int projectId, int templateId, Guid sectionId)
        {
            var result = await _surveyReportService.GetUploadedImageCount(projectId, templateId, sectionId);

            return result;
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> RefreshSurveyStatus(int projectId)
        {
            var result = await _surveyReportService.RefreshCertificate(projectId);

            return Ok(result.Data);
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> RefreshCertificates(int projectId)
        {
            var result = await _surveyReportService.RefreshCertificate(projectId);
            return Ok(result.Data);
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetReport(int projectId, int templateSectionId)
        {
            try
            {
                List<string> sectionIds = new List<string>();
                try
                {
                    var sectionResult = await _surveyReportService
                        .GetSectionIdsByProjectId(projectId, templateSectionId);

                    if (sectionResult.IsSuccess && sectionResult.Data != null)
                    {
                        var reportPart = sectionResult.Data.ReportPartGrid
                            .FirstOrDefault(x =>
                                x.TemplateId == templateSectionId
                            );

                        sectionIds = reportPart?.SectionIds?
                            .Select(x => x.ToString())
                            .ToList()
                            ?? new List<string>();
                    }
                }
                catch
                {
                    // sectionIds empty — GetReport still works
                }

                var reportResult = await _surveyReportService.GetReport(projectId, templateSectionId, sectionIds);

                return Ok(reportResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("[action]")]
        public IActionResult StartExportAllValidation(int projectId)
        {
            var jobId = Guid.NewGuid().ToString();
            _validationJobs[jobId] = new ValidationJobStatus();
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                try
                {
                    var service = scope.ServiceProvider.GetRequiredService<ISurveyReportService>();
                    var result = await service.ExportAllValidation(projectId);
                    _validationJobs[jobId] = new ValidationJobStatus { Status = "Completed", Result = result.Data };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "StartExportAllValidation failed for project {ProjectId}", projectId);
                    _validationJobs[jobId] = new ValidationJobStatus { Status = "Failed", Error = ex.Message };
                }
            });
            return Ok(new { jobId, status = "Processing" });
        }

        [HttpPost("[action]")]
        public IActionResult StartExportAllProjectValidation([FromQuery] int projectId, [FromQuery] int imo, [FromBody] List<ExportPart> settings)
        {
            var jobId = Guid.NewGuid().ToString();
            var safeSettings = settings ?? new List<ExportPart>();
            _validationJobs[jobId] = new ValidationJobStatus();
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                try
                {
                    var service = scope.ServiceProvider.GetRequiredService<IProjectSurveyService>();
                    var result = await service.ExportAllProjectValidationAsync(projectId, imo, safeSettings);
                    _validationJobs[jobId] = result.IsSuccess
                        ? new ValidationJobStatus { Status = "Completed", Result = result.Data }
                        : new ValidationJobStatus { Status = "Failed", Error = result.ErrorMessage };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "StartExportAllProjectValidation failed for project {ProjectId}", projectId);
                    _validationJobs[jobId] = new ValidationJobStatus { Status = "Failed", Error = ex.Message };
                }
            });
            return Ok(new { jobId, status = "Processing" });
        }

        [HttpGet("[action]")]
        public IActionResult GetValidationStatus(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId) || !_validationJobs.TryGetValue(jobId, out var job))
                return Ok(new { status = "NotFound" });
            if (job.Status == "Completed")
            {
                _validationJobs.TryRemove(jobId, out _);
                return Ok(new { status = "Completed", result = job.Result });
            }
            if (job.Status == "Failed")
            {
                _validationJobs.TryRemove(jobId, out _);
                return Ok(new { status = "Failed", error = job.Error });
            }
            return Ok(new { status = "Processing" });
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ExportValidationPartial(int projectId, int sectionid)
        {
            var result = await _surveyReportService.ExportValidationPartial(projectId, sectionid);
            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> UpdateGenericImageDescriptionDropdownCard([FromForm] int templateId, [FromForm] int projectId, [FromForm] Guid sectionId, [FromForm] int cardId, [FromForm] int value, [FromForm] string cardName)
        {
            var result = await _surveyReportService.UpdateGenericImageDescriptionDropdownCard(templateId, projectId, sectionId, cardId, value, cardName);

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> UpdateGenericAdditionalDescription([FromForm] int templateId, [FromForm] int projectId, [FromForm] Guid sectionId, [FromForm] int cardId, [FromForm] string value, [FromForm] string cardName)
        {
            var result = await _surveyReportService.UpdateGenericAdditionalDescription(templateId, projectId, sectionId, cardId, value, cardName);
            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> UpdateGenericCurrentCondition([FromForm] int templateId, [FromForm] int projectId, [FromForm] Guid sectionId, [FromForm] int cardId, [FromForm] int value, [FromForm] string cardName)
        {
            var result = await _surveyReportService.UpdateGenericCurrentCondition(templateId, projectId, sectionId, cardId, value, cardName);
            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> UploadGenericImage(IFormFile file, [FromForm]  int templateId, [FromForm]  int projectId, [FromForm]  Guid sectionId, [FromForm]  int cardId, [FromForm]  int imageId, [FromForm]  bool replaceImage = false, [FromForm]  string cardName = null)
        {
            var result = await _surveyReportService.UploadGenericImage(file, templateId, projectId, sectionId, cardId, imageId, replaceImage, cardName);

            return Ok(result.Data);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> DeleteGenericImage(int templateId, int projectId, Guid sectionId, int cardId, int imageId)
        {
            var result = await _surveyReportService.DeleteGenericImage(templateId, projectId, sectionId, cardId, imageId);

            return Ok(result);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> UploadImage([FromForm] UploadImageDto dto)
        {
            var result = await _surveyReportService.UploadImage(
                dto.File,
                dto.TemplateId,
                dto.ProjectId,
                dto.SectionId,
                dto.CardId,
                dto.ImageId,
                dto.ReplaceImage
            );

            if (result.IsSuccess)
                return Ok(result.Data);

            return BadRequest(result.ErrorMessage);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> DeleteProjectImage([FromQuery] int templateId, [FromQuery] int projectId, [FromQuery] Guid sectionId, [FromQuery] int cardId, [FromQuery] int imageId)
        {
            var result = await _surveyReportService.DeleteImage(
                templateId,
                projectId,
                sectionId,
                cardId,
                imageId);

            return Ok(result);
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateImageDescriptionDropdownCard([FromBody] UpdateImageDescriptionDto dto)
        {
            var result = await _surveyReportService.UpdateImageDescriptionDropdownCard(dto.TemplateId, dto.ProjectId, dto.SectionId, dto.CardId, dto.Value);
            return Ok(result);
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateAdditionalDescription([FromBody] UpdateAdditionalDescriptionDto dto)
        {
            var result = await _surveyReportService.UpdateAdditionalDescription(dto.TemplateId, dto.ProjectId, dto.SectionId, dto.CardId, dto.Value);

            return Ok(result);
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateCurrentCondition([FromBody] UpdateCurrentConditionDto dto)
        {
            var result = await _surveyReportService.UpdateCurrentCondition(
                dto.TemplateId,
                dto.ProjectId,
                dto.SectionId,
                dto.CardId,
                dto.Value
            );

            return Ok(result);
        }
        //New Image Card Funcions

        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateSectionNote(int templateId, int projectId, Guid sectionId, string value)
        {
            var result = await _surveyReportService.UpdateSectionNote(templateId, projectId, sectionId, value);
            return Ok(result);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateGradingNotes([FromBody] UpdateNotesDto dto)
        {
            var result = await _surveyReportService.UpdateGradingNotes(
                dto.TemplateId,
                dto.ProjectId,
                dto.SectionId,
                dto.GradingId,
                dto.Value
            );

            return Ok(result);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateGradingCondition([FromBody] UpdateGradingDto dto)
        {
            var result = await _surveyReportService.UpdateGradingCondition(
                dto.TemplateId,
                dto.ProjectId,
                dto.SectionId,
                dto.GradingId,
                dto.Choices
            );

            return Ok(result);
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateH_IReport([FromBody] UpdateHIReportDto request)
        {
            var result = await _surveyReportService.UpdateH_IReport(
                request.Id,
                request.ProjectId,
                request.CompanyName,
                request.CertificateNumber,
                request.ExpiryDate ?? DateTime.MinValue,
                request.ThicknessMeasurementA,
                request.ThicknessMeasurementB,
                request.ThicknessMeasurementC,
                request.AdditionalSurveyorComments,
                request.MaintanceExecuted,
                request.Accommodation,
                request.PhotoGraphs,
                request.OperationalTestLocation,
                request.OperationalTestStart ?? DateTime.MinValue,
                request.OperationalTestFinish ?? DateTime.MinValue,
                request.OptionalTestSatisFactory,
                request.AssociatedReportNumber,
                request.AssociatedReportDate,
                request.Remarks,
                request.LeadSurveyorName,
                request.LeadSurveyorNumber,
                request.SurveyorName,
                request.SurveyorNumber,
                request.LastVisitDate ?? DateTime.MinValue,
                request.IndividualA,
                request.IndividualB,
                request.IndividualC,
                request.IndividualD,
                request.IndividualE,
                request.IndividualF,
                request.ScouringOrPitting,
                request.SubstantialCorrosionRemaining,
                request.StatutoryDeficiencies,
                request.FoundSatisfactory,
                request.UnderRemarks,
                request.BottomPlattingScouringOrPitting);

            return Ok(result);
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> UpdateReport([FromBody] UpdateReportRequest request)
        {
            var result = await _surveyReportService.UpdateReport(
                request.ProjectId,
                request.TemplateId,
                request.ReportPart,
                request.Value,
                request.Imonumber,
                request.HullNo,
                request.SurveyDate
            );

            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetPhotoPlacementDropdownData(int projectId, int templateId, Guid sectionId)
        {
            var result = await _surveyReportService.GetPhotoPlacementDropdownData(projectId, templateId, sectionId);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ExportCAPReportPdf(int projectId)
        {
            var (pdfBytes, fileName) = await _asposePdfService.GenerateCAPReportPdf(projectId);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return BadRequest("Failed to generate PDF");

            return File(pdfBytes, "application/pdf", $"{fileName}.pdf");
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> ExportPartPdf(int projectId, int templateId, [FromBody] List<ExportPart> selectedSettings = null)
        {
            var (pdfBytes, fileName) = await _asposePdfService.GeneratePartReportPdf(projectId, templateId, selectedSettings);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return BadRequest("Failed to generate PDF");

            return File(pdfBytes, "application/pdf", $"{fileName}_Part.pdf");
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ExportHIPdf(int projectId, int templateId)
        {
            var (pdfBytes, fileName) = await _asposePdfService.GenerateHIReportPdf(projectId, templateId);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return BadRequest("Failed to generate H&I PDF");

            return File(pdfBytes, "application/pdf", $"{fileName}.pdf");
        }

        [HttpPost("[action]")]
        public IActionResult StartExportFullPdf(int projectId, [FromBody] List<ExportPart> selectedSettings = null, int? templateSectionId = null)
        {
            // Read the user BEFORE the background task — HttpContext is not available later.
            var currentUserName = User.FindFirstValue(ClaimTypes.Upn) ?? User.FindFirstValue(ClaimTypes.Name);
            var jobId = Guid.NewGuid().ToString();
            _pdfJobs[jobId] = new PdfJobStatus();
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                try
                {
                    var pdfService = scope.ServiceProvider.GetRequiredService<IAsposePdfService>();
                    var (pdfBytes, fileName) = await pdfService.GenerateFullReportPdf(projectId, currentUserName, templateSectionId, selectedSettings);
                    _pdfJobs[jobId] = (pdfBytes == null || pdfBytes.Length == 0)
                        ? new PdfJobStatus { Status = "Failed", Error = "Failed to generate full PDF" }
                        : new PdfJobStatus { Status = "Completed", Bytes = pdfBytes, FileName = fileName };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "StartExportFullPdf failed for project {ProjectId}", projectId);
                    _pdfJobs[jobId] = new PdfJobStatus { Status = "Failed", Error = ex.Message };
                }
            });
            return Ok(new { jobId, status = "Processing" });
        }

        [HttpGet("[action]")]
        public IActionResult GetExportFullPdfStatus(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId) || !_pdfJobs.TryGetValue(jobId, out var job))
                return Ok(new { status = "NotFound" });
            if (job.Status == "Completed")
            {
                _pdfJobs.TryRemove(jobId, out _);
                return Ok(new { status = "Completed", base64 = Convert.ToBase64String(job.Bytes!), fileName = job.FileName });
            }
            if (job.Status == "Failed")
            {
                _pdfJobs.TryRemove(jobId, out _);
                return Ok(new { status = "Failed", error = job.Error });
            }
            return Ok(new { status = "Processing" });
        }
        [HttpGet("[action]")]
        public IActionResult GetConfig()
        {
            return Ok(new
            {
                defaultTemplateProjectId = _configuration.GetValue<int>("DefaultTemplateProjectId")
            });
        }
    }
}

