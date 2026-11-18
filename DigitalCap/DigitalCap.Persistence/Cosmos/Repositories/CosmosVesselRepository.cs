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
                issued.LLLIDate = data.llliIssueDate;
                expiry.LLLIDate = data.llliExpiryDate;

                issued.SafetyConDate = data.safetyConIssueDate;
                expiry.SafetyConDate = data.safetyConExpiryDate;

                issued.SafetyRadioDate = data.safetyRadioIssueDate;
                expiry.SafetyRadioDate = data.safetyRadioExpiryDate;

                issued.SafetyEquipmentDate = data.safetyEquipmentIssueDate;
                expiry.SafetyEquipmentDate = data.safetyEquipmentExpiryDate;

                issued.IEEDate = data.ieeIssueDate;
                expiry.IEEDate = data.ieeExpiryDate;

                issued.IOPPDate = data.ioppIssueDate;
                expiry.IOPPDate = data.ioppExpiryDate;

                issued.IAPDate = data.iapIssueDate;
                expiry.IAPDate = data.iapExpiryDate;

                issued.IAFSDate = data.iafsIsuueDate;
                expiry.IAFSDate = data.iafsExpiryDate;

                issued.ISPSDate = data.ispsIssueDate;
                expiry.ISPSDate = data.ispsExpiryDate;

                issued.CargoGearReTestDate = data.cargoGearReTestIssueDate;
                expiry.CargoGearReTestDate = data.cargoGearReTestExpiryDate;

                issued.CargoGearAnnualCertificate = data.cargoGearAnnualIssueCertificate;
                expiry.CargoGearAnnualCertificate = data.cargoGearAnnualExpiryCertificate;

                issued.IBWMDate = data.ibwmIssueDate;
                expiry.IBWMDate = data.ibwmExpiryDate;

                issued.ISPPSewageDate = data.isppSewageIssueDate;
                expiry.ISPPSewageDate = data.isppSewageExpiryDate;

                issued.ISMDate = data.ismIssueDate;
                expiry.ISMDate = data.ismExpiryDate;

                issued.MaritimeLabourDate = data.maritimeLabourIssueDate;
                expiry.MaritimeLabourDate = data.maritimeLabourExpiryDate;

                issued.GasCarrierFitness = data.fitnessGasCertificateIssueDate;
                expiry.GasCarrierFitness = data.fitnessGasCertificateExpiryDate;
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

        public async Task<bool> UpdateSurveyAudit(Core.Models.Survey.SurveyStatus surveyStatus, int projectId)
        {
            var data = (await GetSurveyStatus(projectId))?.ToList();
            if (data == null || data.Count == 0)
            {
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
                        specialHullNo = (surveyStatus.SpecialHullNo != null) ?
                            (surveyStatus.SpecialHullNo == 0 ? null : surveyStatus.SpecialHullNo)
                            : (surveyStatusData?.SpecialHullNo == 0 ? null : surveyStatusData?.SpecialHullNo)
                    };

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
                    var doc = new ReportVesselMainDataDocument
                    {
                        id = projectId.ToString(),
                        projectId = projectId,
                        SelectedPrefix = report.Vessel?.SelectedPrefix != null && int.TryParse(report.Vessel.SelectedPrefix, out var sp) ? sp : 0,
                        reportNo = report.ReportNo,
                        reportPort = report.ReportPort,
                        classNo = report.Vessel?.ClassNo,
                        builtBy = report.Vessel?.BuiltBy,
                        vesselName = report.Vessel?.VesselName,
                        length = report.Vessel != null ? report.Vessel.Length.ToString() : null,
                        yearBuilt = (report.Vessel != null && report.Vessel.YearBuilt != 0) ? report.Vessel.YearBuilt.ToString() : null,
                        hullNo = report.Vessel?.HullNo,
                        vesselID = report.Vessel?.ID,
                        imoNumber = report.Vessel?.ImoNumber,
                        vesselType = report.Vessel?.VesselType,
                        flagName = report.Vessel?.FlagName,
                        homeport = report.Vessel?.Homeport,
                        portDate = (report.ReportDate != null && Convert.ToDateTime(report.ReportDate) != DateTime.MinValue) ? report.ReportDate : null,
                        officalNumber = report.Vessel?.OfficalNumber,
                        callSign = report.Vessel?.CallSign,
                        ownerName = report.Vessel?.OwnerName,
                        breadth = report.Vessel != null ? report.Vessel.Breadth.ToString() : null,
                        depth = report.Vessel != null ? report.Vessel.DEPTH.ToString() : null,
                        deadweight = report.Vessel?.DEADWEIGHT,
                        grossTons = report.Vessel != null ? report.Vessel.GrossTons.ToString() : null,
                        netTons = report.Vessel != null ? report.Vessel.NetTons.ToString() : null,
                        actualReportStartDate = (report.Vessel != null && report.Vessel.ActualReportStartDate != DateTime.MinValue) ? report.Vessel.ActualReportStartDate.ToString("yyyy-MM-dd") : null,
                        previousName = report.Vessel?.PreviousName,
                        manager = report.Vessel?.Manager,
                        draft = report.Vessel != null ? report.Vessel.Draft.ToString() : null,
                        propulsionType = report.Vessel?.PropulsionType,
                        kw = report.Vessel?.KW,
                        shaftRPM = report.Vessel != null ? report.Vessel.ShaftRPM.ToString() : null,
                        propulsionManufacturer = report.Vessel?.PropulsionManufacturer,
                        gearManufacturer = report.Vessel?.GearManufacturer,
                        classed = report.Vessel?.Classed,
                        classNotation = report.Vessel?.ClassNotation,
                        machinery = report.Vessel?.Machinery,
                        other = report.Vessel?.Other,
                        environment = report.Vessel?.Environment,
                        firstVisitDate = report.Vessel?.FirstVisitDate,
                        lastVisitDate = report.Vessel?.LastVisitDate
                    };

                    await Sp_Insert_VesselDetails(doc);
                }
                else
                {
                    string? selPrefixVal = report.Vessel?.SelectedPrefix != null
                        ? (report.Vessel.SelectedPrefix == "" ? null : report.Vessel.SelectedPrefix)
                        : (data.selectedPrefix.ToString() == "" ? null : data.selectedPrefix.ToString());
                    int? selPrefix = int.TryParse(selPrefixVal, out var parsedSp) ? parsedSp : 0;

                    var doc = new ReportVesselMainDataDocument
                    {
                        id = projectId.ToString(),
                        projectId = projectId,
                        SelectedPrefix = selPrefix,
                        reportNo = report.ReportNo != null ? (report.ReportNo == "" ? null : report.ReportNo) : (data.reportNo == "" ? null : data.reportNo),
                        reportPort = report.ReportPort != null ? (report.ReportPort == "" ? null : report.ReportPort) : (data.reportPort == "" ? null : data.reportPort),
                        classNo = report.Vessel?.ClassNo != null ? (report.Vessel.ClassNo == "" ? null : report.Vessel.ClassNo) : (data.classNo == "" ? null : data.classNo),
                        builtBy = report.Vessel?.BuiltBy != null ? (report.Vessel.BuiltBy == "" ? null : report.Vessel.BuiltBy) : (data.builtBy == "" ? null : data.builtBy),
                        vesselName = report.Vessel?.VesselName != null ? (report.Vessel.VesselName == "" ? null : report.Vessel.VesselName) : (data.vesselName == "" ? null : data.vesselName),
                        length = (report.Vessel != null && (report.Vessel.Length != 0 || Convert.ToDecimal(report.Vessel.Length) == 0.1m))
                            ? (Convert.ToDecimal(report.Vessel.Length) == 0.1m ? null : report.Vessel.Length.ToString())
                            : (data.length == "" ? null : data.length),
                        yearBuilt = (report.Vessel != null && report.Vessel.YearBuilt != 0)
                            ? report.Vessel.YearBuilt.ToString()
                            : data.yearBuilt,
                        hullNo = report.Vessel?.HullNo != null ? (report.Vessel.HullNo == "" ? null : report.Vessel.HullNo) : data.hullNo,
                        vesselID = report.Vessel?.ID,
                        imoNumber = report.Vessel?.ImoNumber,
                        vesselType = report.Vessel?.VesselType != null ? (report.Vessel.VesselType == "" ? null : report.Vessel.VesselType) : data.vesselType,
                        flagName = report.Vessel?.FlagName != null ? (report.Vessel.FlagName == "" ? null : report.Vessel.FlagName) : data.flagName,
                        homeport = report.Vessel?.Homeport != null ? (report.Vessel.Homeport == "" ? null : report.Vessel.Homeport) : data.homeport,
                        portDate = (report.ReportDate != null && Convert.ToDateTime(report.ReportDate) != DateTime.MinValue)
                            ? report.ReportDate
                            : (data.portDate == "" ? null : data.portDate),
                        officalNumber = report.Vessel?.OfficalNumber != null ? (report.Vessel.OfficalNumber == "" ? null : report.Vessel.OfficalNumber) : (data.officalNumber == "" ? null : data.officalNumber),
                        callSign = report.Vessel?.CallSign != null ? (report.Vessel.CallSign == "" ? null : report.Vessel.CallSign) : (data.callSign == "" ? null : data.callSign),
                        previousName = report.Vessel?.PreviousName != null ? (report.Vessel.PreviousName == "" ? null : report.Vessel.PreviousName) : (data.previousName == "" ? null : data.previousName),
                        ownerName = report.Vessel?.OwnerName != null ? (report.Vessel.OwnerName == "" ? null : report.Vessel.OwnerName) : (data.ownerName == "" ? null : data.ownerName),
                        manager = report.Vessel?.Manager != null ? (report.Vessel.Manager == "" ? null : report.Vessel.Manager) : (data.manager == "" ? null : data.manager),
                        breadth = (report.Vessel != null && (report.Vessel.Breadth != 0 || Convert.ToDecimal(report.Vessel.Breadth) == 0.1m))
                            ? (Convert.ToDecimal(report.Vessel.Breadth) == 0.1m ? null : report.Vessel.Breadth.ToString())
                            : (data.breadth == "" ? null : data.breadth),
                        depth = (report.Vessel != null && (report.Vessel.DEPTH != 0 || Convert.ToDecimal(report.Vessel.DEPTH) == 0.1m))
                            ? (Convert.ToDecimal(report.Vessel.DEPTH) == 0.1m ? null : report.Vessel.DEPTH.ToString())
                            : (data.depth == "" ? null : data.depth),
                        draft = (report.Vessel != null && (report.Vessel.Draft != 0 || Convert.ToDecimal(report.Vessel.Draft) == 0.1m))
                            ? (Convert.ToDecimal(report.Vessel.Draft) == 0.1m ? null : report.Vessel.Draft.ToString())
                            : (data.draft == "" ? null : data.draft),
                        deadweight = report.Vessel?.DEADWEIGHT != null
                            ? (report.Vessel.DEADWEIGHT == "" ? null : report.Vessel.DEADWEIGHT)
                            : (data.deadweight == "" ? null : data.deadweight),
                        grossTons = (report.Vessel != null && (report.Vessel.GrossTons != 0 || Convert.ToDecimal(report.Vessel.GrossTons) == 0.1m))
                            ? (Convert.ToDecimal(report.Vessel.GrossTons) == 0.1m ? null : report.Vessel.GrossTons.ToString())
                            : (data.grossTons == "" ? null : data.grossTons),
                        netTons = (report.Vessel != null && (report.Vessel.NetTons != 0 || Convert.ToDecimal(report.Vessel.NetTons) == 0.1m))
                            ? (Convert.ToDecimal(report.Vessel.NetTons) == 0.1m ? null : report.Vessel.NetTons.ToString())
                            : (data.netTons == "" ? null : data.netTons),
                        propulsionType = report.Vessel?.PropulsionType != null ? (report.Vessel.PropulsionType == "" ? null : report.Vessel.PropulsionType) : (data.propulsionType == "" ? null : data.propulsionType),
                        kw = report.Vessel?.KW != null ? (report.Vessel.KW == "" ? null : report.Vessel.KW) : (data.kw == "" ? null : data.kw),
                        shaftRPM = (report.Vessel != null && (report.Vessel.ShaftRPM != 0 || Convert.ToDecimal(report.Vessel.ShaftRPM) == 0.1m))
                            ? (Convert.ToDecimal(report.Vessel.ShaftRPM) == 0.1m ? null : report.Vessel.ShaftRPM.ToString())
                            : (data.shaftRPM == "" ? null : data.shaftRPM),
                        propulsionManufacturer = report.Vessel?.PropulsionManufacturer != null ? (report.Vessel.PropulsionManufacturer == "" ? null : report.Vessel.PropulsionManufacturer) : (data.propulsionManufacturer == "" ? null : data.propulsionManufacturer),
                        gearManufacturer = report.Vessel?.GearManufacturer != null ? (report.Vessel.GearManufacturer == "" ? null : report.Vessel.GearManufacturer) : (data.gearManufacturer == "" ? null : data.gearManufacturer),
                        classed = report.Vessel?.Classed != null ? (report.Vessel.Classed == "" ? null : report.Vessel.Classed) : (data.classed == "" ? null : data.classed),
                        classNotation = report.Vessel?.ClassNotation != null ? (report.Vessel.ClassNotation == "" ? null : report.Vessel.ClassNotation) : (data.classNotation == "" ? null : data.classNotation),
                        machinery = report.Vessel?.Machinery != null ? (report.Vessel.Machinery == "" ? null : report.Vessel.Machinery) : (data.machinery == "" ? null : data.machinery),
                        other = report.Vessel?.Other != null ? (report.Vessel.Other == "" ? null : report.Vessel.Other) : (data.other == "" ? null : data.other),
                        environment = report.Vessel?.Environment != null ? (report.Vessel.Environment == "" ? null : report.Vessel.Environment) : (data.environment == "" ? null : data.environment),
                        actualReportStartDate = (report.Vessel != null && report.Vessel.ActualReportStartDate != DateTime.MinValue)
                            ? report.Vessel.ActualReportStartDate.ToString("yyyy-MM-dd")
                            : (data.actualReportStartDate == "" ? null : data.actualReportStartDate),
                        firstVisitDate = report.Vessel?.FirstVisitDate != null ? (report.Vessel.FirstVisitDate == "" ? null : report.Vessel.FirstVisitDate) : (data.firstVisitDate == "" ? null : data.firstVisitDate),
                        lastVisitDate = report.Vessel?.LastVisitDate != null ? (report.Vessel.LastVisitDate == "" ? null : report.Vessel.LastVisitDate) : (data.lastVisitDate == "" ? null : data.lastVisitDate)
                    };

                    await Sp_Update_VesselDetails(doc);
                }

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

                await Sp_Create_VesselMainData();
                Commit();
                return classNumber;
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
                await Sp_Create_VesselMainData();
                Commit();
            }
            catch
            {
                throw;
            }
        }

        #region Stored Procedure Helpers

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
        /// Key is projectId; duplicate throws 409. specialHullNo defaults to 0 if not given.
        /// </summary>
        private async Task Sp_Insert_SurveyDetails(ReportSurveyDetailsDocument doc)
        {
            doc.id = doc.projectId.ToString();
            if (!doc.specialHullNo.HasValue)
            {
                doc.specialHullNo = 0;
            }
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
            if (string.IsNullOrWhiteSpace(imoNumber))
                return null;

            QueryDefinition query;
            if (int.TryParse(imoNumber.Trim(), out var parsedImo))
            {
                query = new QueryDefinition("SELECT * FROM c WHERE c.IMONum = @imo").WithParameter("@imo", parsedImo);
            }
            else
            {
                query = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(c.IMONum, @imo, true)").WithParameter("@imo", imoNumber.Trim());
            }

            using var feed = _vesselContainer.GetItemQueryIterator<VesselDocument>(query);
            if (feed.HasMoreResults)
            {
                var resp = await feed.ReadNextAsync();
                var doc = resp.FirstOrDefault();
                if (doc != null)
                {
                    return MapToCoreVessel(doc);
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
                reportNo = d.reportNo ?? string.Empty,
                portDate = d.portDate ?? string.Empty,
                actualReportStartDate = d.actualReportStartDate ?? string.Empty,
                vesselName = d.vesselName ?? string.Empty,
                vesselID = d.vesselID ?? 0,
                imoNumber = d.imoNumber ?? 0,
                vesselType = d.vesselType ?? string.Empty,
                yearBuilt = d.yearBuilt ?? string.Empty,
                builtBy = d.builtBy ?? string.Empty,
                hullNo = d.hullNo ?? string.Empty,
                flagName = d.flagName ?? string.Empty,
                homeport = d.homeport ?? string.Empty,
                officalNumber = d.officalNumber ?? string.Empty,
                callSign = d.callSign ?? string.Empty,
                previousName = d.previousName ?? string.Empty,
                ownerName = d.ownerName ?? string.Empty,
                manager = d.manager ?? string.Empty,
                length = d.length ?? string.Empty,
                breadth = d.breadth ?? string.Empty,
                depth = d.depth ?? string.Empty,
                draft = d.draft ?? string.Empty,
                deadweight = d.deadweight ?? string.Empty,
                grossTons = d.grossTons ?? string.Empty,
                netTons = d.netTons ?? string.Empty,
                propulsionType = d.propulsionType ?? string.Empty,
                kw = d.kw ?? string.Empty,
                shaftRPM = d.shaftRPM ?? string.Empty,
                propulsionManufacturer = d.propulsionManufacturer ?? string.Empty,
                gearManufacturer = d.gearManufacturer ?? string.Empty,
                classed = d.classed ?? string.Empty,
                classNo = d.classNo ?? string.Empty,
                classNotation = d.classNotation ?? string.Empty,
                machinery = d.machinery ?? string.Empty,
                other = d.other ?? string.Empty,
                environment = d.environment ?? string.Empty,
                reportPort = d.reportPort ?? string.Empty,
                firstVisitDate = d.firstVisitDate ?? string.Empty,
                lastVisitDate = d.lastVisitDate ?? string.Empty,
                selectedPrefix = d.SelectedPrefix ?? 0
            };
        }

        private static Core.Models.Vessel MapToCoreVessel(VesselDocument d)
        {
            return new Core.Models.Vessel
            {
                Vessel_ID = d.Vessel_ID,
                Source = d.Source ?? string.Empty,
                Beam = d.Beam ?? 0,
                Breadth = d.Breadth ?? 0,
                ContractDate = d.ContractDate ?? default,
                Depth = d.Depth ?? 0,
                Draft = d.Draft ?? 0,
                HullNum = d.HullNum ?? string.Empty,
                IMONum = d.IMONum ?? 0,
                Length = d.Length ?? 0,
                MarketSegment = d.MarketSegment ?? string.Empty,
                VesselName = d.VesselName ?? string.Empty,
                MarketSubSegment = d.MarketSubSegment ?? string.Empty,
                CallSign = d.CallSign ?? string.Empty,
                NetTonnage = d.NetTonnage,
                ClassNotation = d.ClassNotation ?? string.Empty,
                ShipTypeGrouping = d.ShipTypeGrouping ?? string.Empty,
                Statcode5 = d.Statcode5 ?? string.Empty,
                Liquid = d.Liquid ?? 0,
                RegisteredOwner = d.RegisteredOwner ?? string.Empty,
                ProjectName = d.ProjectName ?? string.Empty,
                Manager = d.Manager ?? string.Empty,
                Operator = d.Operator ?? string.Empty,
                Technical_Manager = d.Technical_Manager ?? string.Empty,
                Registered_Owner_ID = d.Registered_Owner_ID ?? 0,
                Manager_ID = d.Manager_ID ?? 0,
                Operator_ID = d.Operator_ID ?? 0,
                Technical_Manager_ID = d.Technical_Manager_ID ?? 0,
                BuildDate = d.BuildDate,
                Builder = d.Builder ?? string.Empty,
                BuilderName = d.BuilderName ?? string.Empty,
                BuilderNum = d.BuilderNum ?? 0,
                ClassNum = d.ClassNum ?? string.Empty,
                ClassedDate = d.ClassedDate ?? default,
                Country = d.Country ?? string.Empty,
                DeliveryDate = d.DeliveryDate ?? default,
                FlagName = d.FlagName ?? string.Empty,
                KeelLayingDate = d.KeelLayingDate ?? default,
                LaunchingDate = d.LaunchingDate ?? default,
                OwnerCountry = d.OwnerCountry ?? string.Empty,
                OwnerID = d.OwnerID ?? 0,
                OwnerName = d.OwnerName ?? string.Empty,
                VesselDescription = d.VesselDescription ?? string.Empty,
                CommercialCategory = d.CommercialCategory ?? string.Empty,
                ClassedBy = d.ClassedBy ?? string.Empty,
                ClassedBy2 = d.ClassedBy2 ?? string.Empty,
                SubStatus = d.SubStatus ?? string.Empty,
                DWT = d.DWT,
                GT = d.GT,
                VesselType = d.VesselType ?? string.Empty
            };
        }

        #endregion
    }
}
