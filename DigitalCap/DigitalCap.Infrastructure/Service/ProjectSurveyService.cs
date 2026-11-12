using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf.Streaming;
using ImageCards = DigitalCap.Core.Models.Survey.ImageCards;

namespace DigitalCap.Infrastructure.Service
{
    /// <summary>
    /// Project-template survey flow (MVC <c>SurveyController</c>) using <see cref="ProjectReport"/> / report parts.
    /// </summary>
    public partial class ProjectSurveyService : IProjectSurveyService
    {
        private readonly ILogger<ProjectSurveyService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserPermissionRepository _userPermissionRepository;
        private readonly ITransferDataOnlinetoOfflineRepository _transferDataOnlinetoOfflineRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IReportPartRepository _reportPartRepository;
        private readonly IReportPartService _reportPartService;
        private readonly ISurveyReportRepository _surveyReportRepository;
        private readonly ITankRepository _tankRepository;
        private readonly IProjectReportRepository _projectReportRepository;
        private readonly IDescriptionRepository _descriptionRepository;
        private readonly IProjectReportService _projectReportService;

        public ProjectSurveyService(
            ILogger<ProjectSurveyService> logger,
            IConfiguration configuration,
            IUserPermissionRepository userPermissionRepository,
            ITransferDataOnlinetoOfflineRepository transferDataOnlinetoOfflineRepository,
            IUserAccountRepository userAccountRepository,
            IProjectRepository projectRepository,
            IReportPartRepository reportPartRepository,
            IReportPartService reportPartService,
            ISurveyReportRepository surveyReportRepository,
            ITankRepository tankRepository,
            IProjectReportRepository projectReportRepository,
            IDescriptionRepository descriptionRepository, IProjectReportService projectReportService)
        {
            _logger = logger;
            _configuration = configuration;
            _userPermissionRepository = userPermissionRepository;
            _transferDataOnlinetoOfflineRepository = transferDataOnlinetoOfflineRepository;
            _userAccountRepository = userAccountRepository;
            _projectRepository = projectRepository;
            _reportPartRepository = reportPartRepository;
            _reportPartService = reportPartService;
            _surveyReportRepository = surveyReportRepository;
            _tankRepository = tankRepository;
            _projectReportRepository = projectReportRepository;
            _descriptionRepository = descriptionRepository;
            _projectReportService = projectReportService;
        }

        public async Task<ServiceResult<ProjectReportViewModel>> GetSurveyIndexAsync(int projectId, string? currentUserName, int? templateSectionId = null)
        {
            if (string.IsNullOrWhiteSpace(currentUserName))
                return ServiceResult<ProjectReportViewModel>.Failure("Invalid user");

            var defaultTemplateProjectId = _configuration.GetValue<int>("DefaultTemplateProjectId");
            if (projectId < defaultTemplateProjectId)
            {
                return ServiceResult<ProjectReportViewModel>.Failure(
                    "This project uses the legacy survey report. Use the SurveyReport API for this project id.");
            }

            var reportAccess = await _userPermissionRepository.GetRolePermissionByUserName(
                currentUserName,
                EnumExtensions.GetDescription(ManagePages.ReportEdit),
                projectId);

            if (!(Convert.ToBoolean(reportAccess?.Edit) || Convert.ToBoolean(reportAccess?.Read)))
                return ServiceResult<ProjectReportViewModel>.Failure("Access denied");

            var isSync = await _transferDataOnlinetoOfflineRepository.GetDownloadOfflineProjects(projectId);
            if (isSync != null && !string.IsNullOrEmpty(isSync.UserId))
            {
                var userAccount = await _userAccountRepository.GetByAspNetId(isSync.UserId);
                if (userAccount != null)
                    isSync.Name = $"{userAccount.FirstName} {userAccount.LastName}".Trim();
            }

            var project = await _projectRepository.GetProject(projectId);
            if (project == null)
                return ServiceResult<ProjectReportViewModel>.Failure("Project not found");

            var imo = Convert.ToInt32(project.IMO ?? 0);
            var model = await PrepareProjectTemplateViewModelAsync(projectId, null, imo);

            model.Imonumber = project.IMO;
            model.ReportExport = (await _userPermissionRepository.GetRolePermissionByUserName(
                currentUserName, EnumExtensions.GetDescription(ManagePages.ReportExport), projectId))?.Edit;
            model.ReportExportAll = (await _userPermissionRepository.GetRolePermissionByUserName(
                currentUserName, EnumExtensions.GetDescription(ManagePages.ReportExportAll), projectId))?.Edit;
            model.ResetTemplate = (await _userPermissionRepository.GetRolePermissionByUserName(
                currentUserName, EnumExtensions.GetDescription(ManagePages.ResetTemplate), projectId))?.Edit;

            model.IsSynched = isSync?.IsSynched ?? true;
            if (model.Report != null)
                model.Report.IsSynched = model.IsSynched;
            model.SynchedOnline = isSync;

            return ServiceResult<ProjectReportViewModel>.Success(model);
        }

