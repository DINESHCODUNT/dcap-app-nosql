using Dapper;
using DigitalCap.Core.DTO;
using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.WebApi.Core;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DigitalCap.Persistence.Repositories
{
    public class ProjectRepository : RepositoryBase<Project, int>, IProjectRepository
    {
        private readonly ILogger<ProjectRepository> _logger;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        public ProjectRepository(IUnitOfWork unitOfWork, ILogger<ProjectRepository> logger, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
    : base(unitOfWork, logger)
        {
            _logger = logger;
            _userManager = userManager;
        }
        public void Commit()
        {
            _unitOfWork?.Commit();
        }
        public async Task<bool> CheckProjectExistsByImoNumber(int imoNumber)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IMO", imoNumber);

                var result = await Connection.QueryFirstOrDefaultAsync(
                 sql: "[CAP].[Read_ProjectExists_ByImoNumber]",
                 param: parameters,
                 commandType: CommandType.StoredProcedure,
                 transaction: Transaction
             );

                return result != null ? true : false;
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public async Task<int> CreateProject(Project model, string currentUser)
        {
            try
            {
                var CreatedBy = (await _userManager.FindByEmailAsync(currentUser))?.Id;
                var user = string.IsNullOrEmpty(currentUser) ? "SYSTEM" : currentUser;

                var parameters = new DynamicParameters();

                // Basic project information
                parameters.Add("@Name", model.Name);
                parameters.Add("@PIDNumber", model.PIDNumber);
                parameters.Add("@CapRegion", model.CapRegion);
                parameters.Add("@CapScope", model.CapScope);
                parameters.Add("@CapType", (int)model.CapType);
                parameters.Add("@DrydockLocation", model.DrydockLocation);
                parameters.Add("@DrydockStart", model.DrydockStart);
                parameters.Add("@DrydockEnd", model.DrydockEnd);
                parameters.Add("@SpecialHull", model.SpecialHull);
                parameters.Add("@Fatigue", model.Fatigue);
                parameters.Add("@ExpenseMarkup", model.ExpenseMarkup);
                parameters.Add("@SurveyDayrateClient", model.SurveyDayrateClient);
                parameters.Add("@SurveyDayrateAbs", model.SurveyDayrateAbs);
                parameters.Add("@SedRate1", model.SedRate1);
                parameters.Add("@SedRate2", model.SedRate2);
                parameters.Add("@AgreementNumber", model.AgreementNumber);
                parameters.Add("@ProposalRev", model.ProposalRev ?? 0);
                parameters.Add("@CapContractValue", model.CapContractValue);
                parameters.Add("@AgreementOwner", model.AgreementOwner);
                parameters.Add("@AgreementDate", model.AgreementDate);
                parameters.Add("@AgreementSignedDate", model.AgreementSignedDate);
                parameters.Add("@CertificateGrade", model.CertificateGrade);
                parameters.Add("@ProjectComments", model.ProjectComments);

                // Company information
                parameters.Add("@CompanyName", model.CompanyName);
                parameters.Add("@CompanyAddress", model.CompanyAddress);
                parameters.Add("@CompanyBillingEmail", model.CompanyBillingEmail);
                parameters.Add("@CompanyBillingSystemUrl", model.CompanyBillingSystemUrl);
                parameters.Add("@BillSameAsCapClient", model.BillSameAsCapClient);
                parameters.Add("@BillToCompanyName", model.BillToCompanyName);
                parameters.Add("@BillToCompanyAddress", model.BillToCompanyAddress);
                parameters.Add("@BillToCompanyEmail", model.BillToCompanyEmail);
                parameters.Add("@BillToCompanyBillingUrl", model.BillToCompanyBillingUrl);
                parameters.Add("@PocName", model.PocName);
                parameters.Add("@PocEmail", model.PocEmail);
                parameters.Add("@PocPhone", model.PocPhone);

                // Vessel information
                parameters.Add("@IMO", model.IMO);
                parameters.Add("@VesselName", model.VesselName);
                // ShipType: stored procedure parameter is varchar but expects integer value as string
                // Database column is int, so pass the enum's integer value as string
                parameters.Add("@ShipType", model.ShipType != default(ShipType) ? ((int)model.ShipType).ToString() : null);
                parameters.Add("@WCN", model.WCN);
                parameters.Add("@ClassSociety", model.ClassSociety != null ? string.Join(",", model.ClassSociety) : null);
                parameters.Add("@AbsClassID", model.AbsClassID);
                parameters.Add("@SisterVessel", model.SisterVessel);
                parameters.Add("@SisterVesselImoNumber", model.SisterVesselImoNumber);
                parameters.Add("@SisterVesselName", model.SisterVesselName);
                parameters.Add("@Builder", model.Builder);
                parameters.Add("@HullNumber", model.HullNumber);
                parameters.Add("@MonthBuilt", model.MonthBuilt);
                parameters.Add("@YearBuilt", model.YearBuilt);
                parameters.Add("@LengthOverall", model.LengthOverall);
                parameters.Add("@CRMOpportunityNumber", model.CRMOpportunityNumber);
                parameters.Add("@User", user);
                parameters.Add("@PotentialDrydockDate", model.PotentialDrydockDate);
                parameters.Add("@SurveyFirstVisit", model.SurveyFirstVisit);
                parameters.Add("@SurveyLastVisit", model.SurveyLastVisit);

                // Client profile information
                parameters.Add("@ClientProfileId", model.ClientProfileId);
                parameters.Add("@BillToClientProfileId", model.BillToClientProfileId);
                parameters.Add("@BillToWCN", model.BillToWCN);

                // Surveyor information
                parameters.Add("@LeadSurveyor", model.LeadSurveyor);
                parameters.Add("@LeadSurveyorEID", model.LeadSurveyorEID);
                parameters.Add("@SecondSurveyor", model.SecondSurveyor != null ? string.Join(",", model.SecondSurveyor) : null);
                parameters.Add("@SecondSurveyorEID", model.SecondSurveyorEID);
                parameters.Add("@ThirdSurveyor", model.ThirdSurveyor);
                parameters.Add("@ThirdSurveyorEID", model.ThirdSurveyorEID);
                parameters.Add("@FourthSurveyor", model.FourthSurveyor);
                parameters.Add("@FourthSurveyorEID", model.FourthSurveyorEID);
                parameters.Add("@AdditionalSurveyors", model.AdditionalSurveyors);

                // Additional fields
                parameters.Add("@ClassSocietyOther", model.ClassSocietyOther);
                parameters.Add("@CapScopeOther", model.CapScopeOther);
                parameters.Add("@IsCap2", true);
                parameters.Add("@CreatedBy", CreatedBy);
                parameters.Add("@VesselType", model.VesselType);
                parameters.Add("@CopyingVesselID", model.CopyingVesselID);
                parameters.Add("@IsDefaultTank", model.IsDefaultTank);
                parameters.Add("@TankVesselType", model.TankVesselType);
                parameters.Add("@TankVesselIMO", model.TankVesselIMO);

                // Execute stored procedure - it returns the project ID via SELECT @Output
                var projectId = await Connection.QuerySingleAsync<int>(
                    sql: "[CAP].[Create_Project]",
                    param: parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );

                this.Commit();
                return projectId;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating project");
                throw;
            }
        }

        public async Task<List<SelectListItem>> GetProjectListByIMO(int? imoNumber)
        {
            try
            {

                return (await Connection.QueryAsync<SelectListItem>(
                    sql: "CAP.GetProjectListByIMO",
                    param: new { IMO = imoNumber },
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction)).ToList();

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Project> GetProject(int id)
        {
            try
            {
                await using var connection = new SqlConnection(_unitOfWork.ConnectionString);

                await connection.OpenAsync();
                var result = await connection.QueryFirstOrDefaultAsync(
                    sql: "CAP.Read_Project_ById",
                    param: new { id },
                    commandType: CommandType.StoredProcedure
                );

                if (result == null)
                    return null;

                // Helper method to safely get values from Dapper dynamic object
                // Dapper returns dynamic objects that implement IDictionary<string, object>
                T GetValue<T>(dynamic obj, string propertyName, T defaultValue = default(T))
                {
                    try
                    {
                        // Try dictionary access first (Dapper dynamic objects implement IDictionary)
                        var dict = obj as IDictionary<string, object>;
                        if (dict != null)
                        {
                            // Try exact match first
                            if (dict.ContainsKey(propertyName))
                            {
                                var value = dict[propertyName];
                                if (value != null && value != DBNull.Value)
                                {
                                    if (value is T)
                                        return (T)value;
                                    return (T)Convert.ChangeType(value, typeof(T));
                                }
                            }

                            // Try case-insensitive match
                            var key = dict.Keys.FirstOrDefault(k =>
                                string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase));
                            if (key != null)
                            {
                                var value = dict[key];
                                if (value != null && value != DBNull.Value)
                                {
                                    if (value is T)
                                        return (T)value;
                                    return (T)Convert.ChangeType(value, typeof(T));
                                }
                            }
                        }

                        // Fallback: try direct dynamic property access
                        try
                        {
                            var value = ((dynamic)obj)[propertyName];
                            if (value != null && value != DBNull.Value)
                            {
                                if (value is T)
                                    return (T)value;
                                return (T)Convert.ChangeType(value, typeof(T));
                            }
                        }
                        catch { }
                    }
                    catch { }

                    return defaultValue;
                }

                // Helper method to safely get nullable values from Dapper dynamic object
                T? GetNullableValue<T>(dynamic obj, string propertyName) where T : struct
                {
                    try
                    {
                        // Try dictionary access first (Dapper dynamic objects implement IDictionary)
                        var dict = obj as IDictionary<string, object>;
                        if (dict != null)
                        {
                            // Try exact match first
                            if (dict.ContainsKey(propertyName))
                            {
                                var value = dict[propertyName];
                                if (value != null && value != DBNull.Value)
                                {
                                    if (value is T)
                                        return (T)value;
                                    return (T)Convert.ChangeType(value, typeof(T));
                                }
                                return null;
                            }

                            // Try case-insensitive match
                            var key = dict.Keys.FirstOrDefault(k =>
                                string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase));
                            if (key != null)
                            {
                                var value = dict[key];
                                if (value != null && value != DBNull.Value)
                                {
                                    if (value is T)
                                        return (T)value;
                                    return (T)Convert.ChangeType(value, typeof(T));
                                }
                                return null;
                            }
                        }

                        // Fallback: try direct dynamic property access
                        try
                        {
                            var value = ((dynamic)obj)[propertyName];
                            if (value != null && value != DBNull.Value)
                            {
                                if (value is T)
                                    return (T)value;
                                return (T)Convert.ChangeType(value, typeof(T));
                            }
                        }
                        catch { }
                    }
                    catch { }

                    return null;
                }

                // Manually map the result to Project, handling string to string[] conversions
                var project = new Project
                {
                    Id = GetValue<int>(result, "ID", 0),
                    Name = GetValue<string>(result, "Name"),
                    PIDNumber = GetNullableValue<int>(result, "PIDNumber"),
                    CapRegion = GetValue<string>(result, "CapRegion"),
                    CapScope = GetValue<string>(result, "CapScope"),
                    CapScopeOther = GetValue<string>(result, "CapScopeOther"),
                    CapType = (CapType)GetValue<int>(result, "CapType", 0),
                    DrydockLocation = GetNullableValue<int>(result, "DrydockLocation"),
                    DrydockStart = GetNullableValue<DateTime>(result, "DrydockStart"),
                    DrydockEnd = GetNullableValue<DateTime>(result, "DrydockEnd"),
                    PotentialDrydockDate = GetNullableValue<DateTime>(result, "PotentialDrydockDate"),
                    SurveyFirstVisit = GetNullableValue<DateTime>(result, "SurveyFirstVisit"),
                    SurveyLastVisit = GetNullableValue<DateTime>(result, "SurveyLastVisit"),
                    ProjectStatus = GetNullableValue<int>(result, "ProjectStatus"),
                    ProjectPriority = GetNullableValue<int>(result, "ProjectPriority"),
                    SpecialHull = GetValue<bool>(result, "SpecialHull", true),
                    Fatigue = GetValue<bool>(result, "Fatigue", true),
                    ExpenseMarkup = GetNullableValue<int>(result, "ExpenseMarkup"),
                    SurveyDayrateClient = GetNullableValue<decimal>(result, "SurveyDayrateClient"),
                    SurveyDayrateAbs = GetNullableValue<decimal>(result, "SurveyDayrateAbs"),
                    SedRate1 = GetNullableValue<decimal>(result, "SedRate1"),
                    SedRate2 = GetNullableValue<decimal>(result, "SedRate2"),
                    CapContractValue = GetNullableValue<decimal>(result, "CapContractValue"),
                    AgreementNumber = GetValue<string>(result, "AgreementNumber"),
                    ProposalRev = GetNullableValue<int>(result, "ProposalRev"),
                    AgreementOwner = GetValue<string>(result, "AgreementOwner"),
                    AgreementDate = GetNullableValue<DateTime>(result, "AgreementDate"),
                    AgreementSignedDate = GetNullableValue<DateTime>(result, "AgreementSignedDate"),
                    CertificateGrade = GetValue<string>(result, "CertificateGrade"),
                    ProjectComments = GetValue<string>(result, "ProjectComments"),
                    CompanyName = GetValue<string>(result, "CompanyName"),
                    CompanyAddress = GetValue<string>(result, "CompanyAddress"),
                    CompanyBillingEmail = GetValue<string>(result, "CompanyBillingEmail"),
                    CompanyBillingSystemUrl = GetValue<string>(result, "CompanyBillingSystemUrl"),
                    BillSameAsCapClient = GetValue<bool>(result, "BillSameAsCapClient", true),
                    BillToCompanyName = GetValue<string>(result, "BillToCompanyName"),
                    BillToCompanyAddress = GetValue<string>(result, "BillToCompanyAddress"),
                    BillToCompanyEmail = GetValue<string>(result, "BillToCompanyEmail"),
                    BillToCompanyBillingUrl = GetValue<string>(result, "BillToCompanyBillingUrl"),
                    PocName = GetValue<string>(result, "PocName"),
                    PocEmail = GetValue<string>(result, "PocEmail"),
                    PocPhone = GetValue<string>(result, "PocPhone"),
                    IMO = GetNullableValue<int>(result, "IMO"),
                    VesselName = GetValue<string>(result, "VesselName"),
                    VesselType = GetValue<string>(result, "VesselType"),
                    ShipType = GetValue<int>(result, "ShipType", 0) != 0 ? (ShipType)GetValue<int>(result, "ShipType", 0) : default(ShipType),
                    ClientProfileId = GetNullableValue<Guid>(result, "ClientProfileId"),
                    BillToClientProfileId = GetNullableValue<Guid>(result, "BillToClientProfileId"),
                    WCN = GetValue<string>(result, "WCN"),
                    BillToWCN = GetValue<string>(result, "BillToWCN"),
                    // Convert comma-separated string to string array
                    ClassSociety = GetValue<string>(result, "ClassSociety")?.Split(','),
                    ClassSocietyOther = GetValue<string>(result, "ClassSocietyOther"),
                    AbsClassID = GetValue<string>(result, "AbsClassID"),
                    SisterVessel = GetValue<bool>(result, "SisterVessel", false),
                    SisterVesselImoNumber = GetValue<string>(result, "SisterVesselImoNumber"),
                    SisterVesselName = GetValue<string>(result, "SisterVesselName"),
                    IsDefaultTank = GetNullableValue<bool>(result, "IsDefaultTank"),
                    CopyingVesselID = GetNullableValue<int>(result, "CopyingVesselID"),
                    Builder = GetValue<string>(result, "Builder"),
                    HullNumber = GetValue<string>(result, "HullNumber"),
                    MonthBuilt = GetNullableValue<int>(result, "MonthBuilt"),
                    YearBuilt = GetNullableValue<int>(result, "YearBuilt"),
                    LengthOverall = GetNullableValue<decimal>(result, "LengthOverall"),
                    CRMOpportunityNumber = GetValue<string>(result, "CRMOpportunityNumber"),
                    ProjectStatusName = GetValue<string>(result, "ProjectStatusName"),
                    DrydockLocationName = GetValue<string>(result, "DrydockLocationName"),
                    LeadSurveyor = GetValue<string>(result, "LeadSurveyor"),
                    LeadSurveyorEID = GetValue<string>(result, "LeadSurveyorEID"),
                    // Convert comma-separated string to string array
                    SecondSurveyor = GetValue<string>(result, "SecondSurveyor")?.Split(','),
                    SecondSurveyorEID = GetValue<string>(result, "SecondSurveyorEID"),
                    ThirdSurveyor = GetValue<string>(result, "ThirdSurveyor"),
                    ThirdSurveyorEID = GetValue<string>(result, "ThirdSurveyorEID"),
                    FourthSurveyor = GetValue<string>(result, "FourthSurveyor"),
                    FourthSurveyorEID = GetValue<string>(result, "FourthSurveyorEID"),
                    AdditionalSurveyors = GetValue<bool>(result, "AdditionalSurveyors", false),
                    CreatedDate = GetNullableValue<DateTime>(result, "CreatedDate"),
                    CreatedBy = GetValue<string>(result, "CreatedByDisplayName"),
                    StatusChangedBy = GetValue<string>(result, "StatusChangedBy"),
                    StatusChangedDate = GetNullableValue<DateTimeOffset>(result, "StatusChangedDate"),
                    LastModifiedBy = GetValue<string>(result, "LastModifiedBy"),
                    PercentComplete = (byte)GetValue<int>(result, "PercentComplete", 0),
                    TankVesselType = GetValue<string>(result, "TankVesselType"),
                    TankVesselIMO = GetValue<int>(result, "TankVesselIMO", 0),
                    IsReadOnlyEnabled = GetValue<bool>(result, "IsReadOnlyEnabled", false)
                };

                // Populate status colors using StatusColorHelper
                if (!string.IsNullOrWhiteSpace(project.ProjectStatusName))
                {
                    project.ProjectStatusColor = DigitalCap.Core.Helpers.StatusColorHelper.GetProjectStatusColor(project.ProjectStatusName);
                }

                return project;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        private static T GetValue<T>(IDataReader reader, HashSet<string> columns, string columnName)
        {
            if (!columns.Contains(columnName))
                return default;

            var value = reader[columnName];
            if (value == DBNull.Value)
                return default;

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, targetType);
        }

        public async Task<int> UpdatePercentComplete(int projectId, byte percentComplete, string currentUser)
        {
            var result = await Connection.ExecuteAsync(
    sql: "CAP.Update_Project_Percent_Complete",
    param: new
    {
        ID = projectId,
        PercentComplete = percentComplete,
        User = currentUser
    },
    commandType: CommandType.StoredProcedure,
    transaction: Transaction);
            this.Commit();
            return result;
        }

        public async Task<bool> CheckProjectExistsByProjectName(string projectName, int imoNumber, int vesseltypeid)
        {
            var result = await Connection.QueryFirstOrDefaultAsync<string>(
                sql: "CAP.Read_ProjectExists_ByProjectName",
                param: new { ProjectName = projectName, IMO = imoNumber, VesselTypeID = vesseltypeid },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
            return result != null ? true : false;

        }

        public async Task Update_Project(Project project, string currentUser)
        {
            try
            {
                var user = string.IsNullOrEmpty(currentUser) ? "SYSTEM" : currentUser;

                var parameters = new DynamicParameters();

                // ID parameter (required for UPDATE)
                parameters.Add("@ID", project.Id);

                // Basic project information
                parameters.Add("@Name", project.Name);
                parameters.Add("@PIDNumber", project.PIDNumber);
                parameters.Add("@CapRegion", project.CapRegion);
                parameters.Add("@CapScope", project.CapScope);
                parameters.Add("@CapType", (int)project.CapType);
                parameters.Add("@DrydockLocation", project.DrydockLocation);
                parameters.Add("@DrydockStart", project.DrydockStart);
                parameters.Add("@DrydockEnd", project.DrydockEnd);
                parameters.Add("@SpecialHull", project.SpecialHull);
                parameters.Add("@Fatigue", project.Fatigue);
                parameters.Add("@ExpenseMarkup", project.ExpenseMarkup);
                parameters.Add("@SurveyDayrateClient", project.SurveyDayrateClient);
                parameters.Add("@SurveyDayrateAbs", project.SurveyDayrateAbs);
                parameters.Add("@SedRate1", project.SedRate1);
                parameters.Add("@SedRate2", project.SedRate2);
                parameters.Add("@AgreementNumber", project.AgreementNumber);
                parameters.Add("@ProposalRev", project.ProposalRev ?? 0);
                parameters.Add("@CapContractValue", project.CapContractValue);
                parameters.Add("@AgreementOwner", project.AgreementOwner);
                parameters.Add("@AgreementDate", project.AgreementDate);
                parameters.Add("@AgreementSignedDate", project.AgreementSignedDate);
                parameters.Add("@CertificateGrade", project.CertificateGrade);
                parameters.Add("@ProjectComments", project.ProjectComments);

                // Company information
                parameters.Add("@CompanyName", project.CompanyName);
                parameters.Add("@CompanyAddress", project.CompanyAddress);
                parameters.Add("@CompanyBillingEmail", project.CompanyBillingEmail);
                parameters.Add("@CompanyBillingSystemUrl", project.CompanyBillingSystemUrl);
                parameters.Add("@BillSameAsCapClient", project.BillSameAsCapClient);
                parameters.Add("@BillToCompanyName", project.BillToCompanyName);
                parameters.Add("@BillToCompanyAddress", project.BillToCompanyAddress);
                parameters.Add("@BillToCompanyEmail", project.BillToCompanyEmail);
                parameters.Add("@BillToCompanyBillingUrl", project.BillToCompanyBillingUrl);
                parameters.Add("@PocName", project.PocName);
                parameters.Add("@PocEmail", project.PocEmail);
                parameters.Add("@PocPhone", project.PocPhone);

                // Vessel information
                parameters.Add("@IMO", project.IMO);
                parameters.Add("@VesselName", project.VesselName);
                // ShipType: stored procedure parameter is varchar but expects integer value as string
                parameters.Add("@ShipType", project.ShipType != default(ShipType) ? ((int)project.ShipType).ToString() : null);
                parameters.Add("@WCN", project.WCN);
                parameters.Add("@ClassSociety", project.ClassSociety != null ? string.Join(",", project.ClassSociety) : null);
                parameters.Add("@AbsClassID", project.AbsClassID);
                parameters.Add("@SisterVessel", project.SisterVessel);
                parameters.Add("@SisterVesselImoNumber", project.SisterVesselImoNumber);
                parameters.Add("@SisterVesselName", project.SisterVesselName);
                parameters.Add("@Builder", project.Builder);
                parameters.Add("@HullNumber", project.HullNumber);
                parameters.Add("@MonthBuilt", project.MonthBuilt);
                parameters.Add("@YearBuilt", project.YearBuilt);
                parameters.Add("@LengthOverall", project.LengthOverall);
                parameters.Add("@CRMOpportunityNumber", project.CRMOpportunityNumber);

                // Project status and priority
                parameters.Add("@ProjectStatus", project.ProjectStatus);
                parameters.Add("@ProjectPriority", project.ProjectPriority);
                parameters.Add("@User", user);
                parameters.Add("@PotentialDrydockDate", project.PotentialDrydockDate);
                parameters.Add("@SurveyFirstVisit", project.SurveyFirstVisit);
                parameters.Add("@SurveyLastVisit", project.SurveyLastVisit);

                // Client profile information
                parameters.Add("@ClientProfileId", project.ClientProfileId);
                parameters.Add("@BillToClientProfileId", project.BillToClientProfileId);
                parameters.Add("@BillToWCN", project.BillToWCN);

                // Surveyor information
                parameters.Add("@LeadSurveyor", project.LeadSurveyor);
                parameters.Add("@LeadSurveyorEID", project.LeadSurveyorEID);
                parameters.Add("@SecondSurveyor", project.SecondSurveyor != null ? string.Join(",", project.SecondSurveyor) : null);
                parameters.Add("@SecondSurveyorEID", project.SecondSurveyorEID);
                parameters.Add("@ThirdSurveyor", project.ThirdSurveyor);
                parameters.Add("@ThirdSurveyorEID", project.ThirdSurveyorEID);
                parameters.Add("@FourthSurveyor", project.FourthSurveyor);
                parameters.Add("@FourthSurveyorEID", project.FourthSurveyorEID);
                parameters.Add("@AdditionalSurveyors", project.AdditionalSurveyors);

                // Additional fields
                parameters.Add("@ClassSocietyOther", project.ClassSocietyOther);
                parameters.Add("@CapScopeOther", project.CapScopeOther);
                parameters.Add("@VesselType", project.VesselType);
                parameters.Add("@CopyingVesselID", project.CopyingVesselID);
                parameters.Add("@IsDefaultTank", project.IsDefaultTank);
                parameters.Add("@TankVesselType", project.TankVesselType);
                parameters.Add("@TankVesselIMO", project.TankVesselIMO);

                await Connection.ExecuteAsync(
                    sql: "CAP.Update_Project",
                    param: parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: Transaction
                );
                this.Commit();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error updating project with ID {project.Id}");
                throw;
            }
        }
        public async Task<string> GetProjectName(int id)
        {
            var result = await Connection.QueryFirstOrDefaultAsync<string>(
                sql: "CAP.Read_Project_Name_ById",
                param: new { ID = id },
                transaction: Transaction,
                commandType: CommandType.StoredProcedure
                );

            return result;

        }

        public async Task UpdateProjectStatus(int id, string currentUserId, int projectStatus, string currentUser)
        {

            await Connection.ExecuteAsync(
                sql: "CAP.Update_Project_Status_Manual",
                param: new
                {
                    Id = id,
                    ProjectStatus = projectStatus,
                    StatusChangedBy = currentUserId,
                    User = currentUser
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction,
                commandTimeout: 120);
            this.Commit();
        }

        public async Task DeleteProject(int id, string currentUserId, string currentUser)
        {

            await Connection.ExecuteAsync(
                sql: "CAP.Delete_Project",
                param: new
                {
                    Id = id,
                    User = currentUser
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);
            this.Commit();
        }

        public async Task<string> GetIMONumberByProjectId(int projectId)
        {

            var result = await Connection.QueryAsync<string>(
                sql: "CAP.Read_IMONumber_ByProjectId",
                new { ProjectId = projectId },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);

            return result.FirstOrDefault();

        }

        public async Task<List<AllProjectsGridExport>> GetAllProjectsGridExport()
        {
            var result = await Connection.QueryAsync<AllProjectsGridExport>(
                sql: "CAP.Read_Projects_Grid_Export",
                param: null,
                commandType: CommandType.StoredProcedure,
                transaction: Transaction,
                commandTimeout: 120
            );

            var list = result?.ToList() ?? new List<AllProjectsGridExport>();

            foreach (var p in list)
            {
                if (string.IsNullOrWhiteSpace(p.CapRegion) ||
                    !FormValues.CAPRegion.Contains(p.CapRegion))
                {
                    p.CapRegion = FormValues.CAPRegion.FirstOrDefault() ?? "";
                }
            }

            return list;
        }

        public async Task<IEnumerable<ProjectDetailItem>> GetDetails(int id)
        {

            var result = await Connection.QueryAsync<ProjectDetailItem>(
                sql: "CAP.Read_Project_Details",
                param: new { ProjectId = id },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);

            return result;

        }
        public async Task<ProjectSummary> GetSummary(int id)
        {

            var result = await Connection.QueryFirstOrDefaultAsync<ProjectSummary>(
                sql: "CAP.Read_Project_Summary_ById",
                param: new { ID = id },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction);

            return result;

        }


        public async Task UpdatePriority(int id, int? priority, string currentUser)
        {
            await Connection.ExecuteAsync(
                sql: "CAP.Update_Project_Priority",
                param: new
                {
                    Id = id,
                    Priority = priority,
                    User = currentUser
                },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );
            this.Commit();
        }

        public async Task<IEnumerable<AllProjectsGrid>> GetAllProjectsGridData()
        {
            var result = await Connection.QueryAsync<AllProjectsGrid>(
                sql: "CAP.Read_Projects_Grid",
                commandType: CommandType.StoredProcedure,
                transaction: Transaction,
                commandTimeout: 60
            );
            return result;
        }

        public async Task<string> GetProjectVesselType(int id)
        {
            var result = await Connection.QueryFirstOrDefaultAsync<string>(
                   sql: "CAP.ReadProjectVesselTypeById",
                   param: new { ID = id },
                   commandType: CommandType.StoredProcedure,
                   transaction: Transaction);

            return result;
        }

        //For ClientProfiles

        public async Task<IEnumerable<ClientProfileDTO>> GetProjectCountGroupedByClientIdAsync(CancellationToken cancellationToken = default)
        {
            var result = await Connection.QueryAsync<ClientProfileDTO>(
            sql: "[CAP].[Read_Project_Count_Grouped_ByClientId]",
            commandType: CommandType.StoredProcedure,
            transaction: Transaction
    );

            return result;
        }


        public async Task<IEnumerable<ProjectsByClientId>> GetProjectsByClientIdAsync(Guid clientId)
        {
            var sql = @"
                SELECT 
                p.ID,
                p.Name,
                p.PIDNumber,
                p.SurveyFirstVisit,
                p.SurveyLastVisit,
                s.Status AS StatusName
                FROM CAP.Projects p
                LEFT JOIN CAP.Statuses s ON p.ProjectStatus = s.Id
                WHERE p.ClientProfileId = @ClientId";

            return await _unitOfWork.Connection.QueryAsync<ProjectsByClientId>(
                sql,
                new { ClientId = clientId },
                _unitOfWork.Transaction);
        }


        public async Task<List<Project>> GetAllProjects(string user = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@username", user, DbType.String);

            var result = await Connection.QueryAsync<Project>(
                sql: "CAP.Read_Project",
                param: new { username = user },
                commandType: CommandType.StoredProcedure,
                transaction: Transaction
            );

            return result.ToList();
        }

        public async Task<ProjectStats> GetProjectStats()
        {
            var connection = _unitOfWork?.Connection;
            if (connection.State != ConnectionState.Open)
                connection.Open();

            var transaction = _unitOfWork?.Transaction;
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;
            }

            using (var multi = await connection.QueryMultipleAsync(
                sql: "CAP.Get_Projects_Stats",
                commandType: CommandType.StoredProcedure,
                transaction: transaction,
                commandTimeout: 60
            ))
            {
                var stats = await multi.ReadFirstOrDefaultAsync<ProjectStats>() ?? new ProjectStats();
                
                var regions = await multi.ReadAsync<dynamic>();
                foreach (var r in regions)
                {
                    if (r.RegionName != null)
                    {
                        stats.Regions[r.RegionName.ToString()] = (int)r.RegionCount;
                    }
                }
                
                var statuses = await multi.ReadAsync<dynamic>();
                foreach (var s in statuses)
                {
                    if (s.StatusName != null)
                    {
                        stats.Statuses[s.StatusName.ToString()] = (int)s.StatusCount;
                    }
                }

                return stats;
            }
        }
    }
}
