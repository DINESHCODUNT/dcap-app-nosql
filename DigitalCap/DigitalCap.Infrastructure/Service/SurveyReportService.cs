using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Helpers;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Skylight;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.ViewModels;
using DigitalCap.Infrastructure.Service;
using DigitalCap.WebApi.Models;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Telerik.Windows.Documents.Fixed.FormatProviders.Pdf.Streaming;
using Telerik.Windows.Documents.Fixed.Model.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Collections.Specialized.BitVector32;
using Vessel = DigitalCap.Core.Models.Survey.Vessel;

namespace DigitalCap.Infrastructure.Service
{
    public class SurveyReportService : ISurveyReportService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SurveyReportService> _logger;
        private readonly IUserPermissionRepository _userPermissionRepository;
        private readonly ITransferDataOnlinetoOfflineRepository _transferDataOnlinetoOfflineRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly ISecurityService _securityService;
        private readonly ISurveyReportRepository _surveyReportRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ITankRepository _tankRepository;
        private readonly IVesselRepository _vesselRepository;
        private readonly IDescriptionRepository _descriptionRepository;
        private readonly IFreedomAPIRepository _freedomAPIRepository;
        private readonly IProjectReportRepository _projectReportRepository;
        public SurveyReportService(ILogger<SurveyReportService> logger, IConfiguration configuration,
            IUserPermissionRepository userPermissionRepository, ITransferDataOnlinetoOfflineRepository transferDataOnlinetoOfflineRepository,
            IUserAccountRepository userAccountRepository, ISecurityService securityService,
            ISurveyReportRepository surveyReportRepository, IProjectRepository projectRepository,
            ITankRepository tankRepository, IVesselRepository vesselRepository, IDescriptionRepository descriptionRepository
            , IFreedomAPIRepository freedomAPIRepository, IProjectReportRepository projectReportRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _userPermissionRepository = userPermissionRepository;
            _transferDataOnlinetoOfflineRepository = transferDataOnlinetoOfflineRepository;
            _userAccountRepository = userAccountRepository;
            _securityService = securityService;
            _surveyReportRepository = surveyReportRepository;
            _projectRepository = projectRepository;
            _tankRepository = tankRepository;
            _vesselRepository = vesselRepository;
            _descriptionRepository = descriptionRepository;
            _freedomAPIRepository = freedomAPIRepository;
            _projectReportRepository = projectReportRepository;
        }
        public async Task<ServiceResult<SurveyReportViewModel>> GetIndexAsync(int projectId, string currentUserName, int? templateSectionId = null)
        {
            var defaultProjectId = _configuration.GetValue<int>("DefaultTemplateProjectId");

            if (projectId >= defaultProjectId)
            {
                return ServiceResult<SurveyReportViewModel>.Failure("REDIRECT_TO_SURVEY");
            }

            var reportEditPermission = await _userPermissionRepository
                .GetRolePermissionByUserName(currentUserName,
                    EnumExtensions.GetDescription(ManagePages.ReportEdit), projectId);

            if (!(Convert.ToBoolean(reportEditPermission?.Edit) ||
                  Convert.ToBoolean(reportEditPermission?.Read)))
            {
                return ServiceResult<SurveyReportViewModel>.Failure("Access Denied");
            }

            var isSync = await _transferDataOnlinetoOfflineRepository
                .GetDownloadOfflineProjects(projectId);

            if (isSync != null)
            {
                var userAccount = await _userAccountRepository
                    .GetByAspNetId(isSync.UserId);

                if (userAccount != null)
                {
                    isSync.Name = $"{userAccount.FirstName} {userAccount.LastName}";
                }
            }

            var model = await PrepareSurveyReportViewModel(projectId, templateSectionId);

            var exportTask = _userPermissionRepository.GetRolePermissionByUserName(
                currentUserName, EnumExtensions.GetDescription(ManagePages.ReportExport), projectId);

            var exportAllTask = _userPermissionRepository.GetRolePermissionByUserName(
                currentUserName, EnumExtensions.GetDescription(ManagePages.ReportExportAll), projectId);

            var resetTask = _userPermissionRepository.GetRolePermissionByUserName(
                currentUserName, EnumExtensions.GetDescription(ManagePages.ResetTemplate), projectId);

            await Task.WhenAll(exportTask, exportAllTask, resetTask);

            model.ReportExport = exportTask.Result?.Edit;
            model.ReportExportAll = exportAllTask.Result?.Edit;
            model.ResetTemplate = resetTask.Result?.Edit;

            var isSynched = isSync?.IsSynched ?? true;

            model.IsSynched = isSynched;

            if (model.Report != null)
            {
                model.Report.IsSynched = isSynched;
            }

            model.SynchedOnline = isSync;

            return ServiceResult<SurveyReportViewModel>.Success(model);
        }
        public async Task<ServiceResult<Report>> IndexPartial(int projectId, int? templateSectionId = null)
        {
            var surveyReport = await PrepareSurveyReportViewModel(projectId, null);
            var isReportExist = surveyReport.ReportPartGrid.Where(x => x.TemplateId == templateSectionId).Select(x => x.ReportPartExists).FirstOrDefault();
            var isSync = await _transferDataOnlinetoOfflineRepository.GetDownloadOfflineProjects(projectId);
            if (!isReportExist)
            {
                var isCreated = await CreateReport(projectId, templateSectionId.Value);
            }

            SurveyReportViewModel model = await PrepareSurveyReportViewModel(projectId, templateSectionId);
            model.IsSynched = isSync == null ? true : isSync.IsSynched;
            model.Report.IsSynched = isSync == null ? true : isSync.IsSynched;
            return ServiceResult<Report>.Success(model.Report);
        }

