using Dapper;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.VesselModel;
using DigitalCap.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DigitalCap.Persistence.Repositories
{
    public class SurveyReportRepository : ISurveyReportRepository
    {
        private readonly ILogger<SurveyReportRepository> _logger;
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;

        private readonly IFreedomAPIRepository _freedomApiRepository;

        public SurveyReportRepository(IUnitOfWork unitOfWork, ILogger<SurveyReportRepository> logger, IFreedomAPIRepository freedomApiRepository)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _freedomApiRepository = freedomApiRepository;
        }
        public void Commit()
        {
            _unitOfWork?.Commit();
        }
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
        public async Task<List<ReportTemplateSection>> GetTemplateSections(int templateId)
        {
            try
            {
                var result = await Connection.QueryAsync<ReportTemplateSection>(
                    sql: "[dbo].[GetTemplateSections]",
                    param: new { TemplateId = templateId },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                return result.ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<List<ReportTemplate>> GetReportTemplateList()
        {
            var result = await Connection.QueryAsync<ReportTemplate>(
                         sql: "dbo.GetReportTemplateList",
                     commandType: CommandType.StoredProcedure,
                     transaction: Transaction);

            return result.ToList();

        }
        public async Task<List<int>> GetExistingReportPartsSectionIds(int projectId)
        {
            var result = await Connection.QueryAsync<int>(
                   sql: "GetExistingReportPartsSectionIds",
                  param: new { ProjectId = projectId },
                   commandType: CommandType.StoredProcedure,
                   transaction: Transaction);

            return result.ToList();

        }

        public async Task<ReportTemplateUI> GetTemplateTitle(int templateId)
        {
            try
            {
                await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

                await connection.OpenAsync();
                var result = await connection.QueryFirstOrDefaultAsync<ReportTemplateUI>(
                    sql: "[dbo].[GetTemplateTitle]",
                    param: new { TemplateId = templateId },
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<CurrentCondition>> GetCurrentCondition()
        {
            await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

            await connection.OpenAsync();
            var result = await connection.QueryAsync<CurrentCondition>(
                sql: "[dbo].[GetCurrentCondition]",
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }
        public async Task<ProjectReportMapping> GetProjectReportTemplate(int projectId, int templateId, Guid sectionId)
        {
            try
            {
                var result = await Connection.QueryAsync<ProjectReportMapping>(
                 sql: "dbo.SelectProjectTemplateMapping",
                 param: new
                 {
                     ProjectId = projectId,
                     TemplateId = templateId,
                     SectionId = sectionId
                 },
                 commandType: CommandType.StoredProcedure,
                 transaction: Transaction
             );

                return result.FirstOrDefault();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<ProjectReportMapping>> GetProjectReportTemplate(int projectId, int templateId)
        {
            await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

            await connection.OpenAsync();
            var result = await connection.QueryAsync<ProjectReportMapping>(
                sql: "dbo.SelectProjectTemplateMapping",
                param: new
                {
                    ProjectId = projectId,
                    TemplateId = templateId,
                    SectionId = Guid.Empty
                },
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<List<ProjectGradingMapping>> GetProjectGrading(int projectId, int templateId, Guid sectionId)
        {
            DataTable dt = new DataTable();
            try
            {
                await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

                await connection.OpenAsync();
                var result = await connection.QueryAsync<ProjectGradingMapping>(
                        sql: "dbo.SelectProjectGradingMapping",
                        param: new
                        {
                            ProjectId = projectId,
                            TemplateId = templateId,
                            SectionId = sectionId
                        },
                         commandType: CommandType.StoredProcedure,
                 commandTimeout: 120);

                return result.ToList();
            }
            catch (SqlException sqlEx)
            {
            }
            return null;

        }

        public async Task<List<ProjectGradingConditionMapping>> GetProjectGradingConditionMapping(int projectId, int templateId, Guid sectionId, int gradingId)
        {
            try
            {
                await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

                await connection.OpenAsync();
                var result = await connection.QueryAsync<ProjectGradingConditionMapping>(
                    sql: "dbo.SelectProjectGradingConditionMapping",
                    param: new
                    {
                        ProjectId = projectId,
                        TemplateId = templateId,
                        SectionId = sectionId,
                        GradingId = gradingId
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120

                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        public async Task<List<GradingUI>> GetTemplatGradings(int templateId, int projectId)
        {
            var result = await Connection.QueryAsync<GradingUI>(
                sql: "dbo.GetSectionGrading",
                param: new
                {
                    ProjectId = projectId,
                    TemplateId = templateId
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result.ToList();
        }

        public async Task<List<ImageCardUI>> GetImageCard(int templateId)
        {
            var result = await Connection.QueryAsync<ImageCardUI>(
               sql: "dbo.GetImageCards",
               param: new
               {
                   TemplateId = templateId
               },
     commandType: CommandType.StoredProcedure,
               transaction: Transaction,
                commandTimeout: 120
           );
            return result.ToList();
        }

        public async Task<List<GradingConditionUI>> GetGradingCondition(int templateId, int projectid)
        {
            var result = await Connection.QueryAsync<GradingConditionUI>(
             sql: "dbo.GetGradingConditions",
             param: new
             {
                 TemplateId = templateId,
                 ProjectId = projectid
             },
   commandType: CommandType.StoredProcedure,
             transaction: Transaction,
             commandTimeout: 120
         );
            return result.ToList();
        }

        public async Task<List<ProjectCardMapping>> GetProjectImageCard(int projectId, int templateId, Guid sectionId)
        {

            var result = await Connection.QueryAsync<ProjectCardMapping>(
             sql: "dbo.SelectProjectImageCards",
             param: new
             {
                 ProjectId = projectId,
                 TemplateId = templateId,
                 SectionId = sectionId

             },
             commandType: CommandType.StoredProcedure,
             transaction: Transaction,
             commandTimeout: 120
         );
            return result.ToList();
        }

        //dbo.GetH_IReport
        public async Task<HandIReport> GetHIReport(int projectId)
        {
            var result = await Connection.QueryFirstOrDefaultAsync<HandIReport>(
                sql: "dbo.GetH_IReport",
                param: new { ProjectId = projectId },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result;
        }

        public async Task<List<ReportTemplateSection>> GetTemplatSections(int templateId)
        {
            await using var connection =
                new SqlConnection(_unitOfWork.ConnectionString);

            await connection.OpenAsync();

            var result = await connection.QueryAsync<ReportTemplateSection>(
                "dbo.GetTemplateSections",
                new
                {
                    TemplateId = templateId
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 120
            );

            return result.ToList();
        }

        public async Task<List<UpskillImageData>> GetBulkUploadUnplacedImages()
        {
            var result = await Connection.QueryAsync<UpskillImageData>(
                sql: "dbo.GetBulkUploadImage",
                commandType: CommandType.StoredProcedure,
                transaction: Transaction,
                commandTimeout: 120
            );

            return result.ToList();
        }
        public async Task<bool> CreateProjectTemplate(ProjectReportMapping model)
        {
            try
            {
                var result = await Connection.ExecuteAsync(
         sql: "dbo.CreateProjectTemplate",
         param: new
         {
             model.ProjectId,
             model.TemplateId,
             model.SectionId,
             model.CreatedDttm
         },
         commandType: CommandType.StoredProcedure,
         transaction: Transaction
       );
                this.Commit();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        public async Task<bool> DeleteProjectTemplate(int projectId, int templateId)
        {
            try
            {

                var result = await Connection.ExecuteAsync(
                           sql: "dbo.DeleteProjectTemplate",
                           param: new
                           {
                               ProjectId = projectId,
                               TemplateId = templateId
                           },
                           commandType: CommandType.StoredProcedure,
                           transaction: Transaction
                         );
                this.Commit();
                return true;

            }
            catch (SqlException sqlEx)
            {
            }
            return false;

        }
        public async Task<int> DeleteBulkUploadImage(int imageId)
        {
            return await Connection.ExecuteAsync(
                sql: "dbo.DeleteBulkUploadImage",
                param: new { ImageId = imageId },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );
        }
        public async Task<bool> AddEditImageNew(int imgId, string imageBase64)
        {
            var result = await Connection.ExecuteScalarAsync<int>(
                sql: "[upskill].[sp_AddEdit_Image_New]",
                param: new
                {
                    Id = -1,
                    ImageId = imgId,
                    Base64String = imageBase64
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result > 0;
        }

        public async Task<bool> DeleteImage(string assignmentId, string fileId)
        {
            assignmentId ??= "Unknown";

            var affectedRows = await Connection.ExecuteAsync(
                sql: "[upskill].[sp_Delete_Image]",
                param: new
                {
                    AssignmentId = assignmentId,
                    FileId = fileId
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );
            this.Commit();
            return affectedRows > 0;
        }
        public async Task<bool> DeleteCard(int projectId, string cardId, string sequenceId, string assignmentId, string appId)
        {
            var result = await Connection.ExecuteScalarAsync<int>(
                sql: "[upskill].[sp_Delete_Card]",
                param: new
                {
                    ProjectId = projectId,
                    CardId = cardId,
                    SequenceId = sequenceId,
                    AssignmentId = assignmentId,
                    AppId = appId
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );
            this.Commit();
            return result == 1;
        }
        public async Task<int> UploadImage(string assignmentId, string fileId)
        {
            assignmentId ??= "Unknown";

            var result = await Connection.ExecuteScalarAsync<int>(
                sql: "[upskill].[sp_AddEdit_Image]",
                param: new
                {
                    AssignmentId = assignmentId,
                    FileId = fileId
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );
            this.Commit();
            return result;
        }

        public async Task<ProjectCardMapping> GetProjectImagCardById(int projectId, int templateId, Guid sectionId, int cardId)
        {
            try
            {
                var result = await Connection.QueryFirstOrDefaultAsync<ProjectCardMapping>(
                    sql: "dbo.SelectProjectImagecardById",
                    param: new
                    {
                        ProjectId = projectId,
                        TemplateId = templateId,
                        SectionId = sectionId,
                        CardId = cardId
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> CreateProjectImageCard(ProjectCardMapping model)
        {
            try
            {
                var affectedRows = await Connection.ExecuteAsync(
                    sql: "dbo.CreateProjectImageCard",
                    param: new
                    {
                        model.ProjectId,
                        model.TemplateId,
                        model.SectionId,
                        model.CardId,
                        model.ImageId,
                        model.DescriptionId,
                        model.CurrentCondition,
                        model.AdditionalDescription,
                        model.IsDeleted,
                        model.CreatedDttm,
                        model.UpdateDttm
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
                return affectedRows > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateProjectImageCard(ProjectCardMapping model)
        {
            try
            {
                var affectedRows = await Connection.ExecuteAsync(
                    sql: "dbo.UpdateProjectImageCard",
                    param: new
                    {
                        model.Id,
                        model.ProjectId,
                        model.TemplateId,
                        model.SectionId,
                        model.CardId,
                        model.ImageId,
                        model.DescriptionId,
                        model.CurrentCondition,
                        model.AdditionalDescription,
                        model.IsDeleted,
                        model.UpdateDttm
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
                return affectedRows > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<ProjectGradingMapping> GetProjectGradingById(int projectId, int templateId, Guid sectionId, int gradingId)
        {
            try
            {
                var result = await Connection.QueryFirstOrDefaultAsync<ProjectGradingMapping>(
                    sql: "dbo.SelectProjectGradingCardById",
                    param: new
                    {
                        ProjectId = projectId,
                        TemplateId = templateId,
                        SectionId = sectionId,
                        GradingId = gradingId
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction,
                    commandTimeout: 120
                );

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdateProjectGrading(ProjectGradingMapping model)
        {
            try
            {
                var affectedRows = await Connection.ExecuteAsync(
                    sql: "dbo.UpdateProjectGrading",
                    param: new
                    {
                        model.Id,
                        model.ProjectId,
                        model.TemplateId,
                        model.SectionId,
                        model.GradingId,
                        model.Value,
                        model.IsDeleted,
                        model.UpdateDttm
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
                return affectedRows > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CreateProjectGrading(ProjectGradingMapping model)
        {
            try
            {
                var affectedRows = await Connection.ExecuteAsync(
                    sql: "dbo.CreateProjectGrading",
                    param: new
                    {
                        model.ProjectId,
                        model.TemplateId,
                        model.SectionId,
                        model.GradingId,
                        model.Value,
                        model.IsDeleted,
                        model.CreatedDttm,
                        model.UpdateDttm
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
                return affectedRows > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateProjectGradingConditionMapping(ProjectGradingConditionMapping model)
        {
            try
            {
                var affectedRows = await Connection.ExecuteAsync(
                    sql: "dbo.UpdateProjectGradingConditionMapping",
                    param: new
                    {
                        model.Id,
                        model.ProjectId,
                        model.TemplateId,
                        model.SectionId,
                        model.GradingId,
                        model.GradingConditionLabel,
                        model.GradingConditionValue,
                        model.IsDeleted,
                        model.UpdateDttm
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
                return affectedRows > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CreateProjectGradingConditionMapping(ProjectGradingConditionMapping model)
        {
            try
            {
                var affectedRows = await Connection.ExecuteAsync(
                    sql: "dbo.CreateProjectGradingConditionMapping",
                    param: new
                    {
                        model.ProjectId,
                        model.TemplateId,
                        model.SectionId,
                        model.GradingId,
                        model.GradingConditionLabel,
                        model.GradingConditionValue,
                        model.IsDeleted,
                        model.CreatedDttm,
                        model.UpdateDttm
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
                return affectedRows > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateProjectTemplate(ProjectReportMapping model)
        {
            try
            {
                var affectedRows = await Connection.ExecuteAsync(
                    sql: "dbo.UpdateProjectTemplate",
                    param: new
                    {
                        model.Id,
                        model.ProjectId,
                        model.TemplateId,
                        model.SectionId,
                        model.Notes,
                        model.IsDeleted,
                        model.UpdateDttm
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
                return affectedRows > 0;
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

        public async Task<bool> UpdateHIReport(HandIReport model)
        {
            try
            {
                var affectedRows = await Connection.ExecuteAsync(
                    sql: "dbo.UpdateH_IReport",
                    param: new
                    {
                        model.Id,
                        model.ProjectId,
                        model.CompanyName,
                        model.CertificateNumber,
                        ExpiryDate = NormalizeHIDate(model.ExpiryDate),
                        model.ThicknessMeasurementA,
                        model.ThicknessMeasurementB,
                        model.ThicknessMeasurementC,
                        model.AdditionalSurveyorComments,
                        model.MaintenanceExecuted,
                        model.Accommodation,
                        model.PhotoGraphs,
                        model.OperationalTestLocation,
                        OperationalTestStart = NormalizeHIDate(model.OperationalTestStart),
                        OperationalTestFinish = NormalizeHIDate(model.OperationalTestFinish),
                        model.OptionalTestSatisFactory,
                        model.AssociatedReportNumber,
                        model.AssociatedReportDate,
                        model.Remarks,
                        model.LeadSurveyorName,
                        model.LeadSurveyorNumber,
                        model.SurveyorName,
                        model.SurveyorNumber,
                        LastVisitDate = NormalizeHIDate(model.LastVisitDate),
                        model.IndividualA,
                        model.IndividualB,
                        model.IndividualC,
                        model.IndividualD,
                        model.IndividualE,
                        model.IndividualF,
                        model.ScouringOrPitting,
                        model.SubstantialCorrosionRemaining,
                        model.StatutoryDeficiencies,
                        model.FoundSatisfactory,
                        model.UnderRemarks,
                        model.BottomPlattingScouringOrPitting,
                        model.IsActive,
                        model.IsDeleted,
                        model.UpdatedDttm
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
                return affectedRows > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CreateHIReport(HandIReport model)
        {
            try
            {
                var affectedRows = await Connection.ExecuteAsync(
                    sql: "dbo.CreateH_IReport",
                    param: new
                    {
                        model.Id,
                        model.ProjectId,
                        model.CompanyName,
                        model.CertificateNumber,
                        ExpiryDate = NormalizeHIDate(model.ExpiryDate),
                        model.ThicknessMeasurementA,
                        model.ThicknessMeasurementB,
                        model.ThicknessMeasurementC,
                        model.AdditionalSurveyorComments,
                        model.MaintenanceExecuted,
                        model.Accommodation,
                        model.PhotoGraphs,
                        model.OperationalTestLocation,
                        OperationalTestStart = NormalizeHIDate(model.OperationalTestStart),
                        OperationalTestFinish = NormalizeHIDate(model.OperationalTestFinish),
                        model.OptionalTestSatisFactory,
                        model.AssociatedReportNumber,
                        model.AssociatedReportDate,
                        model.Remarks,
                        model.LeadSurveyorName,
                        model.LeadSurveyorNumber,
                        model.SurveyorName,
                        model.SurveyorNumber,
                        LastVisitDate = NormalizeHIDate(model.LastVisitDate),
                        model.IndividualA,
                        model.IndividualB,
                        model.IndividualC,
                        model.IndividualD,
                        model.IndividualE,
                        model.IndividualF,
                        model.ScouringOrPitting,
                        model.SubstantialCorrosionRemaining,
                        model.StatutoryDeficiencies,
                        model.FoundSatisfactory,
                        model.UnderRemarks,
                        model.BottomPlattingScouringOrPitting,
                        model.CreatedDttm,
                        model.UpdatedDttm,
                        model.IsActive,
                        model.IsDeleted
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
                return affectedRows > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
