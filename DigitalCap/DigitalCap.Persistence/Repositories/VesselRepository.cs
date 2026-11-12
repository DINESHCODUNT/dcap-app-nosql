using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.VesselModel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Transactions;

namespace DigitalCap.Persistence.Repositories
{
    public class VesselRepository : IVesselRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;
        private readonly IFreedomAPIRepository _freedomApiRepository;
        public VesselRepository(IUnitOfWork unitOfWork, IFreedomAPIRepository freedomApiRepository)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _freedomApiRepository = freedomApiRepository;
        }
        public void Commit()
        {
            _unitOfWork?.Commit();
        }
        public async Task CreateVesselMainDataAsync(string classNumber, Project project, Core.Models.VesselModel.Vessel vessel)
        {
            try
            {
                var parameters = new DynamicParameters();
                if (vessel != null)
                {
                    parameters.Add("@projectId", project.Id);
                    parameters.Add("@classNo", project.AbsClassID);
                    parameters.Add("@builtBy", project.Builder);
                    parameters.Add("@vesselName", project.VesselName);
                    parameters.Add("@length", project.LengthOverall);
                    parameters.Add("@yearBuilt", project.YearBuilt);
                    parameters.Add("@hullNo", project.HullNumber);
                    parameters.Add("@imoNumber", project.IMO);
                    // Handle ClassSociety - check for null/empty array to match MVC code
                    parameters.Add("@classed", project.ClassSociety != null && project.ClassSociety.Length > 0
                        ? project.ClassSociety[0]
                        : null);

                    // Date formatting to match MVC code (dd-MM-yyy - note: 3 y's as per MVC)
                    parameters.Add("@firstVisitDate",
                        project.SurveyFirstVisit != null &&
                        project.SurveyFirstVisit != DateTime.MinValue
                            ? project.SurveyFirstVisit.Value.ToString("dd-MM-yyy")
                            : DateTime.Now.ToString("dd-MM-yyy"));

                    parameters.Add("@lastVisitDate",
                        project.SurveyLastVisit != null &&
                        project.SurveyLastVisit != DateTime.MinValue
                            ? project.SurveyLastVisit.Value.ToString("dd-MM-yyy")
                            : DateTime.Now.ToString("dd-MM-yyy"));

                    // Optional vessel data
                    if (vessel != null)
                    {
                        parameters.Add("@vesselID", vessel.ImoNumber);
                        parameters.Add("@vesselType", vessel.VesselType);
                        parameters.Add("@flagName", vessel.FlagName);
                        parameters.Add("@homeport", vessel.PortRegistry);
                        parameters.Add("@officalNumber", vessel.OfficialNumber);
                        parameters.Add("@callSign", vessel.CallSign);
                        // Handle RegisteredOwner null case
                        parameters.Add("@ownerName", vessel.RegisteredOwner?.Name);
                        parameters.Add("@breadth", vessel?.BreadthMolded?.Value);
                        parameters.Add("@depth", vessel?.DepthMolded?.Value);
                        parameters.Add("@deadweight", vessel?.DesignDeadWeight?.Value);
                        parameters.Add("@netTons", vessel?.NetTonnage?.Value);
                        parameters.Add("@classNotation", vessel?.ClassNotation);
                    }
                }
                else
                {
                    parameters.Add("@projectId", project.Id);
                    parameters.Add("@classNo", project.AbsClassID);
                    parameters.Add("@builtBy", project.Builder);
                    parameters.Add("@vesselName", project.VesselName);
                    parameters.Add("@length", project.LengthOverall);
                    parameters.Add("@yearBuilt", project.YearBuilt);
                    parameters.Add("@hullNo", project.HullNumber);
                    parameters.Add("@imoNumber", project.IMO);
                    parameters.Add("@classed", project.ClassSociety != null && project.ClassSociety.Length > 0
                       ? project.ClassSociety[0]
                       : null);

                    // Date formatting to match MVC code (dd-MM-yyy - note: 3 y's as per MVC)
                    parameters.Add("@firstVisitDate",
                        project.SurveyFirstVisit != null &&
                        project.SurveyFirstVisit != DateTime.MinValue
                            ? project.SurveyFirstVisit.Value.ToString("dd-MM-yyy")
                            : DateTime.Now.ToString("dd-MM-yyy"));

                    parameters.Add("@lastVisitDate",
                        project.SurveyLastVisit != null &&
                        project.SurveyLastVisit != DateTime.MinValue
                            ? project.SurveyLastVisit.Value.ToString("dd-MM-yyy")
                            : DateTime.Now.ToString("dd-MM-yyy"));
                }
                await Connection.ExecuteAsync(
                    sql: "[dbo].[Insert_VesselDetails]",
                    param: parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction);
                this.Commit();
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
                var data = (await GetVesselMainData(projectId)).FirstOrDefault();

                var parameters = new DynamicParameters();

                if (data == null)
                {
                    parameters.Add("@projectId", projectId);
                    parameters.Add("@selectedPrefix", report.Vessel.SelectedPrefix);
                    parameters.Add("@reportNo", report.ReportNo);
                    parameters.Add("@reportPort", report.ReportPort);
                    parameters.Add("@classNo", report.Vessel.ClassNo);
                    parameters.Add("@builtBy", report.Vessel.BuiltBy);
                    parameters.Add("@vesselName", report.Vessel.VesselName);
                    parameters.Add("@length", report.Vessel.Length);
                    parameters.Add("@yearBuilt", report.Vessel.YearBuilt);
                    parameters.Add("@hullNo", report.Vessel.HullNo);
                    parameters.Add("@vesselID", report.Vessel.ID);
                    parameters.Add("@imoNumber", report.Vessel.ImoNumber);
                    parameters.Add("@vesselType", report.Vessel.VesselType);
                    parameters.Add("@flagName", report.Vessel.FlagName);
                    parameters.Add("@homePort", report.Vessel.Homeport);

                    parameters.Add("@portDate",
                        Convert.ToDateTime(report.ReportDate) != DateTime.MinValue
                            ? report.ReportDate
                            : null);

                    parameters.Add("@officalNumber", report.Vessel.OfficalNumber);
                    parameters.Add("@callSign", report.Vessel.CallSign);
                    parameters.Add("@ownerName", report.Vessel.OwnerName);
                    parameters.Add("@breadth", report.Vessel?.Breadth);
                    parameters.Add("@depth", report.Vessel?.DEPTH);
                    parameters.Add("@deadweight", report.Vessel?.DEADWEIGHT);
                    parameters.Add("@grossTons", report.Vessel?.GrossTons);
                    parameters.Add("@netTons", report.Vessel?.NetTons);

                    parameters.Add("@actualReportStartDate",
                        report.Vessel?.ActualReportStartDate != DateTime.MinValue
                            ? report.Vessel?.ActualReportStartDate
                            : null);

                    parameters.Add("@previousName", report.Vessel.PreviousName);
                    parameters.Add("@manager", report.Vessel.Manager);
                    parameters.Add("@draft", report.Vessel.Draft);
                    parameters.Add("@propulsionType", report.Vessel.PropulsionType);
                    parameters.Add("@kw", report.Vessel.KW);
                    parameters.Add("@shaftRPM", report.Vessel.ShaftRPM);
                    parameters.Add("@propulsionManufacturer", report.Vessel.PropulsionManufacturer);
                    parameters.Add("@gearManufacturer", report.Vessel.GearManufacturer);
                    parameters.Add("@classed", report.Vessel.Classed);
                    parameters.Add("@classNotation", report.Vessel.ClassNotation);
                    parameters.Add("@machinery", report.Vessel.Machinery);
                    parameters.Add("@other", report.Vessel.Other);
                    parameters.Add("@environment", report.Vessel.Environment);
                    parameters.Add("@firstVisitDate", report.Vessel.FirstVisitDate);
                    parameters.Add("@lastVisitDate", report.Vessel.LastVisitDate);

                    await Connection.ExecuteAsync(
                        sql: "[dbo].[Insert_VesselDetails]",
                        param: parameters,
                        commandType: CommandType.StoredProcedure,
                        transaction: Transaction);
                }
                else
                {
                    parameters.Add("@projectId", projectId);

                    parameters.Add("@selectedPrefix",
                        report.Vessel.SelectedPrefix != null
                            ? report.Vessel.SelectedPrefix == ""
                                ? null
                                : report.Vessel.SelectedPrefix
                            : data.selectedPrefix.ToString() == ""
                                ? null
                                : data.selectedPrefix);

                    parameters.Add("@reportNo",
                        report.ReportNo != null
                            ? report.ReportNo == ""
                                ? null
                                : report.ReportNo
                            : data.reportNo == ""
                                ? null
                                : data.reportNo);

                    parameters.Add("@reportPort",
                        report.ReportPort != null
                            ? report.ReportPort == ""
                                ? null
                                : report.ReportPort
                            : data.reportPort == ""
                                ? null
                                : data.reportPort);

                    parameters.Add("@classNo",
                        report.Vessel.ClassNo != null
                            ? report.Vessel.ClassNo == ""
                                ? null
                                : report.Vessel.ClassNo
                            : data.classNo == ""
                                ? null
                                : data.classNo);

                    parameters.Add("@builtBy",
                        report.Vessel.BuiltBy != null
                            ? report.Vessel.BuiltBy == ""
                                ? null
                                : report.Vessel.BuiltBy
                            : data.builtBy == ""
                                ? null
                                : data.builtBy);

                    parameters.Add("@vesselName",
                        report.Vessel.VesselName != null
                            ? report.Vessel.VesselName == ""
                                ? null
                                : report.Vessel.VesselName
                            : data.vesselName == ""
                                ? null
                                : data.vesselName);

                    parameters.Add("@length",
                        report.Vessel.Length != 0 ||
                        Convert.ToDecimal(report.Vessel.Length) == Convert.ToDecimal(0.1)
                            ? Convert.ToDecimal(report.Vessel.Length) == Convert.ToDecimal(0.1)
                                ? null
                                : report.Vessel.Length
                            : data.length == ""
                                ? null
                                : data.length);

                    parameters.Add("@yearBuilt",
                        report.Vessel.YearBuilt != 0
                            ? report.Vessel.YearBuilt
                            : data.yearBuilt == null
                                ? null
                                : data.yearBuilt);

                    parameters.Add("@hullNo",
                        report.Vessel.HullNo != null
                            ? report.Vessel.HullNo == ""
                                ? null
                                : report.Vessel.HullNo
                            : data.hullNo == null
                                ? null
                                : data.hullNo);

                    parameters.Add("@vesselID", report.Vessel.ID);
                    parameters.Add("@imoNumber", report.Vessel.ImoNumber);

                    parameters.Add("@vesselType",
                        report.Vessel.VesselType != null
                            ? report.Vessel.VesselType == ""
                                ? null
                                : report.Vessel.VesselType
                            : data.vesselType == null
                                ? null
                                : data.vesselType);

                    parameters.Add("@flagName",
                        report.Vessel.FlagName != null
                            ? report.Vessel.FlagName == ""
                                ? null
                                : report.Vessel.FlagName
                            : data.flagName == null
                                ? null
                                : data.flagName);

                    parameters.Add("@homePort",
                        report.Vessel.Homeport != null
                            ? report.Vessel.Homeport == ""
                                ? null
                                : report.Vessel.Homeport
                            : data.homeport == null
                                ? null
                                : data.homeport);

                    parameters.Add("@portDate",
                        report.ReportDate != null
                            ? Convert.ToDateTime(report.ReportDate) == DateTime.MinValue
                                ? null
                                : report.ReportDate
                            : Convert.ToDateTime(data.portDate) == DateTime.MinValue
                                ? null
                                : data.portDate);

                    parameters.Add("@officalNumber",
                        report.Vessel.OfficalNumber != null
                            ? report.Vessel.OfficalNumber == ""
                                ? null
                                : report.Vessel.OfficalNumber
                            : data.officalNumber == null
                                ? null
                                : data.officalNumber);

                    parameters.Add("@callSign",
                        report.Vessel.CallSign != null
                            ? report.Vessel.CallSign == ""
                                ? null
                                : report.Vessel.CallSign
                            : data.callSign == null
                                ? null
                                : data.callSign);

                    parameters.Add("@ownerName",
                        report.Vessel.OwnerName != null
                            ? report.Vessel.OwnerName == ""
                                ? null
                                : report.Vessel.OwnerName
                            : data.ownerName == null
                                ? null
                                : data.ownerName);

                    parameters.Add("@breadth",
                        report.Vessel.Breadth != 0 ||
                        Convert.ToDecimal(report.Vessel.Breadth) == Convert.ToDecimal(0.1)
                            ? Convert.ToDecimal(report.Vessel.Breadth) == Convert.ToDecimal(0.1)
                                ? null
                                : report.Vessel.Breadth
                            : data.breadth == ""
                                ? null
                                : data.breadth);

                    parameters.Add("@depth",
                        report.Vessel.DEPTH != 0 ||
                        Convert.ToDecimal(report.Vessel.DEPTH) == Convert.ToDecimal(0.1)
                            ? Convert.ToDecimal(report.Vessel.DEPTH) == Convert.ToDecimal(0.1)
                                ? null
                                : report.Vessel.DEPTH
                            : data.depth == ""
                                ? null
                                : data.depth);

                    parameters.Add("@deadweight",
                        report.Vessel.DEADWEIGHT != null
                            ? report.Vessel.DEADWEIGHT == ""
                                ? null
                                : report.Vessel.DEADWEIGHT
                            : data.deadweight == ""
                                ? null
                                : data.deadweight);

                    parameters.Add("@grossTons",
                        report.Vessel.GrossTons != 0 ||
                        Convert.ToDecimal(report.Vessel.GrossTons) == Convert.ToDecimal(0.1)
                            ? Convert.ToDecimal(report.Vessel.GrossTons) == Convert.ToDecimal(0.1)
                                ? null
                                : report.Vessel.GrossTons
                            : data.grossTons == ""
                                ? null
                                : data.grossTons);

                    parameters.Add("@netTons",
                        report.Vessel.NetTons != 0 ||
                        Convert.ToDecimal(report.Vessel.NetTons) == Convert.ToDecimal(0.1)
                            ? Convert.ToDecimal(report.Vessel.NetTons) == Convert.ToDecimal(0.1)
                                ? null
                                : report.Vessel.NetTons
                            : data.netTons == ""
                                ? null
                                : data.netTons);

                    parameters.Add("@actualReportStartDate",
                        report.Vessel.ActualReportStartDate != DateTime.MinValue
                            ? report.Vessel.ActualReportStartDate
                            : Convert.ToDateTime(data.actualReportStartDate) == DateTime.MinValue
                                ? null
                                : data.actualReportStartDate);

                    parameters.Add("@previousName",
                        report.Vessel.PreviousName != null
                            ? report.Vessel.PreviousName == ""
                                ? null
                                : report.Vessel.PreviousName
                            : data.previousName == null
                                ? null
                                : data.previousName);

                    parameters.Add("@manager",
                        report.Vessel.Manager != null
                            ? report.Vessel.Manager == ""
                                ? null
                                : report.Vessel.Manager
                            : data.manager == null
                                ? null
                                : data.manager);

                    parameters.Add("@draft",
                        report.Vessel.Draft != 0 ||
                        Convert.ToDecimal(report.Vessel.Draft) == Convert.ToDecimal(0.1)
                            ? Convert.ToDecimal(report.Vessel.Draft) == Convert.ToDecimal(0.1)
                                ? null
                                : report.Vessel.Draft
                            : data.draft == ""
                                ? null
                                : data.draft);

                    parameters.Add("@propulsionType",
                        report.Vessel.PropulsionType != null
                            ? report.Vessel.PropulsionType == ""
                                ? null
                                : report.Vessel.PropulsionType
                            : data.propulsionType == null
                                ? null
                                : data.propulsionType);

                    parameters.Add("@kw",
                        report.Vessel.KW != null
                            ? report.Vessel.KW == ""
                                ? null
                                : report.Vessel.KW
                            : data.kw == null
                                ? null
                                : data.kw);

                    parameters.Add("@shaftRPM",
                        report.Vessel.ShaftRPM != 0 ||
                        Convert.ToDecimal(report.Vessel.ShaftRPM) == Convert.ToDecimal(0.1)
                            ? Convert.ToDecimal(report.Vessel.ShaftRPM) == Convert.ToDecimal(0.1)
                                ? null
                                : report.Vessel.ShaftRPM
                            : data.shaftRPM == ""
                                ? null
                                : data.shaftRPM);

                    parameters.Add("@propulsionManufacturer",
                        report.Vessel.PropulsionManufacturer != null
                            ? report.Vessel.PropulsionManufacturer == ""
                                ? null
                                : report.Vessel.PropulsionManufacturer
                            : data.propulsionManufacturer == null
                                ? null
                                : data.propulsionManufacturer);

                    parameters.Add("@gearManufacturer",
                        report.Vessel.GearManufacturer != null
                            ? report.Vessel.GearManufacturer == ""
                                ? null
                                : report.Vessel.GearManufacturer
                            : data.gearManufacturer == null
                                ? null
                                : data.gearManufacturer);

                    parameters.Add("@classed",
                        report.Vessel.Classed != null
                            ? report.Vessel.Classed == ""
                                ? null
                                : report.Vessel.Classed
                            : data.classed == null
                                ? null
                                : data.classed);

                    parameters.Add("@classNotation",
                        report.Vessel.ClassNotation != null
                            ? report.Vessel.ClassNotation == ""
                                ? null
                                : report.Vessel.ClassNotation
                            : data.classNotation == null
                                ? null
                                : data.classNotation);

                    parameters.Add("@machinery",
                        report.Vessel.Machinery != null
                            ? report.Vessel.Machinery == ""
                                ? null
                                : report.Vessel.Machinery
                            : data.machinery == null
                                ? null
                                : data.machinery);

                    parameters.Add("@other",
                        report.Vessel.Other != null
                            ? report.Vessel.Other == ""
                                ? null
                                : report.Vessel.Other
                            : data.other == null
                                ? null
                                : data.other);

                    parameters.Add("@environment",
                        report.Vessel.Environment != null
                            ? report.Vessel.Environment == ""
                                ? null
                                : report.Vessel.Environment
                            : data.environment == null
                                ? null
                                : data.environment);

                    parameters.Add("@firstVisitDate",
                        report.Vessel.FirstVisitDate != null
                            ? report.Vessel.FirstVisitDate == ""
                                ? null
                                : report.Vessel.FirstVisitDate
                            : data.firstVisitDate == null
                                ? null
                                : data.firstVisitDate);

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

                    if (isLastValid && isFirstValid &&
                        lastVisitDate.Date < firstVisitDate.Date)
                    {
                        parameters.Add("@lastVisitDate", DBNull.Value);
                    }
                    else
                    {
                        parameters.Add("@lastVisitDate",
                            report.Vessel.LastVisitDate != null
                                ? report.Vessel.LastVisitDate == ""
                                    ? null
                                    : report.Vessel.LastVisitDate
                                : data.lastVisitDate == ""
                                    ? null
                                    : data.lastVisitDate);
                    }

                    await Connection.ExecuteAsync(
                        sql: "[dbo].[Update_VesselDetails]",
                        param: parameters,
                        commandType: CommandType.StoredProcedure,
                        transaction: Transaction);
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
                Core.Models.VesselModel.Vessel vessel = null;

                if (!string.IsNullOrWhiteSpace(classNumber))
                {
                    vessel = await _freedomApiRepository.GetVessel(classNumber);
                }

                var parameters = new DynamicParameters();

                parameters.Add("@projectId", project.Id);
                parameters.Add("@classNo", project.AbsClassID);
                parameters.Add("@builtBy", project.Builder);
                parameters.Add("@vesselName", project.VesselName);
                parameters.Add("@length", project.LengthOverall);
                parameters.Add("@yearBuilt", project.YearBuilt);
                parameters.Add("@hullNo", project.HullNumber);
                parameters.Add("@imoNumber", project.IMO);

                parameters.Add("@classed",
                    project.ClassSociety != null && project.ClassSociety.Length > 0
                        ? project.ClassSociety[0]
                        : null);

                parameters.Add("@firstVisitDate",
                    project.SurveyFirstVisit != null &&
                    project.SurveyFirstVisit != DateTime.MinValue
                        ? Convert.ToDateTime(project.SurveyFirstVisit).ToString("dd-MM-yyy")
                        : DateTime.Now.ToString("dd-MM-yyy"));

                parameters.Add("@lastVisitDate",
                    project.SurveyLastVisit != null &&
                    project.SurveyLastVisit != DateTime.MinValue
                        ? Convert.ToDateTime(project.SurveyLastVisit).ToString("dd-MM-yyy")
                        : DateTime.Now.ToString("dd-MM-yyy"));

                if (vessel != null)
                {
                    parameters.Add("@vesselID", vessel.ImoNumber);
                    parameters.Add("@vesselType", vessel.VesselType);
                    parameters.Add("@flagName", vessel.FlagName);
                    parameters.Add("@homeport", vessel.PortRegistry);
                    parameters.Add("@officalNumber", vessel.OfficialNumber);
                    parameters.Add("@callSign", vessel.CallSign);
                    parameters.Add("@ownerName", vessel.RegisteredOwner?.Name);
                    parameters.Add("@breadth", vessel?.BreadthMolded?.Value);
                    parameters.Add("@depth", vessel?.DepthMolded?.Value);
                    parameters.Add("@deadweight", vessel?.DesignDeadWeight?.Value);
                    parameters.Add("@netTons", vessel?.NetTonnage?.Value);
                    parameters.Add("@classNotation", vessel?.ClassNotation);
                }

                await Connection.ExecuteAsync(
                    sql: "[dbo].[Create_VesselMainData]",
                    param: parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction);

                Commit();

                return classNumber;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<SurveyStatus>> GetSurveyStatus(int projectId)
        {

            try
            {
                await using var connection =
                new SqlConnection(_unitOfWork.ConnectionString);

                await connection.OpenAsync();
                var result = await connection.QueryAsync<SurveyStatus>(
                    sql: "dbo.GetSurveyStatus",
                    new { projectId = projectId },
                    commandType: CommandType.StoredProcedure);

                return result;

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<IEnumerable<ReportVesselMainData>> GetVesselMainData(int projectId)
        {
            var result = await Connection.QueryAsync<ReportVesselMainData>(
                   sql: "dbo.GetVesselMainData",
                   new { projectId = projectId },
                   commandType: CommandType.StoredProcedure,
                   transaction: Transaction);

            return result;
        }

        public async Task<Core.Models.Survey.Certificates> GetStatutoryCertificate(int projectId)
        {
            await using var connection =
    new SqlConnection(_unitOfWork.ConnectionString);

            await connection.OpenAsync();
            var data =
                await connection.QueryFirstOrDefaultAsync<
                    Core.Models.Survey.ReportStatutoryCertificates>(
                    sql: "dbo.GetStatutoryCertificates",
                    param: new { ProjectId = projectId },
                    commandType: CommandType.StoredProcedure
                    
                );

            var issued = new Core.Models.Survey.StatutoryCertificates();
            var expiry = new Core.Models.Survey.StatutoryCertificates();

            if (data != null)
            {
                issued.LLLIDate = (data.llliIssueDate);
                expiry.LLLIDate = (data.llliExpiryDate);

                issued.SafetyConDate = (data.safetyConIssueDate);
                expiry.SafetyConDate = (data.safetyConExpiryDate);

                issued.SafetyRadioDate = (data.safetyRadioIssueDate);
                expiry.SafetyRadioDate = (data.safetyRadioExpiryDate);

                issued.SafetyEquipmentDate = (data.safetyEquipmentIssueDate);
                expiry.SafetyEquipmentDate = (data.safetyEquipmentExpiryDate);

                issued.IEEDate = (data.ieeIssueDate);
                expiry.IEEDate = (data.ieeExpiryDate);

                issued.IOPPDate = (data.ioppIssueDate);
                expiry.IOPPDate = (data.ioppExpiryDate);

                issued.IAPDate = (data.iapIssueDate);
                expiry.IAPDate = (data.iapExpiryDate);

                issued.IAFSDate = (data.iafsIsuueDate);
                expiry.IAFSDate = (data.iafsExpiryDate);

                issued.ISPSDate = (data.ispsIssueDate);
                expiry.ISPSDate = (data.ispsExpiryDate);

                issued.CargoGearReTestDate = (data.cargoGearReTestIssueDate);
                expiry.CargoGearReTestDate = (data.cargoGearReTestExpiryDate);

                issued.CargoGearAnnualCertificate =
                    (data.cargoGearAnnualIssueCertificate);
                expiry.CargoGearAnnualCertificate =
                    (data.cargoGearAnnualExpiryCertificate);

                issued.IBWMDate = (data.ibwmIssueDate);
                expiry.IBWMDate = (data.ibwmExpiryDate);

                issued.ISPPSewageDate = (data.isppSewageIssueDate);
                expiry.ISPPSewageDate = (data.isppSewageExpiryDate);

                issued.ISMDate = (data.ismIssueDate);
                expiry.ISMDate = (data.ismExpiryDate);

                issued.MaritimeLabourDate = (data.maritimeLabourIssueDate);
                expiry.MaritimeLabourDate = (data.maritimeLabourExpiryDate);

                issued.GasCarrierFitness =
                    (data.fitnessGasCertificateIssueDate);
                expiry.GasCarrierFitness =
                    (data.fitnessGasCertificateExpiryDate);
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
            var data = new Core.Models.Survey.ReportStatutoryCertificates();

            var result = await Connection.QueryAsync<Core.Models.Survey.ReportStatutoryCertificates>(
                sql: "dbo.GetStatutoryCertificates",
                new { @projectId = projectId },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
            data = result.FirstOrDefault();


            if (data == null)
            {
                await Connection.ExecuteAsync(
                    sql: "dbo.Insert_ReportStatutoryCertificates",
                    new
                    {
                        projectId,

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

                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction);
                this.Commit();
            }

            else
            {
                await Connection.ExecuteAsync(
      sql: "dbo.Update_ReportStatutoryCertificates",
      new
      {
          projectId,

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
      },
      commandType: CommandType.StoredProcedure,
      transaction: Transaction);
                this.Commit();
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
            var data = this.GetSurveyStatus(projectId).Result;
            if (data.Count() == 0)
            {
                try
                {
                    await Connection.ExecuteAsync(
                     sql: "dbo.Insert_SurveyDetails",
                     new
                     {
                         projectId,
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
                     },

                          commandType: CommandType.StoredProcedure,
                          transaction: Transaction);
                    this.Commit();
                }
                catch (Exception ex)
                { }

            }
            else
            {
                try
                {
                    var surveyStatusData = data.FirstOrDefault();

                    await Connection.ExecuteAsync(
                        sql: "dbo.Update_SurveyDetails",
                        param: new
                        {
                            ProjectId = projectId,

                            ClassSurveyDate = ResolveDate(surveyStatus.ClassSurveyDate, surveyStatusData.ClassSurveyDate),
                            DryDockDate = ResolveDate(surveyStatus.DryDockDate, surveyStatusData.DryDockDate),
                            SpecialContinuousMachineryDate = ResolveDate(surveyStatus.SpecialContinuousMachineryDate, surveyStatusData.SpecialContinuousMachineryDate),
                            BoilerDate = ResolveDate(surveyStatus.BoilerDate, surveyStatusData.BoilerDate),
                            TailshaftDate = ResolveDate(surveyStatus.TailshaftDate, surveyStatusData.TailshaftDate),
                            SpecialContinuousHull1 = ResolveDate(surveyStatus.SpecialContinuousHull1, surveyStatusData.SpecialContinuousHull1),
                            SpecialContinuousHull2 = ResolveDate(surveyStatus.SpecialContinuousHull2, surveyStatusData.SpecialContinuousHull2),
                            SpecialContinuousHull3 = ResolveDate(surveyStatus.SpecialContinuousHull3, surveyStatusData.SpecialContinuousHull3),
                            SpecialContinuousHull4 = ResolveDate(surveyStatus.SpecialContinuousHull4, surveyStatusData.SpecialContinuousHull4),
                            SpecialContinuousHull5 = ResolveDate(surveyStatus.SpecialContinuousHull5, surveyStatusData.SpecialContinuousHull5),
                            SpecialContinuousHull6 = ResolveDate(surveyStatus.SpecialContinuousHull6, surveyStatusData.SpecialContinuousHull6),
                            SpecialContinuousHull7 = ResolveDate(surveyStatus.SpecialContinuousHull7, surveyStatusData.SpecialContinuousHull7),
                            SpecialContinuousHull8 = ResolveDate(surveyStatus.SpecialContinuousHull8, surveyStatusData.SpecialContinuousHull8),
                            SpecialContinuousHull9 = ResolveDate(surveyStatus.SpecialContinuousHull9, surveyStatusData.SpecialContinuousHull9),
                            SpecialContinuousHull10 = ResolveDate(surveyStatus.SpecialContinuousHull10, surveyStatusData.SpecialContinuousHull10),
                            SpecialContinuousHull11 = ResolveDate(surveyStatus.SpecialContinuousHull11, surveyStatusData.SpecialContinuousHull11),
                            SpecialContinuousHull12 = ResolveDate(surveyStatus.SpecialContinuousHull12, surveyStatusData.SpecialContinuousHull12),
                            SpecialContinuousHull13 = ResolveDate(surveyStatus.SpecialContinuousHull13, surveyStatusData.SpecialContinuousHull13),
                            SpecialContinuousHull14 = ResolveDate(surveyStatus.SpecialContinuousHull14, surveyStatusData.SpecialContinuousHull14),
                            SpecialContinuousHull15 = ResolveDate(surveyStatus.SpecialContinuousHull15, surveyStatusData.SpecialContinuousHull15),
                            SpecialHullNo = (surveyStatus.SpecialHullNo != null) ?
                             surveyStatus.SpecialHullNo == 0 ? null : surveyStatus.SpecialHullNo
                             : surveyStatusData.SpecialHullNo == 0 ? null : surveyStatusData.SpecialHullNo
                        },
                        commandType: CommandType.StoredProcedure,
                        transaction: Transaction);

                    this.Commit();
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
                var result = await Connection.QueryFirstOrDefaultAsync<Core.Models.Vessel>(
                    sql: "CAP.Read_VesselDetails_ByIMONumber",
                    param: new { ImoNumber = imoNumber },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                Commit();

                return result;
            }
            catch
            {
                throw;
            }
        }
    }
}