        private async Task<SurveyReportViewModel> PrepareSurveyReportViewModel(int projectId, int? templateSectionId)
        {
            var sectionResult =
                await GetSectionIdsByProjectId(projectId, templateSectionId);

            var model = sectionResult.Data;

            model.ProjectName =
                await _projectRepository.GetProjectName(projectId);

            if (templateSectionId.HasValue)
            {
                var reportPart = model.ReportPartGrid
                    .FirstOrDefault(x => x.TemplateId == templateSectionId.Value);

                if (reportPart != null)
                {
                    var sectionIds = reportPart.SectionIds
                        .Select(x => x.ToString())
                        .ToList();

                    model.Report = await GetReport(projectId, templateSectionId.Value, sectionIds);
                }
            }
            else
            {
                model.UnplacedImages = await _surveyReportRepository.GetBulkUploadUnplacedImages();
            }

            return model;
        }
        public async Task<ServiceResult<bool>> CreateReport(int projectId, int templateSectionId)
        {
            try
            {
                var project = await _projectRepository.GetProject(projectId);
                var templatesections = await _surveyReportRepository.GetTemplatSections(templateSectionId);
                foreach (var section in templatesections)
                {
                    ProjectReportMapping projectReportMapping = new ProjectReportMapping();
                    projectReportMapping.Id = Guid.NewGuid();
                    projectReportMapping.ProjectId = projectId;
                    projectReportMapping.TemplateId = templateSectionId;
                    projectReportMapping.IsDeleted = false;
                    projectReportMapping.CreatedDttm = DateTime.Now;
                    projectReportMapping.SectionId = section.Id;
                    projectReportMapping.UpdateDttm = DateTime.Now;
                    var result = await _surveyReportRepository.CreateProjectTemplate(projectReportMapping);
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("false");
            }

        }

        public async Task<bool> DeleteReport(int projectId, int templateSectionId)
        {

            var result = _surveyReportRepository.DeleteProjectTemplate(projectId, templateSectionId).Result;

            return result;
        }
        public async Task<ServiceResult<SurveyReportViewModel>> GetSectionIdsByProjectId(int projectId, int? templateId)
        {
            var model = new SurveyReportViewModel();
            model.ProjectId = projectId;
            model.Report = new Report();

            var existingReportParts = await _surveyReportRepository.GetExistingReportPartsSectionIds(projectId);
            var allSectionTemplates = await _surveyReportRepository.GetReportTemplateList();
            model.ReportPartGrid = new List<ReportPart>();
            var project = await _projectRepository.GetProject(projectId);
            if (project != null)
            {
                model.Imonumber = project.IMO ?? 0;
            }
            if (templateId != null)
                allSectionTemplates = allSectionTemplates.Where(x => x.Id == templateId.Value).ToList();
            foreach (var template in allSectionTemplates)
            {
                List<GradingSection> Sections = new List<GradingSection>();
                var sections = await _surveyReportRepository.GetTemplateSections(template.Id);
                if (template.Id == 2 || template.Id == 3 || template.Id == 4)
                {
                    sections = sections.Where(x => x.HasSubSection == false).ToList();
                    if (sections.Count > 0)
                        Sections.AddRange(sections.Select(x => new GradingSection() { SectionId = x.SectionId, SectionName = x.SectionName }));
                    var tanks = await _tankRepository.GetTemplateTanks(template.Id, project?.IMO.ToString(), null, projectId);
                    if (tanks != null)
                    {
                        Sections.AddRange(tanks.Select(x => new GradingSection { SectionId = x.Id, SectionName = x.TankName }).ToList());
                    }
                }
                else
                {
                    var leafSections = sections.Where(x => x.HasSubSection == false).ToList();
                    if (leafSections.Count > 0)
                        Sections.AddRange(leafSections.Select(x => new GradingSection { SectionId = x.SectionId, SectionName = x.SectionName }).ToList());
                    else
                        Sections.AddRange(sections.Select(x => new GradingSection { SectionId = x.Id, SectionName = x.SectionName }).ToList());
                }

                var reportPart = new ReportPart()
                {
                    TemplateId = template.Id,
                    Name = template.Name,
                    ReportPartExists = existingReportParts.Contains(template.Id),
                    SectionIds = Sections.Select(x => x.SectionId).ToList(),
                    SectionsList = Sections
                };
                model.ReportPartGrid.Add(reportPart);
            }
            return ServiceResult<SurveyReportViewModel>.Success(model);
        }

        public async Task<Report> GetReport(int projectId, int templateSectionId, List<string> sectionIds)
        {
            try
            {
                var retApp = new Report();
                retApp.ProjectName = await _projectRepository.GetProjectName(projectId);
                retApp.Vessel = new Core.Models.Survey.Vessel();

                var ReportVessel = new ReportVesselMainData();
                //Get CERTIFICATE USING CLASSNUMBER
                try
                {
                    ReportVessel = (await _vesselRepository.GetVesselMainData(projectId)).First();
                }
                catch (Exception ex)
                { }

                if (ReportVessel != null)
                {
                    retApp.ReportPort = ReportVessel.reportPort;
                    retApp.ReportDate = ReportVessel.portDate != null && Convert.ToDateTime(ReportVessel.portDate) != DateTime.MinValue
                                      ? Convert.ToDateTime(ReportVessel.portDate).ToShortDateString()
                                      : DateTime.Now.ToString("dd-MM-yyy");

                    retApp.ReportNo = ReportVessel.reportNo;
                    retApp.Vessel.ReportNo = ReportVessel.reportNo;

                    if (ReportVessel.actualReportStartDate != null
                        && Convert.ToDateTime(ReportVessel.actualReportStartDate).Date != DateTime.MinValue.Date)
                    {
                        retApp.Vessel.ActualReportStartDate = Convert.ToDateTime(ReportVessel.actualReportStartDate);
                    }

                    retApp.ReportPort = ReportVessel.reportPort;
                    retApp.Vessel.Classed = ReportVessel.classed;
                    retApp.Vessel.VesselName = ReportVessel.vesselName;
                    retApp.Vessel.VesselID = ReportVessel.vesselID;
                    retApp.Vessel.ImoNumber = ReportVessel.imoNumber;
                    retApp.Vessel.VesselType = ReportVessel.vesselType;
                    retApp.Vessel.YearBuilt = Convert.ToInt32(ReportVessel.yearBuilt);
                    retApp.Vessel.BuiltBy = ReportVessel.builtBy;
                    retApp.Vessel.HullNo = ReportVessel.hullNo;
                    retApp.Vessel.FlagName = ReportVessel.flagName;
                    retApp.Vessel.Homeport = ReportVessel.homeport;
                    retApp.Vessel.OfficalNumber = ReportVessel.officalNumber;
                    retApp.Vessel.CallSign = ReportVessel.callSign;
                    retApp.Vessel.PreviousName = ReportVessel.previousName;
                    retApp.Vessel.OwnerName = ReportVessel.ownerName;
                    retApp.Vessel.Manager = ReportVessel.manager;
                    retApp.Vessel.Length = ReportVessel.length != null ? float.Parse(ReportVessel.length) : 0;
                    retApp.Vessel.Breadth = ReportVessel.breadth != null ? float.Parse(ReportVessel.breadth) : 0;
                    retApp.Vessel.DEPTH = ReportVessel.depth != null ? float.Parse(ReportVessel.depth) : 0;
                    retApp.Vessel.Draft = ReportVessel.draft != null ? float.Parse(ReportVessel.draft) : 0;
                    retApp.Vessel.DEADWEIGHT = ReportVessel.deadweight != null ? ReportVessel.deadweight : null;
                    retApp.Vessel.GrossTons = ReportVessel.grossTons != null ? float.Parse(ReportVessel.grossTons) : 0;
                    retApp.Vessel.NetTons = ReportVessel.netTons != null ? float.Parse(ReportVessel.netTons) : 0;
                    retApp.Vessel.PropulsionType = ReportVessel.propulsionType;
                    retApp.Vessel.KW = ReportVessel.kw != null ? ReportVessel.kw : "";
                    retApp.Vessel.ShaftRPM = ReportVessel.shaftRPM != null ? float.Parse(ReportVessel.shaftRPM) : 0;
                    retApp.Vessel.PropulsionManufacturer = ReportVessel.propulsionManufacturer;
                    retApp.Vessel.GearManufacturer = ReportVessel.gearManufacturer;
                    retApp.Vessel.Classed = ReportVessel.classed;
                    retApp.Vessel.ClassNo = ReportVessel.classNo;
                    retApp.Vessel.ClassNotation = ReportVessel.classNotation;
                    retApp.Vessel.Machinery = ReportVessel.machinery;
                    retApp.Vessel.Other = ReportVessel.other;
                    retApp.Vessel.Environment = ReportVessel.environment;
                    retApp.Vessel.SelectedPrefix = ReportVessel.selectedPrefix.ToString();
                    retApp.Vessel.FirstVisitDate = ReportVessel.firstVisitDate;

                    retApp.Vessel.LastVisitDate = ReportVessel.lastVisitDate;
                }

                var surveyStatusTask = _vesselRepository.GetSurveyStatus(projectId);
                var certificatesTask = _vesselRepository.GetStatutoryCertificate(projectId);
                var projectDetailsTask = _projectRepository.GetProject(projectId);
                var templateTitleTask = _surveyReportRepository.GetTemplateTitle(templateSectionId);
                var currentCondTask = _surveyReportRepository.GetCurrentCondition();
                var imageDescTask = _descriptionRepository.GetImageDescriptionsByProjectId(projectId);
                var sectionsTask = _surveyReportRepository.GetTemplatSections(templateSectionId);

                await Task.WhenAll(
                    surveyStatusTask, certificatesTask, projectDetailsTask,
                    templateTitleTask, currentCondTask, imageDescTask, sectionsTask);

                var certificates = await certificatesTask;
                var projectydetails = await projectDetailsTask;
                var templateDetails = await templateTitleTask;
                var currentCondition = await currentCondTask;
                var imagedescriptions = await imageDescTask;
                var sections = (await sectionsTask).ToList();

                if (sections.Count == 0 && templateSectionId != 0)
                {
                    var dynReportResult = await GetReportBySectionId(projectId, templateSectionId, Guid.Empty, projectydetails?.IMO.ToString(), sectionIds);
                    if (dynReportResult.IsSuccess && dynReportResult.Data != null)
                    {
                        return dynReportResult.Data;
                    }
                }
                retApp.SurveyStatus = (await surveyStatusTask).FirstOrDefault();
                retApp.StatutoryCertificatesExpirationDates = certificates.StatutoryCertificatesExpirationDates;
                retApp.StatutoryCertificatesIssuedDates = certificates.StatutoryCertificatesIssuedDates;
                retApp.ProjectId = projectId;

                Core.Models.Survey.View newReport = new Core.Models.Survey.View();
                newReport.Id = templateDetails?.Id ?? templateSectionId;
                newReport.ProjectId = projectId;
                newReport.TemplateTitle = templateDetails?.SectionName ?? $"Part";
                newReport.handireport = new HandIReport();

                if (templateSectionId == 2 || templateSectionId == 3 || templateSectionId == 4)
                {
                    sections = sections.Where(x => x.HasSubSection == false).ToList();
                    retApp.tankReportView = new Core.Models.Tank.TankReportView
                    {
                        ProjectId = projectId,
                        TemplateId = templateSectionId,
                        TemplateName = templateDetails?.SectionName?.Replace("Part ", String.Empty) ?? "",
                        sectionStartCount = sections != null ? sections.Count() + 1 : 1,
                        Sections = new List<TankUI>(),
                        VesselType = projectydetails.VesselType
                    };

                    retApp.tankReportView.Sections = await _tankRepository.GetTemplateTanks(
                        templateSectionId, projectydetails?.IMO.ToString(), null, projectId);

                    var temp_Grading = await _tankRepository.GetTemplateTankGrading(0, projectId, projectydetails.VesselType);
                    var tankreportNotes = await _surveyReportRepository.GetProjectReportTemplate(projectId, templateSectionId);
                    var imageCardsTask = await _tankRepository.GetProjectTankImageCard(projectId, templateSectionId, Guid.Empty);
                    var temp_gradingNotes = await _surveyReportRepository.GetProjectGrading(projectId, templateSectionId, Guid.Empty);
                    var temp_gradingConditionMapTask = await _surveyReportRepository.GetProjectGradingConditionMapping(projectId, templateSectionId, Guid.Empty, 0);

                    // Step 1: assign Grading to all sections before building the condition task dictionary.
                    // GetTemplateTanks does not pre-populate Grading, so this must run first.
                    foreach (var section in retApp.tankReportView.Sections)
                    {
                        section.Grading = temp_Grading
                            .Where(x => x.TankTypeId == section.TankTypeId)
                            .Select(x => new TankGradingUI
                            {
                                Id = x.Id,
                                Name = x.Name,
                                TankTypeId = x.TankTypeId,
                                Value = x.Value,
                                SectionId = section.Id
                            })
                            .ToList();

                        section.GradingLabelName = new List<string>();
                    }

                    // Step 2: start all GetTemplateTankGradingCondition calls in parallel.
                    // Keyed by (SectionId, GradingId) tuple to prevent Distinct() from collapsing
                    // entries when multiple sections share the same template grading Id.
                    var allGradingConditionTasks =
                        retApp.tankReportView.Sections
                            .Where(s => s.Grading?.Count > 0)
                            .SelectMany(s => s.Grading.Select(g => new { SectionId = s.Id, Grading = g }))
                            .ToDictionary(
                                x => (x.SectionId, x.Grading.Id),
                                x => _tankRepository
                                .GetTemplateTankGradingCondition(x.Grading.Id));

                    await Task.WhenAll(allGradingConditionTasks.Values);

                    // Step 3: assign checkboxes, notes and condition mappings using pre-fetched data.
                    foreach (var section in retApp.tankReportView.Sections)
                    {
                        section.OrderNumber = sectionIds.IndexOf(section.Id.ToString()) + 1;

                        var tankReportNotes = tankreportNotes.FirstOrDefault(x => x.SectionId == section.Id);
                        if (tankReportNotes?.Notes != null)
                            section.SectionNotes = tankReportNotes.Notes;

                        const int defaultImageCardCount = 32;

                        var tankImageCards = imageCardsTask
                            .Where(x => x.SectionId == section.Id)
                            .OrderBy(x => x.CardNumber)
                            .ToList();

                        var cardPrefix =
                            $"{retApp.tankReportView.TemplateName}{section.OrderNumber}-";

                        // Always create/prepare cards from 1 to 32
                        for (int cardNumber = 1;
                             cardNumber <= defaultImageCardCount;
                             cardNumber++)
                        {
                            // Find existing DB record by CardNumber
                            var existingCard = tankImageCards
                                .FirstOrDefault(x => x.CardNumber == cardNumber);

                            if (existingCard != null)
                            {
                                // Existing DB record
                                // Keep the record, but generate CardName from
                                // TemplateName + Section OrderNumber + CardNumber

                                existingCard.CardName =
                                    $"{cardPrefix}{cardNumber}";

                                existingCard.SectionId = section.Id;

                                // Do not overwrite existing CurrentCondition
                                // Do not overwrite other DB values
                            }
                            else
                            {
                                // Card does not exist in DB -> create it

                                tankImageCards.Add(new TankImageCard
                                {
                                    SectionId = section.Id,

                                    // Example: B19-21
                                    CardName = $"{cardPrefix}{cardNumber}",

                                    // 1 to 32
                                    CardNumber = cardNumber,

                                    CurrentCondition = cardNumber <= 10 ? 0 : 1,

                                    IsActive = true
                                });
                            }
                        }

                        // Always sort 1 -> N
                        tankImageCards = tankImageCards
                            .OrderBy(x => x.CardNumber)
                            .ToList();

                        section.tankImageCards = tankImageCards;

                        var gradingNotes = temp_gradingNotes != null
                            ? temp_gradingNotes.Where(x => x.SectionId == section.Id).ToList()
                            : new List<ProjectGradingMapping>();

                        foreach (var grading in section.Grading)
                        {
                            grading.Checkbox = allGradingConditionTasks
                                .TryGetValue((section.Id, grading.Id), out var condTask)
                                    ? (await condTask).ToList()
                                    : new List<TankCheckBox>();

                            if (section.GradingLabelName.Count < grading.Checkbox.Count)
                                section.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();

                            if (gradingNotes?.Count > 0)
                                grading.Value = gradingNotes.FirstOrDefault(x => x.GradingId == grading.Id)?.Value;

                            var conditionMappings = temp_gradingConditionMapTask
                                .Where(x => x.SectionId == section.Id && x.GradingId == grading.Id)
                                .ToList();

                            if (grading.Checkbox?.Count > 0 && conditionMappings.Count > 0)
                            {
                                foreach (var checkbox in grading.Checkbox)
                                {
                                    var match = conditionMappings.FirstOrDefault(x => x.GradingConditionLabel == checkbox.Name);
                                    if (match != null && match.GradingConditionValue != null)
                                        checkbox.Value = Convert.ToBoolean(match.GradingConditionValue);
                                }
                            }
                        }

                        section.currentConditions = currentCondition;
                        section.imageDescriptions = imagedescriptions
                            .Where(x => x.TankTypeId == section.TankTypeId)
                            .Select(x => new ImageDescriptionUI { Id = x.Id, Description = x.Description })
                            .ToList();

                        section.tankImageCards = tankImageCards;
                    }
                }

                var gradings = await _surveyReportRepository.GetTemplatGradings(templateSectionId, projectId);
                var imageCards = await _surveyReportRepository.GetImageCard(templateSectionId);
                var gradingCondition = await _surveyReportRepository.GetGradingCondition(templateSectionId, projectId);

                ProjectReportMapping reportnotes = new ProjectReportMapping();
                List<ProjectGradingMapping> gradingnotes = new List<ProjectGradingMapping>();
                List<ProjectGradingConditionMapping> gradingconditions = new List<ProjectGradingConditionMapping>();
                List<ProjectCardMapping> projectcards = new List<ProjectCardMapping>();

                newReport.Sections = new List<Sections>();
                newReport.currentConditions = new List<CurrentCondition>();
                newReport.currentConditions = currentCondition;

                newReport.Sections.AddRange(
                    sections.GroupBy(x => x.SectionName)
                            .ToList()
                            .Select(x => new Sections
                            {
                                SectionName = x.Key,
                                Id = x.FirstOrDefault().SectionId,
                                subSections = new List<SubSection>()
                            })
                            .ToList());

                if (newReport.Sections != null)
                {
                    foreach (var item in newReport.Sections)
                    {
                        item.subSections.AddRange(
                            sections.Where(x => x.SectionId == item.Id)
                                    .Select(x => new SubSection
                                    {
                                        Id = x.Id,
                                        SubSectionName = x.SubSectionName,
                                        Grading = new List<Core.Models.Survey.Grading>(),
                                        ImageCards = new List<Core.Models.Survey.ImageCards>(),
                                        GradingLabelName = new List<String>()
                                    })
                                    .ToList());
                    }

                    foreach (var item in newReport.Sections)
                    {
                        if (newReport.TemplateTitle != "Part H & I")
                        {
                            foreach (var subitem in item.subSections)
                            {
                                subitem.imageDescriptions = new List<ImageDescriptionUI>();
                                subitem.imageDescriptions = imagedescriptions
                                    .Where(x => x.SectionId == subitem.Id)
                                    .Select(x => new ImageDescriptionUI { Id = x.Id, Description = x.Description })
                                    .ToList();

                                subitem.OrderNumber = sectionIds.IndexOf(subitem.Id.ToString()) + 1;

                                subitem.Grading.AddRange(
                                    gradings.Where(x => x.SectionId == subitem.Id)
                                            .Select(x => new Core.Models.Survey.Grading
                                            {
                                                Id = x.Id,
                                                Name = x.LabelName,
                                                Checkbox = new List<CheckBox>()
                                            })
                                            .ToList());

                                reportnotes = await _surveyReportRepository.GetProjectReportTemplate(projectId, templateSectionId, subitem.Id);
                                if (reportnotes != null && reportnotes.Notes != null)
                                    subitem.SectionNotes = reportnotes.Notes;

                                subitem.projectCards = await _surveyReportRepository.GetProjectImageCard(projectId, templateSectionId, subitem.Id);

                                if (imageCards != null && imageCards.Count > 0)
                                {
                                    subitem.ImageCards.AddRange(
                                        imageCards.Where(x => x.SectionId == subitem.Id)
                                                  .Select(x => new Core.Models.Survey.ImageCards
                                                  {
                                                      Id = x.Id,
                                                      CurrentCondition = x.CurrentCondition,
                                                      DescriptionOptions = new List<Description>(),
                                                      FileName = x.FileName,
                                                      CurrentConditionOptions = new List<CurrentCondition>()
                                                  })
                                                  .ToList());

                                    subitem.ImageCards = subitem.ImageCards
                                        .OrderBy(x => Convert.ToInt16(x.FileName.Substring(x.FileName.IndexOf("-") + 1)))
                                        .ToList();

                                }

                                if (gradingCondition != null && gradingCondition.Count > 0)
                                {
                                    gradingnotes = await _surveyReportRepository.GetProjectGrading(projectId, templateSectionId, subitem.Id);

                                    foreach (var grading in subitem.Grading)
                                    {
                                        gradingconditions = await _surveyReportRepository.GetProjectGradingConditionMapping(
                                            projectId, templateSectionId, subitem.Id, grading.Id);

                                        grading.Checkbox.AddRange(
                                            gradingCondition.Where(x => x.GradingId == grading.Id && x.SectionId == subitem.Id)
                                                            .Select(x => new CheckBox { Id = x.Id, Name = x.Condition })
                                                            .ToList());

                                        if (subitem.GradingLabelName.Count < grading.Checkbox.Count)
                                            subitem.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();

                                        if (grading.Checkbox != null && grading.Checkbox.Count > 0)
                                        {
                                            if (gradingconditions != null && gradingconditions.Count > 0)
                                            {
                                                foreach (var gradingcheckbox in grading.Checkbox)
                                                {
                                                    gradingcheckbox.Value = gradingconditions
                                                        .Where(x => x.GradingConditionLabel == gradingcheckbox.Name)
                                                        .Select(x => x.GradingConditionValue)
                                                        .FirstOrDefault();
                                                }
                                            }
                                        }

                                        if (gradingnotes != null && gradingnotes.Count > 0)
                                        {
                                            grading.Value = gradingnotes
                                                .Where(x => x.GradingId == grading.Id)
                                                .Select(x => x.Value)
                                                .FirstOrDefault();
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            newReport.handireport = await _surveyReportRepository.GetHIReport(projectId);
                            if (newReport.handireport == null)
                                newReport.handireport = new HandIReport();

                            newReport.handireport.sectionOptions = new List<Options>()
                    {
                        new Options { Label = "Yes",    Value = "Yes" },
                        new Options { Label = "No",     Value = "No" },
                        new Options { Label = "Others", Value = "Others" }
                    };
                            newReport.handireport.maintanenceOptions = new List<Options>()
                    {
                        new Options { Label = "Yes", Value = "Yes" },
                        new Options { Label = "No",  Value = "No" },
                        new Options { Label = "N/A", Value = "N/A" }
                    };

                            newReport.handireport.Grading = new List<Core.Models.Survey.Grading>();
                            newReport.handireport.GradingLabelName = new List<String>();
                            newReport.handireport.SectionId = newReport.Sections.FirstOrDefault().Id;

                            newReport.handireport.Grading.AddRange(
                                gradings.Where(x => x.SectionId == newReport.Sections.FirstOrDefault().Id)
                                        .Select(x => new Core.Models.Survey.Grading
                                        {
                                            Id = x.Id,
                                            Name = x.LabelName,
                                            Checkbox = new List<CheckBox>()
                                        })
                                        .ToList());

                            if (gradingCondition != null && gradingCondition.Count > 0)
                            {
                                gradingnotes = await _surveyReportRepository.GetProjectGrading(
                                    projectId, templateSectionId, newReport.handireport.SectionId);

                                foreach (var grading in newReport.handireport.Grading)
                                {
                                    gradingconditions = await _surveyReportRepository.GetProjectGradingConditionMapping(
                                        projectId, templateSectionId, newReport.handireport.SectionId, grading.Id);

                                    grading.Checkbox.AddRange(
                                        gradingCondition.Where(x => x.GradingId == grading.Id
                                                                 && x.SectionId == newReport.Sections.FirstOrDefault().Id)
                                                        .Select(x => new CheckBox { Id = x.Id, Name = x.Condition })
                                                        .ToList());

                                    if (newReport.handireport.GradingLabelName.Count < grading.Checkbox.Count)
                                        newReport.handireport.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();

                                    if (grading.Checkbox != null && grading.Checkbox.Count > 0)
                                    {
                                        if (gradingconditions != null && gradingconditions.Count > 0)
                                        {
                                            foreach (var gradingcheckbox in grading.Checkbox)
                                            {
                                                gradingcheckbox.Value = gradingconditions
                                                    .Where(x => x.GradingConditionLabel == gradingcheckbox.Name)
                                                    .Select(x => x.GradingConditionValue)
                                                    .FirstOrDefault();
                                            }
                                        }
                                    }

                                    if (gradingnotes != null && gradingnotes.Count > 0)
                                    {
                                        grading.Value = gradingnotes
                                            .Where(x => x.GradingId == grading.Id)
                                            .Select(x => x.Value)
                                            .FirstOrDefault();
                                    }
                                }
                            }
                        }
                    }
                }

                var sectionIdGroup = new List<GradingSection>();
                if (newReport.Sections != null && newReport.Sections.Count > 0)
                {
                    sectionIdGroup.AddRange(
                        newReport.Sections
                                 .Select(x => new GradingSection { SectionId = x.Id, SectionName = x.SectionName })
                                 .ToList());
                }
                if (retApp.tankReportView?.Sections?.Count > 0)
                {
                    sectionIdGroup.AddRange(
                        retApp.tankReportView.Sections
                              .Select(x => new GradingSection { SectionId = x.Id, SectionName = x.TankName })
                              .ToList());
                }

                retApp.template = newReport;
                retApp.sections = sectionIdGroup;
                return retApp;
            }
            catch (SqlException sqlEx)
            {

            }
            return new Report();
        }
        public async Task<ServiceResult<List<BulkUploadViewModel>>> GetBulkUploadSectionDropdown(int projectId)
        {
            var sectionResult = await GetSectionIdsByProjectId(projectId, null);

            if (!sectionResult.IsSuccess || sectionResult.Data == null || sectionResult.Data.ReportPartGrid == null)
            {
                return ServiceResult<List<BulkUploadViewModel>>.Failure("Failed to load sections");
            }

            var result = new List<BulkUploadViewModel>();

            foreach (var obj in sectionResult.Data.ReportPartGrid)
            {
                if (obj.TemplateId != 0 && obj.TemplateId != 8 && obj.SectionsList != null)
                {
                    foreach (var section in obj.SectionsList)
                    {
                        result.Add(new BulkUploadViewModel
                        {
                            Id = $"{obj.TemplateId}:{section.SectionId}",
                            Name = $"{obj.Name} : {section.SectionName}"
                        });
                    }
                }
            }

            return ServiceResult<List<BulkUploadViewModel>>.Success(result);
        }

        public async Task<ServiceResult<Report>> GetReportBySectionId(int projectId, int templateSectionId, Guid sectionId)
        {
            var imo = await _projectRepository.GetIMONumberByProjectId(projectId);
            var sectionResult = await GetSectionIdsByProjectId(projectId, templateSectionId);
            if (!sectionResult.IsSuccess || sectionResult.Data?.ReportPartGrid == null)
                return ServiceResult<Report>.Failure("Failed to get section ids");
            var reportPart = sectionResult.Data.ReportPartGrid.FirstOrDefault(x => x.TemplateId == templateSectionId);
            if (reportPart == null)
                return ServiceResult<Report>.Failure("Template section not found");
            var sectionIds = reportPart.SectionIds.Select(x => x.ToString()).ToList();
            return await GetReportBySectionId(projectId, templateSectionId, sectionId, imo, sectionIds);
        }

        public async Task<ServiceResult<Report>> GetReportBySectionId(int projectId, int templateSectionId, Guid SectionId, string imoNumber, List<string> SectionIds)
        {
            try
            {
                var retApp = new Report();

                retApp.ProjectName = await _projectRepository.GetProjectName(projectId);

                retApp.Vessel = new Vessel();
                var ReportVessel = new ReportVesselMainData();
                //Get CERTIFICATE USING CLASSNUMBER
                try
                {
                    ReportVessel = _vesselRepository.GetVesselMainData(projectId).Result.First();
                }
                catch (Exception ex)
                { }
                if (ReportVessel != null)
                {
                    retApp.ReportPort = ReportVessel.reportPort;
                    retApp.ReportDate = ReportVessel.portDate != null && Convert.ToDateTime(ReportVessel.portDate) != DateTime.MinValue ? Convert.ToDateTime(ReportVessel.portDate).ToShortDateString() : DateTime.Now.ToString("dd-MM-yyy");

                    retApp.ReportNo = ReportVessel.reportNo;
                    retApp.Vessel.ReportNo = ReportVessel.reportNo;
                    if (ReportVessel.actualReportStartDate != null && Convert.ToDateTime(ReportVessel.actualReportStartDate).Date != DateTime.MinValue.Date)
                    {
                        retApp.Vessel.ActualReportStartDate = Convert.ToDateTime(ReportVessel.actualReportStartDate);
                    }
                    retApp.ReportPort = ReportVessel.reportPort;
                    retApp.Vessel.Classed = ReportVessel.classed;
                    retApp.Vessel.VesselName = ReportVessel.vesselName;
                    retApp.Vessel.VesselID = ReportVessel.vesselID;
                    retApp.Vessel.ImoNumber = ReportVessel.imoNumber;
                    retApp.Vessel.VesselType = ReportVessel.vesselType;
                    retApp.Vessel.YearBuilt = Convert.ToInt32(ReportVessel.yearBuilt);
                    retApp.Vessel.BuiltBy = ReportVessel.builtBy;
                    retApp.Vessel.HullNo = ReportVessel.hullNo;
                    retApp.Vessel.FlagName = ReportVessel.flagName;
                    retApp.Vessel.Homeport = ReportVessel.homeport;
                    retApp.Vessel.OfficalNumber = ReportVessel.officalNumber;
                    retApp.Vessel.CallSign = ReportVessel.callSign;
                    retApp.Vessel.PreviousName = ReportVessel.previousName;
                    retApp.Vessel.OwnerName = ReportVessel.ownerName;
                    retApp.Vessel.Manager = ReportVessel.manager;
                    retApp.Vessel.Length = ReportVessel.length != null ? float.Parse(ReportVessel.length) : 0;
                    retApp.Vessel.Breadth = ReportVessel.breadth != null ? float.Parse(ReportVessel.breadth) : 0;
                    retApp.Vessel.DEPTH = ReportVessel.depth != null ? float.Parse(ReportVessel.depth) : 0;
                    retApp.Vessel.Draft = ReportVessel.draft != null ? float.Parse(ReportVessel.draft) : 0;
                    retApp.Vessel.DEADWEIGHT = ReportVessel.deadweight != null ? ReportVessel.deadweight : null;
                    retApp.Vessel.GrossTons = ReportVessel.grossTons != null ? float.Parse(ReportVessel.grossTons) : 0;
                    retApp.Vessel.NetTons = ReportVessel.netTons != null ? float.Parse(ReportVessel.netTons) : 0;
                    retApp.Vessel.PropulsionType = ReportVessel.propulsionType;
                    retApp.Vessel.KW = ReportVessel.kw != null ? ReportVessel.kw : "";
                    retApp.Vessel.ShaftRPM = ReportVessel.shaftRPM != null ? float.Parse(ReportVessel.shaftRPM) : 0;
                    retApp.Vessel.PropulsionManufacturer = ReportVessel.propulsionManufacturer;
                    retApp.Vessel.GearManufacturer = ReportVessel.gearManufacturer;
                    retApp.Vessel.Classed = ReportVessel.classed;
                    retApp.Vessel.ClassNo = ReportVessel.classNo;
                    retApp.Vessel.ClassNotation = ReportVessel.classNotation;
                    retApp.Vessel.Machinery = ReportVessel.machinery;
                    retApp.Vessel.Other = ReportVessel.other;
                    retApp.Vessel.Environment = ReportVessel.environment;
                    retApp.Vessel.SelectedPrefix = ReportVessel.selectedPrefix.ToString();
                    retApp.Vessel.FirstVisitDate = ReportVessel.firstVisitDate != null ? DateTime.ParseExact(ReportVessel.firstVisitDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None).ToString("dd-MMM-yyyy") : "";

                    retApp.Vessel.LastVisitDate = ReportVessel.lastVisitDate != null ? DateTime.ParseExact(ReportVessel.lastVisitDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None).ToString("dd-MMM-yyyy") : "";

                }

                retApp.SurveyStatus = _vesselRepository.GetSurveyStatus(projectId).Result.FirstOrDefault();
                var certificates = _vesselRepository.GetStatutoryCertificate(projectId).Result;
                retApp.StatutoryCertificatesExpirationDates = certificates.StatutoryCertificatesExpirationDates;
                retApp.StatutoryCertificatesIssuedDates = certificates.StatutoryCertificatesIssuedDates;
                retApp.ProjectId = projectId;
                var projectydetails = _projectRepository.GetProject(projectId).Result;
                //********GetReportTemplate*******

                Core.Models.Survey.View newReport = new Core.Models.Survey.View();

                var templateDetails = _surveyReportRepository.GetTemplateTitle(templateSectionId).Result;
                newReport.Id = templateDetails?.Id ?? templateSectionId;
                newReport.ProjectId = projectId;
                newReport.TemplateTitle = templateDetails?.SectionName ?? $"Part";
                newReport.handireport = new HandIReport();
                var currentCondition = _surveyReportRepository.GetCurrentCondition().Result;
                var imageDescription = _descriptionRepository.GetImageDescriptionsByProjectId(projectId).Result;

                var sections = _surveyReportRepository.GetTemplatSections(templateSectionId).Result;
                if (templateSectionId == 2 || templateSectionId == 3 || templateSectionId == 4)
                {
                    sections = sections.Where(x => x.HasSubSection == false).ToList();
                    retApp.tankReportView = new Core.Models.Tank.TankReportView();
                    retApp.tankReportView.ProjectId = projectId;
                    retApp.tankReportView.TemplateId = templateSectionId;
                    retApp.tankReportView.TemplateName = templateDetails?.SectionName?.Replace("Part ", String.Empty) ?? "";
                    retApp.tankReportView.sectionStartCount = sections != null ? sections.Count() + 1 : 1;
                    retApp.tankReportView.Sections = new List<TankUI>();
                    retApp.tankReportView.Sections = _tankRepository.GetTemplateTanks(templateSectionId, projectydetails?.IMO.ToString(), null, projectId).Result;
                    if (SectionId != Guid.Empty)
                    {
                        retApp.tankReportView.Sections = retApp.tankReportView.Sections.Where(x => x.Id == SectionId).ToList();
                    }
                    foreach (var section in retApp.tankReportView.Sections)
                    {

                        section.OrderNumber = SectionIds.IndexOf(section.Id.ToString()) + 1;
                        section.Grading = new List<TankGradingUI>();
                        section.Grading = _tankRepository.GetTemplateTankGrading(section.TankTypeId, projectId, projectydetails.VesselType).Result;
                        section.GradingLabelName = new List<String>();

                        var tankreportnotes = _surveyReportRepository.GetProjectReportTemplate(projectId, templateSectionId, section.Id)?.Result;
                        if (tankreportnotes != null && tankreportnotes.Notes != null)
                        {
                            section.SectionNotes = tankreportnotes.Notes;
                        }
                        foreach (var grading in section.Grading)
                        {
                            var templategradingnotes = _surveyReportRepository.GetProjectGrading(projectId, templateSectionId, section.Id)?.Result;
                            grading.Checkbox = new List<TankCheckBox>();
                            grading.Checkbox = _tankRepository.GetTemplateTankGradingCondition(grading.Id).Result;
                            if (section.GradingLabelName.Count < grading.Checkbox.Count)
                            {
                                section.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();
                            }
                            var templategradingconditions = _surveyReportRepository.GetProjectGradingConditionMapping(projectId, templateSectionId, section.Id, grading.Id)?.Result;

                            if (templategradingnotes != null && templategradingnotes.Count > 0)
                            {
                                grading.Value = templategradingnotes.Where(x => x.GradingId == grading.Id).Select(x => x.Value).FirstOrDefault();
                            }

                            if (grading.Checkbox != null && grading.Checkbox.Count > 0)
                            {
                                if (templategradingconditions != null && templategradingconditions.Count > 0)
                                {
                                    foreach (var gradingcheckbox in grading.Checkbox)
                                    {
                                        gradingcheckbox.Value = templategradingconditions.Where(x => x.GradingConditionLabel == gradingcheckbox.Name).Select(x => x.GradingConditionValue).FirstOrDefault();
                                    }
                                }
                            }
                        }
                        section.currentConditions = new List<CurrentCondition>();
                        section.currentConditions = currentCondition;
                        section.imageDescriptions = new List<ImageDescriptionUI>();
                        section.imageDescriptions.AddRange(imageDescription.Where(x => x.TankTypeId == section.TankTypeId).Select(x => new ImageDescriptionUI { Id = x.Id, Description = x.Description }).ToList());
                        
                        var existingImageCards = await _tankRepository.GetProjectTankImageCard(projectId, templateSectionId, section.Id);
                        var tankImageCards = existingImageCards.OrderBy(x => x.CardNumber).ToList();
                        var cardPrefix = $"{retApp.tankReportView.TemplateName}{section.OrderNumber}-";
                        const int defaultImageCardCount = 32;

                        for (int cardNumber = 1; cardNumber <= defaultImageCardCount; cardNumber++)
                        {
                            var existingCard = tankImageCards.FirstOrDefault(x => x.CardNumber == cardNumber);
                            if (existingCard != null)
                            {
                                existingCard.CardName = $"{cardPrefix}{cardNumber}";
                                existingCard.SectionId = section.Id;
                            }
                            else
                            {
                                tankImageCards.Add(new TankImageCard
                                {
                                    SectionId = section.Id,
                                    CardName = $"{cardPrefix}{cardNumber}",
                                    CardNumber = cardNumber,
                                    CurrentCondition = cardNumber <= 10 ? 0 : 1,
                                    IsActive = true
                                });
                            }
                        }

                        section.tankImageCards = tankImageCards
                            .OrderBy(x => x.CardNumber)
                            .ToList();

                    }
                }

                var gradings = _surveyReportRepository.GetTemplatGradings(templateSectionId, projectId).Result;
                var imageCards = _surveyReportRepository.GetImageCard(templateSectionId).Result;

                var gradingCondition = _surveyReportRepository.GetGradingCondition(templateSectionId, projectId).Result;

                ProjectReportMapping reportnotes = new ProjectReportMapping();
                List<ProjectGradingMapping> gradingnotes = new List<ProjectGradingMapping>();
                List<ProjectGradingConditionMapping> gradingconditions = new List<ProjectGradingConditionMapping>();
                List<ProjectCardMapping> projectcards = new List<ProjectCardMapping>();

                newReport.Sections = new List<Sections>();
                newReport.currentConditions = new List<CurrentCondition>();
                newReport.currentConditions = currentCondition;

                newReport.Sections.AddRange(sections.GroupBy(x => x.SectionName).ToList().Select(x => new Sections { SectionName = x.Key, Id = x.FirstOrDefault().SectionId, subSections = new List<SubSection>(), SpecialSectionName = x.FirstOrDefault().SpecialSectionName }).ToList());
                newReport.Sections = newReport.Sections.Where(x => x.Id == SectionId).ToList();
                if (SectionId.ToString().ToLower() != "00b4955b-2cda-4605-af94-555b38fbcda0")
                {

                    if (newReport.Sections != null)
                    {
                        foreach (var item in newReport.Sections)
                        {
                            item.subSections.AddRange(sections.Where(x => x.SectionId == item.Id).Select(x => new SubSection { Id = x.Id, SubSectionName = x.SubSectionName, Grading = new List<Core.Models.Survey.Grading>(), ImageCards = new List<Core.Models.Survey.ImageCards>(), GradingLabelName = new List<String>(), SpecialSectionName = x.SpecialSectionName }).ToList());

                        }
                        foreach (var item in newReport.Sections)
                        {

                            if (newReport.TemplateTitle != "Part H & I")
                            {

                                foreach (var subitem in item.subSections)
                                {
                                    Guid specialSectionId = Guid.Empty;
                                    if (templateSectionId != 6)
                                    {
                                        subitem.OrderNumber = SectionIds.IndexOf(subitem.Id.ToString()) + 1;
                                    }
                                    else
                                    {
                                        subitem.OrderNumber = 2;
                                    }

                                    subitem.Grading.AddRange(gradings.Where(x => x.SectionId == subitem.Id).Select(x => new Core.Models.Survey.Grading { Id = x.Id, Name = x.LabelName, Checkbox = new List<CheckBox>() }).ToList());
                                    reportnotes = _surveyReportRepository.GetProjectReportTemplate(projectId, templateSectionId, subitem.Id)?.Result;
                                    subitem.imageDescriptions = new List<ImageDescriptionUI>();
                                    subitem.imageDescriptions.AddRange(imageDescription.Where(x => x.SectionId == subitem.Id).Select(x => new ImageDescriptionUI { Id = x.Id, Description = x.Description }).ToList());
                                    if (reportnotes != null && reportnotes.Notes != null)
                                    {
                                        subitem.SectionNotes = reportnotes.Notes;
                                    }

                                    subitem.projectCards = _surveyReportRepository.GetProjectImageCard(projectId, templateSectionId, subitem.Id)?.Result;
                                    if (imageCards != null && imageCards.Count > 0)
                                    {

                                        subitem.ImageCards.AddRange(imageCards.Where(x => x.SectionId == subitem.Id).Select(x => new Core.Models.Survey.ImageCards { Id = x.Id, CurrentCondition = x.CurrentCondition, DescriptionOptions = new List<Description>(), FileName = x.FileName, CurrentConditionOptions = new List<CurrentCondition>() }).ToList());
                                        subitem.ImageCards = subitem.ImageCards.OrderBy(x => Convert.ToInt16(x.FileName.Substring(x.FileName.IndexOf("-") + 1))).ToList();

                                    }
                                    if (gradingCondition != null && gradingCondition.Count > 0)
                                    {
                                        gradingnotes = _surveyReportRepository.GetProjectGrading(projectId, templateSectionId, subitem.Id)?.Result;

                                        foreach (var grading in subitem.Grading)
                                        {
                                            gradingconditions = _surveyReportRepository.GetProjectGradingConditionMapping(projectId, templateSectionId, subitem.Id, grading.Id)?.Result;
                                            grading.Checkbox.AddRange(gradingCondition.Where(x => x.GradingId == grading.Id && x.SectionId == subitem.Id).Select(x => new CheckBox { Id = x.Id, Name = x.Condition }).ToList());
                                            if (subitem.GradingLabelName.Count < grading.Checkbox.Count)
                                            {
                                                subitem.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();
                                            }
                                            if (grading.Checkbox != null && grading.Checkbox.Count > 0)
                                            {
                                                if (gradingconditions != null && gradingconditions.Count > 0)
                                                {
                                                    foreach (var gradingcheckbox in grading.Checkbox)
                                                    {
                                                        gradingcheckbox.Value = gradingconditions.Where(x => x.GradingConditionLabel == gradingcheckbox.Name).Select(x => x.GradingConditionValue).FirstOrDefault();
                                                    }
                                                }
                                            }
                                            if (gradingnotes != null && gradingnotes.Count > 0)
                                            {
                                                grading.Value = gradingnotes.Where(x => x.GradingId == grading.Id).Select(x => x.Value).FirstOrDefault();
                                            }

                                        }
                                    }

                                }

                            }
                            else
                            {
                                newReport.handireport = _surveyReportRepository.GetHIReport(projectId).Result;
                                if (newReport.handireport == null)
                                {
                                    newReport.handireport = new HandIReport();
                                }
                                newReport.handireport.sectionOptions = new List<Options>() { new Options { Label = "Yes", Value = "Yes" }, new Options { Label = "No", Value = "No" }, new Options { Label = "Others", Value = "Others" } };
                                newReport.handireport.maintanenceOptions = new List<Options>() { new Options { Label = "Yes", Value = "Yes" }, new Options { Label = "No", Value = "No" }, new Options { Label = "N/A", Value = "N/A" } };
                                newReport.handireport.Grading = new List<Core.Models.Survey.Grading>();
                                newReport.handireport.GradingLabelName = new List<String>();
                                newReport.handireport.SectionId = newReport.Sections.FirstOrDefault().Id;
                                newReport.handireport.Grading.AddRange(gradings.Where(x => x.SectionId == newReport.Sections.FirstOrDefault().Id).Select(x => new Core.Models.Survey.Grading { Id = x.Id, Name = x.LabelName, Checkbox = new List<CheckBox>() }).ToList());
                                if (gradingCondition != null && gradingCondition.Count > 0)
                                {

                                    gradingnotes = _surveyReportRepository.GetProjectGrading(projectId, templateSectionId, newReport.handireport.SectionId)?.Result;
                                    foreach (var grading in newReport.handireport.Grading)
                                    {

                                        gradingconditions = _surveyReportRepository.GetProjectGradingConditionMapping(projectId, templateSectionId, newReport.handireport.SectionId, grading.Id)?.Result;
                                        grading.Checkbox.AddRange(gradingCondition.Where(x => x.GradingId == grading.Id && x.SectionId == newReport.Sections.FirstOrDefault().Id).Select(x => new CheckBox { Id = x.Id, Name = x.Condition }).ToList());
                                        if (newReport.handireport.GradingLabelName.Count < grading.Checkbox.Count)
                                        {
                                            newReport.handireport.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();
                                        }
                                        if (grading.Checkbox != null && grading.Checkbox.Count > 0)
                                        {
                                            if (gradingconditions != null && gradingconditions.Count > 0)
                                            {
                                                foreach (var gradingcheckbox in grading.Checkbox)
                                                {
                                                    gradingcheckbox.Value = gradingconditions.Where(x => x.GradingConditionLabel == gradingcheckbox.Name).Select(x => x.GradingConditionValue).FirstOrDefault();
                                                }
                                            }
                                        }
                                        if (gradingnotes != null && gradingnotes.Count > 0)
                                        {
                                            grading.Value = gradingnotes.Where(x => x.GradingId == grading.Id).Select(x => x.Value).FirstOrDefault();
                                        }
                                    }

                                }
                            }

                        }

                    }
                }
                else
                {
                    var sectionName = sections.Where(x => x.Id == SectionId).Select(x => x.SpecialSectionName).FirstOrDefault();
                    var templates = sections.Where(x => x.SpecialSectionName == sectionName).ToList();
                    List<SubSection> specialsections = new List<SubSection>();
                    specialsections.AddRange(templates.Select(x => new SubSection { Id = x.Id, SubSectionName = x.SubSectionName, Grading = new List<Core.Models.Survey.Grading>(), ImageCards = new List<Core.Models.Survey.ImageCards>(), GradingLabelName = new List<String>(), SpecialSectionName = x.SpecialSectionName }).ToList());
                    foreach (var listsubitem in specialsections)
                    {

                        listsubitem.Grading.AddRange(gradings.Where(x => x.SectionId == listsubitem.Id).Select(x => new Core.Models.Survey.Grading { Id = x.Id, Name = x.LabelName, Checkbox = new List<CheckBox>() }).ToList());
                        reportnotes = _surveyReportRepository.GetProjectReportTemplate(projectId, templateSectionId, listsubitem.Id)?.Result;
                        listsubitem.imageDescriptions = new List<ImageDescriptionUI>();
                        listsubitem.imageDescriptions.AddRange(imageDescription.Where(x => x.SectionId == listsubitem.Id).Select(x => new ImageDescriptionUI { Id = x.Id, Description = x.Description }).ToList());
                        if (reportnotes != null && reportnotes.Notes != null)
                        {
                            listsubitem.SectionNotes = reportnotes.Notes;
                        }

                        listsubitem.projectCards = _surveyReportRepository.GetProjectImageCard(projectId, templateSectionId, listsubitem.Id)?.Result;
                        if (imageCards != null && imageCards.Count > 0)
                        {

                            listsubitem.ImageCards.AddRange(imageCards.Where(x => x.SectionId == listsubitem.Id).Select(x => new Core.Models.Survey.ImageCards { Id = x.Id, CurrentCondition = x.CurrentCondition, DescriptionOptions = new List<Description>(), FileName = x.FileName, CurrentConditionOptions = new List<CurrentCondition>() }).ToList());
                            listsubitem.ImageCards = listsubitem.ImageCards.OrderBy(x => Convert.ToInt16(x.FileName.Substring(x.FileName.IndexOf("-") + 1))).ToList();

                        }
                        if (gradingCondition != null && gradingCondition.Count > 0)
                        {
                            gradingnotes = _surveyReportRepository.GetProjectGrading(projectId, templateSectionId, listsubitem.Id)?.Result;

                            foreach (var grading in listsubitem.Grading)
                            {
                                gradingconditions = _surveyReportRepository.GetProjectGradingConditionMapping(projectId, templateSectionId, listsubitem.Id, grading.Id)?.Result;
                                grading.Checkbox.AddRange(gradingCondition.Where(x => x.GradingId == grading.Id && x.SectionId == listsubitem.Id).Select(x => new CheckBox { Id = x.Id, Name = x.Condition }).ToList());
                                if (listsubitem.GradingLabelName.Count < grading.Checkbox.Count)
                                {
                                    listsubitem.GradingLabelName = grading.Checkbox.Select(x => x.Name).ToList();
                                }
                                if (grading.Checkbox != null && grading.Checkbox.Count > 0)
                                {
                                    if (gradingconditions != null && gradingconditions.Count > 0)
                                    {
                                        foreach (var gradingcheckbox in grading.Checkbox)
                                        {
                                            gradingcheckbox.Value = gradingconditions.Where(x => x.GradingConditionLabel == gradingcheckbox.Name).Select(x => x.GradingConditionValue).FirstOrDefault();
                                        }
                                    }
                                }
                                if (gradingnotes != null && gradingnotes.Count > 0)
                                {
                                    grading.Value = gradingnotes.Where(x => x.GradingId == grading.Id).Select(x => x.Value).FirstOrDefault();
                                }

                            }
                        }

                    }
                    foreach (var item in newReport.Sections)
                    {
                        item.subSections.Add(templates.Where(x => x.Id == SectionId).Select(x => new SubSection { Id = x.Id, SubSectionName = x.SubSectionName, Grading = new List<Core.Models.Survey.Grading>(), ImageCards = new List<Core.Models.Survey.ImageCards>(), GradingLabelName = new List<String>(), SpecialSectionName = x.SpecialSectionName }).FirstOrDefault());
                        foreach (var subitem in item.subSections)
                        {
                            subitem.OrderNumber = 1;
                            foreach (var sp_section in specialsections)
                            {
                                if (subitem.Grading == null)
                                {
                                    subitem.Grading = new List<Core.Models.Survey.Grading>();
                                }
                                if (subitem.imageDescriptions == null)
                                {
                                    subitem.imageDescriptions = new List<ImageDescriptionUI>();
                                }
                                if (subitem.ImageCards == null)
                                {
                                    subitem.ImageCards = new List<ImageCards>();
                                }
                                if (subitem.projectCards == null)
                                {
                                    subitem.projectCards = new List<ProjectCardMapping>();
                                }
                                subitem.Grading.AddRange(sp_section.Grading);
                                if (sp_section.SectionNotes != null)
                                {
                                    subitem.SectionNotes = sp_section.SectionNotes;
                                }
                                subitem.imageDescriptions.AddRange(sp_section.imageDescriptions);
                                subitem.ImageCards.AddRange(sp_section.ImageCards);
                                subitem.projectCards.AddRange(sp_section.projectCards);
                            }
                        }
                    }

                }

                var sectionIdGroup = new List<GradingSection>();
                if (newReport.Sections != null && newReport.Sections.Count > 0)
                {
                    sectionIdGroup.AddRange(newReport.Sections.Select(x => new GradingSection { SectionId = x.Id, SectionName = x.SectionName }).ToList());
                }
                if (retApp.tankReportView?.Sections != null && retApp.tankReportView?.Sections?.Count > 0)
                {
                    sectionIdGroup.AddRange(retApp.tankReportView?.Sections?.Select(x => new GradingSection { SectionId = x.Id, SectionName = x.TankName }).ToList());
                }
                retApp.template = newReport;
                retApp.sections = sectionIdGroup.Where(x => x.SectionId == SectionId).ToList();

                return ServiceResult<Report>.Success(retApp);
            }
            catch (SqlException sqlEx)
            {
                return ServiceResult<Report>.Failure("Error while generating report");
            }
        }

        public async Task<int> GetUploadedImageCount(int projectId, int templateId, Guid sectionId)
        {
            try
            {
                var sectionImageCards = await _surveyReportRepository.GetProjectImageCard(projectId, templateId, sectionId);

                return sectionImageCards?.Count(x => !string.IsNullOrEmpty(x.Src)) ?? 0;
            }
            catch
            {
                return 0;
            }
        }
        public List<ImageCards> GetEmptyTankImageCard(List<TankImageCard> existingcards, string templeName, string index)
        {
            List<TankImageCard> imageCards = Enumerable.Range(1, 32)
            .Select(x => new TankImageCard { CardNumber = x, CardName = templeName + index + "-" + (x), CurrentCondition = ((x > 16) ? 1 : 0) }).ToList();

            if (existingcards.Count > 0)
            {
                imageCards = imageCards.Except(existingcards, new TankCardComparer()).ToList();
            }
            return imageCards.Select(x => new ImageCards() { Id = x.CardNumber, FileName = x.CardName, CurrentCondition = (x.CurrentCondition == 1) }).OrderBy(x => x.Id).ToList();
        }
        public SurveyPhotoPlacementDropdownsViewModel GetBulkUploadDropdownValues(int projectId, int templateId, Guid sectionId, List<Guid> sectionIds)
        {
            SurveyPhotoPlacementDropdownsViewModel model = new SurveyPhotoPlacementDropdownsViewModel();
            if (templateId != 0 && templateId != 8)
            {
                model.ProjectId = projectId;
                model.TemplateId = templateId;
                model.SectionId = sectionId;
                var projectydetails = _projectRepository.GetProject(projectId).Result;
                var imagedescriptions = _descriptionRepository.GetImageDescriptionsByProjectId(projectId).Result;
                var imageCards = _surveyReportRepository.GetImageCard(templateId).Result;
                model.CurrentCondition = _surveyReportRepository.GetCurrentCondition().Result;
                if (templateId == 2 || templateId == 3 || templateId == 4)
                {
                    var sections = _surveyReportRepository.GetTemplatSections(templateId).Result.Where(x => x.HasSubSection == false && x.Id == sectionId).FirstOrDefault();
                    var tankSections = _tankRepository.GetTemplateTanks(templateId, projectydetails?.IMO.ToString(), null, projectId).Result;
                    var tankSection = tankSections.Where(x => x.Id == sectionId).FirstOrDefault();
                    if (tankSection != null)
                    {
                        var templateName = templateId == 2 ? "B" : templateId == 3 ? "C" : "D";
                        model.ImageDescription = imagedescriptions.Where(x => x.TankTypeId == tankSection.TankTypeId).Select(x => new ImageDescriptionUI { Id = x.Id, Description = x.Description }).ToList();
                        var TankImageCard = _tankRepository.GetProjectTankImageCard(projectId, templateId, tankSection.Id).Result;
                        int index = sectionIds.IndexOf(sectionId) + 1;
                        var ImageCardlist = GetEmptyTankImageCard(TankImageCard, templateName, index.ToString());
                        foreach (var image in ImageCardlist)
                        {
                            if (image.Id > tankSection.CurrentconditionPlaceholders) { image.CurrentCondition = true; }
                            else { image.CurrentCondition = false; }
                        }
                        model.ImageCard = ImageCardlist;
                    }
                    else if (sections != null)
                    {
                        model.ImageDescription = imagedescriptions.Where(x => x.SectionId == sectionId).Select(x => new ImageDescriptionUI { Id = x.Id, Description = x.Description }).ToList();
                        var sectionImageCards = _surveyReportRepository.GetProjectImageCard(projectId, templateId, sectionId)?.Result;
                        if (sectionImageCards != null && sectionImageCards.Count > 0)
                            sectionImageCards = sectionImageCards.Where(x => !string.IsNullOrEmpty(x.Src)).ToList();
                        else
                            sectionImageCards = new List<ProjectCardMapping>();

                        var imageCard = new List<ImageCards>();
                        imageCard.AddRange(imageCards.Where(x => x.SectionId == sectionId).Select(x => new ImageCards { Id = x.Id, CurrentCondition = x.CurrentCondition, DescriptionOptions = new List<Description>(), FileName = x.FileName, CurrentConditionOptions = new List<CurrentCondition>() }).OrderBy(x => x.Id).ToList());
                        model.ImageCard = imageCard.Where(x => !sectionImageCards.Any(y => y.CardId == x.Id)).ToList();
                    }
                }
                else
                {
                    model.ImageDescription = imagedescriptions.Where(x => x.SectionId == sectionId).Select(x => new ImageDescriptionUI { Id = x.Id, Description = x.Description }).ToList();
                    var sectionImageCards = _surveyReportRepository.GetProjectImageCard(projectId, templateId, sectionId)?.Result;
                    if (sectionImageCards != null && sectionImageCards.Count > 0)
                        sectionImageCards = sectionImageCards.Where(x => !string.IsNullOrEmpty(x.Src)).ToList();
                    else
                        sectionImageCards = new List<ProjectCardMapping>();

                    var imageCard = new List<ImageCards>();
                    imageCard.AddRange(imageCards.Where(x => x.SectionId == sectionId).Select(x => new ImageCards { Id = x.Id, CurrentCondition = x.CurrentCondition, DescriptionOptions = new List<Description>(), FileName = x.FileName, CurrentConditionOptions = new List<CurrentCondition>() }).OrderBy(x => x.Id).ToList());
                    model.ImageCard = imageCard.Where(x => !sectionImageCards.Any(y => y.CardId == x.Id)).ToList();
                }
            }
            return model;
        }

        public async Task<ServiceResult<Report>> RefreshCertificate(int projectId)
        {
            var retApp = new Report();
            var projectydetails = _projectRepository.GetProject(projectId).Result;
            retApp.ProjectId = projectId;
            retApp.ProjectStatus = projectydetails.ProjectStatus;
            var ReportVessel = new ReportVesselMainData();
            try
            {
                ReportVessel = _vesselRepository.GetVesselMainData(projectId).Result.First();
            }
            catch (Exception ex)
            { }
            if (ReportVessel != null)
            {
                retApp.ReportPort = ReportVessel.reportPort;
                retApp.ReportDate = ReportVessel.portDate != null && Convert.ToDateTime(ReportVessel.portDate) != DateTime.MinValue ? Convert.ToDateTime(ReportVessel.portDate).ToShortDateString() : DateTime.Now.ToString("dd-MM-yyy");
                retApp.template = new Core.Models.Survey.View();
                retApp.template.Id = 0;
                retApp.ReportNo = ReportVessel.reportNo;
                retApp.Vessel = new Core.Models.Survey.Vessel();
                retApp.Vessel.ReportNo = ReportVessel.reportNo;
                if (ReportVessel.actualReportStartDate != null && Convert.ToDateTime(ReportVessel.actualReportStartDate).Date != DateTime.MinValue.Date)
                {
                    retApp.Vessel.ActualReportStartDate = Convert.ToDateTime(ReportVessel.actualReportStartDate);
                }
                retApp.ReportPort = ReportVessel.reportPort;
                retApp.Vessel.Classed = ReportVessel.classed;
                retApp.Vessel.VesselName = ReportVessel.vesselName;
                retApp.Vessel.VesselID = ReportVessel.vesselID;
                retApp.Vessel.ImoNumber = ReportVessel.imoNumber;
                retApp.Vessel.VesselType = ReportVessel.vesselType;
                retApp.Vessel.YearBuilt = Convert.ToInt32(ReportVessel.yearBuilt);
                retApp.Vessel.BuiltBy = ReportVessel.builtBy;
                retApp.Vessel.HullNo = ReportVessel.hullNo;
                retApp.Vessel.FlagName = ReportVessel.flagName;
                retApp.Vessel.Homeport = ReportVessel.homeport;
                retApp.Vessel.OfficalNumber = ReportVessel.officalNumber;
                retApp.Vessel.CallSign = ReportVessel.callSign;
                retApp.Vessel.PreviousName = ReportVessel.previousName;
                retApp.Vessel.OwnerName = ReportVessel.ownerName;
                retApp.Vessel.Manager = ReportVessel.manager;
                retApp.Vessel.Length = ReportVessel.length != null ? float.Parse(ReportVessel.length) : 0;
                retApp.Vessel.Breadth = ReportVessel.breadth != null ? float.Parse(ReportVessel.breadth) : 0;
                retApp.Vessel.DEPTH = ReportVessel.depth != null ? float.Parse(ReportVessel.depth) : 0;
                retApp.Vessel.Draft = ReportVessel.draft != null ? float.Parse(ReportVessel.draft) : 0;
                retApp.Vessel.DEADWEIGHT = ReportVessel.deadweight != null ? ReportVessel.deadweight : null;
                retApp.Vessel.GrossTons = ReportVessel.grossTons != null ? float.Parse(ReportVessel.grossTons) : 0;
                retApp.Vessel.NetTons = ReportVessel.netTons != null ? float.Parse(ReportVessel.netTons) : 0;
                retApp.Vessel.PropulsionType = ReportVessel.propulsionType;
                retApp.Vessel.KW = ReportVessel.kw != null ? ReportVessel.kw : "";
                retApp.Vessel.ShaftRPM = ReportVessel.shaftRPM != null ? float.Parse(ReportVessel.shaftRPM) : 0;
                retApp.Vessel.PropulsionManufacturer = ReportVessel.propulsionManufacturer;
                retApp.Vessel.GearManufacturer = ReportVessel.gearManufacturer;
                retApp.Vessel.Classed = ReportVessel.classed;
                retApp.Vessel.ClassNo = ReportVessel.classNo;
                retApp.Vessel.ClassNotation = ReportVessel.classNotation;
                retApp.Vessel.Machinery = ReportVessel.machinery;
                retApp.Vessel.Other = ReportVessel.other;
                retApp.Vessel.Environment = ReportVessel.environment;
                retApp.Vessel.SelectedPrefix = ReportVessel.selectedPrefix.ToString();
                retApp.Vessel.FirstVisitDate = ReportVessel.firstVisitDate;

                retApp.Vessel.LastVisitDate = ReportVessel.lastVisitDate;

            }

            if (projectydetails.ProjectStatus != 11 && projectydetails.ProjectStatus != 10)
            {
                retApp = this.MapStatuatoryCertificates(projectydetails.AbsClassID, retApp);
                retApp.SurveyStatus = await _surveyReportRepository.MapSurveyStatus(projectydetails.AbsClassID, retApp.Vessel.ImoNumber);
                var data = _vesselRepository.UpdateStatutoryCertificate(projectId, retApp).Result;
                var certificates = _vesselRepository.UpdateSurveyAudit(retApp.SurveyStatus, projectId).Result;
            }
            //Get CERTIFICATE USING CLASSNUMBER
            var sections = _surveyReportRepository.GetTemplatSections(0).Result;
            retApp.SurveyStatus = _vesselRepository.GetSurveyStatus(projectId).Result.FirstOrDefault();
            var certificatedates = _vesselRepository.GetStatutoryCertificate(projectId).Result;
            retApp.StatutoryCertificatesExpirationDates = certificatedates.StatutoryCertificatesExpirationDates;
            retApp.StatutoryCertificatesIssuedDates = certificatedates.StatutoryCertificatesIssuedDates;
            Core.Models.Survey.View newReport = new Core.Models.Survey.View();
            newReport.Sections = new List<Sections>();

            newReport.Sections.AddRange(sections.GroupBy(x => x.SectionName).ToList().Select(x => new Sections { SectionName = x.Key, Id = x.FirstOrDefault().SectionId, subSections = new List<SubSection>() }).ToList());
            retApp.template = newReport;
            return ServiceResult<Report>.Success(retApp);
        }

        public Report MapStatuatoryCertificates(string classNumber, Report report)
        {
            try
            {

                var certificates = _freedomAPIRepository.GetCertificates(classNumber).Result;
                if (certificates != null)
                {
                    report.StatutoryCertificatesIssuedDates = new StatutoryCertificates();
                    report.StatutoryCertificatesExpirationDates = new StatutoryCertificates();
                    var LLLICerticate = certificates.Where(certificate => certificate.Name != null && certificate.Name.ToLower().Contains("load") && certificate.Name.ToLower().Contains("line") && !String.IsNullOrEmpty(certificate.ExpiryDate)).FirstOrDefault();
                    if (LLLICerticate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.LLLIDate = !string.IsNullOrEmpty(LLLICerticate.IssueDate) ? Convert.ToDateTime(LLLICerticate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.LLLIDate = !string.IsNullOrEmpty(LLLICerticate.ExpiryDate) ? Convert.ToDateTime(LLLICerticate.ExpiryDate) : null;

                    }

                    var SafetyConstructionDate = certificates.Where(certificate => certificate.Name != null && certificate.Name.ToLower().Contains("safety") && certificate.Name.ToLower().Contains("construction") && !String.IsNullOrEmpty(certificate.ExpiryDate)).FirstOrDefault();
                    if (SafetyConstructionDate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.SafetyConDate = !string.IsNullOrEmpty(SafetyConstructionDate.IssueDate) ? Convert.ToDateTime(SafetyConstructionDate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.SafetyConDate = !string.IsNullOrEmpty(SafetyConstructionDate.ExpiryDate) ? Convert.ToDateTime(SafetyConstructionDate.ExpiryDate) : null;

                    }

                    var SafetyRadioDate = certificates.Where(certificate => certificate.Name != null && certificate.Name.ToLower().Contains("safety") && certificate.Name.ToLower().Contains("radio") && !String.IsNullOrEmpty(certificate.ExpiryDate)).FirstOrDefault();
                    if (SafetyRadioDate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.SafetyRadioDate = !string.IsNullOrEmpty(SafetyRadioDate.IssueDate) ? Convert.ToDateTime(SafetyRadioDate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.SafetyRadioDate = !string.IsNullOrEmpty(SafetyRadioDate.ExpiryDate) ? Convert.ToDateTime(SafetyRadioDate.ExpiryDate) : null;

                    }

                    var SafetyEquipmentDate = certificates.Where(certificate => certificate.Name != null && certificate.Name.ToLower().Contains("safety") && certificate.Name.ToLower().Contains("equipment") && certificate.Abbr.Trim().ToLower().Equals("sle-cert")).FirstOrDefault();
                    if (SafetyEquipmentDate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.SafetyEquipmentDate = !string.IsNullOrEmpty(SafetyEquipmentDate.IssueDate) ? Convert.ToDateTime(SafetyEquipmentDate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.SafetyEquipmentDate = !string.IsNullOrEmpty(SafetyEquipmentDate.ExpiryDate) ? Convert.ToDateTime(SafetyEquipmentDate.ExpiryDate) : null;

                    }
                    var IOPPDate = certificates.Where(certificate => certificate.Name != null && certificate.Name.ToLower().Contains("international") && certificate.Name.ToLower().Contains("oil") && certificate.Name.ToLower().Contains("pollution") && certificate.Name.ToLower().Contains("prevention") && certificate.Abbr.Trim().ToLower().Equals("iopp-cert")).FirstOrDefault();
                    if (IOPPDate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.IOPPDate = !string.IsNullOrEmpty(IOPPDate.IssueDate) ? Convert.ToDateTime(IOPPDate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.IOPPDate = !string.IsNullOrEmpty(IOPPDate.ExpiryDate) ? Convert.ToDateTime(IOPPDate.ExpiryDate) : null;

                    }
                    var IAPDate = certificates.Where(certificate => certificate.Name != null && certificate.Name.ToLower().Contains("international") && certificate.Name.ToLower().Contains("air") && certificate.Name.ToLower().Contains("pollution") && certificate.Name.ToLower().Contains("prevention") && certificate.Abbr.Trim().ToLower().Equals("iapp-cert")).FirstOrDefault();
                    if (IAPDate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.IAPDate = !string.IsNullOrEmpty(IAPDate.IssueDate) ? Convert.ToDateTime(IAPDate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.IAPDate = !string.IsNullOrEmpty(IAPDate.ExpiryDate) ? Convert.ToDateTime(IAPDate.ExpiryDate) : null;

                    }

                    var IAFSDate = certificates.Where(certificate => certificate.Name != null && certificate.Name.ToLower().Contains("international") && certificate.Name.ToLower().Contains("anti") && certificate.Name.ToLower().Contains("fouling") && certificate.Name.ToLower().Contains("system") && certificate.Abbr.Trim().ToLower().Equals("iafs-cert")).FirstOrDefault();
                    if (IAFSDate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.IAFSDate = !string.IsNullOrEmpty(IAFSDate.IssueDate) ? Convert.ToDateTime(IAFSDate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.IAFSDate = !string.IsNullOrEmpty(IAFSDate.ExpiryDate) ? Convert.ToDateTime(IAFSDate.ExpiryDate) : null;

                    }

                    var ISPSDate = certificates.Where(certificate => certificate.Abbr.Trim().ToLower().Equals("isps-cert") && certificate.Name.ToLower().Contains("international") && certificate.Name.ToLower().Contains("ship") && certificate.Name.ToLower().Contains("security")).FirstOrDefault();
                    if (ISPSDate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.ISPSDate = !string.IsNullOrEmpty(ISPSDate.IssueDate) ? Convert.ToDateTime(ISPSDate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.ISPSDate = !string.IsNullOrEmpty(ISPSDate.ExpiryDate) ? Convert.ToDateTime(ISPSDate.ExpiryDate) : null;

                    }
                    var CargoGearReTestDate = certificates.Where(certificate => certificate.Name.ToLower().Contains("cargo") && certificate.Name.ToLower().Contains("gear") && (certificate.Name.ToLower().Contains("retesting") || certificate.Name.ToLower().Contains("re") && certificate.Name.ToLower().Contains("testing"))).FirstOrDefault();
                    if (CargoGearReTestDate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.CargoGearReTestDate = !string.IsNullOrEmpty(CargoGearReTestDate.IssueDate) ? Convert.ToDateTime(CargoGearReTestDate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.CargoGearReTestDate = !string.IsNullOrEmpty(CargoGearReTestDate.ExpiryDate) ? Convert.ToDateTime(CargoGearReTestDate.ExpiryDate) : null;

                    }
                    var CargoGearAnnualCertificate = certificates.Where(certificate => certificate.Abbr.Trim().ToLower().Equals("cg-ann-cert") && certificate.Name.ToLower().Contains("cargo") && certificate.Name.ToLower().Contains("gear") && certificate.Name.ToLower().Contains("annual")).FirstOrDefault();
                    if (CargoGearAnnualCertificate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.CargoGearAnnualCertificate = !string.IsNullOrEmpty(CargoGearAnnualCertificate.IssueDate) ? Convert.ToDateTime(CargoGearAnnualCertificate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.CargoGearAnnualCertificate = !string.IsNullOrEmpty(CargoGearAnnualCertificate.ExpiryDate) ? Convert.ToDateTime(CargoGearAnnualCertificate.ExpiryDate) : null;

                    }
                    var IBWMDate = certificates.Where(certificate => certificate.Abbr.ToLower().Trim().Equals("ibwm-cert") && certificate.Name.ToLower().Contains("international") && certificate.Name.ToLower().Contains("ballast") && certificate.Name.ToLower().Contains("management") && certificate.Name.ToLower().Contains("water")).FirstOrDefault();
                    if (IBWMDate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.IBWMDate = !string.IsNullOrEmpty(IBWMDate.IssueDate) ? Convert.ToDateTime(IBWMDate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.IBWMDate = !string.IsNullOrEmpty(IBWMDate.ExpiryDate) ? Convert.ToDateTime(IBWMDate.ExpiryDate) : null;

                    }
                    var ISPPSewageDate = certificates.Where(certificate => certificate.Abbr.ToLower().Trim().Equals("ispp-cert") && certificate.Name.ToLower().Contains("international") && certificate.Name.ToLower().Contains("sewage") && certificate.Name.ToLower().Contains("pollution") && certificate.Name.ToLower().Contains("prevention")).FirstOrDefault();
                    if (ISPPSewageDate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.ISPPSewageDate = !string.IsNullOrEmpty(ISPPSewageDate.IssueDate) ? Convert.ToDateTime(ISPPSewageDate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.ISPPSewageDate = !string.IsNullOrEmpty(ISPPSewageDate.ExpiryDate) ? Convert.ToDateTime(ISPPSewageDate.ExpiryDate) : null;

                    }

                    var ISMCertificate = certificates.Where(certificate => certificate.ServiceType.ToLower().Contains("ism")).FirstOrDefault();
                    if (ISMCertificate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.ISMDate = !string.IsNullOrEmpty(ISMCertificate.IssueDate) ? Convert.ToDateTime(ISMCertificate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.ISMDate = !string.IsNullOrEmpty(ISMCertificate.ExpiryDate) ? Convert.ToDateTime(ISMCertificate.ExpiryDate) : null;

                    }

                    var MaritimeCertificate = certificates.Where(certificate => certificate.Name != null && certificate.Name.ToLower().Contains("maritime") && certificate.Name.ToLower().Contains("labour") && certificate.Name.ToLower().Contains("certificate") && !String.IsNullOrEmpty(certificate.ExpiryDate)).FirstOrDefault();
                    if (MaritimeCertificate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.MaritimeLabourDate = !string.IsNullOrEmpty(MaritimeCertificate.IssueDate) ? Convert.ToDateTime(MaritimeCertificate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.MaritimeLabourDate = !string.IsNullOrEmpty(MaritimeCertificate.ExpiryDate) ? Convert.ToDateTime(MaritimeCertificate.ExpiryDate) : null;

                    }

                    var IEECertificate = certificates.Where(certificate => certificate.Name != null && certificate.Name.ToLower().Contains("international") && certificate.Name.ToLower().Contains("energy") && certificate.Name.ToLower().Contains("efficiency")).FirstOrDefault();
                    if (IEECertificate != null)
                    {
                        report.StatutoryCertificatesIssuedDates.IEEDate = !string.IsNullOrEmpty(IEECertificate.IssueDate) ? Convert.ToDateTime(IEECertificate.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.IEEDate = !string.IsNullOrEmpty(IEECertificate.ExpiryDate) ? Convert.ToDateTime(IEECertificate.ExpiryDate) : null;

                    }

                    var cargogearretest = certificates.Where(certificate => certificate.Name != null && certificate.Name.ToLower().Contains("cargo") && certificate.Name.ToLower().Contains("gear") && certificate.Name.ToLower().Contains("re-testing")).FirstOrDefault();
                    if (cargogearretest != null)
                    {
                        report.StatutoryCertificatesIssuedDates.CargoGearReTestDate = !string.IsNullOrEmpty(cargogearretest.IssueDate) ? Convert.ToDateTime(cargogearretest.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.CargoGearReTestDate = !string.IsNullOrEmpty(cargogearretest.ExpiryDate) ? Convert.ToDateTime(cargogearretest.ExpiryDate) : null;

                    }

                    var gascarrierfitness = certificates.Where(certificate => certificate.Name != null && certificate.Name.ToLower().Contains("gases") && certificate.Name.ToLower().Contains("fitness") && certificate.Name.ToLower().Contains("liquefied")).FirstOrDefault();
                    if (gascarrierfitness != null)
                    {
                        report.StatutoryCertificatesIssuedDates.GasCarrierFitness = !string.IsNullOrEmpty(gascarrierfitness.IssueDate) ? Convert.ToDateTime(gascarrierfitness.IssueDate) : null;
                        report.StatutoryCertificatesExpirationDates.GasCarrierFitness = !string.IsNullOrEmpty(gascarrierfitness.ExpiryDate) ? Convert.ToDateTime(gascarrierfitness.ExpiryDate) : null;

                    }

                }
                else
                {
                    report.StatutoryCertificatesIssuedDates = new StatutoryCertificates();
                    report.StatutoryCertificatesExpirationDates = new StatutoryCertificates();
                }

                return report;

            }
            catch (Exception ex)
            {
                return report;
            }

        }

        public async Task<ServiceResult<string>> ExportAllValidation(int projectId)
        {
            try
            {
                List<string> sectionIds = new List<string>();
                string Result = "";
                var Allgrading = ""; var Allcurrentcondition = ""; var Alldescription = ""; var Alladditionaldescription = "";
                var reportParts = GetSectionIdsByProjectId(projectId, null).Result;
                int templateSectionId = 9;
                for (int sectionid = 1; sectionid < templateSectionId; sectionid++)
                {
                    var currentSectionIds = reportParts.Data.ReportPartGrid.Where(x => x.TemplateId == sectionid).FirstOrDefault()?.SectionIds.Select(x => x.ToString()).ToList() ?? new List<string>();
                    var ret = await GetReport(projectId, sectionid, currentSectionIds);
                    var TemplateTitle = ret != null && ret.template != null && ret.template.TemplateTitle != null && ret.template.TemplateTitle != "" ? ret.template.TemplateTitle : "";
                    var grading = ""; var currentcondition = ""; var description = ""; var additionaldescription = "";

                    if (ret != null && TemplateTitle != "" && (TemplateTitle == "Part A" || TemplateTitle == "Part B" || TemplateTitle == "Part C" || TemplateTitle == "Part D" || TemplateTitle == "Part E" || TemplateTitle == "Part F" || TemplateTitle == "Part G" || TemplateTitle == "Part H & I"))
                    {
                        var tankgradingList = new List<TankGradingUI>();
                        var tankui = new List<TankUI>();
                        var tankimagecardList = new List<TankImageCard>();

                        var imagecardList = new List<SubSection>();
                        imagecardList = ret.template.Sections.SelectMany(x => x.subSections).ToList();

                        #region Grading
                        var gradingcount = 0; var finalcount = 0; var checkbox = 0;
                        if (imagecardList.Select(p => p.Grading.Count).FirstOrDefault() > 0)
                        {
                            imagecardList.All(p =>
                            {
                                gradingcount = p.Grading.Count;
                                p.Grading.ForEach(g =>
                                {
                                    if (g.Checkbox?.Count > 0)
                                    {
                                        g.Checkbox.ForEach(u =>
                                        {
                                            if (u.Value == true && checkbox == 0)
                                            {
                                                checkbox++;
                                                finalcount++;
                                            }
                                        });
                                        checkbox = 0;
                                    }
                                    else
                                    {
                                        gradingcount--;
                                    }
                                });

                                if (gradingcount != finalcount)
                                {
                                    grading = grading + p.SubSectionName.ToString() + ",";
                                }
                                finalcount = 0;
                                return true;
                            });
                        }
                        #endregion

                        #region CurrentCondition
                        var imagecards = new List<ImageCards>();
                        if (imagecardList.Select(p => p.ImageCards) != null && imagecardList.Select(p => p.ImageCards.Count).FirstOrDefault() > 0 && imagecardList.Select(p => p.projectCards) != null && imagecardList.Select(p => p.projectCards.Count).FirstOrDefault() > 0)
                        {
                            imagecards = imagecardList.SelectMany(s => s.ImageCards).Where(s => s.CurrentCondition == true).ToList();
                            var prjcardmap = new List<ProjectCardMapping>();
                            prjcardmap = imagecardList.SelectMany(s => s.projectCards).Where(s => s.CurrentCondition == 0 && (s.Src != null || s.DescriptionId != 0)).ToList();
                            var all = imagecards.Where(b => prjcardmap.Any(a => a.CardId == b.Id)).ToList();
                            if (all.Count > 0)
                            {
                                currentcondition = string.Join(",", all.Select(a => a.FileName));
                            }
                        }
                        #endregion

                        #region Description
                        var desc_imagecards = new List<ImageCards>();
                        if (imagecardList.Select(p => p.ImageCards) != null && imagecardList.Select(p => p.ImageCards.Count).FirstOrDefault() > 0 && imagecardList.Select(p => p.projectCards) != null && imagecardList.Select(p => p.projectCards.Count).FirstOrDefault() > 0)
                        {
                            desc_imagecards = imagecardList.SelectMany(s => s.ImageCards).ToList();
                            var desc_prjcardmap = new List<ProjectCardMapping>();
                            desc_prjcardmap = imagecardList.SelectMany(s => s.projectCards).Where(s => (s.DescriptionId == 0 && s.Src != null) || (s.DescriptionId == 0 && s.CurrentCondition != 0)).ToList();
                            var all2 = desc_imagecards.Where(b => desc_prjcardmap.Any(a => a.CardId == b.Id)).ToList();
                            if (all2.Count > 0)
                            {
                                description = string.Join(",", all2.Select(a => a.FileName));
                            }
                        }
                        #endregion

                        #region Add'l description
                        var adddesc_imagecards = new List<ImageCards>();
                        if (imagecardList.Select(p => p.ImageCards) != null && imagecardList.Select(p => p.ImageCards.Count).FirstOrDefault() > 0 && imagecardList.Select(p => p.imageDescriptions) != null && imagecardList.Select(p => p.imageDescriptions.Count).FirstOrDefault() > 0 && imagecardList.Select(p => p.projectCards) != null && imagecardList.Select(p => p.projectCards.Count).FirstOrDefault() > 0)
                        {
                            adddesc_imagecards = imagecardList.SelectMany(s => s.ImageCards).ToList();
                            var AdditionalDescId = imagecardList.SelectMany(s => s.imageDescriptions).Where(s => s.Description == "Others").ToList();
                            var AdditionalDesc = imagecardList.SelectMany(s => s.projectCards).Where(s => s.DescriptionId != 0 && (s.AdditionalDescription == "" || s.AdditionalDescription == null)).ToList();
                            var EmptyAdditionaldesc = AdditionalDesc.Where(a => AdditionalDescId.Any(b => b.Id == a.DescriptionId)).ToList();
                            var all3 = adddesc_imagecards.Where(b => EmptyAdditionaldesc.Any(a => a.CardId == b.Id)).ToList();
                            if (all3.Count > 0)
                            {
                                additionaldescription = string.Join(",", all3.Select(a => a.FileName));
                            }
                        }
                        #endregion

                        #region Tank Validation
                        if (ret.tankReportView != null)
                        {

                            tankui = ret.tankReportView.Sections.ToList();

                            #region Tank Grading
                            var tankgradingcount = 0; var tankfinalcount = 0; var tankcheckbox = 0;
                            if (tankui.Select(p => p.Grading.Count).FirstOrDefault() > 0)
                            {
                                tankui.All(p =>
                                {
                                    tankgradingcount = p.Grading.Count;
                                    p.Grading.ForEach(g =>
                                    {
                                        g.Checkbox.ForEach(u =>
                                        {
                                            if (u.Value == true && tankcheckbox == 0)
                                            {
                                                tankcheckbox++;
                                                tankfinalcount++;
                                            }
                                        });
                                        tankcheckbox = 0;
                                    });
                                    if (tankgradingcount != tankfinalcount)
                                    {
                                        grading = grading + p.TankName + ",";

                                    }
                                    tankfinalcount = 0;
                                    return true;
                                });
                            }
                            #endregion

                            #region Tank Current Condition
                            var tanklistnew = new List<TankImageCard>(); var totaltanklist = new List<TankImageCard>();
                            if (tankui != null && tankui.Count > 0)
                            {
                                foreach (var tui in tankui)
                                {
                                    tanklistnew = new List<TankImageCard>();
                                    var CurrentconditionPlaceholders = tui.CurrentconditionPlaceholders;
                                    var tankimagecards = tui.tankImageCards;
                                    if (tankimagecards != null && tankimagecards.Count > 0)
                                    {
                                        tanklistnew = tankimagecards.Where(x => x.CardNumber > CurrentconditionPlaceholders && x.CurrentCondition == 0 && (x.src != null || x.DescriptionId != 0)).ToList();
                                    }
                                    totaltanklist.AddRange(tanklistnew);

                                }
                                if (totaltanklist.Count > 0)
                                {
                                    currentcondition = currentcondition == "" ? string.Join(",", totaltanklist.Select(a => a.CardName)) : currentcondition + "," + string.Join(",", totaltanklist.Select(a => a.CardName));
                                }
                            }
                            #endregion

                            tankimagecardList = ret.tankReportView.Sections.SelectMany(x => x.tankImageCards).ToList();

                            #region Tank Description

                            var desc_tanklist = tankimagecardList.Where(d => d.Id != 0 && ((d.DescriptionId == 0 && d.src != null) || (d.DescriptionId == 0 && d.CurrentCondition != 0))).ToList();
                            if (desc_tanklist.Count > 0)
                            {
                                description = description == "" ? string.Join(",", desc_tanklist.Select(a => a.CardName)) : description + "," + string.Join(",", desc_tanklist.Select(a => a.CardName));
                            }
                            #endregion

                            #region Tank Add'l description
                            var TankAdditionalDescId = tankui.SelectMany(s => s.imageDescriptions).Where(s => s.Description == "Others").ToList();
                            var TankAdditionalDesc = tankimagecardList.Where(d => d.DescriptionId != 0 && (d.AdditionalDescription == null || d.AdditionalDescription == "")).ToList();
                            var TankEmptyAdditionalDescriptionlist = TankAdditionalDesc.Where(a => TankAdditionalDescId.Any(b => b.Id == a.DescriptionId)).ToList();

                            if (TankEmptyAdditionalDescriptionlist.Count > 0)
                            {
                                additionaldescription = additionaldescription == "" ? string.Join(",", TankEmptyAdditionalDescriptionlist.Select(a => a.CardName)) : additionaldescription + "," + string.Join(",", TankEmptyAdditionalDescriptionlist.Select(a => a.CardName));
                            }
                            #endregion

                        }
                        #endregion

                        #region H & I Grading

                        if (ret.template != null && ret.template.handireport != null && ret.template.handireport.Grading != null && ret.template.handireport.Grading.Count > 0)
                        {
                            var handireport = ret.template.handireport.Grading.ToList();
                            var HRgradingcount = 0; var HRfinalcount = 0; var HRcheckbox = 0;
                            HRgradingcount = handireport.Count;
                            handireport.ForEach(e =>
                            {
                                e.Checkbox.ForEach(u =>
                                {
                                    if (u.Value == true && HRcheckbox == 0)
                                    {
                                        HRcheckbox++;
                                        HRfinalcount++;

                                    }
                                });
                                HRcheckbox = 0;

                            });
                            if (HRgradingcount != HRfinalcount)
                            {
                                grading = grading + "Part H & I" + ",";
                            }

                        }
                        #endregion

                    }

                    grading = grading != "" ? grading.Remove(grading.Length - 1) : "";

                    Allgrading = grading != "" ? Allgrading == "" ? grading : Allgrading + "," + grading : Allgrading;

                    Allcurrentcondition = currentcondition != "" ? Allcurrentcondition == "" ? currentcondition : Allcurrentcondition + "," + currentcondition : Allcurrentcondition;

                    Alldescription = description != "" ? Alldescription == "" ? description : Alldescription + "," + description : Alldescription;

                    Alladditionaldescription = additionaldescription != "" ? Alladditionaldescription == "" ? additionaldescription : Alladditionaldescription + "," + additionaldescription : Alladditionaldescription;
                }

                Result = Allcurrentcondition + "#" + Alldescription + "#" + Allgrading + "#" + Alladditionaldescription;

                return ServiceResult<string>.Success(Result);
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<string>> ExportValidationPartial(int projectId, int sectionid)
        {
            try
            {
                string Result = "";
                var Allgrading = ""; var Allcurrentcondition = ""; var Alldescription = ""; var Alladditionaldescription = "";
                var reportParts = GetSectionIdsByProjectId(projectId, null).Result;
                var currentSectionIds = reportParts.Data.ReportPartGrid.Where(x => x.TemplateId == sectionid).FirstOrDefault()?.SectionIds.Select(x => x.ToString()).ToList() ?? new List<string>();
                var ret = await GetReport(projectId, sectionid, currentSectionIds);
                var TemplateTitle = ret != null && ret.template != null && ret.template.TemplateTitle != null && ret.template.TemplateTitle != "" ? ret.template.TemplateTitle : "";
                var grading = ""; var currentcondition = ""; var description = ""; var additionaldescription = "";

                if (ret != null && TemplateTitle != "" && (TemplateTitle == "Part A" || TemplateTitle == "Part B" || TemplateTitle == "Part C" || TemplateTitle == "Part D" || TemplateTitle == "Part E" || TemplateTitle == "Part F" || TemplateTitle == "Part G" || TemplateTitle == "Part H & I"))
                {
                    var tankgradingList = new List<TankGradingUI>();
                    var tankui = new List<TankUI>();
                    var tankimagecardList = new List<TankImageCard>();

                    var imagecardList = new List<SubSection>();
                    imagecardList = ret.template.Sections.SelectMany(x => x.subSections).ToList();

                    #region Grading
                    var gradingcount = 0; var finalcount = 0; var checkbox = 0;
                    if (imagecardList.Select(p => p.Grading.Count).FirstOrDefault() > 0)
                    {
                        imagecardList.All(p =>
                        {
                            gradingcount = p.Grading.Count;
                            p.Grading.ForEach(g =>
                            {
                                if (g.Checkbox?.Count > 0)
                                {
                                    g.Checkbox.ForEach(u =>
                                    {
                                        if (u.Value == true && checkbox == 0)
                                        {
                                            checkbox++;
                                            finalcount++;
                                        }
                                    });
                                    checkbox = 0;
                                }
                                else
                                {
                                    gradingcount--;
                                }
                            });

                            if (gradingcount != finalcount)
                            {
                                grading = grading + p.SubSectionName.ToString() + ",";
                            }
                            finalcount = 0;
                            return true;
                        });
                    }
                    #endregion

                    #region CurrentCondition
                    var imagecards = new List<ImageCards>();
                    if (imagecardList.Select(p => p.ImageCards) != null && imagecardList.Select(p => p.ImageCards.Count).FirstOrDefault() > 0 && imagecardList.Select(p => p.projectCards) != null && imagecardList.Select(p => p.projectCards.Count).FirstOrDefault() > 0)
                    {
                        imagecards = imagecardList.SelectMany(s => s.ImageCards).Where(s => s.CurrentCondition == true).ToList();
                        var prjcardmap = new List<ProjectCardMapping>();
                        prjcardmap = imagecardList.SelectMany(s => s.projectCards).Where(s => s.CurrentCondition == 0 && (s.Src != null || s.DescriptionId != 0)).ToList();
                        var all = imagecards.Where(b => prjcardmap.Any(a => a.CardId == b.Id)).ToList();
                        if (all.Count > 0)
                        {
                            currentcondition = string.Join(",", all.Select(a => a.FileName));
                        }
                    }
                    #endregion

                    #region Description
                    var desc_imagecards = new List<ImageCards>();
                    if (imagecardList.Select(p => p.ImageCards) != null && imagecardList.Select(p => p.ImageCards.Count).FirstOrDefault() > 0 && imagecardList.Select(p => p.projectCards) != null && imagecardList.Select(p => p.projectCards.Count).FirstOrDefault() > 0)
                    {
                        desc_imagecards = imagecardList.SelectMany(s => s.ImageCards).ToList();
                        var desc_prjcardmap = new List<ProjectCardMapping>();
                        desc_prjcardmap = imagecardList.SelectMany(s => s.projectCards).Where(s => (s.DescriptionId == 0 && s.Src != null) || (s.DescriptionId == 0 && s.CurrentCondition != 0)).ToList();
                        var all2 = desc_imagecards.Where(b => desc_prjcardmap.Any(a => a.CardId == b.Id)).ToList();
                        if (all2.Count > 0)
                        {
                            description = string.Join(",", all2.Select(a => a.FileName));
                        }
                    }
                    #endregion

                    #region Add'l description
                    var adddesc_imagecards = new List<ImageCards>();
                    if (imagecardList.Select(p => p.ImageCards) != null && imagecardList.Select(p => p.ImageCards.Count).FirstOrDefault() > 0 && imagecardList.Select(p => p.imageDescriptions) != null && imagecardList.Select(p => p.imageDescriptions.Count).FirstOrDefault() > 0 && imagecardList.Select(p => p.projectCards) != null && imagecardList.Select(p => p.projectCards.Count).FirstOrDefault() > 0)
                    {
                        adddesc_imagecards = imagecardList.SelectMany(s => s.ImageCards).ToList();
                        var AdditionalDescId = imagecardList.SelectMany(s => s.imageDescriptions).Where(s => s.Description == "Others").ToList();
                        var AdditionalDesc = imagecardList.SelectMany(s => s.projectCards).Where(s => s.DescriptionId != 0 && (s.AdditionalDescription == "" || s.AdditionalDescription == null)).ToList();
                        var EmptyAdditionaldesc = AdditionalDesc.Where(a => AdditionalDescId.Any(b => b.Id == a.DescriptionId)).ToList();
                        var all3 = adddesc_imagecards.Where(b => EmptyAdditionaldesc.Any(a => a.CardId == b.Id)).ToList();
                        if (all3.Count > 0)
                        {
                            additionaldescription = string.Join(",", all3.Select(a => a.FileName));
                        }
                    }
                    #endregion

                    #region Tank Validation
                    if (ret.tankReportView != null)
                    {

                        tankui = ret.tankReportView.Sections.ToList();

                        #region Tank Grading
                        var tankgradingcount = 0; var tankfinalcount = 0; var tankcheckbox = 0;
                        if (tankui.Select(p => p.Grading.Count).FirstOrDefault() > 0)
                        {
                            tankui.All(p =>
                            {
                                tankgradingcount = p.Grading.Count;
                                p.Grading.ForEach(g =>
                                {
                                    g.Checkbox.ForEach(u =>
                                    {
                                        if (u.Value == true && tankcheckbox == 0)
                                        {
                                            tankcheckbox++;
                                            tankfinalcount++;
                                        }
                                    });
                                    tankcheckbox = 0;
                                });
                                if (tankgradingcount != tankfinalcount)
                                {
                                    grading = grading + p.TankName + ",";

                                }
                                tankfinalcount = 0;
                                return true;
                            });
                        }
                        #endregion

                        #region Tank Current Condition
                        var tanklistnew = new List<TankImageCard>(); var totaltanklist = new List<TankImageCard>();
                        if (tankui != null && tankui.Count > 0)
                        {
                            foreach (var tui in tankui)
                            {
                                tanklistnew = new List<TankImageCard>();
                                var CurrentconditionPlaceholders = tui.CurrentconditionPlaceholders;
                                var tankimagecards = tui.tankImageCards;
                                if (tankimagecards != null && tankimagecards.Count > 0)
                                {
                                    tanklistnew = tankimagecards.Where(x => x.CardNumber > CurrentconditionPlaceholders && x.CurrentCondition == 0 && (x.src != null || x.DescriptionId != 0)).ToList();
                                }
                                totaltanklist.AddRange(tanklistnew);

                            }
                            if (totaltanklist.Count > 0)
                            {
                                currentcondition = currentcondition == "" ? string.Join(",", totaltanklist.Select(a => a.CardName)) : currentcondition + "," + string.Join(",", totaltanklist.Select(a => a.CardName));
                            }
                        }
                        #endregion

                        tankimagecardList = ret.tankReportView.Sections.SelectMany(x => x.tankImageCards).ToList();

                        #region Tank Description

                        var desc_tanklist = tankimagecardList.Where(d => d.Id != 0 && ((d.DescriptionId == 0 && d.src != null) || (d.DescriptionId == 0 && d.CurrentCondition != 0))).ToList();
                        if (desc_tanklist.Count > 0)
                        {
                            description = description == "" ? string.Join(",", desc_tanklist.Select(a => a.CardName)) : description + "," + string.Join(",", desc_tanklist.Select(a => a.CardName));
                        }
                        #endregion

                        #region Tank Add'l description
                        var TankAdditionalDescId = tankui.SelectMany(s => s.imageDescriptions).Where(s => s.Description == "Others").ToList();
                        var TankAdditionalDesc = tankimagecardList.Where(d => d.DescriptionId != 0 && (d.AdditionalDescription == null || d.AdditionalDescription == "")).ToList();
                        var TankEmptyAdditionalDescriptionlist = TankAdditionalDesc.Where(a => TankAdditionalDescId.Any(b => b.Id == a.DescriptionId)).ToList();

                        if (TankEmptyAdditionalDescriptionlist.Count > 0)
                        {
                            additionaldescription = additionaldescription == "" ? string.Join(",", TankEmptyAdditionalDescriptionlist.Select(a => a.CardName)) : additionaldescription + "," + string.Join(",", TankEmptyAdditionalDescriptionlist.Select(a => a.CardName));
                        }
                        #endregion

                    }
                    #endregion

                    #region H & I Grading

                    if (ret.template != null && ret.template.handireport != null && ret.template.handireport.Grading != null && ret.template.handireport.Grading.Count > 0)
                    {
                        var handireport = ret.template.handireport.Grading.ToList();
                        var HRgradingcount = 0; var HRfinalcount = 0; var HRcheckbox = 0;
                        HRgradingcount = handireport.Count;
                        handireport.ForEach(e =>
                        {
                            e.Checkbox.ForEach(u =>
                            {
                                if (u.Value == true && HRcheckbox == 0)
                                {
                                    HRcheckbox++;
                                    HRfinalcount++;

                                }
                            });
                            HRcheckbox = 0;

                        });
                        if (HRgradingcount != HRfinalcount)
                        {
                            grading = grading + "Part H & I" + ",";
                        }

                    }
                    #endregion

                }

                grading = grading != "" ? grading.Remove(grading.Length - 1) : "";

                Allgrading = grading != "" ? Allgrading == "" ? grading : Allgrading + "," + grading : Allgrading;

                Allcurrentcondition = currentcondition != "" ? Allcurrentcondition == "" ? currentcondition : Allcurrentcondition + "," + currentcondition : Allcurrentcondition;

                Alldescription = description != "" ? Alldescription == "" ? description : Alldescription + "," + description : Alldescription;

                Alladditionaldescription = additionaldescription != "" ? Alladditionaldescription == "" ? additionaldescription : Alladditionaldescription + "," + additionaldescription : Alladditionaldescription;

                Result = Allcurrentcondition + "#" + Alldescription + "#" + Allgrading + "#" + Alladditionaldescription;

                return ServiceResult<string>.Success(Result);
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<int>> DeleteBulkUploadImage(int imageId)
        {
            var result = await _surveyReportRepository.DeleteBulkUploadImage(imageId);

            return ServiceResult<int>.Success(result);
        }

        public async Task<ServiceResult<bool>> DeleteImage(string assignmentId, string fileId)
        {
            var ret = await _surveyReportRepository.DeleteImage(assignmentId, fileId);
            return ServiceResult<bool>.Success(ret);
        }

        public async Task<ServiceResult<bool>> DeleteCard(int projectId, string cardId, string sequenceId, string assignmentId, string applicationId)
        {
            var ret = await _surveyReportRepository.DeleteCard(projectId, cardId, sequenceId, assignmentId, applicationId);
            return ServiceResult<bool>.Success(ret);
        }

        public string ResizeImage(byte[] imageBytes)
        {
            int imageWidthForScale = 624;
            using (var ms = new MemoryStream(imageBytes))
            {
                var img = Image.FromStream(ms);
                var curHt = img.Height;
                var curWd = img.Width;
                var WdRed = imageWidthForScale / (double)curWd;
                var HtRed = (int)(curHt * WdRed);
                var destRect = new Rectangle(0, 0, imageWidthForScale, HtRed);
                var destImage = new Bitmap(imageWidthForScale, HtRed);

                destImage.SetResolution(img.HorizontalResolution, img.VerticalResolution);

                using (var graphics = Graphics.FromImage(destImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                        graphics.DrawImage(img, destRect, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, wrapMode);
                    }
                }
                byte[] finalBytes;
                using (var msFinal = new MemoryStream())
                {
                    destImage.Save(msFinal, System.Drawing.Imaging.ImageFormat.Jpeg);
                    msFinal.Position = 0;
                    finalBytes = msFinal.ToArray();
                }
                return Convert.ToBase64String(finalBytes);
            }
        }

        public async Task<ServiceResult<bool>> UpdateGenericImageDescriptionDropdownCard(int templateId, int projectId, Guid sectionId, int cardId, int value, string cardName)
        {
            try
            {
                cardName = cardName.Trim();
                var imageCard = _projectReportRepository.GetGenericImagCardByName(projectId, templateId, sectionId, cardId).Result;
                if (imageCard != null && imageCard.Id != 0)
                {
                    imageCard.DescriptionId = value;
                    imageCard.AdditionalDescription = null;
                    imageCard.UpdatedDttm = DateTime.Now;
                    imageCard.CardName = cardName;
                    var result = _projectReportRepository.UpdateGenericImageCard(imageCard).Result;
                }
                else
                {
                    GenericImageCard tankImageCard = new GenericImageCard();
                    tankImageCard.ProjectId = projectId;
                    tankImageCard.TemplateId = templateId;
                    tankImageCard.SectionId = sectionId;
                    tankImageCard.CardNumber = cardId;
                    tankImageCard.DescriptionId = value;
                    tankImageCard.CreatedDttm = DateTime.Now;
                    tankImageCard.UpdatedDttm = DateTime.Now;
                    tankImageCard.IsActive = true;
                    tankImageCard.CardName = cardName;
                    tankImageCard.AdditionalDescription = null;
                    var result = await _projectReportRepository.CreateGenericImageCard(tankImageCard);

                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("false");
            }
        }

        public async Task<ServiceResult<bool>> UpdateGenericAdditionalDescription(int templateId, int projectId, Guid sectionId, int cardId, string value, string cardName)
        {
            try
            {
                cardName = cardName.Trim();
                var imageCard = _projectReportRepository.GetGenericImagCardByName(projectId, templateId, sectionId, cardId).Result;
                if (imageCard != null && imageCard.Id != 0)
                {
                    imageCard.AdditionalDescription = value;
                    imageCard.UpdatedDttm = DateTime.Now;
                    imageCard.CardName = cardName;
                    var result = _projectReportRepository.UpdateGenericImageCard(imageCard).Result;
                }
                else
                {
                    GenericImageCard tankImageCard = new GenericImageCard();
                    tankImageCard.ProjectId = projectId;
                    tankImageCard.TemplateId = templateId;
                    tankImageCard.SectionId = sectionId;
                    tankImageCard.CardNumber = cardId;
                    tankImageCard.AdditionalDescription = value;
                    tankImageCard.CreatedDttm = DateTime.Now;
                    tankImageCard.UpdatedDttm = DateTime.Now;
                    tankImageCard.IsActive = true;
                    tankImageCard.CardName = cardName;
                    var result = await _projectReportRepository.CreateGenericImageCard(tankImageCard);

                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("false");
            }
        }
        public async Task<ServiceResult<bool>> UpdateGenericCurrentCondition(int templateId, int projectId, Guid sectionId, int cardId, int value, string cardName)
        {
            try
            {
                cardName = cardName.Trim();
                var imageCard = _projectReportRepository.GetGenericImagCardByName(projectId, templateId, sectionId, cardId).Result;
                if (imageCard != null && imageCard.Id != 0)
                {
                    imageCard.CurrentCondition = value;
                    imageCard.UpdatedDttm = DateTime.Now;
                    imageCard.CardName = cardName;
                    var result = _projectReportRepository.UpdateGenericImageCard(imageCard).Result;
                }
                else
                {
                    GenericImageCard projectCardMapping = new GenericImageCard();
                    projectCardMapping.ProjectId = projectId;
                    projectCardMapping.TemplateId = templateId;
                    projectCardMapping.SectionId = sectionId;
                    projectCardMapping.CardNumber = cardId;
                    projectCardMapping.CurrentCondition = value;
                    projectCardMapping.CreatedDttm = DateTime.Now;
                    projectCardMapping.UpdatedDttm = DateTime.Now;
                    projectCardMapping.CardName = cardName;
                    projectCardMapping.IsActive = true;
                    var result = await _projectReportRepository.CreateGenericImageCard(projectCardMapping);

                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("false");
            }
        }

        public async Task<ServiceResult<UploadPhotoResultDto>> UploadGenericImage(IFormFile file, int templateId, int projectId, Guid sectionId, int cardId, int imageId, bool replaceImage = false, string cardName = null)
        {
            if (cardName == null)
            {
                cardName = "";
            }
            cardName = cardName.Trim();
            string resizedImage = "";
            string newfileId = Guid.NewGuid().ToString();
            if (file != null && file.Length > 0)
            {
                var fileContent = ContentDispositionHeaderValue.Parse(file.ContentDisposition);

                using (var ms = new MemoryStream())
                {
                    file.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    resizedImage = ResizeImage(fileBytes);
                    var imgId = await _surveyReportRepository.UploadImage("unknown", newfileId);
                    await _surveyReportRepository.AddEditImageNew(imgId, resizedImage);
                    var imageCard = _projectReportRepository.GetGenericImagCardByName(projectId, templateId, sectionId, cardId).Result;
                    if (imageCard == null || imageCard.Id == 0)
                    {
                        GenericImageCard projectCardMapping = new GenericImageCard();

                        projectCardMapping.ProjectId = projectId;
                        projectCardMapping.TemplateId = templateId;
                        projectCardMapping.SectionId = sectionId;
                        projectCardMapping.CardNumber = cardId;
                        projectCardMapping.ImageId = imgId;
                        projectCardMapping.CurrentCondition = 0;
                        projectCardMapping.DescriptionId = 0;
                        projectCardMapping.AdditionalDescription = null;
                        projectCardMapping.IsActive = true;
                        projectCardMapping.CreatedDttm = DateTime.Now;
                        projectCardMapping.UpdatedDttm = DateTime.Now;
                        projectCardMapping.CardName = cardName;
                        var result = await _projectReportRepository.CreateGenericImageCard(projectCardMapping);
                    }
                    else
                    {
                        imageCard.UpdatedDttm = DateTime.Now;
                        imageCard.CreatedDttm = DateTime.Now;
                        imageCard.ImageId = imgId;
                        imageCard.CardName = cardName;
                        var result = await _projectReportRepository.UpdateGenericImageCard(imageCard);
                    }

                }
            }

            return ServiceResult<UploadPhotoResultDto>.Success(
                   new UploadPhotoResultDto
                   {
                       FileId = newfileId,
                       Image = resizedImage
                   });
        }

        public async Task<ServiceResult<bool>> DeleteGenericImage(int templateId, int projectId, Guid sectionId, int cardId, int imageId)
        {
            var imageCard = _projectReportRepository.GetGenericImagCardByName(projectId, templateId, sectionId, cardId).Result;
            if (imageCard.ImageId != 0)
            {
                imageCard.ImageId = 0;
                imageCard.UpdatedDttm = DateTime.Now;
                var result = _projectReportRepository.UpdateGenericImageCard(imageCard).Result;
            }
            return ServiceResult<bool>.Success(true);
        }

        // New service methods for converted APIs
        public async Task<ServiceResult<UploadPhotoResultDto>> UploadImage(IFormFile file, int templateId, int projectId, Guid sectionId, int cardId, int imageId, bool replaceImage = false)
        {
            if (file == null || file.Length == 0)
                return ServiceResult<UploadPhotoResultDto>.Failure("Invalid file");

            string resizedImage = "";
            string newfileId = Guid.NewGuid().ToString();

            try
            {
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();
                    resizedImage = ResizeImage(fileBytes);
                    var imgId = await _surveyReportRepository.UploadImage("unknown", newfileId);
                    await _surveyReportRepository.AddEditImageNew(imgId, resizedImage);

                    var imageCard = await _surveyReportRepository.GetProjectImagCardById(projectId, templateId, sectionId, cardId);
                    if (imageCard == null || imageCard.Id == 0)
                    {
                        ProjectCardMapping projectCardMapping = new ProjectCardMapping();
                        projectCardMapping.ProjectId = projectId;
                        projectCardMapping.TemplateId = templateId;
                        projectCardMapping.SectionId = sectionId;
                        projectCardMapping.CardId = cardId;
                        projectCardMapping.ImageId = imgId;
                        projectCardMapping.CurrentCondition = 0;
                        projectCardMapping.DescriptionId = 0;
                        projectCardMapping.AdditionalDescription = null;
                        projectCardMapping.IsDeleted = false;
                        projectCardMapping.CreatedDttm = DateTime.Now;
                        projectCardMapping.UpdateDttm = DateTime.Now;
                        await _surveyReportRepository.CreateProjectImageCard(projectCardMapping);
                    }
                    else
                    {
                        imageCard.UpdateDttm = DateTime.Now;
                        imageCard.CreatedDttm = DateTime.Now;
                        imageCard.ImageId = imgId;
                        await _surveyReportRepository.UpdateProjectImageCard(imageCard);
                    }
                }

                return ServiceResult<UploadPhotoResultDto>.Success(
                    new UploadPhotoResultDto
                    {
                        FileId = newfileId,
                        Image = resizedImage
                    });
            }
            catch (Exception ex)
            {
                return ServiceResult<UploadPhotoResultDto>.Failure("Failed to upload image");
            }
        }

        public async Task<ServiceResult<bool>> DeleteImage(int templateId, int projectId, Guid sectionId, int cardId, int imageId)
        {
            try
            {
                var imageCard = await _surveyReportRepository.GetProjectImagCardById(projectId, templateId, sectionId, cardId);
                if (imageCard != null && imageCard.ImageId != 0)
                {
                    imageCard.ImageId = 0;
                    imageCard.UpdateDttm = DateTime.Now;
                    await _surveyReportRepository.UpdateProjectImageCard(imageCard);
                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("Failed to delete image");
            }
        }

        public async Task<ServiceResult<bool>> UpdateImageDescriptionDropdownCard(int templateId, int projectId, Guid sectionId, int cardId, int value)
        {
            try
            {
                var imageCard = await _surveyReportRepository.GetProjectImagCardById(projectId, templateId, sectionId, cardId);
                if (imageCard != null && imageCard.Id != 0)
                {
                    imageCard.DescriptionId = value;
                    imageCard.AdditionalDescription = null;
                    imageCard.UpdateDttm = DateTime.Now;
                    await _surveyReportRepository.UpdateProjectImageCard(imageCard);
                }
                else
                {
                    ProjectCardMapping projectCardMapping = new ProjectCardMapping();
                    projectCardMapping.ProjectId = projectId;
                    projectCardMapping.TemplateId = templateId;
                    projectCardMapping.SectionId = sectionId;
                    projectCardMapping.CardId = cardId;
                    projectCardMapping.DescriptionId = value;
                    projectCardMapping.IsDeleted = false;
                    projectCardMapping.CreatedDttm = DateTime.Now;
                    projectCardMapping.UpdateDttm = DateTime.Now;
                    await _surveyReportRepository.CreateProjectImageCard(projectCardMapping);
                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("Failed to update image description");
            }
        }

        public async Task<ServiceResult<bool>> UpdateAdditionalDescription(int templateId, int projectId, Guid sectionId, int cardId, string value)
        {
            try
            {
                var imageCard = await _surveyReportRepository.GetProjectImagCardById(projectId, templateId, sectionId, cardId);
                if (imageCard != null && imageCard.Id != 0)
                {
                    imageCard.AdditionalDescription = value;
                    imageCard.UpdateDttm = DateTime.Now;
                    await _surveyReportRepository.UpdateProjectImageCard(imageCard);
                }
                else
                {
                    ProjectCardMapping projectCardMapping = new ProjectCardMapping();
                    projectCardMapping.ProjectId = projectId;
                    projectCardMapping.TemplateId = templateId;
                    projectCardMapping.SectionId = sectionId;
                    projectCardMapping.CardId = cardId;
                    projectCardMapping.AdditionalDescription = value;
                    projectCardMapping.IsDeleted = false;
                    projectCardMapping.CreatedDttm = DateTime.Now;
                    projectCardMapping.UpdateDttm = DateTime.Now;
                    await _surveyReportRepository.CreateProjectImageCard(projectCardMapping);
                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("Failed to update additional description");
            }
        }

        public async Task<ServiceResult<bool>> UpdateCurrentCondition(int templateId, int projectId, Guid sectionId, int cardId, int value)
        {
            try
            {
                var imageCard = await _surveyReportRepository.GetProjectImagCardById(projectId, templateId, sectionId, cardId);
                if (imageCard != null && imageCard.Id != 0)
                {
                    imageCard.CurrentCondition = value;
                    imageCard.UpdateDttm = DateTime.Now;
                    await _surveyReportRepository.UpdateProjectImageCard(imageCard);
                }
                else
                {
                    ProjectCardMapping projectCardMapping = new ProjectCardMapping();
                    projectCardMapping.ProjectId = projectId;
                    projectCardMapping.TemplateId = templateId;
                    projectCardMapping.SectionId = sectionId;
                    projectCardMapping.CardId = cardId;
                    projectCardMapping.CurrentCondition = value;
                    projectCardMapping.IsDeleted = false;
                    projectCardMapping.CreatedDttm = DateTime.Now;
                    projectCardMapping.UpdateDttm = DateTime.Now;
                    await _surveyReportRepository.CreateProjectImageCard(projectCardMapping);
                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("Failed to update current condition");
            }
        }

        public async Task<ServiceResult<bool>> UpdateSectionNote(int templateId, int projectId, Guid sectionId, string value)
        {
            try
            {
                var section = await _surveyReportRepository.GetProjectReportTemplate(projectId, templateId, sectionId);
                if (section != null && section.Id != Guid.Empty)
                {
                    section.Notes = value;
                    section.UpdateDttm = DateTime.Now;
                    await _surveyReportRepository.UpdateProjectTemplate(section);
                }
                else
                {
                    ProjectReportMapping projectReportMapping = new ProjectReportMapping();
                    projectReportMapping.Id = Guid.NewGuid();
                    projectReportMapping.ProjectId = projectId;
                    projectReportMapping.TemplateId = templateId;
                    projectReportMapping.SectionId = sectionId;
                    projectReportMapping.Notes = value;
                    projectReportMapping.IsDeleted = false;
                    projectReportMapping.CreatedDttm = DateTime.Now;
                    projectReportMapping.UpdateDttm = DateTime.Now;
                    await _surveyReportRepository.CreateProjectTemplate(projectReportMapping);
                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("Failed to update section note");
            }
        }

        public async Task<ServiceResult<bool>> UpdateGradingNotes(int templateId, int projectId, Guid sectionId, int gradingId, string value)
        {
            try
            {
                var grading = await _surveyReportRepository.GetProjectGradingById(projectId, templateId, sectionId, gradingId);
                if (grading != null && grading.Id != 0)
                {
                    grading.Value = value;
                    grading.UpdateDttm = DateTime.Now;
                    await _surveyReportRepository.UpdateProjectGrading(grading);
                }
                else
                {
                    ProjectGradingMapping projectGradingMapping = new ProjectGradingMapping();
                    projectGradingMapping.ProjectId = projectId;
                    projectGradingMapping.TemplateId = templateId;
                    projectGradingMapping.SectionId = sectionId;
                    projectGradingMapping.GradingId = gradingId;
                    projectGradingMapping.Value = value;
                    projectGradingMapping.IsDeleted = false;
                    projectGradingMapping.CreatedDttm = DateTime.Now;
                    projectGradingMapping.UpdateDttm = DateTime.Now;
                    await _surveyReportRepository.CreateProjectGrading(projectGradingMapping);
                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("Failed to update grading notes");
            }
        }

        public async Task<ServiceResult<bool>> UpdateGradingCondition(int templateId, int projectId, Guid sectionId, int gradingId, List<GradingChoiceUpdateDto> choices)
        {
            try
            {
                var gradingconditions = await _surveyReportRepository.GetProjectGradingConditionMapping(projectId, templateId, sectionId, gradingId);

                foreach (var choice in choices)
                {
                    var gradingcondition = gradingconditions.FirstOrDefault(x => x.GradingConditionLabel == choice.ChoiceName);

                    if (gradingcondition != null && gradingcondition.Id != 0)
                    {
                        gradingcondition.GradingConditionValue = choice.Value;
                        gradingcondition.UpdateDttm = DateTime.Now;
                        await _surveyReportRepository.UpdateProjectGradingConditionMapping(gradingcondition);
                    }
                    else
                    {
                        ProjectGradingConditionMapping projectGradingConditionMapping = new ProjectGradingConditionMapping();
                        projectGradingConditionMapping.ProjectId = projectId;
                        projectGradingConditionMapping.TemplateId = templateId;
                        projectGradingConditionMapping.SectionId = sectionId;
                        projectGradingConditionMapping.GradingId = gradingId;
                        projectGradingConditionMapping.GradingConditionLabel = choice.ChoiceName;
                        projectGradingConditionMapping.GradingConditionValue = choice.Value;
                        projectGradingConditionMapping.IsDeleted = false;
                        projectGradingConditionMapping.CreatedDttm = DateTime.Now;
                        projectGradingConditionMapping.UpdateDttm = DateTime.Now;
                        await _surveyReportRepository.CreateProjectGradingConditionMapping(projectGradingConditionMapping);
                    }
                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("Failed to update grading condition");
            }
        }

        public async Task<ServiceResult<bool>> UpdateH_IReport(Guid Id, int ProjectId, string CompanyName, string CertificateNumber, DateTime ExpiryDate, string ThicknessMeasurementA, string ThicknessMeasurementB, string ThicknessMeasurementC, string AdditionalSurveyorComments, string MaintanceExecuted, string Accommodation, string PhotoGraphs, string OperationalTestLocation, DateTime OperationalTestStart, DateTime OperationalTestFinish, string OptionalTestSatisFactory, string AssociatedReportNumber, string AssociatedReportDate, string Remarks, string LeadSurveyorName, string LeadSurveyorNumber, string SurveyorName, string SurveyorNumber, DateTime LastVisitDate, string IndividualA, string IndividualB, string IndividualC, string IndividualD, string IndividualE, string IndividualF, string ScouringOrPitting, string SubstantialCorrosionRemaining, string StatutoryDeficiencies, string FoundSatisfactory, string UnderRemarks, string BottomPlattingScouringOrPitting)
        {
            try
            {
                HandIReport handIReport = await _surveyReportRepository.GetHIReport(ProjectId);
                if (handIReport == null)
                {
                    handIReport = new HandIReport();
                }

                handIReport.ProjectId = ProjectId;
                handIReport.CompanyName = CompanyName;
                handIReport.CertificateNumber = CertificateNumber;
                handIReport.ExpiryDate = ExpiryDate;
                handIReport.ThicknessMeasurementA = ThicknessMeasurementA;
                handIReport.ThicknessMeasurementB = ThicknessMeasurementB;
                handIReport.ThicknessMeasurementC = ThicknessMeasurementC;
                handIReport.AdditionalSurveyorComments = AdditionalSurveyorComments;
                handIReport.MaintenanceExecuted = MaintanceExecuted;
                handIReport.Accommodation = Accommodation;
                handIReport.PhotoGraphs = PhotoGraphs;
                handIReport.OperationalTestLocation = OperationalTestLocation;
                handIReport.OperationalTestStart = OperationalTestStart;
                handIReport.OperationalTestFinish = OperationalTestFinish;
                handIReport.OptionalTestSatisFactory = OptionalTestSatisFactory;
                handIReport.AssociatedReportNumber = AssociatedReportNumber;
                handIReport.AssociatedReportDate = AssociatedReportDate;
                handIReport.Remarks = Remarks;
                handIReport.LeadSurveyorName = LeadSurveyorName;
                handIReport.LeadSurveyorNumber = LeadSurveyorNumber;
                handIReport.SurveyorName = SurveyorName;
                handIReport.SurveyorNumber = SurveyorNumber;
                handIReport.LastVisitDate = LastVisitDate;
                handIReport.IndividualA = IndividualA;
                handIReport.IndividualB = IndividualB;
                handIReport.IndividualC = IndividualC;
                handIReport.IndividualD = IndividualD;
                handIReport.IndividualE = IndividualE;
                handIReport.IndividualF = IndividualF;
                handIReport.SubstantialCorrosionRemaining = SubstantialCorrosionRemaining;
                handIReport.StatutoryDeficiencies = StatutoryDeficiencies;
                handIReport.FoundSatisfactory = FoundSatisfactory;
                handIReport.UnderRemarks = UnderRemarks;
                handIReport.ScouringOrPitting = ScouringOrPitting;
                handIReport.BottomPlattingScouringOrPitting = BottomPlattingScouringOrPitting;
                handIReport.UpdatedDttm = DateTime.Now;
                handIReport.IsActive = true;
                handIReport.IsDeleted = false;

                if (handIReport.Id != Guid.Empty)
                {
                    handIReport.UpdatedDttm = DateTime.Now;
                    await _surveyReportRepository.UpdateHIReport(handIReport);
                }
                else
                {
                    handIReport.Id = Guid.NewGuid();
                    handIReport.CreatedDttm = DateTime.Now;
                    handIReport.UpdatedDttm = DateTime.Now;
                    handIReport.IsActive = true;
                    handIReport.IsDeleted = false;
                    await _surveyReportRepository.CreateHIReport(handIReport);
                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("Failed to update H&I report");
            }
        }

        public async Task<ServiceResult<string>> UpdateReport(int projectId, int templateId, int reportPart, string value, int imonumber, int hullNo = 1, string surveyDate = null)
        {
            try
            {
                // Survey status (33-39) and statutory certificate (40-54, 74) dates can be entered as "N/A"
                if ((reportPart >= 33 && reportPart <= 54) || reportPart == 74)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        value = null;
                    }
                    else
                    {
                        value = NotApplicableDate.ToStorable(value);
                    }

                    if (string.IsNullOrWhiteSpace(surveyDate))
                    {
                        surveyDate = null;
                    }
                    else
                    {
                        surveyDate = NotApplicableDate.ToStorable(surveyDate);
                    }
                }

                Report report = new Report();
                if (reportPart >= 1 && reportPart <= 3)
                {
                    switch (reportPart)
                    {
                        case 1:
                            report.ReportNo = value;
                            break;
                        case 2:
                            report.ReportPort = value;
                            break;
                        case 3:
                            report.ReportDate = value;
                            break;
                    }
                    report.Vessel = new Vessel();
                    await UpdateReportMainData(report, projectId);
                }
                if ((reportPart >= 4 && reportPart < 33) || (reportPart == 72 || reportPart == 73) || reportPart == 90)
                {
                    report.Vessel = new Core.Models.Survey.Vessel();
                    switch (reportPart)
                    {
                        case 90:
                            report.Vessel.SelectedPrefix = value == null ? "" : value;
                            break;
                        case 4:
                            report.Vessel.VesselName = value == null ? "" : value;
                            break;
                        case 5:
                            report.Vessel.VesselType = value == null ? "" : value;
                            break;
                        case 6:
                            report.Vessel.YearBuilt = Convert.ToInt32(value);
                            break;
                        case 7:
                            report.Vessel.BuiltBy = value == null ? "" : value;
                            break;
                        case 8:
                            report.Vessel.HullNo = value == null ? "" : value;
                            break;
                        case 9:
                            report.Vessel.FlagName = value == null ? "" : value;
                            break;
                        case 10:
                            report.Vessel.Homeport = value == null ? "" : value;
                            break;
                        case 11:
                            report.Vessel.OfficalNumber = value == null ? "" : value;
                            break;
                        case 12:
                            report.Vessel.CallSign = value == null ? "" : value;
                            break;
                        case 13:
                            report.Vessel.Classed = value == null ? "" : value;
                            break;
                        case 14:
                            report.Vessel.ClassNo = value == null ? "" : value;
                            break;
                        case 15:
                            report.Vessel.ClassNotation = value == null ? "" : value;
                            break;
                        case 16:
                            report.Vessel.Machinery = value == null ? "" : value;
                            break;
                        case 17:
                            report.Vessel.Other = value == null ? "" : value;
                            break;
                        case 18:
                            report.Vessel.PreviousName = value == null ? "" : value;
                            break;
                        case 19:
                            report.Vessel.OwnerName = value == null ? "" : value;
                            break;
                        case 20:
                            report.Vessel.Manager = value == null ? "" : value;
                            break;
                        case 21:
                            report.Vessel.Length = float.Parse(value ?? "0.1");
                            break;
                        case 22:
                            report.Vessel.Breadth = float.Parse(value ?? "0.1");
                            break;
                        case 23:
                            report.Vessel.DEPTH = float.Parse(value ?? "0.1");
                            break;
                        case 24:
                            report.Vessel.Draft = float.Parse(value ?? "0.1");
                            break;
                        case 25:
                            report.Vessel.DEADWEIGHT = value == null ? "" : value;
                            break;
                        case 26:
                            report.Vessel.GrossTons = float.Parse(value ?? "0.1");
                            break;
                        case 27:
                            report.Vessel.NetTons = float.Parse(value ?? "0.1");
                            break;
                        case 28:
                            report.Vessel.PropulsionType = value == null ? "" : value;
                            break;
                        case 29:
                            report.Vessel.KW = value == null ? "" : value;
                            break;
                        case 30:
                            report.Vessel.ShaftRPM = float.Parse(value ?? "0.1");
                            break;
                        case 31:
                            report.Vessel.PropulsionManufacturer = value == null ? "" : value;
                            break;
                        case 32:
                            report.Vessel.GearManufacturer = value == null ? "" : value;
                            break;
                        case 72:
                            report.Vessel.LastVisitDate = value == null ? "" : value;
                            break;
                        case 73:
                            report.Vessel.FirstVisitDate = value == null ? "" : value;
                            break;
                    }
                    await UpdateReportMainData(report, projectId);
                }
                if (reportPart >= 33 && reportPart <= 39)
                {
                    report.SurveyStatus = new SurveyStatus();
                    var temp = _vesselRepository.GetSurveyStatus(projectId).Result.SingleOrDefault()?.SpecialHullNo;
                    switch (reportPart)
                    {
                        case 33:
                            report.SurveyStatus.ClassSurveyDate = Convert.ToDateTime(value);
                            break;
                        case 34:
                            report.SurveyStatus.DryDockDate = Convert.ToDateTime(value);
                            break;
                        case 35:
                            report.SurveyStatus.SpecialHullNo = Convert.ToInt32(value);
                            if (value != null)
                            {
                                report.SurveyStatus.GetType().GetProperty("SpecialContinuousHull" + Convert.ToInt32(value)).SetValue(report.SurveyStatus, surveyDate != null ? Convert.ToDateTime(surveyDate) : null);
                            }
                            break;
                        case 36:
                            if ((report.SurveyStatus.SpecialHullNo != null || temp != null))
                            {
                                report.SurveyStatus.GetType().GetProperty("SpecialContinuousHull" + (report.SurveyStatus.SpecialHullNo != null ? report.SurveyStatus.SpecialHullNo : temp)).SetValue(report.SurveyStatus, Convert.ToDateTime(value));
                            }
                            break;
                        case 37:
                            report.SurveyStatus.SpecialContinuousMachineryDate = Convert.ToDateTime(value);
                            break;
                        case 38:
                            report.SurveyStatus.BoilerDate = Convert.ToDateTime(value);
                            break;
                        case 39:
                            report.SurveyStatus.TailshaftDate = Convert.ToDateTime(value);
                            break;
                    }
                    await _vesselRepository.UpdateSurveyAudit(report.SurveyStatus, projectId);
                }
                if ((reportPart >= 40 && reportPart <= 54) || reportPart == 74)
                {
                    report.StatutoryCertificatesExpirationDates = new StatutoryCertificates();
                    report.StatutoryCertificatesIssuedDates = new StatutoryCertificates();
                    switch (reportPart)
                    {
                        case 40:
                            report.StatutoryCertificatesExpirationDates.LLLIDate = Convert.ToDateTime(value);
                            break;
                        case 41:
                            report.StatutoryCertificatesExpirationDates.SafetyConDate = Convert.ToDateTime(value);
                            break;
                        case 42:
                            report.StatutoryCertificatesExpirationDates.SafetyEquipmentDate = Convert.ToDateTime(value);
                            break;
                        case 43:
                            report.StatutoryCertificatesExpirationDates.SafetyRadioDate = Convert.ToDateTime(value);
                            break;
                        case 44:
                            report.StatutoryCertificatesExpirationDates.IOPPDate = Convert.ToDateTime(value);
                            break;
                        case 45:
                            report.StatutoryCertificatesExpirationDates.IAPDate = Convert.ToDateTime(value);
                            break;
                        case 46:
                            report.StatutoryCertificatesExpirationDates.ISPPSewageDate = Convert.ToDateTime(value);
                            break;
                        case 47:
                            report.StatutoryCertificatesIssuedDates.IEEDate = Convert.ToDateTime(value);
                            break;
                        case 48:
                            report.StatutoryCertificatesIssuedDates.CargoGearAnnualCertificate = Convert.ToDateTime(value);
                            break;
                        case 49:
                            report.StatutoryCertificatesExpirationDates.CargoGearReTestDate = Convert.ToDateTime(value);
                            break;
                        case 50:
                            report.StatutoryCertificatesIssuedDates.IAFSDate = Convert.ToDateTime(value);
                            break;
                        case 51:
                            report.StatutoryCertificatesExpirationDates.IBWMDate = Convert.ToDateTime(value);
                            break;
                        case 52:
                            report.StatutoryCertificatesExpirationDates.ISMDate = Convert.ToDateTime(value);
                            break;
                        case 53:
                            report.StatutoryCertificatesExpirationDates.ISPSDate = Convert.ToDateTime(value);
                            break;
                        case 54:
                            report.StatutoryCertificatesExpirationDates.MaritimeLabourDate = Convert.ToDateTime(value);
                            break;
                        case 74:
                            report.StatutoryCertificatesExpirationDates.GasCarrierFitness = Convert.ToDateTime(value);
                            break;
                    }
                    await _vesselRepository.UpdateStatutoryCertificate(projectId, report);
                }
                return ServiceResult<string>.Success("success");
            }
            catch (Exception ex)
            {
                return ServiceResult<string>.Failure("error");
            }
        }

        private async Task UpdateReportMainData(Report report, int projectId)
        {
            var vesselMainData = await _vesselRepository.GetVesselMainData(projectId);
            var existing = vesselMainData.FirstOrDefault();

            await _vesselRepository.UpdateReportMainDataAsync(report, projectId);

        }

        public async Task<ServiceResult<object>> GetPhotoPlacementDropdownData(int projectId, int templateId, Guid sectionId)
        {
            var reportSectionModel = await GetSectionIdsByProjectId(projectId, templateId);

            var templateSectionIds = reportSectionModel.Data.ReportPartGrid
                .Where(x => x.TemplateId == templateId)
                .FirstOrDefault()
                ?.SectionIds;

            var data = GetBulkUploadDropdownValues(projectId, templateId, sectionId, templateSectionIds);

            return ServiceResult<object>.Success(new
            {
                imageCards = data.ImageCard,
                imageDescriptions = data.ImageDescription,
                conditions = data.CurrentCondition
            });
        }
    }
}

