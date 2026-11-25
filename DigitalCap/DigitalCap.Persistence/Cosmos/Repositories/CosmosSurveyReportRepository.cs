using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.VesselModel;
using DigitalCap.Core.ViewModels;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosSurveyReportRepository : CosmosRepositoryBase<ProjectReportMapping>, ISurveyReportRepository
    {
        private static readonly DateTime SqlMinDate = new DateTime(1753, 1, 1);

        private readonly ICosmosContainerFactory _containerFactory;
        private readonly ISequenceService _sequenceService;
        private readonly IFreedomAPIRepository _freedomApiRepository;
        private readonly ILogger<CosmosSurveyReportRepository> _logger;

        private readonly Container _bulkUploadContainer;
        private readonly Container _currentConditionContainer;
        private readonly Container _genericImageCardContainer;
        private readonly Container _gradingContainer;
        private readonly Container _gradingConditionContainer;
        private readonly Container _gradingSectionMappingContainer;
        private readonly Container _hiReportTemplateContainer;
        private readonly Container _handIProjectGradingContainer;
        private readonly Container _handIProjectGradingConditionsContainer;
        private readonly Container _imageCardContainer;
        private readonly Container _projectGradingConditionMappingContainer;
        private readonly Container _projectGradingMappingContainer;
        private readonly Container _projectImageCardsContainer;
        private readonly Container _projectTemplateMappingContainer;
        private readonly Container _reportTemplateContainer;
        private readonly Container _reportTemplateSectionContainer;
        private readonly Container _subSectionContainer;
        private readonly Container _tankImageCardContainer;
        private readonly Container _templateSectionMappingContainer;
        private readonly Container _capLogContainer;
        private readonly Container _imageDataContainer;
        private readonly Container _imagesContainer;

        public CosmosSurveyReportRepository(
            ICosmosContainerFactory containerFactory,
            ISequenceService sequenceService,
            IFreedomAPIRepository freedomApiRepository,
            ILogger<CosmosSurveyReportRepository> logger)
            : base(containerFactory, CosmosContainers.dbo_ProjectTemplateMapping, logger)
        {
            _containerFactory = containerFactory ?? throw new ArgumentNullException(nameof(containerFactory));
            _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
            _freedomApiRepository = freedomApiRepository ?? throw new ArgumentNullException(nameof(freedomApiRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _bulkUploadContainer = _containerFactory.GetContainer(CosmosContainers.dbo_BulkUpload);
            _currentConditionContainer = _containerFactory.GetContainer(CosmosContainers.dbo_CurrentCondition);
            _genericImageCardContainer = _containerFactory.GetContainer(CosmosContainers.dbo_GenericImageCard);
            _gradingContainer = _containerFactory.GetContainer(CosmosContainers.dbo_Grading);
            _gradingConditionContainer = _containerFactory.GetContainer(CosmosContainers.dbo_GradingCondition);
            _gradingSectionMappingContainer = _containerFactory.GetContainer(CosmosContainers.dbo_GradingSectionMapping);
            _hiReportTemplateContainer = _containerFactory.GetContainer(CosmosContainers.dbo_H_IReportTemplate);
            _handIProjectGradingContainer = _containerFactory.GetContainer(CosmosContainers.dbo_HandIProjectGrading);
            _handIProjectGradingConditionsContainer = _containerFactory.GetContainer(CosmosContainers.dbo_HandIProjectGradingConditions);
            _imageCardContainer = _containerFactory.GetContainer(CosmosContainers.dbo_ImageCard);
            _projectGradingConditionMappingContainer = _containerFactory.GetContainer(CosmosContainers.dbo_ProjectGradingConditionMapping);
            _projectGradingMappingContainer = _containerFactory.GetContainer(CosmosContainers.dbo_ProjectGradingMapping);
            _projectImageCardsContainer = _containerFactory.GetContainer(CosmosContainers.dbo_ProjectImageCards);
            _projectTemplateMappingContainer = _containerFactory.GetContainer(CosmosContainers.dbo_ProjectTemplateMapping);
            _reportTemplateContainer = _containerFactory.GetContainer(CosmosContainers.dbo_ReportTemplate);
            _reportTemplateSectionContainer = _containerFactory.GetContainer(CosmosContainers.dbo_ReportTemplateSection);
            _subSectionContainer = _containerFactory.GetContainer(CosmosContainers.dbo_SubSection);
            _tankImageCardContainer = _containerFactory.GetContainer(CosmosContainers.dbo_TankImageCard);
            _templateSectionMappingContainer = _containerFactory.GetContainer(CosmosContainers.dbo_TemplateSectionMapping);
            _capLogContainer = _containerFactory.GetContainer(CosmosContainers.upskill_CapLog);
            _imageDataContainer = _containerFactory.GetContainer(CosmosContainers.upskill_ImageData);
            _imagesContainer = _containerFactory.GetContainer(CosmosContainers.upskill_Images);
        }

        public void Commit()
        {
            // Cosmos: writes commit individually; no unit of work transaction needed
        }

        #region Non-SQL Methods (copied unchanged from SQL repository)

        public DateTime? FindSpecialHull(int num, IEnumerable<VesselSurvey> vesselSurvey)
        {
            return vesselSurvey.Where(x => x.Description.Contains("Hull " + num.ToString())).Select(x => x.DueDate).FirstOrDefault();
        }

        public async Task<SurveyStatus> MapSurveyStatus(string classNumber, int imo)
        {
            var vesselSurvey = _freedomApiRepository.GetSurveys(classNumber).Result;
            SurveyStatus survey = new SurveyStatus();
            if (vesselSurvey != null)
            {
                survey.IMONum = imo;
                survey.DryDockDate = vesselSurvey.Where(x => x.Type.Contains("Drydocking")).Select(x => x.DueDate).FirstOrDefault();
                survey.SpecialContinuousMachineryDate = vesselSurvey.Where(x => x.Type.Contains("Special") && x.Type.Contains("Survey") && (x.Type.Contains("Machinery") || x.Type.Contains("Production"))).Select(x => x.DueDate).FirstOrDefault();
                survey.BoilerDate = vesselSurvey.Where(x => x.Type.Contains("Boiler")).Select(x => x.DueDate).FirstOrDefault();
                survey.TailshaftDate = vesselSurvey.Where(x => x.Type.Contains("Tail")).Select(x => x.DueDate).FirstOrDefault();
                survey.ClassSurveyDate = vesselSurvey.Where(x => x.Type.Contains("Cargo Gear")).Select(x => x.DueDate).FirstOrDefault();

                var special_survey_hull_list = vesselSurvey.Where(x => x.Type.Contains("Special") && x.Type.Contains("Survey") && x.Type.Contains("Hull")).ToList();
                if (special_survey_hull_list != null && special_survey_hull_list.Count > 0)
                {
                    survey.SpecialContinuousHull1 = FindSpecialHull(1, special_survey_hull_list);
                    survey.SpecialContinuousHull2 = FindSpecialHull(2, special_survey_hull_list);
                    survey.SpecialContinuousHull3 = FindSpecialHull(3, special_survey_hull_list);
                    survey.SpecialContinuousHull4 = FindSpecialHull(4, special_survey_hull_list);
                    survey.SpecialContinuousHull5 = FindSpecialHull(5, special_survey_hull_list);
                    survey.SpecialContinuousHull6 = FindSpecialHull(6, special_survey_hull_list);
                    survey.SpecialContinuousHull7 = FindSpecialHull(7, special_survey_hull_list);
                    survey.SpecialContinuousHull8 = FindSpecialHull(8, special_survey_hull_list);
                    survey.SpecialContinuousHull9 = FindSpecialHull(9, special_survey_hull_list);
                    survey.SpecialContinuousHull10 = FindSpecialHull(10, special_survey_hull_list);
                    survey.SpecialContinuousHull11 = FindSpecialHull(11, special_survey_hull_list);
                    survey.SpecialContinuousHull12 = FindSpecialHull(12, special_survey_hull_list);
                    survey.SpecialContinuousHull13 = FindSpecialHull(13, special_survey_hull_list);
                    survey.SpecialContinuousHull14 = FindSpecialHull(14, special_survey_hull_list);
                    survey.SpecialContinuousHull15 = FindSpecialHull(15, special_survey_hull_list);
                }
            }
            return survey;
        }

        public async Task<Report> MapStatuatoryCertificates(string classNumber, Report report)
        {
            try
            {
                var certificates = _freedomApiRepository.GetCertificates(classNumber).Result;
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

        #endregion

        #region Public Domain Methods (ISurveyReportRepository)

        /// <summary>
        /// Replaces stored procedure [dbo].[GetTemplateSections].
        /// </summary>
        public async Task<List<ReportTemplateSection>> GetTemplateSections(int templateId)
        {
            try
            {
                return await Sp_GetTemplateSections(templateId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetReportTemplateList].
        /// </summary>
        public async Task<List<ReportTemplate>> GetReportTemplateList()
        {
            return await Sp_GetReportTemplateList();
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetExistingReportPartsSectionIds].
        /// </summary>
        public async Task<List<int>> GetExistingReportPartsSectionIds(int projectId)
        {
            return await Sp_GetExistingReportPartsSectionIds(projectId);
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetTemplateTitle].
        /// </summary>
        public async Task<ReportTemplateUI> GetTemplateTitle(int templateId)
        {
            try
            {
                return await Sp_GetTemplateTitle(templateId);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetCurrentCondition].
        /// </summary>
        public async Task<List<CurrentCondition>> GetCurrentCondition()
        {
            return await Sp_GetCurrentCondition();
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectTemplateMapping].
        /// </summary>
        public async Task<ProjectReportMapping> GetProjectReportTemplate(int projectId, int templateId, Guid sectionId)
        {
            try
            {
                var result = await Sp_SelectProjectTemplateMapping(projectId, templateId, sectionId);
                // SQL: no ORDER BY; pick lowest Id in SqlGuid byte order for deterministic parity
                return result.OrderBy(x => new SqlGuid(x.Id)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectTemplateMapping].
        /// </summary>
        public async Task<List<ProjectReportMapping>> GetProjectReportTemplate(int projectId, int templateId)
        {
            return await Sp_SelectProjectTemplateMapping(projectId, templateId, Guid.Empty);
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectGradingMapping].
        /// </summary>
        public async Task<List<ProjectGradingMapping>> GetProjectGrading(int projectId, int templateId, Guid sectionId)
        {
            try
            {
                return await Sp_SelectProjectGradingMapping(projectId, templateId, sectionId);
            }
            catch (CosmosException)
            {
            }
            return null;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectGradingConditionMapping].
        /// </summary>
        public async Task<List<ProjectGradingConditionMapping>> GetProjectGradingConditionMapping(int projectId, int templateId, Guid sectionId, int gradingId)
        {
            try
            {
                return await Sp_SelectProjectGradingConditionMapping(projectId, templateId, sectionId, gradingId);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetSectionGrading].
        /// </summary>
        public async Task<List<GradingUI>> GetTemplatGradings(int templateId, int projectId)
        {
            return await Sp_GetSectionGrading(templateId, projectId);
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetImageCards].
        /// </summary>
        public async Task<List<ImageCardUI>> GetImageCard(int templateId)
        {
            return await Sp_GetImageCards(templateId);
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetGradingConditions].
        /// </summary>
        public async Task<List<GradingConditionUI>> GetGradingCondition(int templateId, int projectid)
        {
            return await Sp_GetGradingConditions(templateId, projectid);
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectImageCards].
        /// </summary>
        public async Task<List<ProjectCardMapping>> GetProjectImageCard(int projectId, int templateId, Guid sectionId)
        {
            return await Sp_SelectProjectImageCards(projectId, templateId, sectionId);
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetH_IReport].
        /// </summary>
        public async Task<HandIReport> GetHIReport(int projectId)
        {
            return await Sp_GetHIReport(projectId);
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetTemplateSections].
        /// </summary>
        public async Task<List<ReportTemplateSection>> GetTemplatSections(int templateId)
        {
            return await Sp_GetTemplateSections(templateId);
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetBulkUploadImage].
        /// </summary>
        public async Task<List<UpskillImageData>> GetBulkUploadUnplacedImages()
        {
            return await Sp_GetBulkUploadImage();
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[CreateProjectTemplate].
        /// </summary>
        public async Task<bool> CreateProjectTemplate(ProjectReportMapping model)
        {
            try
            {
                var result = await Sp_CreateProjectTemplate(model);
                this.Commit();
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[DeleteProjectTemplate].
        /// </summary>
        public async Task<bool> DeleteProjectTemplate(int projectId, int templateId)
        {
            try
            {
                var result = await Sp_DeleteProjectTemplate(projectId, templateId);
                this.Commit();
                return result;
            }
            catch (CosmosException)
            {
            }
            return false;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[DeleteBulkUploadImage].
        /// </summary>
        public async Task<int> DeleteBulkUploadImage(int imageId)
        {
            return await Sp_DeleteBulkUploadImage(imageId);
        }

        /// <summary>
        /// Replaces stored procedure [upskill].[sp_AddEdit_Image_New].
        /// </summary>
        public async Task<bool> AddEditImageNew(int imgId, string imageBase64)
        {
            var result = await Sp_sp_AddEdit_Image_New(imgId, imageBase64);
            return result > 0;
        }

        /// <summary>
        /// Replaces stored procedure [upskill].[sp_Delete_Image].
        /// </summary>
        public async Task<bool> DeleteImage(string assignmentId, string fileId)
        {
            assignmentId ??= "Unknown";

            var affectedRows = await Sp_sp_Delete_Image(assignmentId, fileId);
            this.Commit();
            return affectedRows > 0;
        }

        /// <summary>
        /// Replaces stored procedure [upskill].[sp_Delete_Card].
        /// </summary>
        public async Task<bool> DeleteCard(int projectId, string cardId, string sequenceId, string assignmentId, string appId)
        {
            var result = await Sp_sp_Delete_Card(projectId, cardId, sequenceId, assignmentId, appId);
            this.Commit();
            return result == 1;
        }

        /// <summary>
        /// Replaces stored procedure [upskill].[sp_AddEdit_Image].
        /// </summary>
        public async Task<int> UploadImage(string assignmentId, string fileId)
        {
            assignmentId ??= "Unknown";

            var result = await Sp_sp_AddEdit_Image(assignmentId, fileId);
            this.Commit();
            return result;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectImagecardById].
        /// </summary>
        public async Task<ProjectCardMapping> GetProjectImagCardById(int projectId, int templateId, Guid sectionId, int cardId)
        {
            try
            {
                return await Sp_SelectProjectImagecardById(projectId, templateId, sectionId, cardId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[CreateProjectImageCard].
        /// </summary>
        public async Task<bool> CreateProjectImageCard(ProjectCardMapping model)
        {
            try
            {
                var affected = await Sp_CreateProjectImageCard(model);
                this.Commit();
                return affected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[UpdateProjectImageCard].
        /// </summary>
        public async Task<bool> UpdateProjectImageCard(ProjectCardMapping model)
        {
            try
            {
                var affected = await Sp_UpdateProjectImageCard(model);
                this.Commit();
                return affected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectGradingCardById].
        /// </summary>
        public async Task<ProjectGradingMapping> GetProjectGradingById(int projectId, int templateId, Guid sectionId, int gradingId)
        {
            try
            {
                return await Sp_SelectProjectGradingCardById(projectId, templateId, sectionId, gradingId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[UpdateProjectGrading].
        /// </summary>
        public async Task<bool> UpdateProjectGrading(ProjectGradingMapping model)
        {
            try
            {
                var affected = await Sp_UpdateProjectGrading(model);
                this.Commit();
                return affected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[CreateProjectGrading].
        /// </summary>
        public async Task<bool> CreateProjectGrading(ProjectGradingMapping model)
        {
            try
            {
                var affected = await Sp_CreateProjectGrading(model);
                this.Commit();
                return affected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[UpdateProjectGradingConditionMapping].
        /// </summary>
        public async Task<bool> UpdateProjectGradingConditionMapping(ProjectGradingConditionMapping model)
        {
            try
            {
                var affected = await Sp_UpdateProjectGradingConditionMapping(model);
                this.Commit();
                return affected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[CreateProjectGradingConditionMapping].
        /// </summary>
        public async Task<bool> CreateProjectGradingConditionMapping(ProjectGradingConditionMapping model)
        {
            try
            {
                var affected = await Sp_CreateProjectGradingConditionMapping(model);
                this.Commit();
                return affected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[UpdateProjectTemplate].
        /// </summary>
        public async Task<bool> UpdateProjectTemplate(ProjectReportMapping model)
        {
            try
            {
                var affected = await Sp_UpdateProjectTemplate(model);
                this.Commit();
                return affected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static DateTime? NormalizeHIDate(DateTime? value)
        {
            if (!value.HasValue || value.Value == DateTime.MinValue || value.Value.Date <= new DateTime(1901, 1, 1))
            {
                return null;
            }
            return value.Value;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[UpdateH_IReport].
        /// </summary>
        public async Task<bool> UpdateHIReport(HandIReport model)
        {
            try
            {
                var affected = await Sp_UpdateH_IReport(model);
                this.Commit();
                return affected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[CreateH_IReport].
        /// </summary>
        public async Task<bool> CreateHIReport(HandIReport model)
        {
            try
            {
                var affected = await Sp_CreateH_IReport(model);
                this.Commit();
                return affected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Private SP Helpers (32 procedures)

        private static bool IsValidSqlDate(DateTime dt) => dt >= SqlMinDate;
        private static bool IsValidSqlDate(DateTime? dt) => !dt.HasValue || dt.Value >= SqlMinDate;

        internal static string? TransformGradingValue(string? value)
        {
            if (value == null) return null;
            var trimmedEnd = value.TrimEnd(' ');
            var len = trimmedEnd.Length; // SQL LEN ignores trailing spaces
            string candidate = value;
            if (len >= 20 && !value.Contains(' '))
            {
                candidate = value.Insert(len / 2, " ");
            }
            return candidate.Length <= 20 ? candidate : candidate.Substring(0, 20);
        }

        private async Task<List<TDoc>> QueryAllAsync<TDoc>(Container container, QueryDefinition queryDef, PartitionKey? pk = null)
        {
            var results = new List<TDoc>();
            var options = pk.HasValue ? new QueryRequestOptions { PartitionKey = pk.Value } : null;
            using var feed = container.GetItemQueryIterator<TDoc>(queryDef, requestOptions: options);
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetTemplateSections].
        /// </summary>
        private async Task<List<ReportTemplateSection>> Sp_GetTemplateSections(int templateId)
        {
            var tsmQuery = new QueryDefinition("SELECT * FROM c WHERE c.TemplateId = @templateId")
                .WithParameter("@templateId", templateId);
            var mappings = await QueryAllAsync<TemplateSectionMappingDocument>(
                _templateSectionMappingContainer,
                tsmQuery,
                new PartitionKey((double)templateId));

            if (mappings.Count == 0) return new List<ReportTemplateSection>();

            var sectionIds = mappings
                .Where(m => !string.IsNullOrEmpty(m.SectionId))
                .Select(m => m.SectionId!.ToLowerInvariant())
                .Distinct()
                .ToList();

            if (sectionIds.Count == 0) return new List<ReportTemplateSection>();

            var rtsQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@sectionIds, c.id)")
                .WithParameter("@sectionIds", sectionIds);
            var sections = await QueryAllAsync<ReportTemplateSectionDocument>(
                _reportTemplateSectionContainer,
                rtsQuery);

            var sectionsById = sections
                .Where(s => !string.IsNullOrEmpty(s.Id))
                .ToDictionary(s => s.Id!.ToLowerInvariant(), s => s, StringComparer.OrdinalIgnoreCase);

            var hasSubSectionParentIds = sections
                .Where(s => s.HasSubSection == true && !string.IsNullOrEmpty(s.Id))
                .Select(s => s.Id!.ToLowerInvariant())
                .Distinct()
                .ToList();

            List<SubSectionDocument> subSections = new List<SubSectionDocument>();
            if (hasSubSectionParentIds.Count > 0)
            {
                var subQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@parentIds, c.SectionId)")
                    .WithParameter("@parentIds", hasSubSectionParentIds);
                subSections = await QueryAllAsync<SubSectionDocument>(_subSectionContainer, subQuery);
            }

            var subSectionsByParentId = subSections
                .Where(sub => !string.IsNullOrEmpty(sub.SectionId))
                .GroupBy(sub => sub.SectionId!.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var result = new List<ReportTemplateSection>();

            foreach (var mapping in mappings)
            {
                if (string.IsNullOrEmpty(mapping.SectionId)) continue;
                var sKey = mapping.SectionId.ToLowerInvariant();
                if (!sectionsById.TryGetValue(sKey, out var sec)) continue;

                if (sec.HasSubSection == true)
                {
                    if (subSectionsByParentId.TryGetValue(sKey, out var subs) && subs.Count > 0)
                    {
                        foreach (var sub in subs)
                        {
                            result.Add(new ReportTemplateSection
                            {
                                Id = !string.IsNullOrEmpty(sub.id) ? Guid.Parse(sub.id) : Guid.Empty,
                                SectionId = Guid.Parse(sec.Id!),
                                SectionName = sec.SectionName,
                                HasSubSection = true,
                                SubSectionName = sub.SectionName,
                                SpecialSectionName = sec.SpecialSectionName
                            });
                        }
                    }
                    else
                    {
                        result.Add(new ReportTemplateSection
                        {
                            Id = Guid.Empty,
                            SectionId = Guid.Parse(sec.Id!),
                            SectionName = sec.SectionName,
                            HasSubSection = true,
                            SubSectionName = null!,
                            SpecialSectionName = sec.SpecialSectionName
                        });
                    }
                }
                else
                {
                    result.Add(new ReportTemplateSection
                    {
                        Id = Guid.Parse(sec.Id!),
                        SectionId = Guid.Parse(sec.Id!),
                        SectionName = sec.SectionName,
                        HasSubSection = sec.HasSubSection ?? false,
                        SubSectionName = sec.SectionName,
                        SpecialSectionName = sec.SpecialSectionName
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetReportTemplateList].
        /// </summary>
        private async Task<List<ReportTemplate>> Sp_GetReportTemplateList()
        {
            var q = new QueryDefinition("SELECT * FROM c");
            var docs = await QueryAllAsync<ReportTemplateDocument>(_reportTemplateContainer, q);
            return docs.Select(d => new ReportTemplate
            {
                Id = d.Id,
                Name = d.SectionName
            }).ToList();
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetExistingReportPartsSectionIds].
        /// </summary>
        private async Task<List<int>> Sp_GetExistingReportPartsSectionIds(int projectId)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);
            var docs = await QueryAllAsync<ProjectTemplateMappingDocument>(
                _projectTemplateMappingContainer,
                q,
                new PartitionKey((double)projectId));

            var result = new List<int>();
            foreach (var doc in docs)
            {
                if (!doc.TemplateId.HasValue)
                {
                    throw new InvalidCastException("Null TemplateId cannot be cast to int.");
                }
                result.Add(doc.TemplateId.Value);
            }
            return result;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetTemplateTitle].
        /// </summary>
        private async Task<ReportTemplateUI> Sp_GetTemplateTitle(int templateId)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @templateId")
                .WithParameter("@templateId", templateId);
            var docs = await QueryAllAsync<ReportTemplateDocument>(_reportTemplateContainer, q);

            // SQL: no ORDER BY; pick lowest Id for deterministic parity
            var match = docs.OrderBy(d => d.Id).FirstOrDefault();
            if (match == null) return null!;

            return new ReportTemplateUI
            {
                Id = match.Id,
                SectionName = match.SectionName
            };
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetCurrentCondition].
        /// </summary>
        private async Task<List<CurrentCondition>> Sp_GetCurrentCondition()
        {
            var q = new QueryDefinition("SELECT * FROM c");
            var docs = await QueryAllAsync<CurrentConditionDocument>(_currentConditionContainer, q);
            return docs.Select(d => new CurrentCondition
            {
                Id = d.Id,
                Name = d.CurrentCondition
            }).ToList();
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectTemplateMapping].
        /// </summary>
        private async Task<List<ProjectReportMapping>> Sp_SelectProjectTemplateMapping(int projectId, int templateId, Guid sectionId)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId);

            var docs = await QueryAllAsync<ProjectTemplateMappingDocument>(
                _projectTemplateMappingContainer,
                q,
                new PartitionKey((double)projectId));

            IEnumerable<ProjectTemplateMappingDocument> filtered = docs;
            if (sectionId == Guid.Empty)
            {
                filtered = filtered.Where(d => d.SectionId != null);
            }
            else
            {
                var sTarget = sectionId.ToString();
                filtered = filtered.Where(d => d.SectionId != null && string.Equals(d.SectionId, sTarget, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.Select(d => new ProjectReportMapping
            {
                Id = !string.IsNullOrEmpty(d.Id) ? Guid.Parse(d.Id) : Guid.Empty,
                ProjectId = d.ProjectId ?? 0,
                TemplateId = d.TemplateId ?? 0,
                SectionId = !string.IsNullOrEmpty(d.SectionId) ? Guid.Parse(d.SectionId) : Guid.Empty,
                Notes = d.Notes,
                CreatedDttm = d.CreatedDttm ?? DateTime.MinValue,
                UpdatedDttm = d.UpdatedDttm ?? DateTime.MinValue,
                IsDeleted = d.IsDeleted ?? false
            }).ToList();
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectGradingMapping].
        /// </summary>
        private async Task<List<ProjectGradingMapping>> Sp_SelectProjectGradingMapping(int projectId, int templateId, Guid sectionId)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId);

            var docs = await QueryAllAsync<ProjectGradingMappingDocument>(
                _projectGradingMappingContainer,
                q,
                new PartitionKey((double)projectId));

            IEnumerable<ProjectGradingMappingDocument> filtered = docs;
            if (sectionId == Guid.Empty)
            {
                filtered = filtered.Where(d => d.SectionId != null);
            }
            else
            {
                var sTarget = sectionId.ToString();
                filtered = filtered.Where(d => d.SectionId != null && string.Equals(d.SectionId, sTarget, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.Select(d => new ProjectGradingMapping
            {
                Id = d.Id,
                ProjectId = d.ProjectId ?? 0,
                TemplateId = d.TemplateId ?? 0,
                SectionId = !string.IsNullOrEmpty(d.SectionId) ? Guid.Parse(d.SectionId) : Guid.Empty,
                GradingId = d.GradingId ?? 0,
                Value = TransformGradingValue(d.Value),
                CreatedDttm = d.CreatedDttm ?? DateTime.MinValue,
                UpdatedDttm = d.UpdatedDttm ?? DateTime.MinValue,
                IsDeleted = d.IsDeleted ?? false
            }).ToList();
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectGradingConditionMapping].
        /// </summary>
        private async Task<List<ProjectGradingConditionMapping>> Sp_SelectProjectGradingConditionMapping(int projectId, int templateId, Guid sectionId, int gradingId)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId);

            var docs = await QueryAllAsync<ProjectGradingConditionMappingDocument>(
                _projectGradingConditionMappingContainer,
                q,
                new PartitionKey((double)projectId));

            IEnumerable<ProjectGradingConditionMappingDocument> filtered = docs;
            if (sectionId == Guid.Empty)
            {
                filtered = filtered.Where(d => d.SectionId != null);
            }
            else
            {
                var sTarget = sectionId.ToString();
                filtered = filtered.Where(d => d.SectionId != null && string.Equals(d.SectionId, sTarget, StringComparison.OrdinalIgnoreCase));
            }

            if (gradingId == 0)
            {
                filtered = filtered.Where(d => d.GradingId.HasValue);
            }
            else
            {
                filtered = filtered.Where(d => d.GradingId.HasValue && d.GradingId.Value == gradingId);
            }

            return filtered.Select(d => new ProjectGradingConditionMapping
            {
                Id = d.Id,
                ProjectId = d.ProjectId ?? 0,
                TemplateId = d.TemplateId ?? 0,
                SectionId = !string.IsNullOrEmpty(d.SectionId) ? Guid.Parse(d.SectionId) : Guid.Empty,
                GradingId = d.GradingId ?? 0,
                GradingConditionLabel = d.GradingConditionLabel,
                GradingConditionValue = d.GradingConditionValue ?? false,
                CreatedDttm = d.CreatedDttm ?? DateTime.MinValue,
                UpdatedDttm = d.UpdatedDttm ?? DateTime.MinValue,
                IsDeleted = d.IsDeleted ?? false
            }).ToList();
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetSectionGrading].
        /// </summary>
        private async Task<List<GradingUI>> Sp_GetSectionGrading(int templateId, int projectId)
        {
            // 1. Template-section subquery (trap 8)
            var templateSections = await Sp_GetTemplateSections(templateId);
            var targetSectionIds = templateSections
                .Where(ts => ts.Id != Guid.Empty)
                .Select(ts => ts.Id.ToString().ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (targetSectionIds.Count == 0) return new List<GradingUI>();

            // 2. Query GradingSectionMapping
            var gsmQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@targetIds, c.SectionId)")
                .WithParameter("@targetIds", targetSectionIds.ToList());
            var gsmDocs = await QueryAllAsync<GradingSectionMappingDocument>(_gradingSectionMappingContainer, gsmQuery);

            if (gsmDocs.Count == 0) return new List<GradingUI>();

            // 3. Query Grading in partition ProjectId
            var gradeQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);
            var gradeDocs = await QueryAllAsync<GradingDocument>(_gradingContainer, gradeQuery, new PartitionKey((double)projectId));

            if (gradeDocs.Count == 0) return new List<GradingUI>();

            var gradesByGroupId = gradeDocs
                .Where(g => g.GradingGroupId.HasValue)
                .GroupBy(g => g.GradingGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rawResults = new List<(string SectionId, int GradeId, string? LabelName)>();

            foreach (var gsm in gsmDocs)
            {
                if (gradesByGroupId.TryGetValue(gsm.GradingGroupId, out var matchingGrades))
                {
                    foreach (var grade in matchingGrades)
                    {
                        rawResults.Add((gsm.SectionId, grade.Id, grade.LabelName));
                    }
                }
            }

            // ORDER BY GSMap.SectionId, grade.LabelName (SqlGuid byte order, then LabelName case-insensitive NULL first)
            var sorted = rawResults
                .OrderBy(x => new SqlGuid(Guid.Parse(x.SectionId)))
                .ThenBy(x => x.LabelName == null ? 0 : 1)
                .ThenBy(x => x.LabelName, StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase))
                .Select(x => new GradingUI
                {
                    SectionId = Guid.Parse(x.SectionId),
                    Id = x.GradeId,
                    LabelName = x.LabelName
                })
                .ToList();

            return sorted;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetImageCards].
        /// </summary>
        private async Task<List<ImageCardUI>> Sp_GetImageCards(int templateId)
        {
            var templateSections = await Sp_GetTemplateSections(templateId);
            var targetSectionIds = templateSections
                .Where(ts => ts.Id != Guid.Empty)
                .Select(ts => ts.Id.ToString().ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (targetSectionIds.Count == 0) return new List<ImageCardUI>();

            var icQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@targetIds, c.SectionId)")
                .WithParameter("@targetIds", targetSectionIds.ToList());
            var icDocs = await QueryAllAsync<ImageCardDocument>(_imageCardContainer, icQuery);

            return icDocs.Select(c => new ImageCardUI
            {
                Id = c.Id,
                SectionId = Guid.Parse(c.SectionId),
                FileName = c.FileName,
                CurrentCondition = c.CurrentCondition,
                IsDescDropdown = false,
                Description = null!
            }).ToList();
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetGradingConditions].
        /// </summary>
        private async Task<List<GradingConditionUI>> Sp_GetGradingConditions(int templateId, int projectId)
        {
            // 1. TemplateSectionMapping for templateId
            var tsmQuery = new QueryDefinition("SELECT * FROM c WHERE c.TemplateId = @templateId")
                .WithParameter("@templateId", templateId);
            var mappings = await QueryAllAsync<TemplateSectionMappingDocument>(
                _templateSectionMappingContainer,
                tsmQuery,
                new PartitionKey((double)templateId));

            if (mappings.Count == 0) return new List<GradingConditionUI>();

            var sectionIds = mappings
                .Where(m => !string.IsNullOrEmpty(m.SectionId))
                .Select(m => m.SectionId!.ToLowerInvariant())
                .Distinct()
                .ToList();

            if (sectionIds.Count == 0) return new List<GradingConditionUI>();

            // 2. ReportTemplateSection where HasSubSection != 1 (NULL excluded) -> HasSubSection == false
            var rtsQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@sectionIds, c.id) AND c.HasSubSection = false")
                .WithParameter("@sectionIds", sectionIds);
            var sections = await QueryAllAsync<ReportTemplateSectionDocument>(_reportTemplateSectionContainer, rtsQuery);

            if (sections.Count == 0) return new List<GradingConditionUI>();

            var validSectionIds = sections
                .Where(s => !string.IsNullOrEmpty(s.Id))
                .Select(s => s.Id!.ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 3. GradingSectionMapping
            var gsmQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@sectionIds, c.SectionId)")
                .WithParameter("@sectionIds", validSectionIds.ToList());
            var gsmDocs = await QueryAllAsync<GradingSectionMappingDocument>(_gradingSectionMappingContainer, gsmQuery);

            if (gsmDocs.Count == 0) return new List<GradingConditionUI>();

            // 4. Grading where ProjectId = @projectId (no IsActive/IsDeleted filter on Grading)
            var gradeQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId")
                .WithParameter("@projectId", projectId);
            var gradeDocs = await QueryAllAsync<GradingDocument>(_gradingContainer, gradeQuery, new PartitionKey((double)projectId));

            if (gradeDocs.Count == 0) return new List<GradingConditionUI>();

            var gradesByGroupId = gradeDocs
                .Where(g => g.GradingGroupId.HasValue)
                .GroupBy(g => g.GradingGroupId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var gradingCondGroupIds = gradeDocs
                .Where(g => g.GradingConditionGroupId.HasValue)
                .Select(g => g.GradingConditionGroupId!.Value)
                .Distinct()
                .ToList();

            if (gradingCondGroupIds.Count == 0) return new List<GradingConditionUI>();

            // 5. GradingCondition
            var gcQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@groupIds, c.GradingConditionGroupId)")
                .WithParameter("@groupIds", gradingCondGroupIds);
            var gcDocs = await QueryAllAsync<GradingConditionDocument>(_gradingConditionContainer, gcQuery);

            var gcByGroupId = gcDocs
                .GroupBy(gc => gc.GradingConditionGroupId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<GradingConditionUI>();

            // Inner joins: tsm x rts x gsm x grading x gc
            foreach (var tsm in mappings)
            {
                if (string.IsNullOrEmpty(tsm.SectionId)) continue;
                var secId = tsm.SectionId.ToLowerInvariant();
                if (!validSectionIds.Contains(secId)) continue;

                var matchingGsms = gsmDocs.Where(g => string.Equals(g.SectionId, secId, StringComparison.OrdinalIgnoreCase));
                foreach (var gsm in matchingGsms)
                {
                    if (gradesByGroupId.TryGetValue(gsm.GradingGroupId, out var matchingGrades))
                    {
                        foreach (var grade in matchingGrades)
                        {
                            if (grade.GradingConditionGroupId.HasValue &&
                                gcByGroupId.TryGetValue(grade.GradingConditionGroupId.Value, out var conditions))
                            {
                                foreach (var gc in conditions)
                                {
                                    result.Add(new GradingConditionUI
                                    {
                                        GradingId = grade.Id,
                                        Condition = gc.Condition,
                                        SectionId = Guid.Parse(secId),
                                        Id = gc.Id
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectImageCards].
        /// </summary>
        private async Task<List<ProjectCardMapping>> Sp_SelectProjectImageCards(int projectId, int templateId, Guid sectionId)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId);

            var picDocs = await QueryAllAsync<ProjectImageCardsDocument>(
                _projectImageCardsContainer,
                q,
                new PartitionKey((double)projectId));

            var sTarget = sectionId.ToString();
            var filteredPics = picDocs
                .Where(p => p.SectionId != null && string.Equals(p.SectionId, sTarget, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var imageIds = filteredPics
                .Where(p => p.ImageId.HasValue)
                .Select(p => p.ImageId!.Value)
                .Distinct()
                .ToList();

            Dictionary<int, List<ImageDataDocument>> imagesById = new Dictionary<int, List<ImageDataDocument>>();
            if (imageIds.Count > 0)
            {
                var imgQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@imageIds, c.ImageId)")
                    .WithParameter("@imageIds", imageIds);
                var imgDocs = await QueryAllAsync<ImageDataDocument>(_imageDataContainer, imgQuery);
                imagesById = imgDocs
                    .Where(i => i.ImageId.HasValue)
                    .GroupBy(i => i.ImageId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }

            var result = new List<ProjectCardMapping>();
            foreach (var pic in filteredPics)
            {
                if (pic.ImageId.HasValue && imagesById.TryGetValue(pic.ImageId.Value, out var matches) && matches.Count > 0)
                {
                    foreach (var img in matches)
                    {
                        result.Add(new ProjectCardMapping
                        {
                            Id = pic.Id,
                            ProjectId = pic.ProjectId ?? 0,
                            TemplateId = pic.TemplateId ?? 0,
                            SectionId = !string.IsNullOrEmpty(pic.SectionId) ? Guid.Parse(pic.SectionId) : Guid.Empty,
                            CardId = pic.CardId ?? 0,
                            DescriptionId = pic.DescriptionId ?? 0,
                            AdditionalDescription = pic.AdditionalDescription,
                            CurrentCondition = pic.CurrentCondition,
                            ImageId = pic.ImageId ?? 0,
                            CreatedDttm = pic.CreatedDttm ?? DateTime.MinValue,
                            UpdateDttm = pic.UpdateDttm ?? DateTime.MinValue,
                            IsDeleted = pic.IsDeleted ?? false,
                            Src = img.Base64String
                        });
                    }
                }
                else
                {
                    result.Add(new ProjectCardMapping
                    {
                        Id = pic.Id,
                        ProjectId = pic.ProjectId ?? 0,
                        TemplateId = pic.TemplateId ?? 0,
                        SectionId = !string.IsNullOrEmpty(pic.SectionId) ? Guid.Parse(pic.SectionId) : Guid.Empty,
                        CardId = pic.CardId ?? 0,
                        DescriptionId = pic.DescriptionId ?? 0,
                        AdditionalDescription = pic.AdditionalDescription,
                        CurrentCondition = pic.CurrentCondition,
                        ImageId = pic.ImageId ?? 0,
                        CreatedDttm = pic.CreatedDttm ?? DateTime.MinValue,
                        UpdateDttm = pic.UpdateDttm ?? DateTime.MinValue,
                        IsDeleted = pic.IsDeleted ?? false,
                        Src = null
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetH_IReport].
        /// </summary>
        private async Task<HandIReport> Sp_GetHIReport(int projectId)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var docs = await QueryAllAsync<H_IReportTemplateDocument>(
                _hiReportTemplateContainer,
                q,
                new PartitionKey((double)projectId));

            // SQL: no ORDER BY; pick lowest Id in SqlGuid byte order for deterministic parity
            var match = docs.OrderBy(d => new SqlGuid(Guid.Parse(d.Id!))).FirstOrDefault();
            if (match == null) return null!;

            return new HandIReport
            {
                Id = !string.IsNullOrEmpty(match.Id) ? Guid.Parse(match.Id) : Guid.Empty,
                ProjectId = match.ProjectId ?? 0,
                SectionId = Guid.Empty,
                CompanyName = match.CompanyName,
                CertificateNumber = match.CertificateNumber,
                ExpiryDate = match.ExpiryDate ?? DateTime.MinValue,
                ThicknessMeasurementA = match.ThicknessMeasurementA,
                ThicknessMeasurementB = match.ThicknessMeasurementB,
                ThicknessMeasurementC = match.ThicknessMeasurementC,
                AdditionalSurveyorComments = match.AdditionalSurveyorComments,
                MaintenanceExecuted = match.MaintenanceExecuted,
                Accommodation = match.Accommodation,
                PhotoGraphs = match.PhotoGraphs,
                OperationalTestLocation = match.OperationalTestLocation,
                OperationalTestStart = match.OperationalTestStart ?? DateTime.MinValue,
                OperationalTestFinish = match.OperationalTestFinish ?? DateTime.MinValue,
                OptionalTestSatisFactory = match.OptionalTestSatisFactory,
                AssociatedReportNumber = match.AssociatedReportNumber,
                AssociatedReportDate = match.AssociatedReportDate,
                Remarks = match.Remarks,
                LeadSurveyorName = match.LeadSurveyorName,
                LeadSurveyorNumber = match.LeadSurveyorNumber,
                SurveyorName = match.SurveyorName,
                SurveyorNumber = match.SurveyorNumber,
                LastVisitDate = match.LastVisitDate ?? DateTime.MinValue,
                IndividualA = match.IndividualA,
                IndividualB = match.IndividualB,
                IndividualC = match.IndividualC,
                IndividualD = match.IndividualD,
                IndividualE = match.IndividualE,
                IndividualF = match.IndividualF,
                SubstantialCorrosionRemaining = match.SubstantialCorrosionRemaining,
                StatutoryDeficiencies = match.StatutoryDeficiencies,
                FoundSatisfactory = match.FoundSatisfactory,
                UnderRemarks = match.UnderRemarks,
                ScouringOrPitting = match.ScouringOrPitting,
                BottomPlattingScouringOrPitting = match.BottomPlattingScouringOrPitting,
                IsActive = match.IsActive ?? false,
                IsDeleted = match.IsDeleted ?? false,
                CreatedDttm = match.CreatedDttm ?? DateTime.MinValue,
                UpdatedDttm = match.UpdatedDttm ?? DateTime.MinValue
            };
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[GetBulkUploadImage].
        /// </summary>
        private async Task<List<UpskillImageData>> Sp_GetBulkUploadImage()
        {
            var buQuery = new QueryDefinition("SELECT * FROM c");
            var buDocs = await QueryAllAsync<BulkUploadDocument>(_bulkUploadContainer, buQuery);

            if (buDocs.Count == 0) return new List<UpskillImageData>();

            var imageIds = buDocs.Select(b => b.imageid).Distinct().ToList();

            var imgQuery = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@imageIds, c.ImageId)")
                .WithParameter("@imageIds", imageIds);
            var imgDocs = await QueryAllAsync<ImageDataDocument>(_imageDataContainer, imgQuery);

            var imagesById = imgDocs
                .Where(i => i.ImageId.HasValue)
                .GroupBy(i => i.ImageId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<UpskillImageData>();
            foreach (var bu in buDocs)
            {
                if (imagesById.TryGetValue(bu.imageid, out var matchingImgs))
                {
                    foreach (var img in matchingImgs)
                    {
                        result.Add(new UpskillImageData
                        {
                            Id = img.Id,
                            ImageId = img.ImageId ?? 0,
                            Base64String = img.Base64String,
                            CreateDttm = img.CreateDttm,
                            UpdateDttm = img.UpdateDttm,
                            ProjectId = 0
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[CreateProjectTemplate].
        /// </summary>
        private async Task<bool> Sp_CreateProjectTemplate(ProjectReportMapping model)
        {
            // SQL: @Id is never supplied, so the SP always fails; kept for parity
            return false;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[DeleteProjectTemplate].
        /// </summary>
        private async Task<bool> Sp_DeleteProjectTemplate(int projectId, int templateId)
        {
            // 9 sequential UPDATEs in this order, no transaction
            // 1. ProjectTemplateMapping
            var ptmQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId);
            var ptmDocs = await QueryAllAsync<ProjectTemplateMappingDocument>(
                _projectTemplateMappingContainer,
                ptmQuery,
                new PartitionKey((double)projectId));
            foreach (var doc in ptmDocs)
            {
                doc.IsDeleted = true;
                await _projectTemplateMappingContainer.ReplaceItemAsync(doc, doc.id, new PartitionKey((double)projectId));
            }

            // 2. ProjectImageCards
            var picQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId);
            var picDocs = await QueryAllAsync<ProjectImageCardsDocument>(
                _projectImageCardsContainer,
                picQuery,
                new PartitionKey((double)projectId));
            foreach (var doc in picDocs)
            {
                doc.IsDeleted = true;
                await _projectImageCardsContainer.ReplaceItemAsync(doc, doc.id, new PartitionKey((double)projectId));
            }

            // 3. ProjectGradingMapping
            var pgmQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId);
            var pgmDocs = await QueryAllAsync<ProjectGradingMappingDocument>(
                _projectGradingMappingContainer,
                pgmQuery,
                new PartitionKey((double)projectId));
            foreach (var doc in pgmDocs)
            {
                doc.IsDeleted = true;
                await _projectGradingMappingContainer.ReplaceItemAsync(doc, doc.id, new PartitionKey((double)projectId));
            }

            // 4. ProjectGradingConditionMapping
            var pgcmQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId);
            var pgcmDocs = await QueryAllAsync<ProjectGradingConditionMappingDocument>(
                _projectGradingConditionMappingContainer,
                pgcmQuery,
                new PartitionKey((double)projectId));
            foreach (var doc in pgcmDocs)
            {
                doc.IsDeleted = true;
                await _projectGradingConditionMappingContainer.ReplaceItemAsync(doc, doc.id, new PartitionKey((double)projectId));
            }

            // 5. H_IReportTemplate (ProjectId only)
            var hiQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId")
                .WithParameter("@projectId", projectId);
            var hiDocs = await QueryAllAsync<H_IReportTemplateDocument>(
                _hiReportTemplateContainer,
                hiQuery,
                new PartitionKey((double)projectId));
            foreach (var doc in hiDocs)
            {
                doc.IsDeleted = true;
                await _hiReportTemplateContainer.ReplaceItemAsync(doc, doc.id, new PartitionKey((double)projectId));
            }

            // 6. TankImageCard (IsActive = false only, no IsDeleted column)
            var ticQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId);
            var ticDocs = await QueryAllAsync<TankImageCardDocument>(
                _tankImageCardContainer,
                ticQuery,
                new PartitionKey((double)projectId));
            foreach (var doc in ticDocs)
            {
                doc.IsActive = false;
                await _tankImageCardContainer.ReplaceItemAsync(doc, doc.id, new PartitionKey((double)projectId));
            }

            // 7. GenericImageCard
            var gicQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId);
            var gicDocs = await QueryAllAsync<GenericImageCardDocument>(
                _genericImageCardContainer,
                gicQuery,
                new PartitionKey((double)projectId));
            foreach (var doc in gicDocs)
            {
                doc.IsActive = false;
                doc.IsDeleted = true;
                await _genericImageCardContainer.ReplaceItemAsync(doc, doc.id, new PartitionKey((double)projectId));
            }

            // 8. HandIProjectGrading (ProjectId only, partition key /ProjectID)
            var hipgQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectID = @projectId")
                .WithParameter("@projectId", projectId);
            var hipgDocs = await QueryAllAsync<HandIProjectGradingDocument>(
                _handIProjectGradingContainer,
                hipgQuery,
                new PartitionKey((double)projectId));
            foreach (var doc in hipgDocs)
            {
                doc.IsActive = false;
                doc.IsDeleted = true;
                await _handIProjectGradingContainer.ReplaceItemAsync(doc, doc.id, new PartitionKey((double)projectId));
            }

            // 9. HandIProjectGradingConditions (ProjectId only, partition key /ProjectId)
            var hipgcQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId")
                .WithParameter("@projectId", projectId);
            var hipgcDocs = await QueryAllAsync<HandIProjectGradingConditionsDocument>(
                _handIProjectGradingConditionsContainer,
                hipgcQuery,
                new PartitionKey((double)projectId));
            foreach (var doc in hipgcDocs)
            {
                doc.IsActive = false;
                doc.IsDeleted = true;
                await _handIProjectGradingConditionsContainer.ReplaceItemAsync(doc, doc.id, new PartitionKey((double)projectId));
            }

            return true;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[DeleteBulkUploadImage].
        /// </summary>
        private async Task<int> Sp_DeleteBulkUploadImage(int imageId)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.imageid = @imageId")
                .WithParameter("@imageId", imageId);
            var docs = await QueryAllAsync<BulkUploadDocument>(_bulkUploadContainer, q);

            foreach (var doc in docs)
            {
                await _bulkUploadContainer.DeleteItemAsync<BulkUploadDocument>(doc.id, new PartitionKey(doc.id));
            }

            // SET NOCOUNT ON causes ExecuteAsync to return -1 always
            return -1;
        }

        /// <summary>
        /// Replaces stored procedure [upskill].[sp_AddEdit_Image_New].
        /// </summary>
        private async Task<int> Sp_sp_AddEdit_Image_New(int imgId, string imageBase64)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ImageId = @imgId")
                .WithParameter("@imgId", imgId);
            var existingDocs = await QueryAllAsync<ImageDataDocument>(_imageDataContainer, q);

            if (existingDocs.Count > 0)
            {
                foreach (var doc in existingDocs)
                {
                    doc.Base64String = imageBase64;
                    doc.UpdateDttm = DateTime.UtcNow;
                    await _imageDataContainer.ReplaceItemAsync(doc, doc.id, new PartitionKey(doc.id));
                }
                // UPDATE branch selects nothing -> ExecuteScalarAsync returns 0
                return 0;
            }
            else
            {
                var newId = await _sequenceService.GetNextAsync(CosmosContainers.upskill_ImageData);
                var newDoc = new ImageDataDocument
                {
                    id = newId.ToString(),
                    Id = newId,
                    ImageId = imgId,
                    Base64String = imageBase64,
                    CreateDttm = DateTime.UtcNow,
                    UpdateDttm = null
                };
                await _imageDataContainer.CreateItemAsync(newDoc, new PartitionKey(newDoc.id));
                // INSERT branch selects SCOPE_IDENTITY() -> ExecuteScalarAsync returns newId > 0
                return newId;
            }
        }

        /// <summary>
        /// Replaces stored procedure [upskill].[sp_Delete_Image].
        /// </summary>
        private async Task<int> Sp_sp_Delete_Image(string assignmentId, string fileId)
        {
            if (assignmentId.Length > 300) assignmentId = assignmentId.Substring(0, 300);
            if (fileId != null && fileId.Length > 300) fileId = fileId.Substring(0, 300);
            if (fileId == null) return 0;

            var q = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.AssignmentId), @a, true) AND STRINGEQUALS(RTRIM(c.FileId), @f, true)")
                .WithParameter("@a", assignmentId.TrimEnd(' '))
                .WithParameter("@f", fileId.TrimEnd(' '));

            var matchingImages = await QueryAllAsync<ImagesDocument>(_imagesContainer, q);

            var inMemoryMatches = matchingImages
                .Where(img => img.AssignmentId != null && img.FileId != null
                    && string.Equals(img.AssignmentId.TrimEnd(' '), assignmentId.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(img.FileId.TrimEnd(' '), fileId.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (inMemoryMatches.Count > 1)
            {
                throw new InvalidOperationException("Subquery returned more than 1 value.");
            }

            if (inMemoryMatches.Count == 0)
            {
                return 0;
            }

            var image = inMemoryMatches[0];

            var dataQ = new QueryDefinition("SELECT * FROM c WHERE c.ImageId = @imageId")
                .WithParameter("@imageId", image.Id);
            var matchingData = await QueryAllAsync<ImageDataDocument>(_imageDataContainer, dataQ);

            int deletedCount = 0;
            foreach (var dataDoc in matchingData)
            {
                await _imageDataContainer.DeleteItemAsync<ImageDataDocument>(dataDoc.id, new PartitionKey(dataDoc.id));
                deletedCount++;
            }

            await _imagesContainer.DeleteItemAsync<ImagesDocument>(image.id, new PartitionKey(image.id));
            deletedCount++;

            return deletedCount;
        }

        /// <summary>
        /// Replaces stored procedure [upskill].[sp_Delete_Card].
        /// </summary>
        private async Task<int> Sp_sp_Delete_Card(int projectId, string cardId, string sequenceId, string assignmentId, string appId)
        {
            if (cardId == null || appId == null || assignmentId == null || sequenceId == null) return 0;

            int parsedCardId;
            var trimmedCard = cardId.Trim();
            if (string.IsNullOrEmpty(trimmedCard))
            {
                parsedCardId = 0;
            }
            else if (int.TryParse(trimmedCard, out var p))
            {
                parsedCardId = p;
            }
            else
            {
                throw new FormatException($"Input string '{cardId}' was not in a correct format.");
            }

            var projStr = projectId.ToString();
            var cutAppId = appId.Length > 36 ? appId.Substring(0, 36) : appId;
            var cutAssignId = assignmentId.Length > 36 ? assignmentId.Substring(0, 36) : assignmentId;
            var cutSeqId = sequenceId.Length > 36 ? sequenceId.Substring(0, 36) : sequenceId;

            var q = new QueryDefinition(
                "SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.AbsProjectId), @p, true) AND STRINGEQUALS(RTRIM(c.AppId), @app, true) " +
                "AND STRINGEQUALS(RTRIM(c.AssignmentId), @assign, true) AND STRINGEQUALS(RTRIM(c.SequenceId), @seq, true) AND c.CardId = @cardId")
                .WithParameter("@p", projStr.TrimEnd(' '))
                .WithParameter("@app", cutAppId.TrimEnd(' '))
                .WithParameter("@assign", cutAssignId.TrimEnd(' '))
                .WithParameter("@seq", cutSeqId.TrimEnd(' '))
                .WithParameter("@cardId", parsedCardId);

            var docs = await QueryAllAsync<CapLogDocument>(_capLogContainer, q);

            var inMemoryDocs = docs
                .Where(d => d.AbsProjectId != null && d.AppId != null && d.AssignmentId != null && d.SequenceId != null
                    && string.Equals(d.AbsProjectId.TrimEnd(' '), projStr.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(d.AppId.TrimEnd(' '), cutAppId.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(d.AssignmentId.TrimEnd(' '), cutAssignId.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(d.SequenceId.TrimEnd(' '), cutSeqId.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                    && d.CardId == parsedCardId)
                .ToList();

            foreach (var doc in inMemoryDocs)
            {
                await _capLogContainer.DeleteItemAsync<CapLogDocument>(doc.id, new PartitionKey(doc.id));
            }

            // SP has no SELECT -> Dapper ExecuteScalarAsync<int> returns 0
            return 0;
        }

        /// <summary>
        /// Replaces stored procedure [upskill].[sp_AddEdit_Image].
        /// </summary>
        private async Task<int> Sp_sp_AddEdit_Image(string assignmentId, string fileId)
        {
            if (assignmentId.Length > 300) assignmentId = assignmentId.Substring(0, 300);
            if (fileId != null && fileId.Length > 300) fileId = fileId.Substring(0, 300);

            if (fileId == null)
            {
                var newId = await _sequenceService.GetNextAsync(CosmosContainers.upskill_Images);
                var newDoc = new ImagesDocument
                {
                    id = newId.ToString(),
                    Id = newId,
                    AssignmentId = assignmentId,
                    FileId = null,
                    InsertDate = DateTime.UtcNow
                };
                await _imagesContainer.CreateItemAsync(newDoc, new PartitionKey(newDoc.id));
                return newId;
            }

            var q = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.FileId), @f, true)")
                .WithParameter("@f", fileId.TrimEnd(' '));
            var matching = await QueryAllAsync<ImagesDocument>(_imagesContainer, q);

            var inMemoryMatching = matching
                .Where(m => m.FileId != null && string.Equals(m.FileId.TrimEnd(' '), fileId.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (inMemoryMatching.Count > 1)
            {
                throw new InvalidOperationException("Subquery returned more than 1 value.");
            }

            if (inMemoryMatching.Count == 1)
            {
                var existing = inMemoryMatching[0];
                existing.AssignmentId = assignmentId;
                await _imagesContainer.ReplaceItemAsync(existing, existing.id, new PartitionKey(existing.id));
                return existing.Id;
            }
            else
            {
                var newId = await _sequenceService.GetNextAsync(CosmosContainers.upskill_Images);
                var newDoc = new ImagesDocument
                {
                    id = newId.ToString(),
                    Id = newId,
                    AssignmentId = assignmentId,
                    FileId = fileId,
                    InsertDate = DateTime.UtcNow
                };
                await _imagesContainer.CreateItemAsync(newDoc, new PartitionKey(newDoc.id));
                return newId;
            }
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectImagecardById].
        /// </summary>
        private async Task<ProjectCardMapping> Sp_SelectProjectImagecardById(int projectId, int templateId, Guid sectionId, int cardId)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId AND c.CardId = @cardId AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId)
                .WithParameter("@cardId", cardId);

            var docs = await QueryAllAsync<ProjectImageCardsDocument>(
                _projectImageCardsContainer,
                q,
                new PartitionKey((double)projectId));

            var sTarget = sectionId.ToString();
            var filtered = docs
                .Where(d => d.SectionId != null && string.Equals(d.SectionId, sTarget, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // SQL: no ORDER BY; pick lowest Id for deterministic parity
            var match = filtered.OrderBy(d => d.Id).FirstOrDefault();
            if (match == null) return null!;

            return new ProjectCardMapping
            {
                Id = match.Id,
                ProjectId = match.ProjectId ?? 0,
                TemplateId = match.TemplateId ?? 0,
                SectionId = !string.IsNullOrEmpty(match.SectionId) ? Guid.Parse(match.SectionId) : Guid.Empty,
                CardId = match.CardId ?? 0,
                DescriptionId = match.DescriptionId ?? 0,
                AdditionalDescription = match.AdditionalDescription,
                CurrentCondition = match.CurrentCondition,
                ImageId = match.ImageId ?? 0,
                CreatedDttm = match.CreatedDttm ?? DateTime.MinValue,
                UpdateDttm = match.UpdateDttm ?? DateTime.MinValue,
                IsDeleted = match.IsDeleted ?? false
            };
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[CreateProjectImageCard].
        /// </summary>
        private async Task<bool> Sp_CreateProjectImageCard(ProjectCardMapping model)
        {
            if (!IsValidSqlDate(model.CreatedDttm) || !IsValidSqlDate(model.UpdateDttm))
            {
                return false;
            }

            int? pProjectId = model.ProjectId == 0 ? null : model.ProjectId;
            int? pTemplateId = model.TemplateId == 0 ? null : model.TemplateId;
            string? pSectionId = model.SectionId == Guid.Empty
                ? "00000000-0000-0000-0000-000000000000"
                : model.SectionId.ToString().ToLowerInvariant();
            int? pCardId = model.CardId == 0 ? null : model.CardId;
            int? pDescId = model.DescriptionId == 0 ? null : model.DescriptionId;
            string? pAddDesc = (model.AdditionalDescription == null || model.AdditionalDescription.TrimEnd(' ') == "") ? null : model.AdditionalDescription;
            int? pCondition = (model.CurrentCondition == 0 || !model.CurrentCondition.HasValue) ? null : model.CurrentCondition;
            int? pImageId = model.ImageId == 0 ? null : model.ImageId;
            DateTime? pCreated = (model.CreatedDttm == DateTime.MinValue || model.CreatedDttm == new DateTime(1900, 1, 1)) ? null : model.CreatedDttm;
            DateTime? pUpdate = (model.UpdateDttm == DateTime.MinValue || model.UpdateDttm == new DateTime(1900, 1, 1)) ? null : model.UpdateDttm;

            var newId = await _sequenceService.GetNextAsync(CosmosContainers.dbo_ProjectImageCards);
            var newDoc = new ProjectImageCardsDocument
            {
                id = newId.ToString(),
                Id = newId,
                ProjectId = pProjectId,
                TemplateId = pTemplateId,
                SectionId = pSectionId,
                CardId = pCardId,
                DescriptionId = pDescId,
                AdditionalDescription = pAddDesc,
                CurrentCondition = pCondition,
                ImageId = pImageId,
                CreatedDttm = pCreated,
                UpdateDttm = pUpdate,
                IsDeleted = model.IsDeleted
            };

            PartitionKey pk = pProjectId.HasValue ? new PartitionKey((double)pProjectId.Value) : PartitionKey.Null;
            await _projectImageCardsContainer.CreateItemAsync(newDoc, pk);
            return true;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[UpdateProjectImageCard].
        /// </summary>
        private async Task<bool> Sp_UpdateProjectImageCard(ProjectCardMapping model)
        {
            if (!IsValidSqlDate(model.UpdateDttm))
            {
                return false;
            }

            var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id")
                .WithParameter("@id", model.Id);
            var existingDocs = await QueryAllAsync<ProjectImageCardsDocument>(_projectImageCardsContainer, q);

            if (existingDocs.Count == 0) return false;

            int affectedCount = 0;
            foreach (var doc in existingDocs)
            {
                var oldProjectId = doc.ProjectId;
                var newProjectId = (int?)model.ProjectId;

                doc.ProjectId = model.ProjectId;
                doc.TemplateId = model.TemplateId;
                doc.SectionId = model.SectionId.ToString().ToLowerInvariant();
                doc.CardId = model.CardId;
                doc.DescriptionId = model.DescriptionId;
                doc.AdditionalDescription = model.AdditionalDescription;
                doc.CurrentCondition = model.CurrentCondition;
                doc.ImageId = model.ImageId;
                doc.CreatedDttm = null; // Trap 2: not passed -> NULL
                doc.UpdateDttm = model.UpdateDttm;
                doc.IsDeleted = model.IsDeleted;

                if (oldProjectId != newProjectId)
                {
                    // Partition move
                    PartitionKey newPk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                    PartitionKey oldPk = oldProjectId.HasValue ? new PartitionKey((double)oldProjectId.Value) : PartitionKey.Null;

                    await _projectImageCardsContainer.CreateItemAsync(doc, newPk);
                    try
                    {
                        await _projectImageCardsContainer.DeleteItemAsync<ProjectImageCardsDocument>(doc.id, oldPk);
                    }
                    catch
                    {
                        await _projectImageCardsContainer.DeleteItemAsync<ProjectImageCardsDocument>(doc.id, newPk);
                        throw;
                    }
                }
                else
                {
                    PartitionKey pk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                    await _projectImageCardsContainer.ReplaceItemAsync(doc, doc.id, pk);
                }
                affectedCount++;
            }

            return affectedCount > 0;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[SelectProjectGradingCardById].
        /// </summary>
        private async Task<ProjectGradingMapping> Sp_SelectProjectGradingCardById(int projectId, int templateId, Guid sectionId, int gradingId)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId AND c.GradingId = @gradingId AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId)
                .WithParameter("@gradingId", gradingId);

            var docs = await QueryAllAsync<ProjectGradingMappingDocument>(
                _projectGradingMappingContainer,
                q,
                new PartitionKey((double)projectId));

            var sTarget = sectionId.ToString();
            var filtered = docs
                .Where(d => d.SectionId != null && string.Equals(d.SectionId, sTarget, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // SQL: no ORDER BY; pick lowest Id for deterministic parity
            var match = filtered.OrderBy(d => d.Id).FirstOrDefault();
            if (match == null) return null!;

            return new ProjectGradingMapping
            {
                Id = match.Id,
                ProjectId = match.ProjectId ?? 0,
                TemplateId = match.TemplateId ?? 0,
                SectionId = !string.IsNullOrEmpty(match.SectionId) ? Guid.Parse(match.SectionId) : Guid.Empty,
                GradingId = match.GradingId ?? 0,
                Value = match.Value,
                CreatedDttm = match.CreatedDttm ?? DateTime.MinValue,
                UpdatedDttm = match.UpdatedDttm ?? DateTime.MinValue,
                IsDeleted = match.IsDeleted ?? false
            };
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[UpdateProjectGrading].
        /// </summary>
        private async Task<bool> Sp_UpdateProjectGrading(ProjectGradingMapping model)
        {
            if (!IsValidSqlDate(model.UpdateDttm))
            {
                return false;
            }

            var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id")
                .WithParameter("@id", model.Id);
            var existingDocs = await QueryAllAsync<ProjectGradingMappingDocument>(_projectGradingMappingContainer, q);

            if (existingDocs.Count == 0) return false;

            int affectedCount = 0;
            foreach (var doc in existingDocs)
            {
                var oldProjectId = doc.ProjectId;
                var newProjectId = (int?)model.ProjectId;

                doc.ProjectId = model.ProjectId;
                doc.TemplateId = model.TemplateId;
                doc.SectionId = model.SectionId.ToString().ToLowerInvariant();
                doc.GradingId = model.GradingId;
                doc.Value = model.Value;
                doc.CreatedDttm = null; // Trap 2: not passed -> NULL
                doc.UpdatedDttm = model.UpdateDttm;
                doc.IsDeleted = model.IsDeleted;
                // RequiredInReport kept unchanged (trap 25)

                if (oldProjectId != newProjectId)
                {
                    PartitionKey newPk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                    PartitionKey oldPk = oldProjectId.HasValue ? new PartitionKey((double)oldProjectId.Value) : PartitionKey.Null;

                    await _projectGradingMappingContainer.CreateItemAsync(doc, newPk);
                    try
                    {
                        await _projectGradingMappingContainer.DeleteItemAsync<ProjectGradingMappingDocument>(doc.id, oldPk);
                    }
                    catch
                    {
                        await _projectGradingMappingContainer.DeleteItemAsync<ProjectGradingMappingDocument>(doc.id, newPk);
                        throw;
                    }
                }
                else
                {
                    PartitionKey pk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                    await _projectGradingMappingContainer.ReplaceItemAsync(doc, doc.id, pk);
                }
                affectedCount++;
            }

            return affectedCount > 0;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[CreateProjectGrading].
        /// </summary>
        private async Task<bool> Sp_CreateProjectGrading(ProjectGradingMapping model)
        {
            if (!IsValidSqlDate(model.CreatedDttm) || !IsValidSqlDate(model.UpdateDttm))
            {
                return false;
            }

            var newId = await _sequenceService.GetNextAsync(CosmosContainers.dbo_ProjectGradingMapping);
            var newDoc = new ProjectGradingMappingDocument
            {
                id = newId.ToString(),
                Id = newId,
                ProjectId = model.ProjectId,
                TemplateId = model.TemplateId,
                SectionId = model.SectionId == Guid.Empty ? "00000000-0000-0000-0000-000000000000" : model.SectionId.ToString().ToLowerInvariant(),
                GradingId = model.GradingId,
                Value = model.Value,
                CreatedDttm = model.CreatedDttm == DateTime.MinValue ? null : model.CreatedDttm,
                UpdatedDttm = model.UpdateDttm == DateTime.MinValue ? null : model.UpdateDttm,
                IsDeleted = model.IsDeleted,
                RequiredInReport = null // Trap 25: not in INSERT
            };

            await _projectGradingMappingContainer.CreateItemAsync(newDoc, new PartitionKey((double)model.ProjectId));
            return true;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[UpdateProjectGradingConditionMapping].
        /// </summary>
        private async Task<bool> Sp_UpdateProjectGradingConditionMapping(ProjectGradingConditionMapping model)
        {
            if (!IsValidSqlDate(model.UpdateDttm))
            {
                return false;
            }

            var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id")
                .WithParameter("@id", model.Id);
            var existingDocs = await QueryAllAsync<ProjectGradingConditionMappingDocument>(_projectGradingConditionMappingContainer, q);

            if (existingDocs.Count == 0) return false;

            int affectedCount = 0;
            foreach (var doc in existingDocs)
            {
                var oldProjectId = doc.ProjectId;
                var newProjectId = (int?)model.ProjectId;

                doc.ProjectId = model.ProjectId;
                doc.TemplateId = model.TemplateId;
                doc.SectionId = model.SectionId.ToString().ToLowerInvariant();
                doc.GradingId = model.GradingId;
                doc.GradingConditionLabel = model.GradingConditionLabel;
                doc.GradingConditionValue = model.GradingConditionValue;
                doc.CreatedDttm = null; // Trap 2: not passed -> NULL
                doc.UpdatedDttm = model.UpdateDttm;
                doc.IsDeleted = model.IsDeleted;

                if (oldProjectId != newProjectId)
                {
                    PartitionKey newPk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                    PartitionKey oldPk = oldProjectId.HasValue ? new PartitionKey((double)oldProjectId.Value) : PartitionKey.Null;

                    await _projectGradingConditionMappingContainer.CreateItemAsync(doc, newPk);
                    try
                    {
                        await _projectGradingConditionMappingContainer.DeleteItemAsync<ProjectGradingConditionMappingDocument>(doc.id, oldPk);
                    }
                    catch
                    {
                        await _projectGradingConditionMappingContainer.DeleteItemAsync<ProjectGradingConditionMappingDocument>(doc.id, newPk);
                        throw;
                    }
                }
                else
                {
                    PartitionKey pk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                    await _projectGradingConditionMappingContainer.ReplaceItemAsync(doc, doc.id, pk);
                }
                affectedCount++;
            }

            return affectedCount > 0;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[CreateProjectGradingConditionMapping].
        /// </summary>
        private async Task<bool> Sp_CreateProjectGradingConditionMapping(ProjectGradingConditionMapping model)
        {
            if (!IsValidSqlDate(model.CreatedDttm) || !IsValidSqlDate(model.UpdateDttm))
            {
                return false;
            }

            var newId = await _sequenceService.GetNextAsync(CosmosContainers.dbo_ProjectGradingConditionMapping);
            var newDoc = new ProjectGradingConditionMappingDocument
            {
                id = newId.ToString(),
                Id = newId,
                ProjectId = model.ProjectId,
                TemplateId = model.TemplateId,
                SectionId = model.SectionId == Guid.Empty ? "00000000-0000-0000-0000-000000000000" : model.SectionId.ToString().ToLowerInvariant(),
                GradingId = model.GradingId,
                GradingConditionLabel = model.GradingConditionLabel,
                GradingConditionValue = model.GradingConditionValue,
                CreatedDttm = model.CreatedDttm == DateTime.MinValue ? null : model.CreatedDttm,
                UpdatedDttm = model.UpdateDttm == DateTime.MinValue ? null : model.UpdateDttm,
                IsDeleted = model.IsDeleted
            };

            await _projectGradingConditionMappingContainer.CreateItemAsync(newDoc, new PartitionKey((double)model.ProjectId));
            return true;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[UpdateProjectTemplate].
        /// </summary>
        private async Task<bool> Sp_UpdateProjectTemplate(ProjectReportMapping model)
        {
            if (!IsValidSqlDate(model.UpdateDttm))
            {
                return false;
            }

            var targetId = model.Id.ToString().ToLowerInvariant();
            var q = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                .WithParameter("@id", targetId);
            var existingDocs = await QueryAllAsync<ProjectTemplateMappingDocument>(_projectTemplateMappingContainer, q);

            if (existingDocs.Count == 0) return false;

            int affectedCount = 0;
            foreach (var doc in existingDocs)
            {
                var oldProjectId = doc.ProjectId;
                var newProjectId = (int?)model.ProjectId;

                doc.ProjectId = model.ProjectId;
                doc.TemplateId = model.TemplateId;
                doc.SectionId = model.SectionId.ToString().ToLowerInvariant();
                doc.Notes = model.Notes;
                doc.CreatedDttm = null; // Trap 2: not passed -> NULL
                doc.UpdatedDttm = model.UpdateDttm;
                doc.IsDeleted = model.IsDeleted;

                if (oldProjectId != newProjectId)
                {
                    PartitionKey newPk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                    PartitionKey oldPk = oldProjectId.HasValue ? new PartitionKey((double)oldProjectId.Value) : PartitionKey.Null;

                    await _projectTemplateMappingContainer.CreateItemAsync(doc, newPk);
                    try
                    {
                        await _projectTemplateMappingContainer.DeleteItemAsync<ProjectTemplateMappingDocument>(doc.id, oldPk);
                    }
                    catch
                    {
                        await _projectTemplateMappingContainer.DeleteItemAsync<ProjectTemplateMappingDocument>(doc.id, newPk);
                        throw;
                    }
                }
                else
                {
                    PartitionKey pk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                    await _projectTemplateMappingContainer.ReplaceItemAsync(doc, doc.id, pk);
                }
                affectedCount++;
            }

            return affectedCount > 0;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[UpdateH_IReport].
        /// </summary>
        private async Task<bool> Sp_UpdateH_IReport(HandIReport model)
        {
            if (!IsValidSqlDate(model.UpdatedDttm))
            {
                return false;
            }

            // Trap 19: WHERE ProjectId=@ProjectId and @IsActive=1 and @IsDeleted=0 and Id=@Id
            if (model.IsActive != true || model.IsDeleted != false)
            {
                return false;
            }

            var targetId = model.Id.ToString().ToLowerInvariant();
            var doc = await SafeReadAsync<H_IReportTemplateDocument>(
                _hiReportTemplateContainer,
                targetId,
                new PartitionKey((double)model.ProjectId));

            if (doc == null) return false;

            doc.CompanyName = model.CompanyName;
            doc.CertificateNumber = model.CertificateNumber;
            doc.ExpiryDate = NormalizeHIDate(model.ExpiryDate);
            doc.ThicknessMeasurementA = model.ThicknessMeasurementA;
            doc.ThicknessMeasurementB = model.ThicknessMeasurementB;
            doc.ThicknessMeasurementC = model.ThicknessMeasurementC;
            doc.AdditionalSurveyorComments = model.AdditionalSurveyorComments;
            doc.MaintenanceExecuted = model.MaintenanceExecuted;
            doc.Accommodation = model.Accommodation;
            doc.PhotoGraphs = model.PhotoGraphs;
            doc.OperationalTestLocation = model.OperationalTestLocation;
            doc.OperationalTestStart = NormalizeHIDate(model.OperationalTestStart);
            doc.OperationalTestFinish = NormalizeHIDate(model.OperationalTestFinish);
            doc.OptionalTestSatisFactory = model.OptionalTestSatisFactory;
            doc.AssociatedReportNumber = model.AssociatedReportNumber;
            doc.AssociatedReportDate = model.AssociatedReportDate;
            doc.Remarks = model.Remarks;
            doc.LeadSurveyorName = model.LeadSurveyorName;
            doc.LeadSurveyorNumber = model.LeadSurveyorNumber;
            doc.SurveyorName = model.SurveyorName;
            doc.SurveyorNumber = model.SurveyorNumber;
            doc.LastVisitDate = NormalizeHIDate(model.LastVisitDate);
            doc.IndividualA = model.IndividualA;
            doc.IndividualB = model.IndividualB;
            doc.IndividualC = model.IndividualC;
            doc.IndividualD = model.IndividualD;
            doc.IndividualE = model.IndividualE;
            doc.IndividualF = model.IndividualF;
            doc.SubstantialCorrosionRemaining = model.SubstantialCorrosionRemaining;
            doc.StatutoryDeficiencies = model.StatutoryDeficiencies;
            doc.FoundSatisfactory = model.FoundSatisfactory;
            doc.UnderRemarks = model.UnderRemarks;
            doc.ScouringOrPitting = model.ScouringOrPitting;
            doc.BottomPlattingScouringOrPitting = model.BottomPlattingScouringOrPitting;
            doc.IsActive = model.IsActive;
            doc.IsDeleted = model.IsDeleted;
            doc.CreatedDttm = null; // Trap 2: not passed -> NULL
            doc.UpdatedDttm = model.UpdatedDttm;
            doc.OthersDescription = null; // Trap 2: not passed -> NULL

            await _hiReportTemplateContainer.ReplaceItemAsync(doc, doc.id, new PartitionKey((double)model.ProjectId));
            return true;
        }

        /// <summary>
        /// Replaces stored procedure [dbo].[CreateH_IReport].
        /// </summary>
        private async Task<bool> Sp_CreateH_IReport(HandIReport model)
        {
            if (!IsValidSqlDate(model.CreatedDttm) || !IsValidSqlDate(model.UpdatedDttm))
            {
                return false;
            }

            var targetId = model.Id.ToString().ToLowerInvariant();

            // Trap 20: SQL PK uniqueness check across all partitions
            var checkQ = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.id = @id")
                .WithParameter("@id", targetId);
            int totalCount = 0;
            using (var feed = _hiReportTemplateContainer.GetItemQueryIterator<int>(checkQ))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    totalCount += resp.Sum();
                }
            }
            if (totalCount > 0)
            {
                return false;
            }

            var newDoc = new H_IReportTemplateDocument
            {
                id = targetId,
                Id = targetId,
                ProjectId = model.ProjectId,
                CompanyName = model.CompanyName,
                CertificateNumber = model.CertificateNumber,
                ExpiryDate = NormalizeHIDate(model.ExpiryDate),
                ThicknessMeasurementA = model.ThicknessMeasurementA,
                ThicknessMeasurementB = model.ThicknessMeasurementB,
                ThicknessMeasurementC = model.ThicknessMeasurementC,
                AdditionalSurveyorComments = model.AdditionalSurveyorComments,
                MaintenanceExecuted = model.MaintenanceExecuted,
                Accommodation = model.Accommodation,
                PhotoGraphs = model.PhotoGraphs,
                OperationalTestLocation = model.OperationalTestLocation,
                OperationalTestStart = NormalizeHIDate(model.OperationalTestStart),
                OperationalTestFinish = NormalizeHIDate(model.OperationalTestFinish),
                OptionalTestSatisFactory = model.OptionalTestSatisFactory,
                AssociatedReportNumber = model.AssociatedReportNumber,
                AssociatedReportDate = model.AssociatedReportDate,
                Remarks = model.Remarks,
                LeadSurveyorName = model.LeadSurveyorName,
                LeadSurveyorNumber = model.LeadSurveyorNumber,
                SurveyorName = model.SurveyorName,
                SurveyorNumber = model.SurveyorNumber,
                LastVisitDate = NormalizeHIDate(model.LastVisitDate),
                IndividualA = model.IndividualA,
                IndividualB = model.IndividualB,
                IndividualC = model.IndividualC,
                IndividualD = model.IndividualD,
                IndividualE = model.IndividualE,
                IndividualF = model.IndividualF,
                SubstantialCorrosionRemaining = model.SubstantialCorrosionRemaining,
                StatutoryDeficiencies = model.StatutoryDeficiencies,
                FoundSatisfactory = model.FoundSatisfactory,
                UnderRemarks = model.UnderRemarks,
                ScouringOrPitting = model.ScouringOrPitting,
                BottomPlattingScouringOrPitting = model.BottomPlattingScouringOrPitting,
                IsActive = model.IsActive,
                IsDeleted = model.IsDeleted,
                CreatedDttm = model.CreatedDttm == DateTime.MinValue ? null : model.CreatedDttm,
                UpdatedDttm = model.UpdatedDttm == DateTime.MinValue ? null : model.UpdatedDttm,
                OthersDescription = null // Trap 2
            };

            await _hiReportTemplateContainer.CreateItemAsync(newDoc, new PartitionKey((double)model.ProjectId));
            return true;
        }

        private static async Task<T?> SafeReadAsync<T>(Container container, string id, PartitionKey pk) where T : class
        {
            try
            {
                var resp = await container.ReadItemAsync<T>(id, pk);
                return resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        #endregion
    }
}
