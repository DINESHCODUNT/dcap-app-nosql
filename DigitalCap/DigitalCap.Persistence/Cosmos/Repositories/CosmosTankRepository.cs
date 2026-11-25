using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.Models.VesselModel;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.ViewModels;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosTankRepository : CosmosRepositoryBase<VesselTankMappingDocument>, ITankRepository
    {
        private readonly ISequenceService _sequenceService;
        private readonly ILogger<CosmosTankRepository> _logger;

        public CosmosTankRepository(
            ICosmosContainerFactory containerFactory,
            ISequenceService sequenceService,
            ILogger<CosmosTankRepository> logger)
            : base(containerFactory, CosmosContainers.dbo_Vessel_TankMapping, logger)
        {
            _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region IRepositoryBase Unsupported Methods

        public Task<Guid> InsertAsync(VesselTank entity) => throw new NotSupportedException();
        public Guid Insert(VesselTank entity) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(VesselTank entity) => throw new NotSupportedException();
        public bool Update(VesselTank entity) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(Guid id) => throw new NotSupportedException();
        public bool Delete(Guid id) => throw new NotSupportedException();
        public Task<VesselTank?> GetAsync(Guid id, bool includeDeleted = false, bool useTransaction = true) => throw new NotSupportedException();
        public VesselTank Get(Guid id, bool includeDeleted = false, bool useTransaction = true) => throw new NotSupportedException();
        public Task<IQueryable<VesselTank>> GetAllAsync(bool useTransaction = true) => throw new NotSupportedException();
        public IQueryable<VesselTank> GetFiltered(Expression<Func<VesselTank, bool>> filter) => throw new NotSupportedException();
        public VesselTank FirstOrDefault(Expression<Func<VesselTank, bool>> filter) => throw new NotSupportedException();
        public VesselTank First(Expression<Func<VesselTank, bool>> filter) => throw new NotSupportedException();
        public IQueryable<VesselTank> ExecuteQuery(string sql) => throw new NotSupportedException();
        public IQueryable<VesselTank> ExecuteQuery(string sql, object[] parameters) => throw new NotSupportedException();
        public void Rollback() => throw new NotSupportedException();
        public void Commit() { }

        #endregion

        #region ITankRepository Domain Methods

        public async Task<List<VesselTank>> GetTanks_VesselByIMO(string imonumber)
        {
            return await Sp_Get_Vessel_Tank_ByIMONumber(imonumber);
        }

        public async Task<List<VesselTank>> CreateTanks_VesselByProject(string imonumber, int projectId, int newprojectId, string newimo)
        {
            return await Sp_CreateTanksofVesselByProject(imonumber, projectId, newprojectId, newimo);
        }

        public async Task<bool> PopulateTemplate(string imonumber = null, string vesseltype = null, string copyingimonumber = null)
        {
            try
            {
                var tanks = new List<VesselTank>();

                if (!string.IsNullOrEmpty(copyingimonumber))
                {
                    tanks = await GetTanks_VesselByIMO(copyingimonumber);

                    foreach (var tank in tanks)
                    {
                        tank.Id = Guid.NewGuid();
                        tank.ImoNumber = imonumber;
                        tank.CreatedDttm = DateTime.UtcNow;
                        tank.UpdateDttm = DateTime.UtcNow;
                        await CreateTank(tank);
                    }
                }

                if (!string.IsNullOrEmpty(vesseltype))
                {
                    tanks = await GetTanks_VesselByVesselType(vesseltype);

                    foreach (var tank in tanks)
                    {
                        tank.Id = Guid.NewGuid();
                        tank.ImoNumber = imonumber;
                        tank.CreatedDttm = DateTime.UtcNow;
                        tank.VesselType = null;
                        tank.UpdateDttm = DateTime.UtcNow;
                        await CreateTank(tank);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> PopulateTemplate(string imonumber = null, string vesseltype = null, string copyingimonumber = null, int projectid = 0, int copyingProjectId = 0)
        {
            try
            {
                var tanks = new List<VesselTank>();
                if (!string.IsNullOrEmpty(copyingimonumber))
                {
                    tanks = copyingProjectId == 0
                        ? await GetTanks_VesselByIMO(copyingimonumber)
                        : await CreateTanks_VesselByProject(copyingimonumber, copyingProjectId, projectid, imonumber);
                }
                if (!string.IsNullOrEmpty(vesseltype))
                {
                    tanks = await GetTanks_VesselByVesselType(vesseltype);
                    foreach (var tank in tanks)
                    {
                        tank.Id = Guid.NewGuid();
                        tank.ImoNumber = imonumber;
                        tank.CreatedDttm = DateTime.UtcNow;
                        tank.VesselType = null;
                        tank.UpdateDttm = DateTime.UtcNow;
                        tank.ProjectId = projectid;
                        await CreateTank(tank);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<VesselTank>> GetTanks_VesselByVesselType(string vesseltype)
        {
            try
            {
                return await Sp_Get_Vessel_Tank_ByVesselType(vesseltype);
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<bool> CreateTank(VesselTank vesselTank)
        {
            try
            {
                return await Sp_Create_Vessel_TankMapping(vesselTank);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vessel tank");
                return false;
            }
        }

        public async Task<List<Core.Models.Tank.ShipType>> GetShipType()
        {
            return await Sp_GetShipType();
        }

        public async Task<List<VesselTank>> GetTanks_VesselByProject(string imoNumber, int projectId)
        {
            return await Sp_Get_Vessel_Tank_ByProject(imoNumber, projectId);
        }

        public async Task<List<VesselTankGrading>> GetVessel_GradingByVesselType(string vesseltype)
        {
            try
            {
                return await Sp_Get_Vessel_Tank_Grading_ByVesselType(vesseltype);
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<bool> CreateVessel_Grading(VesselTankGrading vesselTankGrading)
        {
            try
            {
                return await Sp_Create_Vessel_Grading(vesselTankGrading);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<TankUI>> GetTemplateTanks(int templateId, string imonumber, string vesseltype, int projectId)
        {
            try
            {
                return await Sp_GetTempaleTanks(templateId, imonumber, vesseltype, projectId);
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<List<TankCheckBox>> GetTemplateTankGradingCondition(int Id)
        {
            try
            {
                return await Sp_GetTankGradingConditionById(Id);
            }
            catch (Exception)
            {
                return new List<TankCheckBox>();
            }
        }

        public async Task<List<TankGradingUI>> GetTemplateTankGrading(int tanktypeId, int projectId, string vesseltype)
        {
            try
            {
                return await Sp_GetTempaleTankGrading(tanktypeId, projectId, vesseltype);
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<List<TankImageCard>> GetProjectTankImageCard(int projectId, int templateId, Guid sectionId)
        {
            // No try/catch in SQL repository; errors propagate
            return await Sp_GetTankImageCard(projectId, templateId, sectionId);
        }

        public async Task<TankImageCard> GetProjectTankImagCardByName(int projectId, int templateId, Guid sectionId, int cardNumber)
        {
            try
            {
                return await Sp_GetProjectTankImagecardByName(projectId, templateId, sectionId, cardNumber);
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<bool> UpdateProjectTankImageCard(TankImageCard tankImageCard)
        {
            return await Sp_UpdateTankImageCard(tankImageCard);
        }

        public async Task<bool> CreateProjectTankImageCard(TankImageCard tankImageCard)
        {
            return await Sp_CreateTankImageCard(tankImageCard);
        }

        public async Task<List<TankTypes>> GetTankTypes()
        {
            try
            {
                return await Sp_GetTankType();
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<List<TankTypes>> GetTankTypes(int projectId, string vesseltype)
        {
            try
            {
                return await Sp_GetTankTypeByTankMapping(projectId, vesseltype);
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<List<VesselDetails>> GetVesselIMONo()
        {
            try
            {
                return await Sp_GetIMONumber();
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<List<Core.Models.View.Admin.VesselTypes>> GetVesselType()
        {
            try
            {
                return await Sp_GetVesselType();
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<List<Core.Models.View.Admin.Tank>> GetTanks_Vessel(string username = null)
        {
            try
            {
                return await Sp_Get_Vessel_Tank(username);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<VesselTank> GetTanks_VesselById(Guid? Id, int? Projectid)
        {
            try
            {
                return await Sp_Get_Vessel_Tank_ById(Id, Projectid);
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<bool> UpdateTank(VesselTank vesselTank)
        {
            try
            {
                return await Sp_Update_Vessel_TankMapping(vesselTank);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vessel tank");
                return false;
            }
        }

        public async Task<List<VesselTankDetails>> GetVesselTypeList()
        {
            try
            {
                return await Sp_GetVesselTypeList();
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<List<IMOTankFilterModel>> GetProjectNames(string imoNumber)
        {
            try
            {
                return await Sp_GET_PROJECTNAME_BYIMO(imoNumber);
            }
            catch (Exception)
            {
                return null!;
            }
        }

        public async Task<bool> UpdateStatus(List<Guid> tankIds, bool status)
        {
            if (tankIds == null || tankIds.Count == 0)
                return false;

            try
            {
                return await Sp_UpdateTankStatus(tankIds, status);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task<IEnumerable<Core.Enumerations.ShipType>> GetShipTypes()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Stored Procedure Implementations

        private async Task<List<VesselTank>> Sp_Get_Vessel_Tank_ByIMONumber(string imonumber)
        {
            if (imonumber == null)
            {
                return new List<VesselTank>();
            }

            // INNER JOIN cap.Projects cp on cp.ID = vtm.ProjectId
            // Tank without an existing project -> dropped
            var projContainer = ContainerFactory.GetContainer(CosmosContainers.CAP_Projects);
            var projIds = new HashSet<int>();
            using (var feed = projContainer.GetItemQueryIterator<ProjectDocument>(new QueryDefinition("SELECT c.ID FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var p in response)
                    {
                        projIds.Add(p.EffectiveId);
                    }
                }
            }

            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var imoTrim = imonumber.TrimEnd(' ');
            var q = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.ImoNumber), @imo, true) AND (NOT IS_DEFINED(c.VesselType) OR IS_NULL(c.VesselType)) AND c.IsDeleted = false")
                .WithParameter("@imo", imoTrim);

            var list = new List<VesselTank>();
            using (var feed = container.GetItemQueryIterator<VesselTankMappingDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (doc.ImoNumber != null && string.Equals(doc.ImoNumber.TrimEnd(' '), imoTrim, StringComparison.OrdinalIgnoreCase) &&
                            doc.ProjectId.HasValue && projIds.Contains(doc.ProjectId.Value))
                        {
                            list.Add(MapToVesselTank(doc));
                        }
                    }
                }
            }

            return list;
        }

        private async Task<List<VesselTank>> Sp_CreateTanksofVesselByProject(string imonumber, int projectId, int newprojectId, string newimo)
        {
            if (imonumber == null)
            {
                return new List<VesselTank>();
            }

            // Returns no rows (it only INSERTs) -> Dapper QueryAsync gives an empty list
            var projContainer = ContainerFactory.GetContainer(CosmosContainers.CAP_Projects);
            var projQ = new QueryDefinition("SELECT * FROM c WHERE c.id = @pid OR c.ID = @pnum OR c.Id = @pnum")
                .WithParameter("@pid", projectId.ToString())
                .WithParameter("@pnum", projectId);
            var projDocs = new List<ProjectDocument>();
            using (var feed = projContainer.GetItemQueryIterator<ProjectDocument>(projQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(projectId.ToString()) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    projDocs.AddRange(response);
                }
            }

            if (projDocs.Count == 0)
            {
                using var crossFeed = projContainer.GetItemQueryIterator<ProjectDocument>(projQ);
                while (crossFeed.HasMoreResults)
                {
                    var response = await crossFeed.ReadNextAsync();
                    projDocs.AddRange(response);
                }
            }

            var matchingProj = projDocs.Where(d => d.EffectiveId == projectId).OrderBy(d => d.EffectiveId).ToList();
            string? vesseltype = matchingProj.LastOrDefault()?.VesselType;
            vesseltype = vesseltype?.Length > 50 ? vesseltype.Substring(0, 50) : vesseltype;

            // (Select Id from VesselType where VesselType = @vesseltype) is a scalar subquery: 2+ matches -> throw
            int? vesseltypeId = null;
            if (vesseltype != null)
            {
                var vtContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_VesselType);
                var vtList = new List<VesselTypeDocument>();
                using (var feed = vtContainer.GetItemQueryIterator<VesselTypeDocument>(new QueryDefinition("SELECT * FROM c")))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vtList.AddRange(response);
                    }
                }

                var matchingVt = vtList.Where(v => v.VesselType != null && string.Equals(v.VesselType.TrimEnd(' '), vesseltype.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)).ToList();
                if (matchingVt.Count > 1)
                {
                    throw new InvalidOperationException("Subquery returned more than 1 value. This is not permitted when the subquery follows =, !=, <, <= , >, >= or when the subquery is used as an expression.");
                }
                if (matchingVt.Count == 1)
                {
                    vesseltypeId = matchingVt[0].Id;
                }
            }

            var allowedTankTypeIds = new HashSet<int>();
            if (vesseltypeId.HasValue)
            {
                var vttContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
                var vttQ = new QueryDefinition("SELECT c.TankTypeId FROM c WHERE c.VesselTypeId = @vtid AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@vtid", vesseltypeId.Value);

                using var feed = vttContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(vttQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesseltypeId.Value) });
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (doc.TankTypeId.HasValue) allowedTankTypeIds.Add(doc.TankTypeId.Value);
                    }
                }
            }

            if (allowedTankTypeIds.Count > 0)
            {
                var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
                var imoTrim = imonumber.TrimEnd(' ');
                var q = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.ImoNumber), @imo, true) AND (NOT IS_DEFINED(c.VesselType) OR IS_NULL(c.VesselType)) AND c.IsDeleted = false AND c.ProjectId = @pid")
                    .WithParameter("@imo", imoTrim)
                    .WithParameter("@pid", projectId);

                var sourceTanks = new List<VesselTankMappingDocument>();
                using (var feed = container.GetItemQueryIterator<VesselTankMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var doc in response)
                        {
                            if (doc.ImoNumber != null && string.Equals(doc.ImoNumber.TrimEnd(' '), imoTrim, StringComparison.OrdinalIgnoreCase) &&
                                doc.TankTypeId.HasValue && allowedTankTypeIds.Contains(doc.TankTypeId.Value))
                            {
                                sourceTanks.Add(doc);
                            }
                        }
                    }
                }

                // ORDER BY TankTypeId, TankName, CreatedDttm (NULL first)
                var sortedSource = sourceTanks
                    .OrderBy(t => t.TankTypeId.HasValue)
                    .ThenBy(t => t.TankTypeId)
                    .ThenBy(t => t.TankName != null)
                    .ThenBy(t => t.TankName)
                    .ThenBy(t => t.CreatedDttm.HasValue)
                    .ThenBy(t => t.CreatedDttm)
                    .ToList();

                var now = DateTime.UtcNow;
                foreach (var src in sortedSource)
                {
                    var newGuid = Guid.NewGuid();
                    var newDoc = new VesselTankMappingDocument
                    {
                        id = newGuid.ToString(),
                        Id = newGuid.ToString(),
                        ImoNumber = newimo,
                        TankTypeId = src.TankTypeId,
                        TankName = src.TankName,
                        IsActive = src.IsActive,
                        CreatedDttm = now,
                        UpdateDttm = now,
                        IsDeleted = src.IsDeleted,
                        Subheader = src.Subheader,
                        RequiredInReport = src.RequiredInReport,
                        UpdatedBy = src.UpdatedBy,
                        ProjectId = newprojectId,
                        VesselType = null
                    };

                    await container.CreateItemAsync(newDoc, new PartitionKey((double)newprojectId));
                }
            }

            return new List<VesselTank>();
        }

        private async Task<List<VesselTank>> Sp_Get_Vessel_Tank_ByVesselType(string vesseltype)
        {
            if (vesseltype == null)
            {
                return new List<VesselTank>();
            }
            // Subquery VTM:
            // select distinct vts.VesselTypeId, v.VesselType ,vts.TankTypeId from config.VesselTypePartMapping vpm
            // inner join config.VesselTypeTankSectionMapping vts on vpm.VesselTypePartMappingId = vts.VesselTypePartMappingId
            // INNER JOIN VesselType v ON vts.VesselTypeId = v.Id
            // where vpm.IsActive=1 and vpm.IsDeleted=0 and vts.IsActive=1 and vts.IsDeleted=0
            var vpmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vpmList = new List<VesselTypePartMappingDocument>();
            using (var feed = vpmContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vpmList.AddRange(response);
                }
            }

            var vtsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
            var vtsList = new List<VesselTypeTankSectionMappingDocument>();
            using (var feed = vtsContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtsList.AddRange(response);
                }
            }

            var vtContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_VesselType);
            var vtList = new List<VesselTypeDocument>();
            using (var feed = vtContainer.GetItemQueryIterator<VesselTypeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtList.AddRange(response);
                }
            }
            var vtMap = vtList.ToDictionary(v => v.Id, v => v.VesselType);

            // Distinct (vts.VesselTypeId, v.VesselType, vts.TankTypeId)
            var vtmTuples = new List<(int VesselTypeId, string VesselType, int TankTypeId)>();
            foreach (var vpm in vpmList)
            {
                var matchingVts = vtsList.Where(vts => vts.VesselTypePartMappingId == vpm.VesselTypePartMappingId);
                foreach (var vts in matchingVts)
                {
                    if (vts.VesselTypeId.HasValue && vts.TankTypeId.HasValue && vtMap.TryGetValue(vts.VesselTypeId.Value, out var vtName))
                    {
                        vtmTuples.Add((vts.VesselTypeId.Value, vtName, vts.TankTypeId.Value));
                    }
                }
            }
            var distinctVtm = vtmTuples.Distinct().ToList();

            // Query Vessel_TankMapping: Where VesselType = @VesselType and ImoNumber is null and IsActive = 1 and IsDeleted = 0
            var tankContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var tankList = new List<VesselTankMappingDocument>();
            var tankQ = new QueryDefinition("SELECT * FROM c WHERE (NOT IS_DEFINED(c.ImoNumber) OR IS_NULL(c.ImoNumber)) AND c.IsActive = true AND c.IsDeleted = false");
            using (var feed = tankContainer.GetItemQueryIterator<VesselTankMappingDocument>(tankQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (string.Equals(doc.VesselType?.TrimEnd(' '), vesseltype.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                        {
                            tankList.Add(doc);
                        }
                    }
                }
            }

            var results = new List<VesselTank>();
            foreach (var tank in tankList)
            {
                // Join VTM on VTM.VesselType = tank.VesselType and VTM.TankTypeId = tank.TankTypeId
                var matchingVtm = distinctVtm.Where(v => string.Equals(v.VesselType?.TrimEnd(' '), tank.VesselType?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase) && v.TankTypeId == tank.TankTypeId);
                foreach (var match in matchingVtm)
                {
                    var vt = MapToVesselTank(tank);
                    // VesselType comes from the VesselType table spelling
                    vt.VesselType = match.VesselType;
                    results.Add(vt);
                }
            }

            return results;
        }

        private async Task<bool> Sp_Create_Vessel_TankMapping(VesselTank vesselTank)
        {
            if (vesselTank.CreatedDttm < new DateTime(1753, 1, 1) || vesselTank.UpdateDttm < new DateTime(1753, 1, 1))
            {
                throw new ArgumentOutOfRangeException("SqlDateTime overflow: must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");
            }

            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            string idStr = vesselTank.Id.ToString();

            // Check Id does not exist in ANY partition first
            var checkQ = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.Id = @id").WithParameter("@id", idStr);
            int totalCount = 0;
            using (var feed = container.GetItemQueryIterator<int>(checkQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    totalCount += response.Sum();
                }
            }
            if (totalCount > 0)
            {
                throw new InvalidOperationException($"Violation of PRIMARY KEY constraint. Cannot insert duplicate key in object 'dbo.Vessel_TankMapping'. The duplicate key value is ({idStr}).");
            }

            string? subheader = vesselTank.Subheader?.Length > 100 ? vesselTank.Subheader.Substring(0, 100) : vesselTank.Subheader;
            var now = DateTime.UtcNow;

            var newDoc = new VesselTankMappingDocument
            {
                id = idStr,
                Id = idStr,
                VesselType = vesselTank.VesselType,
                ImoNumber = vesselTank.ImoNumber,
                TankTypeId = vesselTank.TankTypeId,
                TankName = vesselTank.TankName,
                IsActive = vesselTank.IsActive,
                CreatedDttm = now,
                UpdateDttm = now,
                IsDeleted = vesselTank.IsDeleted,
                Subheader = subheader,
                RequiredInReport = vesselTank.RequiredInReport,
                UpdatedBy = null,
                ProjectId = vesselTank.ProjectId
            };

            PartitionKey pk = vesselTank.ProjectId.HasValue ? new PartitionKey((double)vesselTank.ProjectId.Value) : PartitionKey.Null;
            await container.CreateItemAsync(newDoc, pk);
            return true;
        }

        private async Task<List<Core.Models.Tank.ShipType>> Sp_GetShipType()
        {
            var vtContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_VesselType);
            var list = new List<Core.Models.Tank.ShipType>();
            using (var feed = vtContainer.GetItemQueryIterator<VesselTypeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        list.Add(new Core.Models.Tank.ShipType
                        {
                            Id = doc.Id,
                            VesselType = doc.VesselType
                        });
                    }
                }
            }
            return list;
        }

        private async Task<List<VesselTank>> Sp_Get_Vessel_Tank_ByProject(string imonumber, int projectId)
        {
            if (imonumber == null)
            {
                return new List<VesselTank>();
            }
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var imoTrim = imonumber.TrimEnd(' ');
            var q = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.ImoNumber), @imo, true) AND (NOT IS_DEFINED(c.VesselType) OR IS_NULL(c.VesselType)) AND c.IsDeleted = false AND c.ProjectId = @pid")
                .WithParameter("@imo", imoTrim)
                .WithParameter("@pid", projectId);

            var list = new List<VesselTankMappingDocument>();
            using (var feed = container.GetItemQueryIterator<VesselTankMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (doc.ImoNumber != null && string.Equals(doc.ImoNumber.TrimEnd(' '), imoTrim, StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(doc);
                        }
                    }
                }
            }

            // ORDER BY TankTypeId, TankName, CreatedDttm (NULL first)
            return list
                .OrderBy(t => t.TankTypeId.HasValue)
                .ThenBy(t => t.TankTypeId)
                .ThenBy(t => t.TankName != null)
                .ThenBy(t => t.TankName)
                .ThenBy(t => t.CreatedDttm.HasValue)
                .ThenBy(t => t.CreatedDttm)
                .Select(MapToVesselTank)
                .ToList();
        }

        private async Task<List<VesselTankGrading>> Sp_Get_Vessel_Tank_Grading_ByVesselType(string vesseltype)
        {
            if (vesseltype == null)
            {
                return new List<VesselTankGrading>();
            }
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_GradingMapping);
            var q = new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND (NOT IS_DEFINED(c.ProjectId) OR IS_NULL(c.ProjectId) OR c.ProjectId = 0) AND (NOT IS_DEFINED(c.IsDeleted) OR IS_NULL(c.IsDeleted) OR c.IsDeleted = false)");

            var list = new List<VesselTankGrading>();
            using (var feed = container.GetItemQueryIterator<VesselGradingMappingDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (string.Equals(doc.VesselType?.TrimEnd(' '), vesseltype.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(new VesselTankGrading
                            {
                                Id = doc.Id,
                                VesselName = null!,
                                VesselType = doc.VesselType!,
                                TankTypeId = doc.TankTypeId ?? 0,
                                GradingName = doc.GradingName!,
                                GradingGroupId = doc.GradingGroupId?.ToString()!,
                                GradingConditionGroupId = doc.GradingConditionGroupId?.ToString()!,
                                IsActive = doc.IsActive ?? false,
                                CreatedDttm = doc.CreatedDttm ?? default,
                                UpdateDttm = doc.UpdateDttm ?? default,
                                TemplateId = 0,
                                ProjectId = doc.ProjectId ?? 0,
                                RequiredInReport = doc.RequiredInReport ?? false
                            });
                        }
                    }
                }
            }

            return list;
        }

        private static int? ParseIntParam(string? val)
        {
            if (val == null) return null;
            if (string.IsNullOrWhiteSpace(val)) return 0;
            if (int.TryParse(val, out var parsed)) return parsed;
            throw new FormatException($"Conversion failed when converting the varchar value '{val}' to data type int.");
        }

        private async Task<bool> Sp_Create_Vessel_Grading(VesselTankGrading vesselTankGrading)
        {
            int? gradingGroupId = ParseIntParam(vesselTankGrading.GradingGroupId);
            int? gradingConditionGroupId = ParseIntParam(vesselTankGrading.GradingConditionGroupId);

            DateTime minSqlDate = new DateTime(1753, 1, 1);
            if (vesselTankGrading.CreatedDttm < minSqlDate)
                throw new ArgumentOutOfRangeException(nameof(vesselTankGrading.CreatedDttm), "SqlDateTime overflow: must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");
            if (vesselTankGrading.UpdateDttm < minSqlDate)
                throw new ArgumentOutOfRangeException(nameof(vesselTankGrading.UpdateDttm), "SqlDateTime overflow: must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");

            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_GradingMapping);
            var newId = (int)await _sequenceService.GetNextAsync(CosmosContainers.dbo_Vessel_GradingMapping);

            var newDoc = new VesselGradingMappingDocument
            {
                id = newId.ToString(),
                Id = newId,
                VesselType = vesselTankGrading.VesselType,
                TankTypeId = vesselTankGrading.TankTypeId,
                GradingName = vesselTankGrading.GradingName,
                GradingGroupId = gradingGroupId,
                GradingConditionGroupId = gradingConditionGroupId,
                IsActive = vesselTankGrading.IsActive,
                CreatedDttm = vesselTankGrading.CreatedDttm,
                UpdateDttm = vesselTankGrading.UpdateDttm,
                IsDeleted = false,
                ProjectId = vesselTankGrading.ProjectId,
                RequiredInReport = vesselTankGrading.RequiredInReport
            };

            PartitionKey pk = new PartitionKey((double)vesselTankGrading.ProjectId);
            await container.CreateItemAsync(newDoc, pk);
            return true;
        }

        private async Task<List<TankUI>> Sp_GetTempaleTanks(int templateId, string? imonumber, string? vesseltype, int projectId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var ttContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_TankType);

            int vesselmatchcount = 0;
            if (vesseltype != null)
            {
                var q = new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true");
                var vtmDocs = new List<VesselTankMappingDocument>();
                using (var feed = container.GetItemQueryIterator<VesselTankMappingDocument>(q))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var doc in response)
                        {
                            if (string.Equals(doc.VesselType?.TrimEnd(' '), vesseltype.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                            {
                                vtmDocs.Add(doc);
                            }
                        }
                    }
                }

                vesselmatchcount = vtmDocs.Count;
                if (vesselmatchcount > 0)
                {
                    var validTanks = vtmDocs.Where(d => d.IsDeleted == false).ToList();
                    var ttQ = new QueryDefinition("SELECT * FROM c WHERE c.TemplateId = @templateId").WithParameter("@templateId", templateId);
                    var ttDocs = new List<TankTypeDocument>();
                    using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(ttQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)templateId) }))
                    {
                        while (feed.HasMoreResults)
                        {
                            var response = await feed.ReadNextAsync();
                            ttDocs.AddRange(response);
                        }
                    }
                    var ttMap = ttDocs.ToDictionary(t => t.Id, t => t);

                    var list = new List<TankUI>();
                    foreach (var tank in validTanks)
                    {
                        if (tank.TankTypeId.HasValue && ttMap.TryGetValue(tank.TankTypeId.Value, out var tt))
                        {
                            list.Add(new TankUI
                            {
                                Id = Guid.TryParse(tank.Id, out var g) ? g : Guid.Empty,
                                TankName = tank.TankName!,
                                TankTypeId = tank.TankTypeId.Value,
                                CurrentconditionPlaceholders = tt.CurrentconditionPlaceholders,
                                Subheader = tank.Subheader!
                            });
                        }
                    }

                    return list
                        .OrderBy(t => t.TankTypeId)
                        .ThenBy(t => t.TankName != null)
                        .ThenBy(t => t.TankName)
                        .ToList();
                }
            }

            if (imonumber != null && vesselmatchcount == 0)
            {
                var imoTrim = imonumber.TrimEnd(' ');
                var q = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.ImoNumber), @imo, true) AND c.IsActive = true AND c.ProjectId = @pid")
                    .WithParameter("@imo", imoTrim)
                    .WithParameter("@pid", projectId);

                var imoDocs = new List<VesselTankMappingDocument>();
                using (var feed = container.GetItemQueryIterator<VesselTankMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var doc in response)
                        {
                            if (doc.ImoNumber != null && string.Equals(doc.ImoNumber.TrimEnd(' '), imoTrim, StringComparison.OrdinalIgnoreCase))
                            {
                                imoDocs.Add(doc);
                            }
                        }
                    }
                }

                if (imoDocs.Count > 0)
                {
                    var validTanks = imoDocs.Where(d => d.IsDeleted == false).ToList();
                    var ttQ = new QueryDefinition("SELECT * FROM c WHERE c.TemplateId = @templateId").WithParameter("@templateId", templateId);
                    var ttDocs = new List<TankTypeDocument>();
                    using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(ttQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)templateId) }))
                    {
                        while (feed.HasMoreResults)
                        {
                            var response = await feed.ReadNextAsync();
                            ttDocs.AddRange(response);
                        }
                    }
                    var ttMap = ttDocs.ToDictionary(t => t.Id, t => t);

                    var list = new List<TankUI>();
                    foreach (var tank in validTanks)
                    {
                        if (tank.TankTypeId.HasValue && ttMap.TryGetValue(tank.TankTypeId.Value, out var tt))
                        {
                            list.Add(new TankUI
                            {
                                Id = Guid.TryParse(tank.Id, out var g) ? g : Guid.Empty,
                                TankName = tank.TankName!,
                                TankTypeId = tank.TankTypeId.Value,
                                CurrentconditionPlaceholders = tt.CurrentconditionPlaceholders,
                                Subheader = tank.Subheader!
                            });
                        }
                    }

                    return list
                        .OrderBy(t => t.TankTypeId)
                        .ThenBy(t => t.TankName != null)
                        .ThenBy(t => t.TankName)
                        .ToList();
                }
            }

            return new List<TankUI>();
        }

        private async Task<List<TankCheckBox>> Sp_GetTankGradingConditionById(int id)
        {
            var vgmContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_GradingMapping);
            var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", id);
            var vgmDocs = new List<VesselGradingMappingDocument>();
            using (var feed = vgmContainer.GetItemQueryIterator<VesselGradingMappingDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vgmDocs.AddRange(response);
                }
            }

            var groupIds = vgmDocs
                .Where(v => v.GradingConditionGroupId.HasValue)
                .Select(v => v.GradingConditionGroupId!.Value)
                .Distinct()
                .ToList();

            if (groupIds.Count == 0)
            {
                return new List<TankCheckBox>();
            }

            var gcContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_GradingCondition);
            var gcQ = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@gids, c.GradingConditionGroupId)")
                .WithParameter("@gids", groupIds);
            var gcDocs = new List<GradingConditionDocument>();
            using (var feed = gcContainer.GetItemQueryIterator<GradingConditionDocument>(gcQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    gcDocs.AddRange(response);
                }
            }

            var results = new List<TankCheckBox>();
            var seen = new HashSet<int>();
            foreach (var vgm in vgmDocs.Where(v => v.GradingConditionGroupId.HasValue))
            {
                var matchingGc = gcDocs.Where(gc => gc.GradingConditionGroupId == vgm.GradingConditionGroupId.Value);
                foreach (var gc in matchingGc)
                {
                    if (seen.Add(gc.Id))
                    {
                        results.Add(new TankCheckBox
                        {
                            Id = gc.Id,
                            Name = gc.Condition,
                            Value = false
                        });
                    }
                }
            }
            return results;
        }

        private async Task<List<TankGradingUI>> Sp_GetTempaleTankGrading(int tankTypeId, int projectId, string vesselType)
        {
            vesselType = vesselType?.Length > 1000 ? vesselType.Substring(0, 1000) : vesselType;

            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_GradingMapping);
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@pid", projectId);

            var docs = new List<VesselGradingMappingDocument>();
            using (var feed = container.GetItemQueryIterator<VesselGradingMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (vesselType != null && string.Equals(doc.VesselType?.TrimEnd(' '), vesselType.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                        {
                            if (tankTypeId == 0)
                            {
                                // @TankTypeId = NULL -> TankTypeId = TankTypeId excludes NULL TankTypeId
                                if (doc.TankTypeId.HasValue) docs.Add(doc);
                            }
                            else
                            {
                                if (doc.TankTypeId.HasValue && doc.TankTypeId.Value == tankTypeId) docs.Add(doc);
                            }
                        }
                    }
                }
            }

            // ORDER BY TankTypeId, Name (NULL first)
            return docs
                .OrderBy(d => d.TankTypeId.HasValue)
                .ThenBy(d => d.TankTypeId)
                .ThenBy(d => d.GradingName != null)
                .ThenBy(d => d.GradingName)
                .Select(d => new TankGradingUI
                {
                    Id = d.Id,
                    Name = d.GradingName!,
                    GradingConditionGroupId = d.GradingConditionGroupId ?? 0,
                    RequiredInReport = d.RequiredInReport ?? false,
                    TankTypeId = d.TankTypeId ?? 0
                })
                .ToList();
        }

        private async Task<List<TankImageCard>> Sp_GetTankImageCard(int projectId, int templateId, Guid sectionId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_TankImageCard);
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid AND c.TemplateId = @tid AND c.IsActive = true")
                .WithParameter("@pid", projectId)
                .WithParameter("@tid", templateId);

            var cards = new List<TankImageCardDocument>();
            using (var feed = container.GetItemQueryIterator<TankImageCardDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (sectionId == Guid.Empty)
                        {
                            // @SectionId = null -> SectionId = ISNULL(@SectionId, SectionId) matches any non-null SectionId
                            if (!string.IsNullOrEmpty(doc.SectionId)) cards.Add(doc);
                        }
                        else
                        {
                            if (string.Equals(doc.SectionId, sectionId.ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                cards.Add(doc);
                            }
                        }
                    }
                }
            }

            // LEFT OUTER JOIN upskill.ImageData on TankImageCard.ImageId = ImageData.ImageId
            var distinctImageIds = cards.Where(c => c.ImageId.HasValue).Select(c => c.ImageId!.Value).Distinct().ToList();
            var imgLookup = Enumerable.Empty<ImageDataDocument>().ToLookup(i => i.ImageId!.Value, i => i.Base64String);
            if (distinctImageIds.Count > 0)
            {
                var imgContainer = ContainerFactory.GetContainer(CosmosContainers.upskill_ImageData);
                var imgQ = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@ids, c.ImageId)")
                    .WithParameter("@ids", distinctImageIds);
                var imgDocs = new List<ImageDataDocument>();
                using (var feed = imgContainer.GetItemQueryIterator<ImageDataDocument>(imgQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        imgDocs.AddRange(response);
                    }
                }
                imgLookup = imgDocs.Where(i => i.ImageId.HasValue).ToLookup(i => i.ImageId!.Value, i => i.Base64String);
            }

            var results = new List<TankImageCard>();
            foreach (var card in cards)
            {
                Guid cardSectionGuid = Guid.TryParse(card.SectionId, out var g) ? g : Guid.Empty;
                if (card.ImageId.HasValue && imgLookup.Contains(card.ImageId.Value))
                {
                    // Several ImageData rows with same ImageId duplicate the card
                    foreach (var base64 in imgLookup[card.ImageId.Value])
                    {
                        results.Add(new TankImageCard
                        {
                            Id = card.Id,
                            CardName = card.CardName,
                            CardNumber = card.CardNumber ?? 0,
                            DescriptionId = card.DescriptionId ?? 0,
                            AdditionalDescription = card.AdditionalDescription,
                            CurrentCondition = card.CurrentCondition,
                            ImageId = card.ImageId ?? 0,
                            IsActive = card.IsActive ?? false,
                            src = base64,
                            SectionId = cardSectionGuid
                        });
                    }
                }
                else
                {
                    results.Add(new TankImageCard
                    {
                        Id = card.Id,
                        CardName = card.CardName,
                        CardNumber = card.CardNumber ?? 0,
                        DescriptionId = card.DescriptionId ?? 0,
                        AdditionalDescription = card.AdditionalDescription,
                        CurrentCondition = card.CurrentCondition,
                        ImageId = card.ImageId ?? 0,
                        IsActive = card.IsActive ?? false,
                        src = null,
                        SectionId = cardSectionGuid
                    });
                }
            }

            return results;
        }

        private async Task<TankImageCard> Sp_GetProjectTankImagecardByName(int projectId, int templateId, Guid sectionId, int cardNumber)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_TankImageCard);
            string secIdStr = sectionId.ToString();

            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid AND c.TemplateId = @tid AND c.CardNumber = @num AND c.SectionId = @sec AND c.IsActive = true")
                .WithParameter("@pid", projectId)
                .WithParameter("@tid", templateId)
                .WithParameter("@num", cardNumber)
                .WithParameter("@sec", secIdStr);

            var docs = new List<TankImageCardDocument>();
            using (var feed = container.GetItemQueryIterator<TankImageCardDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    docs.AddRange(response);
                }
            }

            var doc = docs.OrderBy(d => d.Id).FirstOrDefault();
            if (doc == null) return null!;

            return new TankImageCard
            {
                Id = doc.Id,
                ProjectId = doc.ProjectId ?? 0,
                TemplateId = doc.TemplateId ?? 0,
                SectionId = Guid.TryParse(doc.SectionId, out var g) ? g : Guid.Empty,
                CardNumber = doc.CardNumber ?? 0,
                DescriptionId = doc.DescriptionId ?? 0,
                AdditionalDescription = doc.AdditionalDescription,
                CurrentCondition = doc.CurrentCondition,
                ImageId = doc.ImageId ?? 0,
                CreatedDttm = doc.CreatedDttm ?? default,
                UpdatedDttm = doc.UpdatedDttm ?? default,
                IsActive = doc.IsActive ?? false,
                CardName = doc.CardName
            };
        }

        private Task<bool> Sp_UpdateTankImageCard(TankImageCard tankImageCard)
        {
            // SQL BUG KEPT: The C# does not pass @Id, WHERE Id = NULL in SQL matches 0 rows -> affected rows is always 0 -> returns false
            return Task.FromResult(false);
        }

        private async Task<bool> Sp_CreateTankImageCard(TankImageCard tankImageCard)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_TankImageCard);
            var newId = (int)await _sequenceService.GetNextAsync(CosmosContainers.dbo_TankImageCard);

            var newDoc = new TankImageCardDocument
            {
                id = newId.ToString(),
                Id = newId,
                ProjectId = tankImageCard.ProjectId,
                TemplateId = tankImageCard.TemplateId,
                SectionId = tankImageCard.SectionId.ToString(),
                CardNumber = tankImageCard.CardNumber,
                DescriptionId = tankImageCard.DescriptionId,
                AdditionalDescription = tankImageCard.AdditionalDescription,
                CurrentCondition = tankImageCard.CurrentCondition,
                ImageId = tankImageCard.ImageId,
                CreatedDttm = null,
                UpdatedDttm = null,
                IsActive = tankImageCard.IsActive,
                CardName = tankImageCard.CardName
            };

            await container.CreateItemAsync(newDoc, new PartitionKey((double)tankImageCard.ProjectId));
            return true;
        }

        private async Task<List<TankTypes>> Sp_GetTankType()
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_TankType);
            var list = new List<TankTypes>();
            using (var feed = container.GetItemQueryIterator<TankTypeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        list.Add(new TankTypes
                        {
                            Id = doc.Id,
                            TankType = doc.TankType,
                            TankName = doc.TankType
                        });
                    }
                }
            }
            return list;
        }

        private async Task<List<TankTypes>> Sp_GetTankTypeByTankMapping(int projectId, string? veseltype)
        {
            var ttContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_TankType);
            var ttList = new List<TankTypeDocument>();
            using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    ttList.AddRange(response);
                }
            }
            var ttMap = ttList.ToDictionary(t => t.Id, t => t);

            if (projectId == 0)
            {
                // config.VesselTypeTankSectionMapping vts inner join TankType tt on vts.TankTypeId = tt.id
                // inner join VesselType vt on vt.Id = vts.VesselTypeId
                // inner join config.VesselTypePartMapping vtp on vtp.VesselTypePartMappingId = vts.VesselTypePartMappingId and vtp.VesselTypeId = vts.VesselTypeId and vtp.VesselTypeId = vt.Id and vtp.IsActive = 1 and vtp.IsDeleted = 0
                // where vts.IsDeleted = 0 and vts.IsActive = 1 and TRIM(vt.VesselType) = TRIM(ISNULL(@Veseltype, vt.VesselType))
                var vtsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
                var vtsList = new List<VesselTypeTankSectionMappingDocument>();
                using (var feed = vtsContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsDeleted = false AND c.IsActive = true")))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vtsList.AddRange(response);
                    }
                }

                var vtContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_VesselType);
                var vtList = new List<VesselTypeDocument>();
                using (var feed = vtContainer.GetItemQueryIterator<VesselTypeDocument>(new QueryDefinition("SELECT * FROM c")))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vtList.AddRange(response);
                    }
                }
                var vtMap = vtList.ToDictionary(v => v.Id, v => v.VesselType);

                var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
                var vtpList = new List<VesselTypePartMappingDocument>();
                using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false")))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vtpList.AddRange(response);
                    }
                }

                var distinctTankTypes = new Dictionary<int, TankTypes>();
                foreach (var vts in vtsList)
                {
                    if (vts.TankTypeId.HasValue && vts.VesselTypeId.HasValue && ttMap.TryGetValue(vts.TankTypeId.Value, out var tt))
                    {
                        if (vtMap.TryGetValue(vts.VesselTypeId.Value, out var vtName))
                        {
                            bool vtpMatches = vtpList.Any(vtp => vtp.VesselTypePartMappingId == vts.VesselTypePartMappingId && vtp.VesselTypeId == vts.VesselTypeId.Value);
                            if (vtpMatches)
                            {
                                // TRIM(vt.VesselType) = TRIM(ISNULL(@Veseltype, vt.VesselType)) (NULL VesselType rows never match)
                                if (vtName != null)
                                {
                                    bool nameMatches = veseltype != null
                                        ? string.Equals(vtName.Trim(' '), veseltype.Trim(' '), StringComparison.OrdinalIgnoreCase)
                                        : true;

                                    if (nameMatches && !distinctTankTypes.ContainsKey(tt.Id))
                                    {
                                        distinctTankTypes[tt.Id] = new TankTypes
                                        {
                                            Id = tt.Id,
                                            TankName = tt.TankType,
                                            TankType = tt.TankType
                                        };
                                    }
                                }
                            }
                        }
                    }
                }

                return distinctTankTypes.Values.ToList();
            }
            else
            {
                // UNION of config.ProjectTankSectionMapping and Vessel_TankMapping for ProjectId
                var distinctTankTypes = new Dictionary<int, TankTypes>();

                var ptsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectTankSectionMapping);
                var ptsQ = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid AND c.IsDeleted = false AND c.IsActive = true").WithParameter("@pid", projectId);
                using (var feed = ptsContainer.GetItemQueryIterator<ProjectTankSectionMappingDocument>(ptsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var doc in response)
                        {
                            if (doc.TankTypeId.HasValue && ttMap.TryGetValue(doc.TankTypeId.Value, out var tt) && !distinctTankTypes.ContainsKey(tt.Id))
                            {
                                distinctTankTypes[tt.Id] = new TankTypes
                                {
                                    Id = tt.Id,
                                    TankName = tt.TankType,
                                    TankType = tt.TankType
                                };
                            }
                        }
                    }
                }

                var vtmContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
                var vtmQ = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid AND c.IsDeleted = false AND c.IsActive = true").WithParameter("@pid", projectId);
                using (var feed = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(vtmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var doc in response)
                        {
                            if (doc.TankTypeId.HasValue && ttMap.TryGetValue(doc.TankTypeId.Value, out var tt) && !distinctTankTypes.ContainsKey(tt.Id))
                            {
                                distinctTankTypes[tt.Id] = new TankTypes
                                {
                                    Id = tt.Id,
                                    TankName = tt.TankType,
                                    TankType = tt.TankType
                                };
                            }
                        }
                    }
                }

                return distinctTankTypes.Values.ToList();
            }
        }

        private async Task<List<VesselDetails>> Sp_GetIMONumber()
        {
            var syncContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_IsSynchedOnline);
            var syncProjectIds = new HashSet<int>();
            using (var feed = syncContainer.GetItemQueryIterator<IsSynchedOnlineDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsSynched = false AND c.IsReadOnly = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var s in response)
                    {
                        if (s.ProjectId.HasValue) syncProjectIds.Add(s.ProjectId.Value);
                    }
                }
            }

            var projContainer = ContainerFactory.GetContainer(CosmosContainers.CAP_Projects);
            var q = new QueryDefinition("SELECT * FROM c WHERE IS_DEFINED(c.IMO) AND NOT IS_NULL(c.IMO) AND c.IsDeleted = false");
            var projects = new List<ProjectDocument>();
            using (var feed = projContainer.GetItemQueryIterator<ProjectDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    projects.AddRange(response);
                }
            }

            // DISTINCT IMO, VesselName where b.ProjectId is null, order by IMO
            var validProjects = projects
                .Where(p => p.IMO.HasValue && !syncProjectIds.Contains(p.EffectiveId))
                .OrderBy(p => p.EffectiveId)
                .ToList();

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<(int IMO, string? VesselName)>();

            foreach (var p in validProjects)
            {
                var trimmedName = p.VesselName?.TrimEnd(' ');
                var key = $"{p.IMO!.Value}::{trimmedName ?? "<null>"}";
                if (seen.Add(key))
                {
                    results.Add((p.IMO.Value, p.VesselName));
                }
            }

            return results
                .OrderBy(r => r.IMO)
                .Select(r => new VesselDetails
                {
                    IMO = r.IMO.ToString(),
                    VesselName = r.VesselName
                })
                .ToList();
        }

        private async Task<List<Core.Models.View.Admin.VesselTypes>> Sp_GetVesselType()
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var q = new QueryDefinition("SELECT DISTINCT VALUE c.VesselType FROM c WHERE IS_DEFINED(c.VesselType) AND NOT IS_NULL(c.VesselType)");
            var list = new List<string>();
            using (var feed = container.GetItemQueryIterator<string>(q))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    list.AddRange(response);
                }
            }

            // DISTINCT in SQL ignores case and trailing spaces
            var distinctMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var vt in list)
            {
                var trimmed = vt.TrimEnd(' ');
                if (!distinctMap.ContainsKey(trimmed))
                {
                    distinctMap[trimmed] = vt;
                }
            }

            return distinctMap.Values
                .Select(v => new Core.Models.View.Admin.VesselTypes { VesselType = v })
                .ToList();
        }

        private async Task<List<Core.Models.View.Admin.Tank>> Sp_Get_Vessel_Tank(string? username)
        {
            int? ParseImo(string? s)
            {
                if (s == null) return null;
                var trimmed = s.Trim();
                if (trimmed.Length == 0) return 0;
                if (int.TryParse(trimmed, out var val)) return val;
                throw new FormatException($"Conversion failed when converting the varchar value '{s}' to data type int.");
            }

            int? ParseShipType(object? shipType)
            {
                if (shipType == null) return null;
                if (shipType is int i) return i;
                if (shipType is long l) return (int)l;
                if (int.TryParse(shipType.ToString(), out var parsed)) return parsed;
                return null;
            }

            var tankContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var allTanks = new List<VesselTankMappingDocument>();
            using (var feed = tankContainer.GetItemQueryIterator<VesselTankMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsDeleted = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    allTanks.AddRange(response);
                }
            }

            var ttContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_TankType);
            var ttList = new List<TankTypeDocument>();
            using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    ttList.AddRange(response);
                }
            }
            var ttMap = ttList.ToDictionary(t => t.Id, t => t.TankType);

            var projContainer = ContainerFactory.GetContainer(CosmosContainers.CAP_Projects);
            var projList = new List<ProjectDocument>();
            using (var feed = projContainer.GetItemQueryIterator<ProjectDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    projList.AddRange(response);
                }
            }

            var syncContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_IsSynchedOnline);
            var syncProjectIds = new HashSet<int>();
            using (var feed = syncContainer.GetItemQueryIterator<IsSynchedOnlineDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsSynched = false AND c.IsReadOnly = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var s in response)
                    {
                        if (s.ProjectId.HasValue) syncProjectIds.Add(s.ProjectId.Value);
                    }
                }
            }

            var vtContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_VesselType);
            var vtList = new List<VesselTypeDocument>();
            using (var feed = vtContainer.GetItemQueryIterator<VesselTypeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtList.AddRange(response);
                }
            }
            var vtMap = vtList.ToDictionary(v => v.Id, v => v.VesselType);

            if (username == null)
            {
                var results = new List<Core.Models.View.Admin.Tank>();

                // data_c for Part 1
                var vpmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
                var vpmList = new List<VesselTypePartMappingDocument>();
                using (var feed = vpmContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false")))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vpmList.AddRange(response);
                    }
                }

                var vttContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
                var vttList = new List<VesselTypeTankSectionMappingDocument>();
                using (var feed = vttContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false")))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vttList.AddRange(response);
                    }
                }

                var data_c = new List<(int VesselTypeId, string VesselType, int TankTypeId)>();
                foreach (var vpm in vpmList)
                {
                    var matchingVtt = vttList.Where(vts => vts.VesselTypePartMappingId == vpm.VesselTypePartMappingId);
                    foreach (var vts in matchingVtt)
                    {
                        if (vts.VesselTypeId.HasValue && vts.TankTypeId.HasValue && vtMap.TryGetValue(vts.VesselTypeId.Value, out var vtName))
                        {
                            data_c.Add((vts.VesselTypeId.Value, vtName, vts.TankTypeId.Value));
                        }
                    }
                }
                var distinctDataC = data_c.Distinct().ToList();

                // Part 1: (ImoNumber is null or ImoNumber = 0) and vt.IsDeleted = 0
                foreach (var vt in allTanks)
                {
                    int? parsedImo = ParseImo(vt.ImoNumber);
                    if (parsedImo == null || parsedImo.Value == 0)
                    {
                        if (vt.TankTypeId.HasValue && ttMap.TryGetValue(vt.TankTypeId.Value, out var tankTypeName))
                        {
                            var matchingC = distinctDataC.Where(c => string.Equals(c.VesselType?.TrimEnd(' '), vt.VesselType?.TrimEnd(' '), StringComparison.OrdinalIgnoreCase) && c.TankTypeId == vt.TankTypeId.Value);
                            foreach (var c in matchingC)
                            {
                                results.Add(new Core.Models.View.Admin.Tank
                                {
                                    TankId = Guid.TryParse(vt.Id, out var g) ? g : Guid.Empty,
                                    VesselType = vt.VesselType ?? c.VesselType,
                                    TankName = vt.TankName,
                                    Status = vt.IsActive ?? true, // Model Tank.Status default true
                                    IMONumber = vt.ImoNumber,
                                    Subheader = vt.Subheader,
                                    VesselName = null,
                                    TankType = tankTypeName,
                                    IsDeleted = vt.IsDeleted ?? false,
                                    IsDownload = false,
                                    ProjectName = "",
                                    ProjectId = vt.ProjectId ?? 0
                                });
                            }
                        }
                    }
                }

                // Part 2: (ImoNumber is not null and ImoNumber != 0) and vt.IsDeleted = 0
                var projectRows = projList.Select(p => new
                {
                    ID = p.EffectiveId,
                    p.IMO,
                    ShipType = ParseShipType(p.ShipType),
                    p.VesselName,
                    IsDownload = syncProjectIds.Contains(p.EffectiveId) ? 1 : 0,
                    Name = p.Name ?? ""
                }).Distinct().ToList();

                foreach (var vt in allTanks)
                {
                    int? parsedImo = ParseImo(vt.ImoNumber);
                    if (parsedImo != null && parsedImo.Value != 0 && vt.ProjectId.HasValue)
                    {
                        if (vt.TankTypeId.HasValue && ttMap.TryGetValue(vt.TankTypeId.Value, out var tankTypeName))
                        {
                            var matchingProjects = projectRows.Where(p => p.IMO.HasValue && p.IMO.Value == parsedImo.Value && p.ID == vt.ProjectId.Value);
                            foreach (var proj in matchingProjects)
                            {
                                string? vName = proj.ShipType.HasValue && vtMap.TryGetValue(proj.ShipType.Value, out var vtNameVal) ? vtNameVal : null;
                                results.Add(new Core.Models.View.Admin.Tank
                                {
                                    TankId = Guid.TryParse(vt.Id, out var g) ? g : Guid.Empty,
                                    VesselType = vt.VesselType ?? vName,
                                    TankName = vt.TankName,
                                    Status = vt.IsActive ?? true,
                                    IMONumber = vt.ImoNumber,
                                    Subheader = vt.Subheader,
                                    VesselName = proj.VesselName,
                                    TankType = tankTypeName,
                                    IsDeleted = vt.IsDeleted ?? false,
                                    IsDownload = proj.IsDownload != 0,
                                    ProjectName = proj.Name,
                                    ProjectId = vt.ProjectId ?? 0
                                });
                            }
                        }
                    }
                }

                return results;
            }
            else
            {
                // Username branch
                bool MatchesLike(string? input, string pattern)
                {
                    if (input == null) return false;
                    // SQL LIKE pattern: % is .*, _ is ., [abc] is [abc]
                    string regexPattern = "^.*" + Regex.Escape(pattern).Replace("%", ".*").Replace("_", ".") + ".*$";
                    return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
                }

                var matchingImos = projList
                    .Where(p => (p.LeadSurveyor != null && string.Equals(p.LeadSurveyor.TrimEnd(' '), username.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)) ||
                                (p.SecondSurveyor != null && MatchesLike(p.SecondSurveyor, username)))
                    .Where(p => p.IMO.HasValue)
                    .Select(p => p.IMO!.Value)
                    .Distinct()
                    .ToHashSet();

                var projectRows = projList.Select(p => new
                {
                    ID = p.EffectiveId,
                    p.IMO,
                    ShipType = ParseShipType(p.ShipType),
                    p.VesselName,
                    IsDownload = syncProjectIds.Contains(p.EffectiveId) ? 1 : 0,
                    Name = p.Name ?? ""
                }).Distinct().ToList();

                var results = new List<Core.Models.View.Admin.Tank>();
                foreach (var vt in allTanks)
                {
                    if (vt.VesselType == null && vt.ImoNumber == null)
                        continue;

                    int? parsedImo = ParseImo(vt.ImoNumber);
                    if (parsedImo.HasValue && matchingImos.Contains(parsedImo.Value) && vt.ProjectId.HasValue)
                    {
                        if (vt.TankTypeId.HasValue && ttMap.TryGetValue(vt.TankTypeId.Value, out var tankTypeName))
                        {
                            var matchingProjects = projectRows.Where(p => p.IMO.HasValue && p.IMO.Value == parsedImo.Value && p.ID == vt.ProjectId.Value);
                            foreach (var proj in matchingProjects)
                            {
                                string? vName = proj.ShipType.HasValue && vtMap.TryGetValue(proj.ShipType.Value, out var vtNameVal) ? vtNameVal : null;
                                results.Add(new Core.Models.View.Admin.Tank
                                {
                                    TankId = Guid.TryParse(vt.Id, out var g) ? g : Guid.Empty,
                                    VesselType = vt.VesselType ?? vName,
                                    TankName = vt.TankName,
                                    Status = vt.IsActive ?? true,
                                    IMONumber = vt.ImoNumber,
                                    Subheader = vt.Subheader,
                                    VesselName = proj.VesselName,
                                    TankType = tankTypeName,
                                    IsDeleted = vt.IsDeleted ?? false,
                                    IsDownload = proj.IsDownload != 0,
                                    ProjectName = proj.Name,
                                    ProjectId = vt.ProjectId ?? 0
                                });
                            }
                        }
                    }
                }

                // Order by result.VesselType, result.TankType (NULL first)
                return results
                    .OrderBy(r => r.VesselType != null)
                    .ThenBy(r => r.VesselType)
                    .ThenBy(r => r.TankType != null)
                    .ThenBy(r => r.TankType)
                    .ToList();
            }
        }

        private async Task<VesselTank> Sp_Get_Vessel_Tank_ById(Guid? id, int? projectid)
        {
            if (!id.HasValue) return null!;
            string idStr = id.Value.ToString();

            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            if (!projectid.HasValue || projectid.Value == 0)
            {
                var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", idStr);
                using var feed = container.GetItemQueryIterator<VesselTankMappingDocument>(q);
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    var doc = response.FirstOrDefault();
                    if (doc != null) return MapToVesselTank(doc);
                }
                return null!;
            }
            else
            {
                var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id AND c.ProjectId = @pid AND c.IsDeleted = false")
                    .WithParameter("@id", idStr)
                    .WithParameter("@pid", projectid.Value);

                using var feed = container.GetItemQueryIterator<VesselTankMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectid.Value) });
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    var doc = response.FirstOrDefault();
                    if (doc != null) return MapToVesselTank(doc);
                }
                return null!;
            }
        }

        private async Task<bool> Sp_Update_Vessel_TankMapping(VesselTank vesselTank)
        {
            if (vesselTank.UpdateDttm < new DateTime(1753, 1, 1))
            {
                throw new ArgumentOutOfRangeException(nameof(vesselTank.UpdateDttm), "SqlDateTime overflow: must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");
            }

            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            string idStr = vesselTank.Id.ToString();

            // Find existing document across all partitions
            var findQ = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", idStr);
            var existingDocs = new List<VesselTankMappingDocument>();
            using (var feed = container.GetItemQueryIterator<VesselTankMappingDocument>(findQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    existingDocs.AddRange(response);
                }
            }

            var existing = existingDocs.FirstOrDefault();
            if (existing == null)
            {
                return false;
            }

            string? subheader = vesselTank.Subheader?.Length > 100 ? vesselTank.Subheader.Substring(0, 100) : vesselTank.Subheader;

            // Overwrites CreatedDttm with NULL (SQL bug kept!)
            existing.VesselType = vesselTank.VesselType;
            existing.ImoNumber = vesselTank.ImoNumber;
            existing.TankTypeId = vesselTank.TankTypeId;
            existing.TankName = vesselTank.TankName;
            existing.IsActive = vesselTank.IsActive;
            existing.CreatedDttm = null; // SQL bug kept: C# does not pass @CreatedDttm, SQL sets CreatedDttm = @CreatedDttm (NULL)
            existing.UpdateDttm = vesselTank.UpdateDttm;
            existing.IsDeleted = vesselTank.IsDeleted;
            existing.Subheader = subheader;
            existing.RequiredInReport = vesselTank.RequiredInReport;
            // ProjectId is NOT updated in SQL

            PartitionKey pk = existing.ProjectId.HasValue ? new PartitionKey((double)existing.ProjectId.Value) : PartitionKey.Null;
            await container.UpsertItemAsync(existing, pk);
            return true;
        }

        private async Task<List<VesselTankDetails>> Sp_GetVesselTypeList()
        {
            var projContainer = ContainerFactory.GetContainer(CosmosContainers.CAP_Projects);
            var q = new QueryDefinition("SELECT * FROM c WHERE IS_DEFINED(c.IMO) AND NOT IS_NULL(c.IMO)");
            var projects = new List<ProjectDocument>();
            using (var feed = projContainer.GetItemQueryIterator<ProjectDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    projects.AddRange(response);
                }
            }

            var vtContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_VesselType);
            var vtList = new List<VesselTypeDocument>();
            using (var feed = vtContainer.GetItemQueryIterator<VesselTypeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtList.AddRange(response);
                }
            }
            var vtMap = vtList.ToDictionary(v => v.Id, v => v.VesselType);

            int? ParseShipType(object? shipType)
            {
                if (shipType == null) return null;
                if (shipType is int i) return i;
                if (shipType is long l) return (int)l;
                if (int.TryParse(shipType.ToString(), out var parsed)) return parsed;
                return null;
            }

            var list = new List<VesselTankDetails>();
            foreach (var p in projects)
            {
                int? shipType = ParseShipType(p.ShipType);
                if (shipType.HasValue && vtMap.TryGetValue(shipType.Value, out var vtName))
                {
                    // In SQL: any NULL part -> whole CombinedDescription is NULL
                    string? combined;
                    if (p.IMO == null || p.VesselName == null || p.Name == null)
                    {
                        combined = null;
                    }
                    else if ((p.PIDNumberInt ?? 0) == 0)
                    {
                        combined = $"{p.IMO}/{p.VesselName}/{p.Name}";
                    }
                    else
                    {
                        combined = $"{p.IMO}/{p.VesselName}/{p.Name}/{p.PIDNumberInt}";
                    }

                    list.Add(new VesselTankDetails
                    {
                        ID = p.EffectiveId,
                        Name = p.Name!,
                        IMO = p.IMO?.ToString() ?? "",
                        VesselType = vtName!,
                        CombinedDescription = combined!
                    });
                }
            }

            // ORDER BY IMO, v.vesselType
            return list
                .OrderBy(d => int.TryParse(d.IMO, out var n) ? n : 0)
                .ThenBy(d => d.VesselType != null)
                .ThenBy(d => d.VesselType)
                .ToList();
        }

        private async Task<List<IMOTankFilterModel>> Sp_GET_PROJECTNAME_BYIMO(string? imoNumber)
        {
            if (imoNumber == null)
            {
                return new List<IMOTankFilterModel>();
            }

            int imoVal;
            if (string.IsNullOrWhiteSpace(imoNumber))
            {
                imoVal = 0;
            }
            else if (!int.TryParse(imoNumber.Trim(), out imoVal))
            {
                throw new FormatException($"Conversion failed when converting the varchar value '{imoNumber}' to data type int.");
            }

            var projContainer = ContainerFactory.GetContainer(CosmosContainers.CAP_Projects);
            var q = new QueryDefinition("SELECT * FROM c WHERE c.IMO = @imo").WithParameter("@imo", imoVal);
            var projects = new List<ProjectDocument>();
            using (var feed = projContainer.GetItemQueryIterator<ProjectDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    projects.AddRange(response);
                }
            }

            return projects
                .Where(p => p.IMO.HasValue && p.IMO.Value == imoVal)
                .Select(p => new IMOTankFilterModel
                {
                    Value = p.IMO!.Value.ToString(),
                    Text = p.Name!,
                    ProjectId = p.EffectiveId
                })
                .DistinctBy(m => new { m.Value, m.Text, m.ProjectId })
                .OrderBy(m => m.Value)
                .ToList();
        }

        private async Task<bool> Sp_UpdateTankStatus(List<Guid> tankIds, bool status)
        {
            if (tankIds == null || tankIds.Count == 0)
            {
                return false;
            }

            var idStrings = tankIds.Select(g => g.ToString().ToLowerInvariant()).Distinct().ToList();
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var q = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@ids, c.Id)")
                .WithParameter("@ids", idStrings);

            var matchingDocs = new List<VesselTankMappingDocument>();
            using (var feed = container.GetItemQueryIterator<VesselTankMappingDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    matchingDocs.AddRange(response);
                }
            }

            if (matchingDocs.Count == 0)
            {
                return false;
            }

            foreach (var doc in matchingDocs)
            {
                doc.IsActive = status;
                PartitionKey pk = doc.ProjectId.HasValue ? new PartitionKey((double)doc.ProjectId.Value) : PartitionKey.Null;
                await container.UpsertItemAsync(doc, pk);
            }

            return true;
        }

        private static VesselTank MapToVesselTank(VesselTankMappingDocument d)
        {
            return new VesselTank
            {
                Id = Guid.TryParse(d.Id, out var g) ? g : Guid.Empty,
                VesselType = d.VesselType!,
                ImoNumber = d.ImoNumber!,
                TankTypeId = d.TankTypeId ?? 0,
                TankName = d.TankName!,
                IsActive = d.IsActive ?? false,
                CreatedDttm = d.CreatedDttm ?? default,
                UpdateDttm = d.UpdateDttm ?? default,
                IsDeleted = d.IsDeleted ?? false,
                Subheader = d.Subheader!,
                RequiredInReport = d.RequiredInReport ?? false,
                ProjectId = d.ProjectId
            };
        }

        #endregion
    }
}
