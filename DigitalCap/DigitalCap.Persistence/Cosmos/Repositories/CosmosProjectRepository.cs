using DigitalCap.Core.DTO;
using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosProjectRepository : CosmosRepositoryBase<ProjectDocument>, IProjectRepository
    {
        private readonly ISequenceService _sequenceService;
        private readonly CosmosActivityWriter _activityWriter;
        private readonly ProjectStatusRecalculator _recalculator;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly Container _statusesContainer;
        private readonly Container _tasksContainer;
        private readonly Container _ptsContainer;
        private readonly Container _gradesContainer;
        private readonly Container _commentsContainer;
        private readonly Container _activitiesContainer;
        private readonly Container _portOfCallContainer;
        private readonly Container _vesselTypeContainer;
        private readonly Container _isSynchedContainer;
        private readonly Container _userLoginsContainer;
        private readonly Container _tankMappingContainer;
        private readonly Container _gradingMappingContainer;
        private readonly Container _imageDescContainer;
        private readonly Container _clientsContainer;

        public CosmosProjectRepository(
            ICosmosContainerFactory containerFactory,
            ISequenceService sequenceService,
            CosmosActivityWriter activityWriter,
            ProjectStatusRecalculator recalculator,
            UserManager<ApplicationUser> userManager,
            ILogger<CosmosProjectRepository> logger)
            : base(containerFactory, CosmosContainers.CAP_Projects, logger)
        {
            _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
            _activityWriter = activityWriter ?? throw new ArgumentNullException(nameof(activityWriter));
            _recalculator = recalculator ?? throw new ArgumentNullException(nameof(recalculator));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

            _statusesContainer = containerFactory.GetContainer(CosmosContainers.CAP_Statuses);
            _tasksContainer = containerFactory.GetContainer(CosmosContainers.CAP_Tasks);
            _ptsContainer = containerFactory.GetContainer(CosmosContainers.CAP_ProjectTaskStatus);
            _gradesContainer = containerFactory.GetContainer(CosmosContainers.CAP_ProjectGrades);
            _commentsContainer = containerFactory.GetContainer(CosmosContainers.CAP_Comments);
            _activitiesContainer = containerFactory.GetContainer(CosmosContainers.CAP_Activities);
            _portOfCallContainer = containerFactory.GetContainer(CosmosContainers.core_PortOfCall);
            _vesselTypeContainer = containerFactory.GetContainer(CosmosContainers.dbo_VesselType);
            _isSynchedContainer = containerFactory.GetContainer(CosmosContainers.dbo_IsSynchedOnline);
            _userLoginsContainer = containerFactory.GetContainer(CosmosContainers.dbo_AspNetUserLogins);
            _tankMappingContainer = containerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            _gradingMappingContainer = containerFactory.GetContainer(CosmosContainers.dbo_Vessel_GradingMapping);
            _imageDescContainer = containerFactory.GetContainer(CosmosContainers.dbo_ImageDescriptions);
            _clientsContainer = containerFactory.GetContainer(CosmosContainers.CAP_Clients);
        }

        public void Commit() { }

        public async Task<bool> CheckProjectExistsByImoNumber(int imoNumber)
        {
            try
            {
                var result = await Sp_Read_ProjectExists_ByImoNumber(imoNumber);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> CreateProject(Project model, string currentUser)
        {
            try
            {
                var createdBy = (await _userManager.FindByEmailAsync(currentUser))?.Id;
                var user = currentUser ?? "SYSTEM";

                var projectId = await Sp_Create_Project(model, user, createdBy, isCap2: true);
                Commit();
                return projectId;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error creating project");
                throw;
            }
        }

        public async Task<List<SelectListItem>> GetProjectListByIMO(int? imoNumber)
        {
            try
            {
                return (await Sp_GetProjectListByIMO(imoNumber)).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Project> GetProject(int id)
        {
            try
            {
                var result = await Sp_Read_Project_ById(id);
                if (result == null)
                    return null!;

                T GetValue<T>(dynamic obj, string propertyName, T defaultValue = default(T)!)
                {
                    try
                    {
                        var dict = obj as IDictionary<string, object>;
                        if (dict != null)
                        {
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
                    }
                    catch { }

                    return defaultValue;
                }

                T? GetNullableValue<T>(dynamic obj, string propertyName) where T : struct
                {
                    try
                    {
                        var dict = obj as IDictionary<string, object>;
                        if (dict != null)
                        {
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
                    }
                    catch { }

                    return null;
                }

                var project = new Project
                {
                    Id = GetValue<int>(result, "ID", 0),
                    Name = GetValue<string>(result, "Name")!,
                    PIDNumber = GetNullableValue<int>(result, "PIDNumber"),
                    CapRegion = GetValue<string>(result, "CapRegion")!,
                    CapScope = GetValue<string>(result, "CapScope")!,
                    CapScopeOther = GetValue<string>(result, "CapScopeOther")!,
                    CapType = (CapType)GetValue<int>(result, "CapType", 0),
                    DrydockLocation = GetNullableValue<int>(result, "DrydockLocation"),
                    DrydockStart = GetNullableValue<DateTime>(result, "DrydockStart"),
                    DrydockEnd = GetNullableValue<DateTime>(result, "DrydockEnd"),
                    SpecialHull = GetValue<bool>(result, "SpecialHull", true),
                    Fatigue = GetValue<bool>(result, "FatigueAnalysis", true),
                    ExpenseMarkup = GetNullableValue<int>(result, "ExpenseMarkup"),
                    SurveyDayrateClient = GetNullableValue<decimal>(result, "SurveyDayrateClient"),
                    SurveyDayrateAbs = GetNullableValue<decimal>(result, "SurveyDayrateAbs"),
                    SedRate1 = GetNullableValue<decimal>(result, "SedRate1"),
                    SedRate2 = GetNullableValue<decimal>(result, "SedRate2"),
                    AgreementNumber = GetValue<string>(result, "AgreementNumber")!,
                    ProposalRev = GetNullableValue<int>(result, "ProposalRev"),
                    CapContractValue = GetNullableValue<decimal>(result, "CapContractValue"),
                    AgreementOwner = GetValue<string>(result, "AgreementOwner")!,
                    AgreementDate = GetNullableValue<DateTime>(result, "AgreementDate"),
                    AgreementSignedDate = GetNullableValue<DateTime>(result, "AgreementSignedDate"),
                    CertificateGrade = GetValue<string>(result, "CertificateGrade")!,
                    ProjectComments = GetValue<string>(result, "ProjectComments")!,
                    CompanyName = GetValue<string>(result, "CompanyName")!,
                    CompanyAddress = GetValue<string>(result, "CompanyAddress")!,
                    CompanyBillingEmail = GetValue<string>(result, "CompanyBillingEmail")!,
                    CompanyBillingSystemUrl = GetValue<string>(result, "CompanyBillingSystemUrl")!,
                    BillSameAsCapClient = GetValue<bool>(result, "BillSameAsCapClient", true),
                    BillToCompanyName = GetValue<string>(result, "BillToCompanyName")!,
                    BillToCompanyAddress = GetValue<string>(result, "BillToCompanyAddress")!,
                    BillToCompanyEmail = GetValue<string>(result, "BillToCompanyEmail")!,
                    BillToCompanyBillingUrl = GetValue<string>(result, "BillToCompanyBillingUrl")!,
                    PocName = GetValue<string>(result, "PocName")!,
                    PocEmail = GetValue<string>(result, "PocEmail")!,
                    PocPhone = GetValue<string>(result, "PocPhone")!,
                    IMO = GetNullableValue<int>(result, "IMO"),
                    VesselName = GetValue<string>(result, "VesselName")!,
                    ShipType = (ShipType)GetValue<int>(result, "VesselTypeId", 0),
                    WCN = GetValue<string>(result, "WCN")!,
                    ClassSociety = GetValue<string>(result, "ClassSociety")?.Split(',')!,
                    AbsClassID = GetValue<string>(result, "AbsClassID")!,
                    SisterVessel = GetValue<bool>(result, "SisterVessel", false),
                    SisterVesselImoNumber = GetValue<string>(result, "SisterVesselImoNumber")!,
                    SisterVesselName = GetValue<string>(result, "SisterVesselName")!,
                    Builder = GetValue<string>(result, "Builder")!,
                    HullNumber = GetValue<string>(result, "HullNumber")!,
                    MonthBuilt = GetNullableValue<int>(result, "MonthBuilt"),
                    YearBuilt = GetNullableValue<int>(result, "YearBuilt"),
                    LengthOverall = GetNullableValue<decimal>(result, "LengthOverall"),
                    CRMOpportunityNumber = GetValue<string>(result, "CRMOpportunityNumber")!,
                    ProjectStatus = GetNullableValue<int>(result, "ProjectStatus"),
                    ProjectPriority = GetNullableValue<int>(result, "ProjectPriority"),
                    CreatedDate = GetNullableValue<DateTime>(result, "CreatedDate"),
                    LastModifiedBy = GetValue<string>(result, "LastModifiedBy")!,
                    PotentialDrydockDate = GetNullableValue<DateTime>(result, "PotentialDrydockDate"),
                    SurveyFirstVisit = GetNullableValue<DateTime>(result, "SurveyFirstVisit"),
                    SurveyLastVisit = GetNullableValue<DateTime>(result, "SurveyLastVisit"),
                    ClientProfileId = GetNullableValue<Guid>(result, "ClientProfileId"),
                    BillToClientProfileId = GetNullableValue<Guid>(result, "BillToClientProfileId"),
                    BillToWCN = GetValue<string>(result, "BillToWCN")!,
                    LeadSurveyor = GetValue<string>(result, "CreatedByDisplayName") ?? GetValue<string>(result, "LeadSurveyor")!,
                    LeadSurveyorEID = GetValue<string>(result, "LeadSurveyorEID")!,
                    SecondSurveyor = GetValue<string>(result, "SecondSurveyor")?.Split(',')!,
                    SecondSurveyorEID = GetValue<string>(result, "SecondSurveyorEID")!,
                    ThirdSurveyor = GetValue<string>(result, "ThirdSurveyor")!,
                    ThirdSurveyorEID = GetValue<string>(result, "ThirdSurveyorEID")!,
                    FourthSurveyor = GetValue<string>(result, "FourthSurveyor")!,
                    FourthSurveyorEID = GetValue<string>(result, "FourthSurveyorEID")!,
                    AdditionalSurveyors = GetValue<bool>(result, "AdditionalSurveyors", false),
                    ClassSocietyOther = GetValue<string>(result, "ClassSocietyOther")!,
                    CreatedBy = GetValue<string>(result, "CreatedBy")!,
                    StatusChangedBy = GetValue<string>(result, "StatusChangedByDisplayName") ?? GetValue<string>(result, "StatusChangedBy")!,
                    StatusChangedDate = GetNullableValue<DateTimeOffset>(result, "StatusChangedDate"),
                    VesselType = GetValue<string>(result, "VesselType")!,
                    CopyingVesselID = GetNullableValue<int>(result, "CopyingVesselID"),
                    IsDefaultTank = GetNullableValue<bool>(result, "IsDefaultTank"),
                    TankVesselType = GetValue<string>(result, "TankVesselType")!,
                    TankVesselIMO = GetValue<int>(result, "TankVesselIMO", 0)
                };

                return project;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error getting project {ProjectId}", id);
                throw;
            }
        }

        public async Task<int> UpdatePercentComplete(int projectId, byte percentComplete, string currentUser)
        {
            try
            {
                var user = currentUser ?? "SYSTEM";
                await Sp_Update_Project_Percent_Complete(projectId, percentComplete, user);
                Commit();
                return 1;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error updating percent complete for project {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<bool> CheckProjectExistsByProjectName(string projectName, int imoNumber, int vesseltypeid)
        {
            try
            {
                var result = await Sp_Read_ProjectExists_ByProjectName(projectName, imoNumber, vesseltypeid);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task Update_Project(Project model, string currentUser)
        {
            try
            {
                var user = currentUser ?? "SYSTEM";
                await Sp_Update_Project(model, user);
                Commit();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error updating project {ProjectId}", model.Id);
                throw;
            }
        }

        public async Task<string> GetProjectName(int id)
        {
            try
            {
                var result = await Sp_Read_Project_Name_ById(id);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UpdateProjectStatus(int id, string currentUserId, int projectStatus, string currentUser)
        {
            try
            {
                var user = currentUser ?? "SYSTEM";
                await Sp_Update_Project_Status_Manual(id, projectStatus, currentUserId, user);
                Commit();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error updating project status for project {ProjectId}", id);
                throw;
            }
        }

        public async Task DeleteProject(int id, string currentUserId, string currentUser)
        {
            try
            {
                var user = currentUser ?? "SYSTEM";
                await Sp_Delete_Project(id, user);
                Commit();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error deleting project {ProjectId}", id);
                throw;
            }
        }

        public async Task<string> GetIMONumberByProjectId(int projectId)
        {
            try
            {
                var result = await Sp_Read_IMONumber_ByProjectId(projectId);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<ProjectDetailItem>> GetDetails(int id)
        {
            try
            {
                var result = await Sp_Read_Project_Details(id);
                return result;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error getting project details for project {ProjectId}", id);
                throw;
            }
        }

        public async Task<ProjectSummary> GetSummary(int id)
        {
            try
            {
                var result = await Sp_Read_Project_Summary_ById(id);
                return result;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error getting project summary for project {ProjectId}", id);
                throw;
            }
        }

        public async Task UpdatePriority(int id, int? priority, string currentUser)
        {
            try
            {
                var user = currentUser ?? "SYSTEM";
                await Sp_Update_Project_Priority(id, priority, user);
                Commit();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error updating priority for project {ProjectId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<AllProjectsGrid>> GetAllProjectsGridData()
        {
            try
            {
                var result = await Sp_Read_Projects_Grid();
                return result;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error getting all projects grid data");
                throw;
            }
        }

        public async Task<List<AllProjectsGridExport>> GetAllProjectsGridExport()
        {
            try
            {
                var result = await Sp_Read_Projects_Grid_Export();
                return result;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error getting all projects grid export");
                throw;
            }
        }

        public async Task<string> GetProjectVesselType(int id)
        {
            try
            {
                var result = await Sp_ReadProjectVesselTypeById(id);
                return result;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error getting project vessel type for project {ProjectId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ProjectsByClientId>> GetProjectsByClientIdAsync(Guid clientId)
        {
            try
            {
                var result = await Sp_GetProjectsByClientId(clientId);
                return result;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error getting projects by client ID {ClientId}", clientId);
                throw;
            }
        }

        public async Task<IEnumerable<ClientProfileDTO>> GetProjectCountGroupedByClientIdAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await Sp_Read_Project_Count_Grouped_ByClientId(cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error getting project count grouped by client ID");
                throw;
            }
        }

        public async Task<List<Project>> GetAllProjects(string user = null!)
        {
            try
            {
                var result = await Sp_Read_Project_Projects();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ProjectStats> GetProjectStats()
        {
            try
            {
                var result = await Sp_Get_Projects_Stats();
                return result;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error getting project stats");
                throw;
            }
        }

        #region Unused IRepositoryBase Members

        public Task<int> InsertAsync(Project entity) => throw new NotSupportedException("CosmosProjectRepository.InsertAsync is not used by the app and is not converted to Cosmos.");
        public int Insert(Project entity) => throw new NotSupportedException("CosmosProjectRepository.Insert is not used by the app and is not converted to Cosmos.");
        public Task<bool> UpdateAsync(Project entity) => throw new NotSupportedException("CosmosProjectRepository.UpdateAsync is not used by the app and is not converted to Cosmos.");
        public bool Update(Project entity) => throw new NotSupportedException("CosmosProjectRepository.Update is not used by the app and is not converted to Cosmos.");
        public Task<bool> DeleteAsync(int id) => throw new NotSupportedException("CosmosProjectRepository.DeleteAsync is not used by the app and is not converted to Cosmos.");
        public bool Delete(int id) => throw new NotSupportedException("CosmosProjectRepository.Delete is not used by the app and is not converted to Cosmos.");
        public Task<Project?> GetAsync(int id, bool includeDeleted = false, bool useTransaction = true) => throw new NotSupportedException("CosmosProjectRepository.GetAsync is not used by the app and is not converted to Cosmos.");
        public Project Get(int id, bool includeDeleted = false, bool useTransaction = true) => throw new NotSupportedException("CosmosProjectRepository.Get is not used by the app and is not converted to Cosmos.");
        public Task<IQueryable<Project>> GetAllAsync(bool useTransaction = true) => throw new NotSupportedException("CosmosProjectRepository.GetAllAsync is not used by the app and is not converted to Cosmos.");
        public IQueryable<Project> GetFiltered(Expression<Func<Project, bool>> filter) => throw new NotSupportedException("CosmosProjectRepository.GetFiltered is not used by the app and is not converted to Cosmos.");
        public Project FirstOrDefault(Expression<Func<Project, bool>> filter) => throw new NotSupportedException("CosmosProjectRepository.FirstOrDefault is not used by the app and is not converted to Cosmos.");
        public Project First(Expression<Func<Project, bool>> filter) => throw new NotSupportedException("CosmosProjectRepository.First is not used by the app and is not converted to Cosmos.");
        public IQueryable<Project> ExecuteQuery(string sql) => throw new NotSupportedException("CosmosProjectRepository.ExecuteQuery is not used by the app and is not converted to Cosmos.");
        public IQueryable<Project> ExecuteQuery(string sql, object[] parameters) => throw new NotSupportedException("CosmosProjectRepository.ExecuteQuery is not used by the app and is not converted to Cosmos.");
        public void Rollback() => throw new NotSupportedException("CosmosProjectRepository.Rollback is not used by the app and is not converted to Cosmos.");

        #endregion

        #region Stored Procedure Helpers

        /// <summary>
        /// Replaces [CAP].[Read_ProjectExists_ByImoNumber].
        /// </summary>
        private async Task<bool> Sp_Read_ProjectExists_ByImoNumber(int imoNumber)
        {
            var query = new QueryDefinition("SELECT TOP 1 c.IMO FROM c WHERE c.IMO = @imo").WithParameter("@imo", imoNumber);
            var items = await QueryAsync(query);
            return items.Count > 0;
        }

        /// <summary>
        /// Replaces [CAP].[Create_Project].
        /// Trap 6: Insert with defaults, new id from sequence, status 7/8, create tasks (if none exist), create activity, create grades.
        /// </summary>
        private async Task<int> Sp_Create_Project(Project model, string user, string? createdBy, bool isCap2)
        {
            var newId = await _sequenceService.GetNextAsync(CosmosContainers.CAP_Projects);

            // Status: 7 if PIDNumber is null else 8
            int status = model.PIDNumber == null ? 7 : 8;

            var doc = new ProjectDocument
            {
                id = newId.ToString(),
                ID = newId,
                IdProp = newId,
                Name = model.Name,
                PIDNumber = model.PIDNumber,
                CapRegion = model.CapRegion,
                CapScope = model.CapScope,
                CapScopeOther = model.CapScopeOther,
                CapType = (int)model.CapType,
                DrydockLocation = model.DrydockLocation,
                DrydockStart = model.DrydockStart,
                DrydockEnd = model.DrydockEnd,
                SpecialHull = model.SpecialHull,
                FatigueAnalysis = model.Fatigue,
                ExpenseMarkup = model.ExpenseMarkup,
                SurveyDayRateClient = model.SurveyDayrateClient,
                SurveyDayRateAbs = model.SurveyDayrateAbs,
                SedRate1 = model.SedRate1,
                SedRate2 = model.SedRate2,
                AgreementNumber = model.AgreementNumber,
                ProposalRev = model.ProposalRev ?? 0,
                CapContractValue = model.CapContractValue,
                AgreementOwner = model.AgreementOwner,
                AgreementDate = model.AgreementDate,
                AgreementSignedDate = model.AgreementSignedDate,
                CertificateGrade = model.CertificateGrade,
                ProjectComments = model.ProjectComments,
                CompanyName = model.CompanyName,
                CompanyAddress = model.CompanyAddress,
                CompanyBillingEmail = model.CompanyBillingEmail,
                CompanyBillingSystemUrl = model.CompanyBillingSystemUrl,
                BillSameAsCapClient = model.BillSameAsCapClient,
                BillToCompanyName = model.BillToCompanyName,
                BillToCompanyAddress = model.BillToCompanyAddress,
                BillToCompanyEmail = model.BillToCompanyEmail,
                BillToCompanyBillingUrl = model.BillToCompanyBillingUrl,
                PocName = model.PocName,
                PocEmail = model.PocEmail,
                PocPhone = model.PocPhone,
                IMO = model.IMO,
                VesselName = model.VesselName,
                ShipType = model.ShipType != default(ShipType) ? ((int)model.ShipType).ToString() : null,
                WCN = model.WCN,
                ClassSociety = model.ClassSociety != null ? string.Join(",", model.ClassSociety) : null,
                AbsClassID = model.AbsClassID,
                SisterVessel = model.SisterVessel,
                SisterVesselImoNumber = model.SisterVesselImoNumber,
                SisterVesselName = model.SisterVesselName,
                Builder = model.Builder,
                HullNumber = model.HullNumber,
                MonthBuilt = model.MonthBuilt,
                YearBuilt = model.YearBuilt,
                LengthOverall = model.LengthOverall,
                CRMOpportunityNumber = model.CRMOpportunityNumber,
                ProjectStatus = status,
                ProjectPriority = null,
                CreatedDate = DateTime.UtcNow,
                LastModifiedBy = user,
                PotentialDrydockDate = model.PotentialDrydockDate,
                SurveyFirstVisit = model.SurveyFirstVisit,
                SurveyLastVisit = model.SurveyLastVisit,
                ClientProfileId = model.ClientProfileId,
                BillToClientProfileId = model.BillToClientProfileId,
                BillToWCN = model.BillToWCN,
                LeadSurveyor = model.LeadSurveyor,
                LeadSurveyorEID = model.LeadSurveyorEID,
                SecondSurveyor = model.SecondSurveyor != null ? string.Join(",", model.SecondSurveyor) : null,
                SecondSurveyorEID = model.SecondSurveyorEID,
                ThirdSurveyor = model.ThirdSurveyor,
                ThirdSurveyorEID = model.ThirdSurveyorEID,
                FourthSurveyor = model.FourthSurveyor,
                FourthSurveyorEID = model.FourthSurveyorEID,
                AdditionalSurveyors = model.AdditionalSurveyors,
                ClassSocietyOther = model.ClassSocietyOther,
                CreatedBy = createdBy,
                VesselType = model.VesselType,
                CopyingVesselID = model.CopyingVesselID,
                IsDefaultTank = model.IsDefaultTank,
                TankVesselType = model.TankVesselType,
                TankVesselIMO = model.TankVesselIMO,
                IsDeleted = false
            };

            await Container.CreateItemAsync(doc, new PartitionKey(doc.id));

            // Nested SPs
            await Sp_Create_Project_Tasks(newId, isCap2);
            await _activityWriter.CreateActivityAsync(newId, "Project Created", user);
            await Sp_Create_Project_Grades(newId);

            return newId;
        }

        /// <summary>
        /// Replaces [CAP].[Create_Project_Tasks].
        /// Does nothing if project already has tasks; else one PTS row per task.
        /// </summary>
        private async Task Sp_Create_Project_Tasks(int projectId, bool isCap2)
        {
            var pk = new PartitionKey((double)projectId);
            var existingQuery = new QueryDefinition("SELECT TOP 1 c.id FROM c WHERE c.ProjectId = @projectId")
                .WithParameter("@projectId", projectId);

            using (var feed = _ptsContainer.GetItemQueryIterator<ProjectTaskStatusDocument>(existingQuery, requestOptions: new QueryRequestOptions { PartitionKey = pk }))
            {
                if (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    if (resp.Count > 0)
                        return;
                }
            }

            var tasksQuery = isCap2
                ? new QueryDefinition("SELECT * FROM c WHERE IS_DEFINED(c.Cap2Name) AND NOT IS_NULL(c.Cap2Name)")
                : new QueryDefinition("SELECT * FROM c");

            var tasks = new List<TaskDocument>();
            using (var feed = _tasksContainer.GetItemQueryIterator<TaskDocument>(tasksQuery))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    tasks.AddRange(resp);
                }
            }

            foreach (var t in tasks)
            {
                var ptsId = await _sequenceService.GetNextAsync(CosmosContainers.CAP_ProjectTaskStatus);
                var ptsDoc = new ProjectTaskStatusDocument
                {
                    id = ptsId.ToString(),
                    ID = ptsId,
                    ProjectId = projectId,
                    StatusId = t.InitialStatus,
                    TaskId = t.Id,
                    IsDeleted = false
                };
                await _ptsContainer.CreateItemAsync(ptsDoc, pk);
            }
        }

        /// <summary>
        /// Replaces [CAP].[Create_Project_Grades].
        /// </summary>
        private async Task Sp_Create_Project_Grades(int projectId)
        {
            var gradeId = await _sequenceService.GetNextAsync(CosmosContainers.CAP_ProjectGrades);
            var gradeDoc = new ProjectGradeDocument
            {
                id = gradeId.ToString(),
                Id = gradeId,
                ProjectId = projectId,
                IsDeleted = false
            };
            await _gradesContainer.CreateItemAsync(gradeDoc, new PartitionKey((double)projectId));
        }

        /// <summary>
        /// Replaces [CAP].[GetProjectListByIMO].
        /// Trap 13: DISTINCT, IMO = @IMO AND IsDeleted = 0, Id NOT IN (IsSynchedOnline where IsSynched = 0), ORDER BY Id.
        /// </summary>
        private async Task<List<SelectListItem>> Sp_GetProjectListByIMO(int? imoNumber)
        {
            if (!imoNumber.HasValue)
            {
                return new List<SelectListItem>();
            }

            var query = new QueryDefinition("SELECT * FROM c WHERE c.IMO = @imo AND c.IsDeleted = false")
                .WithParameter("@imo", imoNumber.Value);

            var projects = await QueryAsync(query);

            // Read unsynced project IDs from dbo_IsSynchedOnline
            var unsyncedQuery = new QueryDefinition("SELECT c.ProjectId FROM c WHERE c.IsSynched = false");
            var unsyncedIds = new HashSet<int>();
            using (var feed = _isSynchedContainer.GetItemQueryIterator<IsSynchedOnlineDocument>(unsyncedQuery))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var doc in resp)
                    {
                        if (doc.ProjectId.HasValue)
                            unsyncedIds.Add(doc.ProjectId.Value);
                    }
                }
            }

            return projects
                .Where(p => p.IMO.HasValue && !unsyncedIds.Contains(p.EffectiveId))
                .OrderBy(p => p.EffectiveId)
                .Select(p => new SelectListItem
                {
                    Value = p.EffectiveId.ToString(),
                    Text = p.Name ?? string.Empty
                })
                .GroupBy(x => (x.Value, x.Text))
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>
        /// Replaces [CAP].[Read_Project_ById].
        /// Trap 11: left joins AspNetUserLogins (CreatedBy & StatusChangedBy), left joins VesselType (case-insensitive text).
        /// Returns case-insensitive dictionary for Dapper dynamic simulation.
        /// </summary>
        private async Task<IDictionary<string, object?>?> Sp_Read_Project_ById(int id)
        {
            var idStr = id.ToString();
            ProjectDocument? p = null;
            try
            {
                var resp = await Container.ReadItemAsync<ProjectDocument>(idStr, new PartitionKey(idStr));
                p = resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            if (p == null || p.IsDeleted != false)
                return null;

            // Logins
            string? createdByDisplayName = null;
            if (!string.IsNullOrEmpty(p.CreatedBy))
            {
                var q = new QueryDefinition("SELECT TOP 1 c.ProviderDisplayName FROM c WHERE c.UserId = @uid")
                    .WithParameter("@uid", p.CreatedBy);
                using var feed = _userLoginsContainer.GetItemQueryIterator<AspNetUserLoginDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(p.CreatedBy) });
                if (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    createdByDisplayName = resp.FirstOrDefault()?.ProviderDisplayName;
                }
            }

            string? statusChangedByDisplayName = null;
            if (!string.IsNullOrEmpty(p.StatusChangedBy))
            {
                var q = new QueryDefinition("SELECT TOP 1 c.ProviderDisplayName FROM c WHERE c.UserId = @uid")
                    .WithParameter("@uid", p.StatusChangedBy);
                using var feed = _userLoginsContainer.GetItemQueryIterator<AspNetUserLoginDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(p.StatusChangedBy) });
                if (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    statusChangedByDisplayName = resp.FirstOrDefault()?.ProviderDisplayName;
                }
            }

            // VesselType
            int? vesselTypeId = null;
            if (!string.IsNullOrEmpty(p.VesselType))
            {
                var q = new QueryDefinition("SELECT TOP 1 c.Id FROM c WHERE STRINGEQUALS(c.VesselType, @vt, true)")
                    .WithParameter("@vt", p.VesselType);
                using var feed = _vesselTypeContainer.GetItemQueryIterator<VesselTypeDocument>(q);
                if (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    vesselTypeId = resp.FirstOrDefault()?.Id;
                }
            }

            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ID"] = p.EffectiveId,
                ["Name"] = p.Name,
                ["PIDNumber"] = p.PIDNumberInt,
                ["CapRegion"] = p.CapRegion,
                ["CapScope"] = p.CapScope,
                ["CapScopeOther"] = p.CapScopeOther,
                ["CapType"] = p.CapType,
                ["DrydockLocation"] = p.DrydockLocation,
                ["DrydockStart"] = p.DrydockStart,
                ["DrydockEnd"] = p.DrydockEnd,
                ["SpecialHull"] = p.SpecialHull,
                ["FatigueAnalysis"] = p.FatigueAnalysis,
                ["ExpenseMarkup"] = p.ExpenseMarkup,
                ["SurveyDayrateClient"] = p.SurveyDayRateClient,
                ["SurveyDayrateAbs"] = p.SurveyDayRateAbs,
                ["SedRate1"] = p.SedRate1,
                ["SedRate2"] = p.SedRate2,
                ["AgreementNumber"] = p.AgreementNumber,
                ["ProposalRev"] = p.ProposalRev,
                ["CapContractValue"] = p.CapContractValue,
                ["AgreementOwner"] = p.AgreementOwner,
                ["AgreementDate"] = p.AgreementDate,
                ["AgreementSignedDate"] = p.AgreementSignedDate,
                ["CertificateGrade"] = p.CertificateGrade,
                ["ProjectComments"] = p.ProjectComments,
                ["CompanyName"] = p.CompanyName,
                ["CompanyAddress"] = p.CompanyAddress,
                ["CompanyBillingEmail"] = p.CompanyBillingEmail,
                ["CompanyBillingSystemUrl"] = p.CompanyBillingSystemUrl,
                ["BillSameAsCapClient"] = p.BillSameAsCapClient,
                ["BillToCompanyName"] = p.BillToCompanyName,
                ["BillToCompanyAddress"] = p.BillToCompanyAddress,
                ["BillToCompanyEmail"] = p.BillToCompanyEmail,
                ["BillToCompanyBillingUrl"] = p.BillToCompanyBillingUrl,
                ["PocName"] = p.PocName,
                ["PocEmail"] = p.PocEmail,
                ["PocPhone"] = p.PocPhone,
                ["IMO"] = p.IMO,
                ["VesselName"] = p.VesselName,
                ["ShipType"] = p.ShipType,
                ["WCN"] = p.WCN,
                ["ClassSociety"] = p.ClassSociety,
                ["AbsClassID"] = p.AbsClassID,
                ["SisterVessel"] = p.SisterVessel,
                ["SisterVesselImoNumber"] = p.SisterVesselImoNumber,
                ["SisterVesselName"] = p.SisterVesselName,
                ["Builder"] = p.Builder,
                ["HullNumber"] = p.HullNumber,
                ["MonthBuilt"] = p.MonthBuilt,
                ["YearBuilt"] = p.YearBuilt,
                ["LengthOverall"] = p.LengthOverall,
                ["CRMOpportunityNumber"] = p.CRMOpportunityNumber,
                ["ProjectStatus"] = p.ProjectStatus,
                ["ProjectPriority"] = p.ProjectPriority,
                ["CreatedDate"] = p.CreatedDate,
                ["CreatedBy"] = p.CreatedBy,
                ["StatusChangedBy"] = p.StatusChangedBy,
                ["StatusChangedDate"] = p.StatusChangedDate,
                ["ModifiedDate"] = p.ModifiedDate,
                ["LastModifiedBy"] = p.LastModifiedBy,
                ["PotentialDrydockDate"] = p.PotentialDrydockDate,
                ["SurveyFirstVisit"] = p.SurveyFirstVisit,
                ["SurveyLastVisit"] = p.SurveyLastVisit,
                ["ClientProfileId"] = p.ClientProfileId,
                ["BillToClientProfileId"] = p.BillToClientProfileId,
                ["BillToWCN"] = p.BillToWCN,
                ["LeadSurveyor"] = p.LeadSurveyor,
                ["LeadSurveyorEID"] = p.LeadSurveyorEID,
                ["SecondSurveyor"] = p.SecondSurveyor,
                ["SecondSurveyorEID"] = p.SecondSurveyorEID,
                ["ThirdSurveyor"] = p.ThirdSurveyor,
                ["ThirdSurveyorEID"] = p.ThirdSurveyorEID,
                ["FourthSurveyor"] = p.FourthSurveyor,
                ["FourthSurveyorEID"] = p.FourthSurveyorEID,
                ["AdditionalSurveyors"] = p.AdditionalSurveyors,
                ["ClassSocietyOther"] = p.ClassSocietyOther,
                ["VesselType"] = p.VesselType,
                ["CopyingVesselID"] = p.CopyingVesselID,
                ["IsDefaultTank"] = p.IsDefaultTank,
                ["TankVesselType"] = p.TankVesselType,
                ["TankVesselIMO"] = p.TankVesselIMO,
                ["CreatedByDisplayName"] = createdByDisplayName,
                ["StatusChangedByDisplayName"] = statusChangedByDisplayName,
                ["VesselTypeId"] = vesselTypeId
            };

            return dict;
        }

        /// <summary>
        /// Replaces [CAP].[Update_Project_Percent_Complete].
        /// </summary>
        private async Task Sp_Update_Project_Percent_Complete(int id, byte percentComplete, string user)
        {
            var idStr = id.ToString();
            var pk = new PartitionKey(idStr);
            var resp = await Container.ReadItemAsync<ProjectDocument>(idStr, pk);
            if (resp.Resource != null)
            {
                var doc = resp.Resource;
                doc.PercentComplete = percentComplete;
                doc.LastModifiedBy = user;
                await Container.ReplaceItemAsync(doc, idStr, pk);
            }
        }

        /// <summary>
        /// Replaces [CAP].[Read_ProjectExists_ByProjectName].
        /// SQL: SELECT DISTINCT Name FROM [CAP].[Projects] WHERE Name = @ProjectName and IMO = @IMO and ShipType = @VesselTypeID (no IsDeleted filter).
        /// </summary>
        private async Task<bool> Sp_Read_ProjectExists_ByProjectName(string projectName, int imoNumber, int vesseltypeid)
        {
            if (projectName == null)
            {
                return false;
            }

            var query = new QueryDefinition("SELECT * FROM c WHERE c.IMO = @imo")
                .WithParameter("@imo", imoNumber);

            var items = await QueryAsync(query);
            string? matchedName = null;

            foreach (var p in items)
            {
                if (p.Name == null)
                    continue;

                if (!string.Equals(p.Name.TrimEnd(), projectName.TrimEnd(), StringComparison.OrdinalIgnoreCase))
                    continue;

                if (p.ShipType == null)
                    continue;

                var rawShipType = p.ShipType.ToString();
                if (rawShipType == null)
                    continue;

                if (int.TryParse(rawShipType.Trim(), out var parsedSt))
                {
                    if (parsedSt == vesseltypeid)
                    {
                        matchedName = p.Name;
                        break;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Conversion failed when converting the varchar value '{rawShipType}' to data type int.");
                }
            }

            return matchedName != null ? true : false;
        }

        /// <summary>
        /// Replaces [CAP].[Update_Project].
        /// Trap 5: calls ProjectStatusRecalculator.RecalculateAllAsync().
        /// </summary>
        private async Task Sp_Update_Project(Project model, string user)
        {
            var idStr = model.Id.ToString();
            var pk = new PartitionKey(idStr);
            var resp = await Container.ReadItemAsync<ProjectDocument>(idStr, pk);
            if (resp.Resource == null)
                return;

            var doc = resp.Resource;
            doc.Name = model.Name;
            doc.PIDNumber = model.PIDNumber;
            doc.CapRegion = model.CapRegion;
            doc.CapScope = model.CapScope;
            doc.CapType = (int)model.CapType;
            doc.DrydockLocation = model.DrydockLocation;
            doc.DrydockStart = model.DrydockStart;
            doc.DrydockEnd = model.DrydockEnd;
            doc.SpecialHull = model.SpecialHull;
            doc.FatigueAnalysis = model.Fatigue;
            doc.ExpenseMarkup = model.ExpenseMarkup;
            doc.SurveyDayRateClient = model.SurveyDayrateClient;
            doc.SurveyDayRateAbs = model.SurveyDayrateAbs;
            doc.SedRate1 = model.SedRate1;
            doc.SedRate2 = model.SedRate2;
            doc.AgreementNumber = model.AgreementNumber;
            doc.ProposalRev = model.ProposalRev ?? 0;
            doc.CapContractValue = model.CapContractValue;
            doc.AgreementOwner = model.AgreementOwner;
            doc.AgreementDate = model.AgreementDate;
            doc.AgreementSignedDate = model.AgreementSignedDate;
            doc.CertificateGrade = model.CertificateGrade;
            doc.ProjectComments = model.ProjectComments;
            doc.CompanyName = model.CompanyName;
            doc.CompanyAddress = model.CompanyAddress;
            doc.CompanyBillingEmail = model.CompanyBillingEmail;
            doc.CompanyBillingSystemUrl = model.CompanyBillingSystemUrl;
            doc.BillSameAsCapClient = model.BillSameAsCapClient;
            doc.BillToCompanyName = model.BillToCompanyName;
            doc.BillToCompanyAddress = model.BillToCompanyAddress;
            doc.BillToCompanyEmail = model.BillToCompanyEmail;
            doc.BillToCompanyBillingUrl = model.BillToCompanyBillingUrl;
            doc.PocName = model.PocName;
            doc.PocEmail = model.PocEmail;
            doc.PocPhone = model.PocPhone;
            doc.IMO = model.IMO;
            doc.VesselName = model.VesselName;
            doc.ShipType = model.ShipType != default(ShipType) ? ((int)model.ShipType).ToString() : null;
            doc.WCN = model.WCN;
            doc.ClassSociety = model.ClassSociety != null ? string.Join(",", model.ClassSociety) : null;
            doc.AbsClassID = model.AbsClassID;
            doc.SisterVessel = model.SisterVessel;
            doc.SisterVesselImoNumber = model.SisterVesselImoNumber;
            doc.SisterVesselName = model.SisterVesselName;
            doc.Builder = model.Builder;
            doc.HullNumber = model.HullNumber;
            doc.MonthBuilt = model.MonthBuilt;
            doc.YearBuilt = model.YearBuilt;
            doc.LengthOverall = model.LengthOverall;
            doc.CRMOpportunityNumber = model.CRMOpportunityNumber;
            doc.ProjectStatus = model.ProjectStatus;
            doc.ProjectPriority = model.ProjectPriority;
            doc.ModifiedDate = DateTime.UtcNow;
            doc.LastModifiedBy = user;
            doc.PotentialDrydockDate = model.PotentialDrydockDate;
            doc.SurveyFirstVisit = model.SurveyFirstVisit;
            doc.SurveyLastVisit = model.SurveyLastVisit;
            doc.ClientProfileId = model.ClientProfileId;
            doc.BillToClientProfileId = model.BillToClientProfileId;
            doc.BillToWCN = model.BillToWCN;
            doc.LeadSurveyor = model.LeadSurveyor;
            doc.LeadSurveyorEID = model.LeadSurveyorEID;
            doc.SecondSurveyor = model.SecondSurveyor != null ? string.Join(",", model.SecondSurveyor) : null;
            doc.SecondSurveyorEID = model.SecondSurveyorEID;
            doc.ThirdSurveyor = model.ThirdSurveyor;
            doc.ThirdSurveyorEID = model.ThirdSurveyorEID;
            doc.FourthSurveyor = model.FourthSurveyor;
            doc.FourthSurveyorEID = model.FourthSurveyorEID;
            doc.AdditionalSurveyors = model.AdditionalSurveyors;
            doc.ClassSocietyOther = model.ClassSocietyOther;
            doc.CapScopeOther = model.CapScopeOther;
            doc.VesselType = model.VesselType;
            doc.CopyingVesselID = model.CopyingVesselID;
            doc.IsDefaultTank = model.IsDefaultTank;
            doc.TankVesselType = model.TankVesselType;
            doc.TankVesselIMO = model.TankVesselIMO;

            await Container.ReplaceItemAsync(doc, idStr, pk);

            // Nested SPs: Update_Project_Status (recalculate all) and Create_Activity
            await _recalculator.RecalculateAllAsync();
            await _activityWriter.CreateActivityAsync(model.Id, "Updated project", user);
        }

        /// <summary>
        /// Replaces [CAP].[Read_Project_Name_ById].
        /// </summary>
        private async Task<string> Sp_Read_Project_Name_ById(int id)
        {
            var idStr = id.ToString();
            try
            {
                var resp = await Container.ReadItemAsync<ProjectDocument>(idStr, new PartitionKey(idStr));
                return resp.Resource?.Name ?? string.Empty;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Replaces [CAP].[Update_Project_Status_Manual].
        /// Trap 8: 13 -> 'Project Cancelled', else always 'Project Closed'.
        /// </summary>
        private async Task Sp_Update_Project_Status_Manual(int id, int projectStatus, string statusChangedBy, string user)
        {
            var idStr = id.ToString();
            var pk = new PartitionKey(idStr);
            var resp = await Container.ReadItemAsync<ProjectDocument>(idStr, pk);
            if (resp.Resource != null)
            {
                var doc = resp.Resource;
                doc.ProjectStatus = projectStatus;
                doc.ModifiedDate = DateTime.UtcNow;
                doc.StatusChangedBy = statusChangedBy;
                doc.StatusChangedDate = DateTimeOffset.UtcNow;
                doc.LastModifiedBy = user;
                await Container.ReplaceItemAsync(doc, idStr, pk);
            }

            var activityStatus = projectStatus == 13 ? "Project Cancelled" : "Project Closed";
            await _activityWriter.CreateActivityAsync(id, activityStatus, user);
        }

        /// <summary>
        /// Replaces [CAP].[Delete_Project].
        /// Trap 7: soft-delete in 7 tables in order, then activity 'Project Deleted'.
        /// </summary>
        private async Task Sp_Delete_Project(int id, string user)
        {
            var pk = new PartitionKey((double)id);

            // 1. ProjectGrades
            var gradesQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @id").WithParameter("@id", id);
            using (var feed = _gradesContainer.GetItemQueryIterator<ProjectGradeDocument>(gradesQuery, requestOptions: new QueryRequestOptions { PartitionKey = pk }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var g in resp)
                    {
                        g.IsDeleted = true;
                        await _gradesContainer.ReplaceItemAsync(g, g.id, pk);
                    }
                }
            }

            // 2. ProjectTaskStatus
            var ptsQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @id").WithParameter("@id", id);
            using (var feed = _ptsContainer.GetItemQueryIterator<ProjectTaskStatusDocument>(ptsQuery, requestOptions: new QueryRequestOptions { PartitionKey = pk }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var pts in resp)
                    {
                        pts.IsDeleted = true;
                        await _ptsContainer.ReplaceItemAsync(pts, pts.id, pk);
                    }
                }
            }

            // 3. Comments
            var commentsQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @id").WithParameter("@id", id);
            using (var feed = _commentsContainer.GetItemQueryIterator<CommentDocument>(commentsQuery, requestOptions: new QueryRequestOptions { PartitionKey = pk }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var c in resp)
                    {
                        c.IsDeleted = true;
                        await _commentsContainer.ReplaceItemAsync(c, c.id, pk);
                    }
                }
            }

            // 4. Projects
            var idStr = id.ToString();
            var projectPk = new PartitionKey(idStr);
            var projResp = await Container.ReadItemAsync<ProjectDocument>(idStr, projectPk);
            if (projResp.Resource != null)
            {
                var doc = projResp.Resource;
                doc.IsDeleted = true;
                await Container.ReplaceItemAsync(doc, idStr, projectPk);
            }

            // 5. Vessel_TankMapping
            var tankQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @id").WithParameter("@id", id);
            using (var feed = _tankMappingContainer.GetItemQueryIterator<VesselTankMappingDocument>(tankQuery, requestOptions: new QueryRequestOptions { PartitionKey = pk }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var tm in resp)
                    {
                        tm.IsDeleted = true;
                        tm.UpdateDttm = DateTime.UtcNow;
                        await _tankMappingContainer.ReplaceItemAsync(tm, tm.id, pk);
                    }
                }
            }

            // 6. ImageDescriptions
            var imgQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @id").WithParameter("@id", id);
            using (var feed = _imageDescContainer.GetItemQueryIterator<ImageDescriptionDocument>(imgQuery, requestOptions: new QueryRequestOptions { PartitionKey = pk }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var img in resp)
                    {
                        img.IsDeleted = true;
                        await _imageDescContainer.ReplaceItemAsync(img, img.id, pk);
                    }
                }
            }

            // 7. Vessel_GradingMapping
            var gradingQuery = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @id").WithParameter("@id", id);
            using (var feed = _gradingMappingContainer.GetItemQueryIterator<VesselGradingMappingDocument>(gradingQuery, requestOptions: new QueryRequestOptions { PartitionKey = pk }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var gm in resp)
                    {
                        gm.IsDeleted = true;
                        gm.UpdateDttm = DateTime.UtcNow;
                        await _gradingMappingContainer.ReplaceItemAsync(gm, gm.id, pk);
                    }
                }
            }

            // Activity
            await _activityWriter.CreateActivityAsync(id, "Project Deleted", user);
        }

        /// <summary>
        /// Replaces [CAP].[Read_IMONumber_ByProjectId].
        /// </summary>
        private async Task<string> Sp_Read_IMONumber_ByProjectId(int projectId)
        {
            var idStr = projectId.ToString();
            try
            {
                var resp = await Container.ReadItemAsync<ProjectDocument>(idStr, new PartitionKey(idStr));
                return resp.Resource?.IMO?.ToString() ?? string.Empty;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Replaces [CAP].[Read_Project_Details].
        /// Trap 10: ROW_NUMBER() OVER (PARTITION BY t.Type ORDER BY pts.LastModifiedDate) (NULLs first in ASC).
        /// Ties broken by t.Id ASC.
        /// </summary>
        private async Task<List<ProjectDetailItem>> Sp_Read_Project_Details(int projectId)
        {
            var idStr = projectId.ToString();
            try
            {
                var resp = await Container.ReadItemAsync<ProjectDocument>(idStr, new PartitionKey(idStr));
                if (resp.Resource == null)
                    return new List<ProjectDetailItem>();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new List<ProjectDetailItem>();
            }

            var ptsList = new List<ProjectTaskStatusDocument>();
            var pk = new PartitionKey((double)projectId);
            var ptsQuery = new QueryDefinition("SELECT c.TaskId, c.LastModifiedDate FROM c WHERE c.ProjectId = @id").WithParameter("@id", projectId);
            using (var feed = _ptsContainer.GetItemQueryIterator<ProjectTaskStatusDocument>(ptsQuery, requestOptions: new QueryRequestOptions { PartitionKey = pk }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    ptsList.AddRange(resp);
                }
            }

            var tasksQuery = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.Category), 'Project', true)");
            var tasks = new List<TaskDocument>();
            using (var feed = _tasksContainer.GetItemQueryIterator<TaskDocument>(tasksQuery))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    tasks.AddRange(resp);
                }
            }
            var tasksById = tasks.ToDictionary(t => t.Id);

            // Join pts and tasks
            var joined = new List<(int TaskId, string Type, DateTime? LastModifiedDate, string Name)>();
            foreach (var pts in ptsList)
            {
                if (pts.TaskId.HasValue && tasksById.TryGetValue(pts.TaskId.Value, out var t))
                {
                    var typeVal = t.Cap2Type ?? t.Type ?? string.Empty;
                    var nameVal = t.Cap2Name ?? t.Name ?? string.Empty;
                    joined.Add((t.Id, typeVal, pts.LastModifiedDate, nameVal));
                }
            }

            // ROW_NUMBER() OVER (PARTITION BY t.Type ORDER BY pts.LastModifiedDate ASC [NULLs first], then t.Id ASC)
            var result = joined
                .GroupBy(x => x.Type)
                .Select(g => g
                    .OrderBy(x => x.LastModifiedDate.HasValue ? 1 : 0)
                    .ThenBy(x => x.LastModifiedDate)
                    .ThenBy(x => x.TaskId)
                    .First())
                .OrderBy(x => x.TaskId)
                .Select(x => new ProjectDetailItem
                {
                    Type = x.Type,
                    LastModifiedDate = x.LastModifiedDate,
                    Name = x.Name
                })
                .ToList();

            return result;
        }

        /// <summary>
        /// Replaces [CAP].[Read_Project_Summary_ById].
        /// Trap 9: Stored StatusId used (not dynamic status); percentages are float; DaysWorked is max per category.
        /// </summary>
        private async Task<ProjectSummary> Sp_Read_Project_Summary_ById(int id)
        {
            var idStr = id.ToString();
            ProjectDocument? p = null;
            try
            {
                var resp = await Container.ReadItemAsync<ProjectDocument>(idStr, new PartitionKey(idStr));
                p = resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null!;
            }

            if (p == null)
                return null!;

            var pk = new PartitionKey((double)id);
            var ptsList = new List<ProjectTaskStatusDocument>();
            var ptsQuery = new QueryDefinition("SELECT c.TaskId, c.StatusId, c.StatusDate, c.LastModifiedDate FROM c WHERE c.ProjectId = @id").WithParameter("@id", id);
            using (var feed = _ptsContainer.GetItemQueryIterator<ProjectTaskStatusDocument>(ptsQuery, requestOptions: new QueryRequestOptions { PartitionKey = pk }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    ptsList.AddRange(resp);
                }
            }

            var tasks = new List<TaskDocument>();
            using (var feed = _tasksContainer.GetItemQueryIterator<TaskDocument>(new QueryDefinition("SELECT c.Id, c.Category FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    tasks.AddRange(resp);
                }
            }
            var tasksById = tasks.ToDictionary(t => t.Id);

            var joined = ptsList
                .Where(pts => pts.TaskId.HasValue && tasksById.ContainsKey(pts.TaskId.Value))
                .Select(pts => new
                {
                    Category = tasksById[pts.TaskId!.Value].Category ?? string.Empty,
                    pts.StatusId,
                    pts.StatusDate,
                    pts.LastModifiedDate
                })
                .ToList();

            // Technical tasks ('Project')
            var techCompleted = joined.Count(x => string.Equals(x.Category, "Project", StringComparison.OrdinalIgnoreCase) && x.StatusId == 1);
            var techTotal = joined.Count(x => string.Equals(x.Category, "Project", StringComparison.OrdinalIgnoreCase) && x.StatusId != 4);
            float? techPct = techTotal == 0 ? null : (float)techCompleted / techTotal;

            // Invoice tasks ('Invoicing')
            var invCompleted = joined.Count(x => string.Equals(x.Category, "Invoicing", StringComparison.OrdinalIgnoreCase) && x.StatusId == 1);
            var invTotal = joined.Count(x => string.Equals(x.Category, "Invoicing", StringComparison.OrdinalIgnoreCase) && x.StatusId != 4);
            float? invPct = invTotal == 0 ? null : (float)invCompleted / invTotal;

            // Days worked
            var utcNow = DateTime.UtcNow;
            var daysByCategory = joined
                .GroupBy(x => x.Category)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(pts =>
                    {
                        if (pts.StatusId == 3 || pts.StatusId == 4) return 0;
                        if (pts.StatusId == 1)
                        {
                            return SqlDate.DiffDays(pts.StatusDate ?? utcNow, pts.LastModifiedDate ?? utcNow);
                        }
                        return SqlDate.DiffDays(pts.StatusDate ?? utcNow, utcNow);
                    }),
                    StringComparer.OrdinalIgnoreCase
                );

            int? invDays = daysByCategory.TryGetValue("Invoicing", out var idays) ? idays : null;
            int? techDays = daysByCategory.TryGetValue("Project", out var tdays) ? tdays : null;

            int stInt = 0;
            if (p.ShipType != null && int.TryParse(p.ShipType.ToString(), out var parsedSt))
                stInt = parsedSt;

            return new ProjectSummary
            {
                ID = p.EffectiveId,
                Name = p.Name ?? string.Empty,
                IMO = p.IMO,
                VesselName = p.VesselName ?? string.Empty,
                ShipType = (ShipType)stInt,
                CapRegion = p.CapRegion ?? string.Empty,
                YearBuilt = p.YearBuilt,
                AgreementOwner = p.AgreementOwner ?? string.Empty,
                AgreementNumber = p.AgreementNumber ?? string.Empty,
                PIDNumber = p.PIDNumberInt,
                ProjectComments = p.ProjectComments ?? string.Empty,
                AreTechnicalTasksComplete = techPct.HasValue && techPct.Value >= 1.0f,
                AreInvoiceTasksComplete = invPct.HasValue && invPct.Value >= 1.0f,
                ProjectPriority = p.ProjectPriority,
                TechnicalTasksCompletionPercentage = techPct,
                InvoiceTasksCompletionPercentage = invPct,
                InvoiceDaysWorked = invDays,
                TechnicalDaysWorked = techDays
            };
        }

        /// <summary>
        /// Replaces [CAP].[Update_Project_Priority].
        /// Recursive CTE: level 0 = project @Id gets @Priority if ISNULL(ProjectPriority,-1) <> ISNULL(@Priority,-1).
        /// Level n+1 = every project (p.Id <> @Id) whose current ProjectPriority equals NewPriority of level n
        /// gets CASE ProjectPriority WHEN 9 THEN NULL ELSE ProjectPriority + 1 END.
        /// Repeat until a level is empty. All rows found get ProjectPriority = NewPriority, LastModifiedBy = user.
        /// Activity is ALWAYS written (CONCAT('Changed priority to ', @Priority)), even when nothing changed or project does not exist.
        /// </summary>
        private async Task Sp_Update_Project_Priority(int id, int? priority, string user)
        {
            var effectiveUser = user ?? "SYSTEM";
            ProjectDocument? target = null;
            var idStr = id.ToString();
            var pk = new PartitionKey(idStr);

            try
            {
                var resp = await Container.ReadItemAsync<ProjectDocument>(idStr, pk);
                target = resp.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                target = null;
            }

            if (target != null)
            {
                int currentPri = target.ProjectPriority ?? -1;
                int requestedPri = priority ?? -1;

                if (currentPri != requestedPri)
                {
                    // Load all projects to simulate recursive chain push
                    var allProjects = await QueryAsync(new QueryDefinition("SELECT * FROM c"));
                    var projectsById = allProjects.ToDictionary(p => p.EffectiveId);

                    // CTE simulation:
                    // Level 0: (id, priority)
                    var updates = new Dictionary<int, int?>();
                    updates[id] = priority;

                    // Current level targets
                    var currentLevelTargets = new List<(int ProjectId, int? NewPriority)> { (id, priority) };

                    while (currentLevelTargets.Count > 0)
                    {
                        var nextLevelTargets = new List<(int ProjectId, int? NewPriority)>();

                        foreach (var targetItem in currentLevelTargets)
                        {
                            if (!targetItem.NewPriority.HasValue)
                                continue; // NULL never matches

                            int targetPriVal = targetItem.NewPriority.Value;

                            // In SQL:
                            // SELECT ProjectId = p.Id, NewPriority = CASE p.ProjectPriority WHEN 9 THEN NULL ELSE p.ProjectPriority + 1 END
                            // FROM CAP.Projects p
                            // INNER JOIN PriorityUpdateTargets put ON put.NewPriority = p.ProjectPriority and p.Id <> @Id
                            var clashingProjects = allProjects
                                .Where(p => p.EffectiveId != id &&
                                            !updates.ContainsKey(p.EffectiveId) &&
                                            p.ProjectPriority == targetPriVal)
                                .ToList();

                            foreach (var clash in clashingProjects)
                            {
                                int? nextPriority = clash.ProjectPriority == 9 ? null : clash.ProjectPriority + 1;
                                updates[clash.EffectiveId] = nextPriority;
                                nextLevelTargets.Add((clash.EffectiveId, nextPriority));
                            }
                        }

                        currentLevelTargets = nextLevelTargets;
                    }

                    foreach (var (projId, newPri) in updates)
                    {
                        if (projectsById.TryGetValue(projId, out var projDoc))
                        {
                            projDoc.ProjectPriority = newPri;
                            projDoc.LastModifiedBy = effectiveUser;
                            var pIdStr = projId.ToString();
                            await Container.ReplaceItemAsync(projDoc, pIdStr, new PartitionKey(pIdStr));
                        }
                    }
                }
            }

            // Record activity - ALWAYS written even when project does not exist or nothing changed
            var act = $"Changed priority to {priority}";
            if (act.Length > 100)
                act = act[..100];

            await _activityWriter.CreateActivityAsync(id, act, effectiveUser);
        }

        /// <summary>
        /// Replaces [CAP].[Read_Projects_Grid].
        /// Trap 12: outer apply TOP 1 comment and TOP 1 activity; IsDownload = 'Offline' when IsSynchedOnline row exists.
        /// </summary>
        private async Task<List<AllProjectsGrid>> Sp_Read_Projects_Grid()
        {
            var projects = await QueryAsync(new QueryDefinition("SELECT * FROM c WHERE c.IsDeleted = false"));

            // Statuses
            var statuses = new Dictionary<int, string>();
            using (var feed = _statusesContainer.GetItemQueryIterator<StatusDocument>(new QueryDefinition("SELECT c.ID, c.Status FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var s in resp) statuses[s.ID] = s.Status ?? string.Empty;
                }
            }

            // Offline rows (IsSynched=0 and IsReadOnly=0)
            var offlineRows = new List<IsSynchedOnlineDocument>();
            using (var feed = _isSynchedContainer.GetItemQueryIterator<IsSynchedOnlineDocument>(new QueryDefinition("SELECT c.ProjectId FROM c WHERE c.IsSynched = false AND c.IsReadOnly = false")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    offlineRows.AddRange(resp);
                }
            }
            var offlineByProj = offlineRows
                .Where(r => r.ProjectId.HasValue)
                .GroupBy(r => r.ProjectId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // Comments
            var allComments = new List<CommentDocument>();
            using (var feed = _commentsContainer.GetItemQueryIterator<CommentDocument>(new QueryDefinition("SELECT c.ProjectId, c.Comment, c.CreatedBy, c.LastModifiedDate FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    allComments.AddRange(resp);
                }
            }
            var topCommentByProj = allComments
                .Where(c => c.ProjectId > 0)
                .GroupBy(c => c.ProjectId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(c => c.LastModifiedDate).First()
                );

            // Logins for comments
            var logins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var feed = _userLoginsContainer.GetItemQueryIterator<AspNetUserLoginDocument>(new QueryDefinition("SELECT c.UserId, c.ProviderDisplayName FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var l in resp)
                    {
                        if (!string.IsNullOrEmpty(l.UserId) && !string.IsNullOrEmpty(l.ProviderDisplayName))
                            logins[l.UserId] = l.ProviderDisplayName;
                    }
                }
            }

            // Activities
            var allActivities = new List<ActivityDocument>();
            using (var feed = _activitiesContainer.GetItemQueryIterator<ActivityDocument>(new QueryDefinition("SELECT c.ProjectId, c.CreateDttm FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    allActivities.AddRange(resp);
                }
            }
            var topActivityByProj = allActivities
                .Where(a => a.ProjectId.HasValue)
                .GroupBy(a => a.ProjectId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(a => a.CreateDttm).First()
                );

            var result = new List<AllProjectsGrid>();
            foreach (var p in projects)
            {
                if (!p.ProjectStatus.HasValue || !statuses.TryGetValue(p.ProjectStatus.Value, out var statusName))
                    continue;

                topCommentByProj.TryGetValue(p.EffectiveId, out var comm);
                topActivityByProj.TryGetValue(p.EffectiveId, out var act);

                string? commentAuthor = null;
                if (comm != null && !string.IsNullOrEmpty(comm.CreatedBy))
                {
                    logins.TryGetValue(comm.CreatedBy, out commentAuthor);
                }

                int offlineCopies = offlineByProj.TryGetValue(p.EffectiveId, out var oCount) ? Math.Max(1, oCount) : 1;
                string isDownload = offlineByProj.ContainsKey(p.EffectiveId) ? "Offline" : "Online";

                for (int i = 0; i < offlineCopies; i++)
                {
                    result.Add(new AllProjectsGrid
                    {
                        Id = p.EffectiveId,
                        Name = p.Name ?? string.Empty,
                        PIDNumber = p.PIDNumber?.ToString() ?? string.Empty,
                        CompanyName = p.CompanyName ?? string.Empty,
                        IMO = p.IMO?.ToString() ?? string.Empty,
                        WCN = p.WCN ?? string.Empty,
                        PotentialDrydockDate = p.PotentialDrydockDate,
                        CapRegion = p.CapRegion ?? string.Empty,
                        ProjectPriority = p.ProjectPriority?.ToString() ?? string.Empty,
                        ProjectStatusName = statusName,
                        StatusName = statusName,
                        CommentCreatedBy = commentAuthor,
                        MostRecentComment = comm?.Comment,
                        CommentLastModifiedDate = comm?.LastModifiedDate,
                        ProjectLastUpdatedDate = act?.CreateDttm,
                        PercentComplete = p.PercentComplete ?? 0,
                        IsDownload = isDownload
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Replaces [CAP].[Read_Projects_Grid_Export].
        /// Trap 3: only ONE comment in the whole table (the newest) -> only that project gets MostRecentProjectComment;
        /// Left join ProjectGrades without IsDeleted filter -> 2 grade rows produce duplicate project rows.
        /// </summary>
        private async Task<List<AllProjectsGridExport>> Sp_Read_Projects_Grid_Export()
        {
            var projects = await QueryAsync(new QueryDefinition("SELECT * FROM c WHERE c.IsDeleted = false"));

            // Statuses
            var statuses = new Dictionary<int, string>();
            using (var feed = _statusesContainer.GetItemQueryIterator<StatusDocument>(new QueryDefinition("SELECT c.ID, c.Status FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var s in resp) statuses[s.ID] = s.Status ?? string.Empty;
                }
            }

            // Ports
            var ports = new Dictionary<int, string>();
            using (var feed = _portOfCallContainer.GetItemQueryIterator<PortOfCallDocument>(new QueryDefinition("SELECT c.PortId, c.PortName FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var poc in resp) ports[poc.PortId] = poc.PortName ?? string.Empty;
                }
            }

            // ProjectGrades (WITHOUT IsDeleted filter, matching SQL LEFT JOIN ProjectGrades)
            var grades = new List<ProjectGradeDocument>();
            using (var feed = _gradesContainer.GetItemQueryIterator<ProjectGradeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    grades.AddRange(resp);
                }
            }
            var gradesByProj = grades.GroupBy(g => g.ProjectId).ToDictionary(g => g.Key, g => g.ToList());

            // SQL BUG KEPT: SELECT TOP(1) projectId, Comment FROM CAP.Comments ORDER BY LastModifiedDate DESC
            // Only 1 comment in the ENTIRE table is selected!
            var comments = new List<CommentDocument>();
            using (var feed = _commentsContainer.GetItemQueryIterator<CommentDocument>(new QueryDefinition("SELECT c.ProjectId, c.Comment, c.LastModifiedDate FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    comments.AddRange(resp);
                }
            }
            var newestCommentOverall = comments
                .OrderByDescending(c => c.LastModifiedDate)
                .FirstOrDefault();

            var result = new List<AllProjectsGridExport>();
            foreach (var p in projects)
            {
                if (!p.ProjectStatus.HasValue || !statuses.TryGetValue(p.ProjectStatus.Value, out var statusName))
                    continue;

                string? portName = null;
                if (p.DrydockLocation.HasValue)
                    ports.TryGetValue(p.DrydockLocation.Value, out portName);

                string? comment = (newestCommentOverall != null && newestCommentOverall.ProjectId == p.EffectiveId)
                    ? newestCommentOverall.Comment
                    : null;

                int shipTypeVal = 0;
                if (p.ShipType != null && int.TryParse(p.ShipType.ToString(), out var parsedSt))
                    shipTypeVal = parsedSt;

                List<ProjectGradeDocument?> projGrades;
                if (gradesByProj.TryGetValue(p.EffectiveId, out var gList) && gList.Count > 0)
                {
                    projGrades = gList.Cast<ProjectGradeDocument?>().ToList();
                }
                else
                {
                    projGrades = new List<ProjectGradeDocument?> { null };
                }

                foreach (var pg in projGrades)
                {
                    result.Add(new AllProjectsGridExport
                    {
                        ID = p.EffectiveId,
                        Name = p.Name ?? string.Empty,
                        PIDNumber = p.PIDNumberInt,
                        CapRegion = p.CapRegion,
                        CapScope = p.CapScope,
                        CapType = (CapType)(p.CapType ?? 0),
                        DrydockLocation = p.DrydockLocation,
                        DrydockStart = p.DrydockStart,
                        DrydockEnd = p.DrydockEnd,
                        SpecialHull = p.SpecialHull ?? true,
                        Fatigue = p.FatigueAnalysis ?? true,
                        ExpenseMarkup = p.ExpenseMarkup,
                        SurveyDayrateClient = p.SurveyDayRateClient,
                        SurveyDayrateAbs = p.SurveyDayRateAbs,
                        SedRate1 = p.SedRate1,
                        SedRate2 = p.SedRate2,
                        AgreementNumber = p.AgreementNumber,
                        ProposalRev = p.ProposalRev,
                        CapContractValue = p.CapContractValue,
                        AgreementOwner = p.AgreementOwner,
                        AgreementDate = p.AgreementDate,
                        AgreementSignedDate = p.AgreementSignedDate,
                        CertificateGrade = p.CertificateGrade,
                        ProjectComments = p.ProjectComments,
                        CompanyName = p.CompanyName,
                        CompanyAddress = p.CompanyAddress,
                        CompanyBillingEmail = p.CompanyBillingEmail,
                        CompanyBillingSystemUrl = p.CompanyBillingSystemUrl,
                        BillSameAsCapClient = p.BillSameAsCapClient ?? true,
                        BillToCompanyName = p.BillToCompanyName,
                        BillToCompanyAddress = p.BillToCompanyAddress,
                        BillToCompanyEmail = p.BillToCompanyEmail,
                        BillToCompanyBillingUrl = p.BillToCompanyBillingUrl,
                        PocName = p.PocName,
                        PocEmail = p.PocEmail,
                        PocPhone = p.PocPhone,
                        IMO = p.IMO,
                        VesselName = p.VesselName,
                        ShipType = (ShipType)shipTypeVal,
                        WCN = p.WCN,
                        ClassSociety = p.ClassSociety,
                        AbsClassID = p.AbsClassID,
                        SisterVessel = p.SisterVessel ?? false,
                        SisterVesselImoNumber = p.SisterVesselImoNumber,
                        SisterVesselName = p.SisterVesselName,
                        Builder = p.Builder,
                        HullNumber = p.HullNumber,
                        MonthBuilt = p.MonthBuilt,
                        YearBuilt = p.YearBuilt,
                        LengthOverall = p.LengthOverall,
                        CRMOpportunityNumber = p.CRMOpportunityNumber,
                        ProjectStatus = p.ProjectStatus,
                        ProjectPriority = p.ProjectPriority,
                        CreatedDate = p.CreatedDate,
                        CreatedBy = p.CreatedBy,
                        StatusChangedBy = p.StatusChangedBy,
                        StatusChangedDate = p.StatusChangedDate,
                        LastModifiedBy = p.LastModifiedBy,
                        PotentialDrydockDate = p.PotentialDrydockDate,
                        SurveyFirstVisit = p.SurveyFirstVisit,
                        SurveyLastVisit = p.SurveyLastVisit,
                        ClientProfileId = p.ClientProfileId,
                        BillToClientProfileId = p.BillToClientProfileId,
                        BillToWCN = p.BillToWCN,
                        LeadSurveyor = p.LeadSurveyor,
                        LeadSurveyorEID = p.LeadSurveyorEID,
                        SecondSurveyor = p.SecondSurveyor,
                        SecondSurveyorEID = p.SecondSurveyorEID,
                        ThirdSurveyor = p.ThirdSurveyor,
                        ThirdSurveyorEID = p.ThirdSurveyorEID,
                        FourthSurveyor = p.FourthSurveyor,
                        FourthSurveyorEID = p.FourthSurveyorEID,
                        AdditionalSurveyors = p.AdditionalSurveyors ?? false,
                        ClassSocietyOther = p.ClassSocietyOther,
                        CapScopeOther = p.CapScopeOther,
                        ProjectStatusName = statusName,
                        DrydockLocationName = portName,
                        FatigueGrade = (Grades?)pg?.FatigueGrade,
                        RenewalGrade = (Grades?)pg?.RenewalGrade,
                        MaterialGrade = (Grades?)pg?.MaterialGrade,
                        GaugingGrade = (Grades?)pg?.GaugingGrade,
                        StructuralGrade = (Grades?)pg?.StructuralGrade,
                        FinalGrade = (FinalGrades?)pg?.FinalGrade,
                        CAPCertificateIssuanceDate = pg?.CAPCertificateIssuanceDate,
                        CAP1FinalGrade = pg?.CAP1FinalGrade,
                        CAP1CertificateIssuanceDate = pg?.CAP1CertificateIssuanceDate,
                        HullGirderStrength = (Grades?)pg?.HullGirderStrength,
                        ClassReportDate = pg?.ClassReportDate,
                        MostRecentProjectComment = comment
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Replaces [CAP].[ReadProjectVesselTypeById].
        /// </summary>
        private async Task<string> Sp_ReadProjectVesselTypeById(int id)
        {
            var idStr = id.ToString();
            try
            {
                var resp = await Container.ReadItemAsync<ProjectDocument>(idStr, new PartitionKey(idStr));
                return resp.Resource?.VesselType ?? string.Empty;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Replaces inline SQL: GetProjectsByClientIdAsync.
        /// </summary>
        private async Task<List<ProjectsByClientId>> Sp_GetProjectsByClientId(Guid clientId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.ClientProfileId = @cid")
                .WithParameter("@cid", clientId);

            var projects = await QueryAsync(query);

            var statuses = new Dictionary<int, string>();
            using (var feed = _statusesContainer.GetItemQueryIterator<StatusDocument>(new QueryDefinition("SELECT c.ID, c.Status FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var s in resp) statuses[s.ID] = s.Status ?? string.Empty;
                }
            }

            return projects.Select(p =>
            {
                string? statusName = null;
                if (p.ProjectStatus.HasValue)
                    statuses.TryGetValue(p.ProjectStatus.Value, out statusName);

                return new ProjectsByClientId
                {
                    ID = p.EffectiveId,
                    Name = p.Name ?? string.Empty,
                    PIDNumber = p.PIDNumber?.ToString() ?? string.Empty,
                    SurveyFirstVisit = p.SurveyFirstVisit,
                    SurveyLastVisit = p.SurveyLastVisit,
                    StatusName = statusName ?? string.Empty
                };
            }).ToList();
        }

        /// <summary>
        /// Replaces [CAP].[Read_Project_Count_Grouped_ByClientId].
        /// </summary>
        private async Task<List<ClientProfileDTO>> Sp_Read_Project_Count_Grouped_ByClientId(CancellationToken ct)
        {
            var clients = new List<ClientsDocument>();
            using (var feed = _clientsContainer.GetItemQueryIterator<ClientsDocument>(new QueryDefinition("SELECT c.Id FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync(ct);
                    clients.AddRange(resp);
                }
            }

            var projects = await QueryAsync(new QueryDefinition("SELECT c.ClientProfileId FROM c WHERE IS_DEFINED(c.ClientProfileId) AND NOT IS_NULL(c.ClientProfileId)"));

            var countsByClient = projects
                .Where(p => p.ClientProfileId.HasValue)
                .GroupBy(p => p.ClientProfileId!.Value.ToString(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var result = new List<ClientProfileDTO>();
            foreach (var c in clients)
            {
                var cidStr = c.id;
                int count = countsByClient.TryGetValue(cidStr, out var cnt) ? cnt : 0;
                result.Add(new ClientProfileDTO
                {
                    ClientId = Guid.TryParse(cidStr, out var parsedGuid) ? parsedGuid : Guid.Empty,
                    ProjectCount = (uint)count
                });
            }

            return result;
        }

        /// <summary>
        /// Replaces [CAP].[Read_Project] for GetAllProjects.
        /// </summary>
        private async Task<List<Project>> Sp_Read_Project_Projects()
        {
            var projects = await QueryAsync(new QueryDefinition("SELECT * FROM c"));

            var statuses = new Dictionary<int, string>();
            using (var feed = _statusesContainer.GetItemQueryIterator<StatusDocument>(new QueryDefinition("SELECT c.ID, c.Status FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var s in resp) statuses[s.ID] = s.Status ?? string.Empty;
                }
            }

            var ports = new Dictionary<int, string>();
            using (var feed = _portOfCallContainer.GetItemQueryIterator<PortOfCallDocument>(new QueryDefinition("SELECT c.PortId, c.PortName FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var poc in resp) ports[poc.PortId] = poc.PortName ?? string.Empty;
                }
            }

            var logins = new Dictionary<string, List<AspNetUserLoginDocument>>(StringComparer.OrdinalIgnoreCase);
            using (var feed = _userLoginsContainer.GetItemQueryIterator<AspNetUserLoginDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var l in resp)
                    {
                        if (!string.IsNullOrEmpty(l.UserId))
                        {
                            if (!logins.TryGetValue(l.UserId, out var list))
                            {
                                list = new List<AspNetUserLoginDocument>();
                                logins[l.UserId] = list;
                            }
                            list.Add(l);
                        }
                    }
                }
            }

            var result = new List<Project>();
            foreach (var p in projects)
            {
                if (!p.ProjectStatus.HasValue || !statuses.TryGetValue(p.ProjectStatus.Value, out var statusName))
                    continue;

                string? portName = null;
                if (p.DrydockLocation.HasValue)
                    ports.TryGetValue(p.DrydockLocation.Value, out portName);

                List<AspNetUserLoginDocument?> uList;
                if (!string.IsNullOrEmpty(p.CreatedBy) && logins.TryGetValue(p.CreatedBy, out var uLogins) && uLogins.Count > 0)
                    uList = uLogins.Cast<AspNetUserLoginDocument?>().ToList();
                else
                    uList = new List<AspNetUserLoginDocument?> { null };

                List<AspNetUserLoginDocument?> u2List;
                if (!string.IsNullOrEmpty(p.StatusChangedBy) && logins.TryGetValue(p.StatusChangedBy, out var u2Logins) && u2Logins.Count > 0)
                    u2List = u2Logins.Cast<AspNetUserLoginDocument?>().ToList();
                else
                    u2List = new List<AspNetUserLoginDocument?> { null };

                foreach (var _ in uList)
                {
                    foreach (var _2 in u2List)
                    {
                        int shipTypeInt = 0;
                        if (p.ShipType != null && int.TryParse(p.ShipType.ToString(), out var parsedSt))
                            shipTypeInt = parsedSt;

                        result.Add(new Project
                        {
                            Id = p.EffectiveId,
                            Name = p.Name ?? string.Empty,
                            PIDNumber = p.PIDNumberInt,
                            CapRegion = p.CapRegion ?? string.Empty,
                            CapScope = p.CapScope ?? string.Empty,
                            CapScopeOther = p.CapScopeOther ?? string.Empty,
                            CapType = (CapType)(p.CapType ?? 0),
                            DrydockLocation = p.DrydockLocation,
                            DrydockStart = p.DrydockStart,
                            DrydockEnd = p.DrydockEnd,
                            PotentialDrydockDate = p.PotentialDrydockDate,
                            SurveyFirstVisit = p.SurveyFirstVisit,
                            SurveyLastVisit = p.SurveyLastVisit,
                            ProjectStatus = p.ProjectStatus,
                            ProjectPriority = p.ProjectPriority,
                            SpecialHull = p.SpecialHull ?? true,
                            Fatigue = p.FatigueAnalysis ?? true,
                            ExpenseMarkup = p.ExpenseMarkup,
                            SurveyDayrateClient = p.SurveyDayRateClient,
                            SurveyDayrateAbs = p.SurveyDayRateAbs,
                            SedRate1 = p.SedRate1,
                            SedRate2 = p.SedRate2,
                            CapContractValue = p.CapContractValue,
                            AgreementNumber = p.AgreementNumber ?? string.Empty,
                            ProposalRev = p.ProposalRev,
                            AgreementOwner = p.AgreementOwner ?? string.Empty,
                            AgreementDate = p.AgreementDate,
                            AgreementSignedDate = p.AgreementSignedDate,
                            CertificateGrade = p.CertificateGrade ?? string.Empty,
                            ProjectComments = p.ProjectComments ?? string.Empty,
                            CompanyName = p.CompanyName ?? string.Empty,
                            CompanyAddress = p.CompanyAddress ?? string.Empty,
                            CompanyBillingEmail = p.CompanyBillingEmail ?? string.Empty,
                            CompanyBillingSystemUrl = p.CompanyBillingSystemUrl ?? string.Empty,
                            BillSameAsCapClient = p.BillSameAsCapClient ?? true,
                            BillToCompanyName = p.BillToCompanyName ?? string.Empty,
                            BillToCompanyAddress = p.BillToCompanyAddress ?? string.Empty,
                            BillToCompanyEmail = p.BillToCompanyEmail ?? string.Empty,
                            BillToCompanyBillingUrl = p.BillToCompanyBillingUrl ?? string.Empty,
                            PocName = p.PocName ?? string.Empty,
                            PocEmail = p.PocEmail ?? string.Empty,
                            PocPhone = p.PocPhone ?? string.Empty,
                            IMO = p.IMO,
                            VesselName = p.VesselName ?? string.Empty,
                            VesselType = p.VesselType ?? string.Empty,
                            ShipType = (ShipType)shipTypeInt,
                            ClientProfileId = p.ClientProfileId,
                            BillToClientProfileId = p.BillToClientProfileId,
                            WCN = p.WCN ?? string.Empty,
                            BillToWCN = p.BillToWCN ?? string.Empty,
                            ClassSociety = p.ClassSociety?.Split(','),
                            ClassSocietyOther = p.ClassSocietyOther ?? string.Empty,
                            AbsClassID = p.AbsClassID ?? string.Empty,
                            SisterVessel = p.SisterVessel ?? false,
                            SisterVesselImoNumber = p.SisterVesselImoNumber ?? string.Empty,
                            SisterVesselName = p.SisterVesselName ?? string.Empty,
                            IsDefaultTank = p.IsDefaultTank,
                            CopyingVesselID = p.CopyingVesselID,
                            Builder = p.Builder ?? string.Empty,
                            HullNumber = p.HullNumber ?? string.Empty,
                            MonthBuilt = p.MonthBuilt,
                            YearBuilt = p.YearBuilt,
                            LengthOverall = p.LengthOverall,
                            CRMOpportunityNumber = p.CRMOpportunityNumber ?? string.Empty,
                            ProjectStatusName = statusName,
                            DrydockLocationName = portName ?? string.Empty,
                            LeadSurveyor = p.LeadSurveyor ?? string.Empty,
                            LeadSurveyorEID = p.LeadSurveyorEID ?? string.Empty,
                            SecondSurveyor = p.SecondSurveyor?.Split(','),
                            SecondSurveyorEID = p.SecondSurveyorEID ?? string.Empty,
                            ThirdSurveyor = p.ThirdSurveyor ?? string.Empty,
                            ThirdSurveyorEID = p.ThirdSurveyorEID ?? string.Empty,
                            FourthSurveyor = p.FourthSurveyor ?? string.Empty,
                            FourthSurveyorEID = p.FourthSurveyorEID ?? string.Empty,
                            AdditionalSurveyors = p.AdditionalSurveyors ?? false,
                            CreatedDate = p.CreatedDate,
                            CreatedBy = p.CreatedBy ?? string.Empty,
                            StatusChangedBy = p.StatusChangedBy ?? string.Empty,
                            StatusChangedDate = p.StatusChangedDate,
                            LastModifiedBy = p.LastModifiedBy ?? string.Empty,
                            PercentComplete = p.PercentComplete ?? 0,
                            TankVesselType = p.TankVesselType ?? string.Empty,
                            TankVesselIMO = p.TankVesselIMO ?? 0
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Replaces [CAP].[Get_Projects_Stats].
        /// Trap 14: 3 result sets: totals (online/offline), regions, statuses. Non-deleted projects with existing status.
        /// </summary>
        private async Task<ProjectStats> Sp_Get_Projects_Stats()
        {
            var projects = await QueryAsync(new QueryDefinition("SELECT * FROM c WHERE c.IsDeleted = false"));

            var statuses = new Dictionary<int, string>();
            using (var feed = _statusesContainer.GetItemQueryIterator<StatusDocument>(new QueryDefinition("SELECT c.ID, c.Status FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var s in resp) statuses[s.ID] = s.Status ?? string.Empty;
                }
            }

            // Filter projects with valid status
            var activeProjects = projects
                .Where(p => p.ProjectStatus.HasValue && statuses.ContainsKey(p.ProjectStatus.Value))
                .ToList();

            // Offline projects (IsSynched=0 and IsReadOnly=0)
            var offlineProjectIds = new HashSet<int>();
            using (var feed = _isSynchedContainer.GetItemQueryIterator<IsSynchedOnlineDocument>(new QueryDefinition("SELECT c.ProjectId FROM c WHERE c.IsSynched = false AND c.IsReadOnly = false")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    foreach (var doc in resp)
                    {
                        if (doc.ProjectId.HasValue)
                            offlineProjectIds.Add(doc.ProjectId.Value);
                    }
                }
            }

            int total = activeProjects.Count;
            int offline = activeProjects.Count(p => offlineProjectIds.Contains(p.EffectiveId));
            int online = total - offline;

            // Group by region: ISNULL(p.CapRegion, 'Unknown')
            var regions = activeProjects
                .GroupBy(p => p.CapRegion, SqlStringEqualityComparer.Instance)
                .ToDictionary(
                    g => g.Key == null ? "Unknown" : g.Key,
                    g => g.Count(),
                    SqlStringEqualityComparer.Instance);

            // Group by status: ISNULL(s.Status, 'Unknown')
            var statusCounts = activeProjects
                .GroupBy(p => statuses[p.ProjectStatus!.Value], SqlStringEqualityComparer.Instance)
                .ToDictionary(g => g.Key, g => g.Count(), SqlStringEqualityComparer.Instance);

            return new ProjectStats
            {
                Total = total,
                Online = online,
                Offline = offline,
                Regions = regions,
                Statuses = statusCounts
            };
        }

        private sealed class SqlStringEqualityComparer : IEqualityComparer<string?>
        {
            public static readonly SqlStringEqualityComparer Instance = new();

            public bool Equals(string? x, string? y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;
                return string.Equals(x.TrimEnd(), y.TrimEnd(), StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(string? obj)
            {
                if (obj == null) return 0;
                return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.TrimEnd());
            }
        }

        #endregion
    }
}