        public async Task<ServiceResult<ProjectReportViewModel>> IndexPartialAsync(int projectId, int? templateSectionId, int imo)
        {
            var isSync = await _transferDataOnlinetoOfflineRepository.GetDownloadOfflineProjects(projectId);

            if (templateSectionId.HasValue)
            {
                var pre = await PrepareProjectTemplateViewModelAsync(projectId, null, imo);
                var partRow = pre.ReportPartGrid?.FirstOrDefault(x => x.PartId == templateSectionId.Value);
                if (partRow != null)
                    await CreateProjectReportAsync(projectId, templateSectionId.Value);
            }

            var model = await PrepareProjectTemplateViewModelAsync(projectId, templateSectionId, imo);
            model.IsSynched = isSync?.IsSynched ?? true;
            if (model.Report != null)
            {
                model.Report.IsSynched = model.IsSynched;
                if (templateSectionId.HasValue)
                {
                    model.Report.ShowExportSettings = await _reportPartRepository.CheckToShowSectionExportConfig(
                        projectId, imo, templateSectionId.Value);
                }
            }

            return ServiceResult<ProjectReportViewModel>.Success(model);
        }

        public async Task<ServiceResult<ProjectExportSettings>> PrepareProjectExportSettingsAsync(int projectId, int? templateId, int imo)
        {
            var settings = new ProjectExportSettings();
            var parts = await _reportPartRepository.GetProjectExportPartConfiguration(projectId) ?? new List<ExportPart>();
            
            if (parts.Count == 0)
            {
                // Fallback to default Vessel Type configuration for old projects
                var project = await _projectRepository.GetProject(projectId);
                if (project != null && !string.IsNullOrEmpty(project.VesselType))
                {
                    var vesselTypesList = await _reportPartRepository.GetVesselTypes();
                    var match = vesselTypesList?.vesselTypes?.FirstOrDefault(x => string.Equals(x.VesselType, project.VesselType, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        var defaultParts = await _reportPartRepository.GetVesselTypeExportPartConfiguration(match.VesselTypeId) ?? new List<ExportPart>();
                        foreach (var part in defaultParts)
                        {
                            var exportPart = new ExportPart
                            {
                                PartId = part.PartId,
                                PartName = part.PartName,
                                RequiredInReport = part.RequiredInReport,
                                IncludeInReport = part.IncludeInReport,
                                sections = (await _reportPartRepository.GetVesselTypeExportSectionConfiguration(part.PartId, match.VesselTypeId))
                                    ?.Select(s => new ExportSections
                                    {
                                        SectionId = s.SectionId,
                                        SectionName = s.SectionName,
                                        RequiredInReport = s.RequiredInReport,
                                        IncludeInReport = s.IncludeInReport,
                                        TankTypeId = s.TankTypeId,
                                        IsSubSection = s.IsSubSection,
                                        ParentId = s.ParentId,
                                        TankType = s.TankType
                                    }).ToList() ?? new List<ExportSections>()
                            };
                            parts.Add(exportPart);
                        }
                    }
                }
            }

            if (templateId != null)
                parts = parts.Where(x => x.PartId == templateId).ToList();

            settings.exportParts = parts;
            foreach (var item in settings.exportParts)
            {
                if (item.sections == null || item.sections.Count == 0)
                {
                    item.sections = await _reportPartRepository.GetProjectExportSectionConfiguration(item.PartId, projectId)
                                      ?? new List<ExportSections>();
                }
            }

            return ServiceResult<ProjectExportSettings>.Success(settings);
        }

        public async Task<ServiceResult<bool>> CheckShowExportSettingsAsync(int projectId, int imo, int templateId)
        {
            var show = await _reportPartRepository.CheckToShowSectionExportConfig(projectId, imo, templateId);
            return ServiceResult<bool>.Success(show);
        }

        public async Task<ServiceResult<bool>> CheckShowExportAllSettingsAsync(int projectId, int imo)
        {
            var parts = await _reportPartRepository.GetProjectExportPartConfiguration(projectId) ?? new List<ExportPart>();
            var any = false;
            foreach (var p in parts)
            {
                if (await _reportPartRepository.CheckToShowSectionExportConfig(projectId, imo, p.PartId))
                {
                    any = true;
                    break;
                }
            }

            return ServiceResult<bool>.Success(any);
        }

        public async Task<ServiceResult<List<ExportSections>>> GetAllSectionsAsync(int projectId, int templateId)
        {
            var list = await _reportPartRepository.GetProjectExportSectionConfiguration(templateId, projectId)
                       ?? new List<ExportSections>();
            return ServiceResult<List<ExportSections>>.Success(list);
        }

        public async Task<ServiceResult<List<ExportPart>>> GetAllPartsAsync(int projectId, int imo)
        {
            var settings = await PrepareProjectExportSettingsAsync(projectId, null, imo);
            if (!settings.IsSuccess || settings.Data == null)
                return ServiceResult<List<ExportPart>>.Failure(settings.ErrorMessage ?? "Failed to load parts");
            return ServiceResult<List<ExportPart>>.Success(settings.Data.exportParts ?? new List<ExportPart>());
        }

        public async Task<ServiceResult<bool>> CreateProjectReportAsync(int projectId, int templateSectionId)
        {
            try
            {
                var templatesections = await _surveyReportRepository.GetTemplatSections(templateSectionId);
                foreach (var section in templatesections)
                {
                    var projectReportMapping = new ProjectReportMapping
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        TemplateId = templateSectionId,
                        IsDeleted = false,
                        CreatedDttm = DateTime.Now,
                        SectionId = section.Id,
                        UpdateDttm = DateTime.Now
                    };
                    await _surveyReportRepository.CreateProjectTemplate(projectReportMapping);
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateProjectReportAsync failed for project {ProjectId}, template {TemplateId}", projectId, templateSectionId);
                return ServiceResult<bool>.Failure("Failed to create project report template");
            }
        }

        private async Task<ProjectReportViewModel> PrepareProjectTemplateViewModelAsync(int projectId, int? templateSectionId, int imonumber)
        {
            var model = await GetSectionIdsByProjectIdAsync(projectId, templateSectionId, imonumber);
            model.Report ??= new ProjectReport();
            model.Report.template ??= new ProjectView();

            model.ProjectName = await _projectRepository.GetProjectName(projectId);

            if (templateSectionId != null)
            {
                try
                {
                    List<string> sectionIds = null;

                    model.Report = await _projectReportService.GetReport(projectId, templateSectionId.Value, imonumber, sectionIds);
                    if (templateSectionId == 1)
                    {

                        model.Report.ProjectVesselType = _projectRepository.GetProjectVesselType(projectId).Result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "PrepareProjectTemplateViewModelAsync load report failed");
                }
            }
            else
            {
                model.UnplacedImages = await _surveyReportRepository.GetBulkUploadUnplacedImages();
            }

            return model;
        }

        private async Task<ProjectReportViewModel> GetSectionIdsByProjectIdAsync(int projectId, int? templateId, int? imo)
        {
            var model = new ProjectReportViewModel();
            model.ProjectId = projectId;
            model.Report = new ProjectReport();
            model.Report.template = new ProjectView();


            var allSectionTemplates = _projectReportRepository.GetProjectTemplatebyProjectId(projectId).Result;


            model.ReportPartGrid = new List<ProjectPart>();

            if (templateId != null && templateId != 0)
                allSectionTemplates = allSectionTemplates.Where(x => x.PartId == templateId.Value).ToList();
            foreach (var template in allSectionTemplates)
            {

                List<ProjectGradingSection> Sections = new List<ProjectGradingSection>();
                var sections = _reportPartRepository.GetNormalSectionByProjectId(projectId, template.PartId).Result;

                if (sections?.Count > 0)
                {
                    Sections.AddRange(sections.Select(x => new ProjectGradingSection() { SectionId = x.SectionMappingId, SectionName = x.SectionName, Order = x.Order }));
                    foreach (var section in sections)
                    {
                        if (section.HasSubsection)
                        {
                            var subsection = _reportPartRepository.GetProjectSubsectionBySectionId(section.SectionMappingId).Result;
                            Sections.AddRange(subsection.Select(x => new ProjectGradingSection() { SectionId = x.SectionMappingId, SectionName = x.SectionName, Order = x.Order, IsSubSection = true, TankTypeId = 0 }));
                        }
                    }
                }


                // Fetch tank sections for this part
                var tanks = _reportPartRepository.GetTankSectionByProjectId(projectId, template.PartId, Convert.ToInt32(imo)).Result;

                // Use max order of normal sections so tanks always sort AFTER normal sections.
                // Each tank gets a unique offset to preserve its own DB order.
                var max_normal_order = Sections.Count > 0 ? Sections.Max(x => x.Order) : 0;
                if (tanks?.Count > 0)
                {
                    for (int ti = 0; ti < tanks.Count; ti++)
                    {
                        Sections.Add(new ProjectGradingSection
                        {
                            SectionId = tanks[ti].Id,
                            SectionName = tanks[ti].TankName,
                            Order = max_normal_order + ti + 1,  // after all normal sections, in DB order
                            TankTypeId = tanks[ti].TankTypeId
                        });
                    }
                }


                var reportPart = new ProjectPart()
                {
                    PartId = template.PartId,
                    PartName = template.PartName,
                    SectionIds = Sections.OrderBy(x => x.Order).Select(x => x.SectionId).ToList(),
                    SectionsList = Sections.OrderBy(x => x.Order).ToList(),
                };
                model.ReportPartGrid.Add(reportPart);

            }
            return model;
        }
    }
}
