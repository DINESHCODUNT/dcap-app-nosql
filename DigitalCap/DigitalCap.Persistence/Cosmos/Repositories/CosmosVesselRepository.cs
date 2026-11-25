using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.VesselModel;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosVesselRepository : CosmosRepositoryBase<ReportVesselMainDataDocument>, IVesselRepository
    {
        private readonly IFreedomAPIRepository _freedomApiRepository;
        private readonly Container _vesselContainer;
        private readonly Container _mainDataContainer;
        private readonly Container _certContainer;
        private readonly Container _surveyContainer;

        public CosmosVesselRepository(
            ICosmosContainerFactory containerFactory,
            IFreedomAPIRepository freedomApiRepository,
            ILogger<CosmosVesselRepository> logger)
            : base(containerFactory, CosmosContainers.dbo_ReportVesselMainData, logger)
        {
            _freedomApiRepository = freedomApiRepository;
            _vesselContainer = containerFactory.GetContainer(CosmosContainers.core_Vessel);
            _mainDataContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportVesselMainData);
            _certContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportStatutoryCertificates);
            _surveyContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportSurveyDetails);
        }

        public void Commit() { }

        public async Task<IEnumerable<SurveyStatus>> GetSurveyStatus(int projectId)
        {
            try
            {
                var result = await Sp_GetSurveyStatus(projectId);
                return result;
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<IEnumerable<ReportVesselMainData>> GetVesselMainData(int projectId)
        {
            var result = await Sp_GetVesselMaindata(projectId);
            return result;
        }

        public async Task<Core.Models.Survey.Certificates> GetStatutoryCertificate(int projectId)
        {
            var data = await Sp_GetStatutoryCertificates(projectId);

            var issued = new Core.Models.Survey.StatutoryCertificates();
            var expiry = new Core.Models.Survey.StatutoryCertificates();

            if (data != null)
            {
                issued.LLLIDate = data.llliIssueDate ?? DateTime.MinValue;
                expiry.LLLIDate = data.llliExpiryDate ?? DateTime.MinValue;

                issued.SafetyConDate = data.safetyConIssueDate ?? DateTime.MinValue;
                expiry.SafetyConDate = data.safetyConExpiryDate ?? DateTime.MinValue;

                issued.SafetyRadioDate = data.safetyRadioIssueDate ?? DateTime.MinValue;
                expiry.SafetyRadioDate = data.safetyRadioExpiryDate ?? DateTime.MinValue;

                issued.SafetyEquipmentDate = data.safetyEquipmentIssueDate ?? DateTime.MinValue;
                expiry.SafetyEquipmentDate = data.safetyEquipmentExpiryDate ?? DateTime.MinValue;

                issued.IEEDate = data.ieeIssueDate ?? DateTime.MinValue;
                expiry.IEEDate = data.ieeExpiryDate ?? DateTime.MinValue;

                issued.IOPPDate = data.ioppIssueDate ?? DateTime.MinValue;
                expiry.IOPPDate = data.ioppExpiryDate ?? DateTime.MinValue;

                issued.IAPDate = data.iapIssueDate ?? DateTime.MinValue;
                expiry.IAPDate = data.iapExpiryDate ?? DateTime.MinValue;

                issued.IAFSDate = data.iafsIsuueDate ?? DateTime.MinValue;
                expiry.IAFSDate = data.iafsExpiryDate ?? DateTime.MinValue;

                issued.ISPSDate = data.ispsIssueDate ?? DateTime.MinValue;
                expiry.ISPSDate = data.ispsExpiryDate ?? DateTime.MinValue;

                issued.CargoGearReTestDate = data.cargoGearReTestIssueDate ?? DateTime.MinValue;
                expiry.CargoGearReTestDate = data.cargoGearReTestExpiryDate ?? DateTime.MinValue;

                issued.CargoGearAnnualCertificate = data.cargoGearAnnualIssueCertificate ?? DateTime.MinValue;
                expiry.CargoGearAnnualCertificate = data.cargoGearAnnualExpiryCertificate ?? DateTime.MinValue;

                issued.IBWMDate = data.ibwmIssueDate ?? DateTime.MinValue;
                expiry.IBWMDate = data.ibwmExpiryDate ?? DateTime.MinValue;

                issued.ISPPSewageDate = data.isppSewageIssueDate ?? DateTime.MinValue;
                expiry.ISPPSewageDate = data.isppSewageExpiryDate ?? DateTime.MinValue;

                issued.ISMDate = data.ismIssueDate ?? DateTime.MinValue;
                expiry.ISMDate = data.ismExpiryDate ?? DateTime.MinValue;

                issued.MaritimeLabourDate = data.maritimeLabourIssueDate ?? DateTime.MinValue;
                expiry.MaritimeLabourDate = data.maritimeLabourExpiryDate ?? DateTime.MinValue;

                issued.GasCarrierFitness = data.fitnessGasCertificateIssueDate ?? DateTime.MinValue;
                expiry.GasCarrierFitness = data.fitnessGasCertificateExpiryDate ?? DateTime.MinValue;
            }

            return new Core.Models.Survey.Certificates
            {
                StatutoryCertificatesIssuedDates = issued,
                StatutoryCertificatesExpirationDates = expiry
            };
        }

        private static DateTime? Pick(DateTime? newValue, DateTime? oldValue)
        {
            if (newValue != null)
                return newValue == DateTime.MinValue ? null : newValue;

            return oldValue == DateTime.MinValue ? null : oldValue;
        }

        private static DateTime? Normalize(DateTime? date)
        {
            return date == null || date == DateTime.MinValue ? null : date;
        }

        public async Task<bool> UpdateStatutoryCertificate(int projectId, Core.Models.Survey.Report report)
        {
            var data = await Sp_GetStatutoryCertificates(projectId);

            if (data == null)
            {
                var doc = new ReportStatutoryCertificatesDocument
                {
                    id = projectId.ToString(),
                    projectId = projectId,
                    llliIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.LLLIDate),
                    safetyConIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.SafetyConDate),
                    safetyRadioIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.SafetyRadioDate),
                    safetyEquipmentIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.SafetyEquipmentDate),
                    ioppIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.IOPPDate),
                    iapIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.IAPDate),
                    iafsIsuueDate = Normalize(report.StatutoryCertificatesIssuedDates?.IAFSDate),
                    ismIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.ISMDate),
                    ispsIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.ISPSDate),
                    cargoGearReTestIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.CargoGearReTestDate),
                    cargoGearAnnualIssueCertificate = Normalize(report.StatutoryCertificatesIssuedDates?.CargoGearAnnualCertificate),
                    ibwmIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.IBWMDate),
                    isppSewageIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.ISPPSewageDate),
                    maritimeLabourIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.MaritimeLabourDate),
                    ieeIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.IEEDate),
                    fitnessGasCertificateIssueDate = Normalize(report.StatutoryCertificatesIssuedDates?.GasCarrierFitness),

                    ieeExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.IEEDate),
                    llliExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.LLLIDate),
                    safetyConExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.SafetyConDate),
                    safetyRadioExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.SafetyRadioDate),
                    safetyEquipmentExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.SafetyEquipmentDate),
                    cargoGearReTestExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.CargoGearReTestDate),
                    cargoGearAnnualExpiryCertificate = Normalize(report.StatutoryCertificatesExpirationDates?.CargoGearAnnualCertificate),
                    ibwmExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.IBWMDate),
                    isppSewageExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.ISPPSewageDate),
                    maritimeLabourExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.MaritimeLabourDate),
                    ioppExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.IOPPDate),
                    iapExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.IAPDate),
                    iafsExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.IAFSDate),
                    ismExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.ISMDate),
                    ispsExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.ISPSDate),
                    fitnessGasCertificateExpiryDate = Normalize(report.StatutoryCertificatesExpirationDates?.GasCarrierFitness)
                };

                await Sp_Insert_ReportStatutoryCertificates(doc);
                Commit();
            }
            else
            {
                var doc = new ReportStatutoryCertificatesDocument
                {
                    id = projectId.ToString(),
                    projectId = projectId,
                    ieeIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.IEEDate, data.ieeIssueDate),
                    llliIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.LLLIDate, data.llliIssueDate),
                    safetyConIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.SafetyConDate, data.safetyConIssueDate),
                    safetyRadioIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.SafetyRadioDate, data.safetyRadioIssueDate),
                    safetyEquipmentIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.SafetyEquipmentDate, data.safetyEquipmentIssueDate),
                    ioppIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.IOPPDate, data.ioppIssueDate),
                    iapIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.IAPDate, data.iapIssueDate),
                    iafsIsuueDate = Pick(report.StatutoryCertificatesIssuedDates?.IAFSDate, data.iafsIsuueDate),
                    ismIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.ISMDate, data.ismIssueDate),
                    ispsIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.ISPSDate, data.ispsIssueDate),
                    cargoGearReTestIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.CargoGearReTestDate, data.cargoGearReTestIssueDate),
                    cargoGearAnnualIssueCertificate = Pick(report.StatutoryCertificatesIssuedDates?.CargoGearAnnualCertificate, data.cargoGearAnnualIssueCertificate),
                    ibwmIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.IBWMDate, data.ibwmIssueDate),
                    isppSewageIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.ISPPSewageDate, data.isppSewageIssueDate),
                    maritimeLabourIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.MaritimeLabourDate, data.maritimeLabourIssueDate),
                    fitnessGasCertificateIssueDate = Pick(report.StatutoryCertificatesIssuedDates?.GasCarrierFitness, data.fitnessGasCertificateIssueDate),

                    ieeExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.IEEDate, data.ieeExpiryDate),
                    llliExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.LLLIDate, data.llliExpiryDate),
                    safetyConExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.SafetyConDate, data.safetyConExpiryDate),
                    safetyRadioExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.SafetyRadioDate, data.safetyRadioExpiryDate),
                    safetyEquipmentExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.SafetyEquipmentDate, data.safetyEquipmentExpiryDate),
                    cargoGearReTestExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.CargoGearReTestDate, data.cargoGearReTestExpiryDate),
                    cargoGearAnnualExpiryCertificate = Pick(report.StatutoryCertificatesExpirationDates?.CargoGearAnnualCertificate, data.cargoGearAnnualExpiryCertificate),
                    ibwmExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.IBWMDate, data.ibwmExpiryDate),
                    isppSewageExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.ISPPSewageDate, data.isppSewageExpiryDate),
                    maritimeLabourExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.MaritimeLabourDate, data.maritimeLabourExpiryDate),
                    ioppExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.IOPPDate, data.ioppExpiryDate),
                    iapExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.IAPDate, data.iapExpiryDate),
                    iafsExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.IAFSDate, data.iafsExpiryDate),
                    ismExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.ISMDate, data.ismExpiryDate),
                    ispsExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.ISPSDate, data.ispsExpiryDate),
                    fitnessGasCertificateExpiryDate = Pick(report.StatutoryCertificatesExpirationDates?.GasCarrierFitness, data.fitnessGasCertificateExpiryDate)
                };

                await Sp_Update_ReportStatutoryCertificates(doc);
                Commit();
            }

            return true;
        }

        private static DateTime? ResolveDate(DateTime? input, DateTime? existing)
        {
            if (input != null)
                return input == DateTime.MinValue ? null : input;

            return existing == DateTime.MinValue ? null : existing;
        }

        private static bool HasPre1753Date(params DateTime?[] dates)
        {
            foreach (var d in dates)
            {
                if (d.HasValue && d.Value != DateTime.MinValue && d.Value.Year < 1753)
                    return true;
            }
            return false;
        }

        public async Task<bool> UpdateSurveyAudit(Core.Models.Survey.SurveyStatus surveyStatus, int projectId)
        {
            var data = (await GetSurveyStatus(projectId))?.ToList();
            if (data == null)
            {
                throw new ArgumentNullException("source", "Value cannot be null. (Parameter 'source')");
            }

            if (data.Count == 0)
            {
                if (surveyStatus.ClassSurveyDate < new DateTime(1753, 1, 1) || surveyStatus.DryDockDate < new DateTime(1753, 1, 1) || HasPre1753Date(
                    surveyStatus.ClassSurveyDate, surveyStatus.DryDockDate,
                    surveyStatus.SpecialContinuousMachineryDate, surveyStatus.BoilerDate, surveyStatus.TailshaftDate,
                    surveyStatus.SpecialContinuousHull1, surveyStatus.SpecialContinuousHull2, surveyStatus.SpecialContinuousHull3,
                    surveyStatus.SpecialContinuousHull4, surveyStatus.SpecialContinuousHull5, surveyStatus.SpecialContinuousHull6,
                    surveyStatus.SpecialContinuousHull7, surveyStatus.SpecialContinuousHull8, surveyStatus.SpecialContinuousHull9,
                    surveyStatus.SpecialContinuousHull10, surveyStatus.SpecialContinuousHull11, surveyStatus.SpecialContinuousHull12,
                    surveyStatus.SpecialContinuousHull13, surveyStatus.SpecialContinuousHull14, surveyStatus.SpecialContinuousHull15))
                {
                    // SQL datetime overflow (< 1753) is swallowed, writes nothing
                    return true;
                }

                try
                {
                    var doc = new ReportSurveyDetailsDocument
                    {
                        id = projectId.ToString(),
                        projectId = projectId,
                        classSurveyDate = surveyStatus.ClassSurveyDate,
                        dryDockDate = surveyStatus.DryDockDate,
                        specialContinuousMachineryDate = surveyStatus.SpecialContinuousMachineryDate != DateTime.MinValue ? surveyStatus.SpecialContinuousMachineryDate : null,
                        boilerDate = surveyStatus.BoilerDate != DateTime.MinValue ? surveyStatus.BoilerDate : null,
                        tailshaftDate = surveyStatus.TailshaftDate != DateTime.MinValue ? surveyStatus.TailshaftDate : null,
                        specialContinuousHull1 = surveyStatus.SpecialContinuousHull1 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull1 : null,
                        specialContinuousHull2 = surveyStatus.SpecialContinuousHull2 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull2 : null,
                        specialContinuousHull3 = surveyStatus.SpecialContinuousHull3 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull3 : null,
                        specialContinuousHull4 = surveyStatus.SpecialContinuousHull4 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull4 : null,
                        specialContinuousHull5 = surveyStatus.SpecialContinuousHull5 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull5 : null,
                        specialContinuousHull6 = surveyStatus.SpecialContinuousHull6 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull6 : null,
                        specialContinuousHull7 = surveyStatus.SpecialContinuousHull7 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull7 : null,
                        specialContinuousHull8 = surveyStatus.SpecialContinuousHull8 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull8 : null,
                        specialContinuousHull9 = surveyStatus.SpecialContinuousHull9 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull9 : null,
                        specialContinuousHull10 = surveyStatus.SpecialContinuousHull10 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull10 : null,
                        specialContinuousHull11 = surveyStatus.SpecialContinuousHull11 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull11 : null,
                        specialContinuousHull12 = surveyStatus.SpecialContinuousHull12 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull12 : null,
                        specialContinuousHull13 = surveyStatus.SpecialContinuousHull13 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull13 : null,
                        specialContinuousHull14 = surveyStatus.SpecialContinuousHull14 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull14 : null,
                        specialContinuousHull15 = surveyStatus.SpecialContinuousHull15 != DateTime.MinValue ? surveyStatus.SpecialContinuousHull15 : null,
                        specialHullNo = surveyStatus.SpecialHullNo
                    };

                    await Sp_Insert_SurveyDetails(doc);
                    Commit();
                }
                catch (Exception)
                { }
            }
            else
            {
                try
                {
                    var surveyStatusData = data.FirstOrDefault();

                    var doc = new ReportSurveyDetailsDocument
                    {
                        id = projectId.ToString(),
                        projectId = projectId,
                        classSurveyDate = ResolveDate(surveyStatus.ClassSurveyDate, surveyStatusData?.ClassSurveyDate),
                        dryDockDate = ResolveDate(surveyStatus.DryDockDate, surveyStatusData?.DryDockDate),
                        specialContinuousMachineryDate = ResolveDate(surveyStatus.SpecialContinuousMachineryDate, surveyStatusData?.SpecialContinuousMachineryDate),
                        boilerDate = ResolveDate(surveyStatus.BoilerDate, surveyStatusData?.BoilerDate),
                        tailshaftDate = ResolveDate(surveyStatus.TailshaftDate, surveyStatusData?.TailshaftDate),
                        specialContinuousHull1 = ResolveDate(surveyStatus.SpecialContinuousHull1, surveyStatusData?.SpecialContinuousHull1),
                        specialContinuousHull2 = ResolveDate(surveyStatus.SpecialContinuousHull2, surveyStatusData?.SpecialContinuousHull2),
                        specialContinuousHull3 = ResolveDate(surveyStatus.SpecialContinuousHull3, surveyStatusData?.SpecialContinuousHull3),
                        specialContinuousHull4 = ResolveDate(surveyStatus.SpecialContinuousHull4, surveyStatusData?.SpecialContinuousHull4),
                        specialContinuousHull5 = ResolveDate(surveyStatus.SpecialContinuousHull5, surveyStatusData?.SpecialContinuousHull5),
                        specialContinuousHull6 = ResolveDate(surveyStatus.SpecialContinuousHull6, surveyStatusData?.SpecialContinuousHull6),
                        specialContinuousHull7 = ResolveDate(surveyStatus.SpecialContinuousHull7, surveyStatusData?.SpecialContinuousHull7),
                        specialContinuousHull8 = ResolveDate(surveyStatus.SpecialContinuousHull8, surveyStatusData?.SpecialContinuousHull8),
                        specialContinuousHull9 = ResolveDate(surveyStatus.SpecialContinuousHull9, surveyStatusData?.SpecialContinuousHull9),
                        specialContinuousHull10 = ResolveDate(surveyStatus.SpecialContinuousHull10, surveyStatusData?.SpecialContinuousHull10),
                        specialContinuousHull11 = ResolveDate(surveyStatus.SpecialContinuousHull11, surveyStatusData?.SpecialContinuousHull11),
                        specialContinuousHull12 = ResolveDate(surveyStatus.SpecialContinuousHull12, surveyStatusData?.SpecialContinuousHull12),
                        specialContinuousHull13 = ResolveDate(surveyStatus.SpecialContinuousHull13, surveyStatusData?.SpecialContinuousHull13),
                        specialContinuousHull14 = ResolveDate(surveyStatus.SpecialContinuousHull14, surveyStatusData?.SpecialContinuousHull14),
                        specialContinuousHull15 = ResolveDate(surveyStatus.SpecialContinuousHull15, surveyStatusData?.SpecialContinuousHull15),
                        specialHullNo = (surveyStatus.SpecialHullNo != null)
                            ? (surveyStatus.SpecialHullNo == 0 ? null : surveyStatus.SpecialHullNo)
                            : (surveyStatusData?.SpecialHullNo == 0 ? null : surveyStatusData?.SpecialHullNo)
                    };

                    if (HasPre1753Date(
                        doc.classSurveyDate, doc.dryDockDate,
                        doc.specialContinuousMachineryDate, doc.boilerDate, doc.tailshaftDate,
                        doc.specialContinuousHull1, doc.specialContinuousHull2, doc.specialContinuousHull3,
                        doc.specialContinuousHull4, doc.specialContinuousHull5, doc.specialContinuousHull6,
                        doc.specialContinuousHull7, doc.specialContinuousHull8, doc.specialContinuousHull9,
                        doc.specialContinuousHull10, doc.specialContinuousHull11, doc.specialContinuousHull12,
                        doc.specialContinuousHull13, doc.specialContinuousHull14, doc.specialContinuousHull15))
                    {
                        // SQL datetime overflow (< 1753) is swallowed, writes nothing
                        return true;
                    }

                    await Sp_Update_SurveyDetails(doc);
                    Commit();
                }
                catch
                {
                    // log error
                }
            }

            return true;
        }

        public async Task<Core.Models.Vessel?> Details(string imoNumber)
        {
            try
            {
                var result = await Sp_Read_VesselDetails_ByIMONumber(imoNumber);
                Commit();
                return result;
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> UpdateReportMainDataAsync(Report report, int projectId)
        {
            try
            {
                var data = (await GetVesselMainData(projectId))?.FirstOrDefault();

                if (data == null)
                {
                    // SQL evaluates report.Vessel.SelectedPrefix (NRE if Vessel is null) and Convert.ToDateTime(report.ReportDate) before executing the SP
                    _ = report.Vessel.SelectedPrefix;
                    _ = Convert.ToDateTime(report.ReportDate);
                    // KNOWN SQL BUG (reported to dcap-app team)
                    throw new InvalidOperationException("Procedure or function Insert_VesselDetails has too many arguments specified.");
                }

                _ = report.Vessel.ClassNo; // Let it throw NullReferenceException if report.Vessel is null (matching SQL)

                int? selPrefix;
                if (report.Vessel.SelectedPrefix != null)
                {
                    if (report.Vessel.SelectedPrefix == "")
                    {
                        selPrefix = null;
                    }
                    else if (report.Vessel.SelectedPrefix.TrimEnd(' ') == "")
                    {
                        selPrefix = 0; // Spaces-only in SQL converts to 0
                    }
                    else if (int.TryParse(report.Vessel.SelectedPrefix, out var parsedSp))
                    {
                        selPrefix = parsedSp;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Conversion failed when converting the varchar value '{report.Vessel.SelectedPrefix}' to data type int.");
                    }
                }
                else
                {
                    selPrefix = data.selectedPrefix;
                }

                var rawFirstVisit = report.Vessel.FirstVisitDate != null
                    ? (report.Vessel.FirstVisitDate == "" ? null : report.Vessel.FirstVisitDate)
                    : (data.firstVisitDate == null ? null : data.firstVisitDate);

                DateTime lastVisitDate;
                DateTime firstVisitDate;

                bool isFirstValid = DateTime.TryParseExact(
                    report?.Vessel?.FirstVisitDate,
                    "dd-MM-yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out firstVisitDate) && firstVisitDate.Date != new DateTime(1900, 1, 1);

                bool isLastValid = DateTime.TryParseExact(
                    data?.lastVisitDate,
                    "dd-MM-yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out lastVisitDate) && lastVisitDate.Date != new DateTime(1900, 1, 1);

                string? finalLastVisit;
                if (isLastValid && isFirstValid && lastVisitDate.Date < firstVisitDate.Date)
                {
                    finalLastVisit = null;
                }
                else
                {
                    finalLastVisit = report.Vessel.LastVisitDate != null
                        ? (report.Vessel.LastVisitDate == "" ? null : report.Vessel.LastVisitDate)
                        : (data.lastVisitDate == "" ? null : data.lastVisitDate);
                }

                string? actualReportStartDate = report.Vessel.ActualReportStartDate != DateTime.MinValue
                    ? FormatSqlStyle0(report.Vessel.ActualReportStartDate)
                    : (Convert.ToDateTime(data.actualReportStartDate) == DateTime.MinValue ? null : data.actualReportStartDate);

                string? portDate = report.ReportDate != null
                    ? (Convert.ToDateTime(report.ReportDate) == DateTime.MinValue ? null : report.ReportDate)
                    : (Convert.ToDateTime(data.portDate) == DateTime.MinValue ? null : data.portDate);

                var doc = new ReportVesselMainDataDocument
                {
                    id = projectId.ToString(),
                    projectId = projectId,
                    SelectedPrefix = selPrefix,
                    reportNo = report.ReportNo != null ? (report.ReportNo == "" ? null : report.ReportNo) : (data.reportNo == "" ? null : data.reportNo),
                    reportPort = report.ReportPort != null ? (report.ReportPort == "" ? null : report.ReportPort) : (data.reportPort == "" ? null : data.reportPort),
                    classNo = report.Vessel.ClassNo != null ? (report.Vessel.ClassNo == "" ? null : report.Vessel.ClassNo) : (data.classNo == "" ? null : data.classNo),
                    builtBy = report.Vessel.BuiltBy != null ? (report.Vessel.BuiltBy == "" ? null : report.Vessel.BuiltBy) : (data.builtBy == "" ? null : data.builtBy),
                    vesselName = report.Vessel.VesselName != null ? (report.Vessel.VesselName == "" ? null : report.Vessel.VesselName) : (data.vesselName == "" ? null : data.vesselName),
                    length = (report.Vessel.Length != 0 || Convert.ToDecimal(report.Vessel.Length) == 0.1m)
                        ? (Convert.ToDecimal(report.Vessel.Length) == 0.1m ? null : FormatReal(report.Vessel.Length))
                        : (data.length == "" ? null : data.length),
                    yearBuilt = report.Vessel.YearBuilt != 0
                        ? report.Vessel.YearBuilt.ToString()
                        : data.yearBuilt,
                    hullNo = report.Vessel.HullNo != null ? (report.Vessel.HullNo == "" ? null : report.Vessel.HullNo) : data.hullNo,
                    vesselID = report.Vessel.ID,
                    imoNumber = report.Vessel.ImoNumber,
                    vesselType = report.Vessel.VesselType != null ? (report.Vessel.VesselType == "" ? null : report.Vessel.VesselType) : data.vesselType,
                    flagName = report.Vessel.FlagName != null ? (report.Vessel.FlagName == "" ? null : report.Vessel.FlagName) : data.flagName,
                    homeport = report.Vessel.Homeport != null ? (report.Vessel.Homeport == "" ? null : report.Vessel.Homeport) : data.homeport,
                    portDate = portDate,
                    officalNumber = report.Vessel.OfficalNumber != null ? (report.Vessel.OfficalNumber == "" ? null : report.Vessel.OfficalNumber) : (data.officalNumber == null ? null : data.officalNumber),
                    callSign = report.Vessel.CallSign != null ? (report.Vessel.CallSign == "" ? null : report.Vessel.CallSign) : (data.callSign == null ? null : data.callSign),
                    previousName = report.Vessel.PreviousName != null ? (report.Vessel.PreviousName == "" ? null : report.Vessel.PreviousName) : (data.previousName == null ? null : data.previousName),
                    ownerName = report.Vessel.OwnerName != null ? (report.Vessel.OwnerName == "" ? null : report.Vessel.OwnerName) : (data.ownerName == null ? null : data.ownerName),
                    manager = report.Vessel.Manager != null ? (report.Vessel.Manager == "" ? null : report.Vessel.Manager) : (data.manager == null ? null : data.manager),
                    breadth = (report.Vessel.Breadth != 0 || Convert.ToDecimal(report.Vessel.Breadth) == 0.1m)
                        ? (Convert.ToDecimal(report.Vessel.Breadth) == 0.1m ? null : FormatReal(report.Vessel.Breadth))
                        : (data.breadth == "" ? null : data.breadth),
                    depth = (report.Vessel.DEPTH != 0 || Convert.ToDecimal(report.Vessel.DEPTH) == 0.1m)
                        ? (Convert.ToDecimal(report.Vessel.DEPTH) == 0.1m ? null : FormatReal(report.Vessel.DEPTH))
                        : (data.depth == "" ? null : data.depth),
                    draft = (report.Vessel.Draft != 0 || Convert.ToDecimal(report.Vessel.Draft) == 0.1m)
                        ? (Convert.ToDecimal(report.Vessel.Draft) == 0.1m ? null : FormatReal(report.Vessel.Draft))
                        : (data.draft == "" ? null : data.draft),
                    deadweight = report.Vessel.DEADWEIGHT != null
                        ? (report.Vessel.DEADWEIGHT == "" ? null : report.Vessel.DEADWEIGHT)
                        : (data.deadweight == "" ? null : data.deadweight),
                    grossTons = (report.Vessel.GrossTons != 0 || Convert.ToDecimal(report.Vessel.GrossTons) == 0.1m)
                        ? (Convert.ToDecimal(report.Vessel.GrossTons) == 0.1m ? null : FormatReal(report.Vessel.GrossTons))
                        : (data.grossTons == "" ? null : data.grossTons),
                    netTons = (report.Vessel.NetTons != 0 || Convert.ToDecimal(report.Vessel.NetTons) == 0.1m)
                        ? (Convert.ToDecimal(report.Vessel.NetTons) == 0.1m ? null : FormatReal(report.Vessel.NetTons))
                        : (data.netTons == "" ? null : data.netTons),
                    propulsionType = report.Vessel.PropulsionType != null ? (report.Vessel.PropulsionType == "" ? null : report.Vessel.PropulsionType) : (data.propulsionType == null ? null : data.propulsionType),
                    kw = report.Vessel.KW != null ? (report.Vessel.KW == "" ? null : report.Vessel.KW) : (data.kw == null ? null : data.kw),
                    shaftRPM = (report.Vessel.ShaftRPM != 0 || Convert.ToDecimal(report.Vessel.ShaftRPM) == 0.1m)
                        ? (Convert.ToDecimal(report.Vessel.ShaftRPM) == 0.1m ? null : FormatReal(report.Vessel.ShaftRPM))
                        : (data.shaftRPM == "" ? null : data.shaftRPM),
                    propulsionManufacturer = report.Vessel.PropulsionManufacturer != null ? (report.Vessel.PropulsionManufacturer == "" ? null : report.Vessel.PropulsionManufacturer) : (data.propulsionManufacturer == null ? null : data.propulsionManufacturer),
                    gearManufacturer = report.Vessel.GearManufacturer != null ? (report.Vessel.GearManufacturer == "" ? null : report.Vessel.GearManufacturer) : (data.gearManufacturer == null ? null : data.gearManufacturer),
                    classed = report.Vessel.Classed != null ? (report.Vessel.Classed == "" ? null : report.Vessel.Classed) : (data.classed == null ? null : data.classed),
                    classNotation = report.Vessel.ClassNotation != null ? (report.Vessel.ClassNotation == "" ? null : report.Vessel.ClassNotation) : (data.classNotation == null ? null : data.classNotation),
                    machinery = report.Vessel.Machinery != null ? (report.Vessel.Machinery == "" ? null : report.Vessel.Machinery) : (data.machinery == null ? null : data.machinery),
                    other = report.Vessel.Other != null ? (report.Vessel.Other == "" ? null : report.Vessel.Other) : (data.other == null ? null : data.other),
                    environment = report.Vessel.Environment != null ? (report.Vessel.Environment == "" ? null : report.Vessel.Environment) : (data.environment == null ? null : data.environment),
                    actualReportStartDate = actualReportStartDate,
                    firstVisitDate = rawFirstVisit,
                    lastVisitDate = finalLastVisit
                };

                await Sp_Update_VesselDetails(doc);
                Commit();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<string> CreateVesselMainDataAsync(string classNumber, Project project)
        {
            try
            {
                Core.Models.VesselModel.Vessel? vessel = null;
                if (!string.IsNullOrWhiteSpace(classNumber))
                {
                    vessel = await _freedomApiRepository.GetVessel(classNumber);
                }

                // In SQL, calls non-existent procedure Create_VesselMainData -> catch returns null, writes nothing
                return null!;
            }
            catch
            {
                return null!;
            }
        }

        public async Task CreateVesselMainDataAsync(string classNumber, Project project, Core.Models.VesselModel.Vessel vessel)
        {
            try
            {
                var firstVisit = project.SurveyFirstVisit != null && project.SurveyFirstVisit != DateTime.MinValue
                    ? project.SurveyFirstVisit.Value.ToString("dd-MM-yyy")
                    : DateTime.Now.ToString("dd-MM-yyy");
                var lastVisit = project.SurveyLastVisit != null && project.SurveyLastVisit != DateTime.MinValue
                    ? project.SurveyLastVisit.Value.ToString("dd-MM-yyy")
                    : DateTime.Now.ToString("dd-MM-yyy");

                var doc = new ReportVesselMainDataDocument
                {
                    id = project.Id.ToString(),
                    projectId = project.Id,
                    classNo = project.AbsClassID,
                    builtBy = project.Builder,
                    vesselName = project.VesselName,
                    length = project.LengthOverall?.ToString(CultureInfo.InvariantCulture),
                    yearBuilt = project.YearBuilt.HasValue ? project.YearBuilt.Value.ToString() : null,
                    hullNo = project.HullNumber,
                    imoNumber = project.IMO,
                    classed = project.ClassSociety != null && project.ClassSociety.Length > 0 ? project.ClassSociety[0] : null,
                    firstVisitDate = firstVisit,
                    lastVisitDate = lastVisit,
                    SelectedPrefix = 0
                };

                if (vessel != null)
                {
                    int? vImo = null;
                    if (vessel.ImoNumber != null)
                    {
                        var trimmed = vessel.ImoNumber.TrimEnd(' ');
                        if (trimmed == "")
                        {
                            vImo = 0;
                        }
                        else if (int.TryParse(vessel.ImoNumber.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                        {
                            vImo = parsed;
                        }
                        else
                        {
                            throw new FormatException($"Conversion failed when converting the varchar value '{vessel.ImoNumber}' to data type int.");
                        }
                    }

                    doc.vesselID = vImo;
                    doc.vesselType = vessel.VesselType;
                    doc.flagName = vessel.FlagName;
                    doc.homeport = vessel.PortRegistry;
                    doc.officalNumber = vessel.OfficialNumber;
                    doc.callSign = vessel.CallSign;
                    doc.ownerName = vessel.RegisteredOwner?.Name;
                    doc.breadth = vessel.BreadthMolded?.Value;
                    doc.depth = vessel.DepthMolded?.Value;
                    doc.deadweight = vessel.DesignDeadWeight?.Value;
                    doc.netTons = vessel.NetTonnage?.Value;
                    doc.classNotation = vessel.ClassNotation;
                }

                await Sp_Insert_VesselDetails(doc);
                Commit();
            }
            catch
            {
                throw;
            }
        }

        #region Stored Procedure Helpers

        internal static string FormatSqlStyle0(DateTime d)
        {
            int hour12 = d.Hour % 12;
            if (hour12 == 0) hour12 = 12;
            string ampm = d.Hour >= 12 ? "PM" : "AM";
            return string.Format(CultureInfo.InvariantCulture, "{0} {1,2} {2:D4} {3,2}:{4:D2}{5}",
                d.ToString("MMM", CultureInfo.InvariantCulture),
                d.Day,
                d.Year,
                hour12,
                d.Minute,
                ampm);
        }

        internal static string? FormatReal(object? val)
        {
            if (val == null) return null;
            float f;
            if (val is float fl)
            {
                f = fl;
            }
            else if (val is double dbl)
            {
                f = (float)dbl;
            }
            else if (val is decimal dec)
            {
                f = (float)dec;
            }
            else if (val is int i)
            {
                f = (float)i;
            }
            else if (val is long l)
            {
                f = (float)l;
            }
            else if (float.TryParse(Convert.ToString(val, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                f = parsed;
            }
            else
            {
                return Convert.ToString(val, CultureInfo.InvariantCulture);
            }

            if (f == 0f) return "0";

            string g6 = f.ToString("G6", CultureInfo.InvariantCulture);
            int eIndex = g6.IndexOfAny(new[] { 'e', 'E' });
            if (eIndex >= 0)
            {
                string mantissa = g6.Substring(0, eIndex);
                string expStr = g6.Substring(eIndex + 1);
                if (int.TryParse(expStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var exp))
                {
                    return mantissa + "e" + (exp >= 0 ? "+" : "-") + Math.Abs(exp).ToString("D3", CultureInfo.InvariantCulture);
                }
            }

            return g6;
        }

        /// <summary>
        /// Replaces [dbo].[GetSurveyStatus].
        /// SELECT * FROM [dbo].[ReportSurveyDetails] WHERE [projectId] = @projectId
        /// </summary>
        private async Task<List<SurveyStatus>> Sp_GetSurveyStatus(int projectId)
        {
            var idStr = projectId.ToString();
            try
            {
                var resp = await _surveyContainer.ReadItemAsync<ReportSurveyDetailsDocument>(idStr, new PartitionKey(idStr));
                if (resp.Resource != null)
                {
                    var d = resp.Resource;
                    if (d.imoNum != null)
                    {
                        throw new InvalidCastException("Error parsing column 22 (imoNum=... - DateTime to Int32)");
                    }
                    return new List<SurveyStatus>
                    {
                        new SurveyStatus
                        {
                            ClassSurveyDate = d.classSurveyDate,
                            DryDockDate = d.dryDockDate,
                            SpecialContinuousMachineryDate = d.specialContinuousMachineryDate,
                            BoilerDate = d.boilerDate,
                            TailshaftDate = d.tailshaftDate,
                            SpecialContinuousHull1 = d.specialContinuousHull1,
                            SpecialContinuousHull2 = d.specialContinuousHull2,
                            SpecialContinuousHull3 = d.specialContinuousHull3,
                            SpecialContinuousHull4 = d.specialContinuousHull4,
                            SpecialContinuousHull5 = d.specialContinuousHull5,
                            SpecialContinuousHull6 = d.specialContinuousHull6,
                            SpecialContinuousHull7 = d.specialContinuousHull7,
                            SpecialContinuousHull8 = d.specialContinuousHull8,
                            SpecialContinuousHull9 = d.specialContinuousHull9,
                            SpecialContinuousHull10 = d.specialContinuousHull10,
                            SpecialContinuousHull11 = d.specialContinuousHull11,
                            SpecialContinuousHull12 = d.specialContinuousHull12,
                            SpecialContinuousHull13 = d.specialContinuousHull13,
                            SpecialContinuousHull14 = d.specialContinuousHull14,
                            SpecialContinuousHull15 = d.specialContinuousHull15,
                            SpecialHullNo = d.specialHullNo
                        }
                    };
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }

            return new List<SurveyStatus>();
        }

        /// <summary>
        /// Replaces [dbo].[GetVesselMaindata].
        /// SELECT * FROM dbo.ReportVesselMainData rt WHERE rt.projectId = @projectId
        /// </summary>
        private async Task<List<ReportVesselMainData>> Sp_GetVesselMaindata(int projectId)
        {
            var idStr = projectId.ToString();
            try
            {
                var resp = await _mainDataContainer.ReadItemAsync<ReportVesselMainDataDocument>(idStr, new PartitionKey(idStr));
                if (resp.Resource != null)
                {
                    var d = resp.Resource;
                    return new List<ReportVesselMainData> { MapToReportVesselMainData(d) };
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }

            return new List<ReportVesselMainData>();
        }

        /// <summary>
        /// Replaces [dbo].[GetStatutoryCertificates].
        /// SELECT * FROM [dbo].[ReportStatutoryCertificates] WHERE [projectId] = @projectId
        /// </summary>
        private async Task<ReportStatutoryCertificatesDocument?> Sp_GetStatutoryCertificates(int projectId)
        {
            var idStr = projectId.ToString();
            try
            {
                var resp = await _certContainer.ReadItemAsync<ReportStatutoryCertificatesDocument>(idStr, new PartitionKey(idStr));
                return resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Replaces [dbo].[Insert_ReportStatutoryCertificates].
        /// Key is projectId; duplicate throws 409 (PK violation).
        /// </summary>
        private async Task Sp_Insert_ReportStatutoryCertificates(ReportStatutoryCertificatesDocument doc)
        {
            doc.id = doc.projectId.ToString();
            await _certContainer.CreateItemAsync(doc, new PartitionKey(doc.id));
        }

        /// <summary>
        /// Replaces [dbo].[Update_ReportStatutoryCertificates].
        /// UPDATE WHERE projectId = @projectId (0 rows -> no error).
        /// </summary>
        private async Task Sp_Update_ReportStatutoryCertificates(ReportStatutoryCertificatesDocument doc)
        {
            var idStr = doc.projectId.ToString();
            var pk = new PartitionKey(idStr);
            try
            {
                var existing = await _certContainer.ReadItemAsync<ReportStatutoryCertificatesDocument>(idStr, pk);
                if (existing.Resource != null)
                {
                    doc.isExpiryDate = existing.Resource.isExpiryDate;
                    await _certContainer.ReplaceItemAsync(doc, idStr, pk);
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // 0 rows updated in SQL -> no error
            }
        }

        /// <summary>
        /// Replaces [dbo].[Insert_SurveyDetails].
        /// Key is projectId; duplicate throws 409.
        /// </summary>
        private async Task Sp_Insert_SurveyDetails(ReportSurveyDetailsDocument doc)
        {
            doc.id = doc.projectId.ToString();
            await _surveyContainer.CreateItemAsync(doc, new PartitionKey(doc.id));
        }

        /// <summary>
        /// Replaces [dbo].[Update_SurveyDetails].
        /// </summary>
        private async Task Sp_Update_SurveyDetails(ReportSurveyDetailsDocument doc)
        {
            var idStr = doc.projectId.ToString();
            var pk = new PartitionKey(idStr);
            try
            {
                var existing = await _surveyContainer.ReadItemAsync<ReportSurveyDetailsDocument>(idStr, pk);
                if (existing.Resource != null)
                {
                    var cur = existing.Resource;
                    // Trap 15: CAST(@imoNum AS int) != 0 -> if non-zero int update, else keep old
                    // In UpdateSurveyAudit, doc.imoNum is null, so cur.imoNum is kept
                    doc.imoNum = doc.imoNum ?? cur.imoNum;
                    await _surveyContainer.ReplaceItemAsync(doc, idStr, pk);
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // 0 rows in SQL -> no error
            }
        }

        /// <summary>
        /// Replaces [CAP].[Read_VesselDetails_ByIMONumber].
        /// SELECT * FROM core.Vessel WHERE IMONum = @IMONumber
        /// </summary>
        private async Task<Core.Models.Vessel?> Sp_Read_VesselDetails_ByIMONumber(string imoNumber)
        {
            if (imoNumber == null)
                return null;

            var cut = imoNumber.Length > 7 ? imoNumber.Substring(0, 7) : imoNumber;
            var p = cut.TrimEnd(' ');
            var query = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.IMONum), @p, true)").WithParameter("@p", p);

            using var feed = _vesselContainer.GetItemQueryIterator<VesselDocument>(query);
            while (feed.HasMoreResults)
            {
                var resp = await feed.ReadNextAsync();
                foreach (var doc in resp)
                {
                    var docImo = doc.IMONum?.TrimEnd(' ');
                    if (string.Equals(docImo, p, StringComparison.OrdinalIgnoreCase))
                    {
                        return MapToCoreVessel(doc);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Replaces [dbo].[Insert_VesselDetails].
        /// Key is projectId; duplicate throws 409 (PK violation).
        /// </summary>
        private async Task Sp_Insert_VesselDetails(ReportVesselMainDataDocument doc)
        {
            doc.id = doc.projectId.ToString();
            await _mainDataContainer.CreateItemAsync(doc, new PartitionKey(doc.id));
        }

        /// <summary>
        /// Replaces [dbo].[Update_VesselDetails].
        /// Trap 15: imoNumber = IIF(@imoNumber != 0, @imoNumber, imoNumber);
        /// yearBuilt = IIF(CAST(@yearBuilt AS INT) != 0, ...): NULL -> keep old, non-numeric text -> fails (conversion error).
        /// </summary>
        private async Task Sp_Update_VesselDetails(ReportVesselMainDataDocument doc)
        {
            var idStr = doc.projectId.ToString();
            var pk = new PartitionKey(idStr);

            // Trap 15: validation of yearBuilt if provided
            if (!string.IsNullOrWhiteSpace(doc.yearBuilt))
            {
                if (!int.TryParse(doc.yearBuilt, out var ybInt))
                {
                    throw new InvalidOperationException("Conversion failed when converting the varchar value to data type int.");
                }
            }

            try
            {
                var existing = await _mainDataContainer.ReadItemAsync<ReportVesselMainDataDocument>(idStr, pk);
                if (existing.Resource != null)
                {
                    var cur = existing.Resource;
                    // imoNumber == 0 or null -> keep old
                    if (doc.imoNumber == null || doc.imoNumber == 0)
                    {
                        doc.imoNumber = cur.imoNumber;
                    }

                    // yearBuilt: null or empty or 0 -> keep old
                    if (string.IsNullOrWhiteSpace(doc.yearBuilt) || (int.TryParse(doc.yearBuilt, out var yb) && yb == 0))
                    {
                        doc.yearBuilt = cur.yearBuilt;
                    }

                    await _mainDataContainer.ReplaceItemAsync(doc, idStr, pk);
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // 0 rows updated in SQL -> no error
            }
        }

        /// <summary>
        /// Replaces [dbo].[Create_VesselMainData].
        /// Trap 17: SP does not exist in database (decided: not needed). Throws NotSupportedException.
        /// </summary>
        private Task Sp_Create_VesselMainData()
        {
            throw new NotSupportedException("Stored procedure [dbo].[Create_VesselMainData] not present in source database");
        }

        private static ReportVesselMainData MapToReportVesselMainData(ReportVesselMainDataDocument d)
        {
            return new ReportVesselMainData
            {
                projectId = d.projectId,
                reportNo = d.reportNo!,
                portDate = d.portDate!,
                actualReportStartDate = d.actualReportStartDate!,
                vesselName = d.vesselName!,
                vesselID = d.vesselID ?? 0,
                imoNumber = d.imoNumber ?? 0,
                vesselType = d.vesselType!,
                yearBuilt = d.yearBuilt!,
                builtBy = d.builtBy!,
                hullNo = d.hullNo!,
                flagName = d.flagName!,
                homeport = d.homeport!,
                officalNumber = d.officalNumber!,
                callSign = d.callSign!,
                previousName = d.previousName!,
                ownerName = d.ownerName!,
                manager = d.manager!,
                length = d.length!,
                breadth = d.breadth!,
                depth = d.depth!,
                draft = d.draft!,
                deadweight = d.deadweight!,
                grossTons = d.grossTons!,
                netTons = d.netTons!,
                propulsionType = d.propulsionType!,
                kw = d.kw!,
                shaftRPM = d.shaftRPM!,
                propulsionManufacturer = d.propulsionManufacturer!,
                gearManufacturer = d.gearManufacturer!,
                classed = d.classed!,
                classNo = d.classNo!,
                classNotation = d.classNotation!,
                machinery = d.machinery!,
                other = d.other!,
                environment = d.environment!,
                reportPort = d.reportPort!,
                firstVisitDate = d.firstVisitDate!,
                lastVisitDate = d.lastVisitDate!,
                selectedPrefix = d.SelectedPrefix ?? 0
            };
        }

        private static Core.Models.Vessel MapToCoreVessel(VesselDocument d)
        {
            int imoNum = 0;
            if (d.IMONum != null)
            {
                if (int.TryParse(d.IMONum, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedImo))
                {
                    imoNum = parsedImo;
                }
                else
                {
                    throw new System.Data.DataException($"Error parsing column 8 (IMONum={d.IMONum} - String) -> Int32");
                }
            }

            return new Core.Models.Vessel
            {
                Vessel_ID = d.Vessel_ID,
                Source = d.Source!,
                Beam = d.Beam ?? 0,
                Breadth = d.Breadth ?? 0,
                ContractDate = d.ContractDate ?? default,
                Depth = d.Depth ?? 0,
                Draft = d.Draft ?? 0,
                HullNum = d.HullNum!,
                IMONum = imoNum,
                Length = d.Length ?? 0,
                MarketSegment = d.MarketSegment!,
                VesselName = d.VesselName!,
                MarketSubSegment = d.MarketSubSegment!,
                CallSign = d.CallSign!,
                NetTonnage = d.NetTonnage,
                ClassNotation = d.ClassNotation!,
                ShipTypeGrouping = d.ShipTypeGrouping!,
                Statcode5 = d.Statcode5!,
                Liquid = d.Liquid ?? 0,
                RegisteredOwner = d.RegisteredOwner!,
                ProjectName = d.ProjectName!,
                Manager = d.Manager!,
                Operator = d.Operator!,
                Technical_Manager = d.Technical_Manager!,
                Registered_Owner_ID = d.Registered_Owner_ID ?? 0,
                Manager_ID = d.Manager_ID ?? 0,
                Operator_ID = d.Operator_ID ?? 0,
                Technical_Manager_ID = d.Technical_Manager_ID ?? 0,
                BuildDate = d.BuildDate,
                Builder = d.Builder!,
                BuilderName = d.BuilderName!,
                BuilderNum = d.BuilderNum ?? 0,
                ClassNum = d.ClassNum!,
                ClassedDate = d.ClassedDate ?? default,
                Country = d.Country!,
                DeliveryDate = d.DeliveryDate ?? default,
                FlagName = d.FlagName!,
                KeelLayingDate = d.KeelLayingDate ?? default,
                LaunchingDate = d.LaunchingDate ?? default,
                OwnerCountry = d.OwnerCountry!,
                OwnerID = d.OwnerID ?? 0,
                OwnerName = d.OwnerName!,
                VesselDescription = d.VesselDescription!,
                CommercialCategory = d.CommercialCategory!,
                ClassedBy = d.ClassedBy!,
                ClassedBy2 = d.ClassedBy2!,
                SubStatus = d.SubStatus!,
                DWT = d.DWT,
                GT = d.GT,
                VesselType = d.VesselType!
            };
        }

        #endregion
    }
}
