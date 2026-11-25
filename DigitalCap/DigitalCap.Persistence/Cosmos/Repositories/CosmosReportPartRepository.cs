using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosReportPartRepository : CosmosRepositoryBase<ReportPartDocument>, IReportPartRepository
    {
        private readonly ISequenceService _sequenceService;
        private readonly ILogger<CosmosReportPartRepository> _logger;

        public CosmosReportPartRepository(
            ICosmosContainerFactory containerFactory,
            ISequenceService sequenceService,
            ILogger<CosmosReportPartRepository> logger)
            : base(containerFactory, CosmosContainers.Config_ReportPart, logger)
        {
            _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Commit() { }

        #region IReportPartRepository Implementation

        public async Task<bool> CreateProjectReportTemplate(int vesselTypeId, string userName, int projectId, bool imoExists = false, int copyprojectId = 0)
        {
            try
            {
                return await Sp_CreateProject_Templates(vesselTypeId, userName, projectId, imoExists, copyprojectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project report template");
                return false;
            }
        }

        public async Task<ReportConfigDropDownDataList> GetVesselTypes()
        {
            try
            {
                return await Sp_Get_VesselTypes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vessel types");
                return null!;
            }
        }

        public async Task<ReportConfigDropDownDataList> GetReportPartsByVesselTypeAsync(int vesselTypeId)
        {
            try
            {
                return await Sp_Get_ReportPartsByVesselType(vesselTypeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report parts by vessel type");
                return null!;
            }
        }

        public async Task<bool> CreateReportPart(VesselTypeReportConfigList reportConfigList, int vesselTypeId, string modifiedBy)
        {
            try
            {
                return await Sp_Create_VesseltypeReportConfig(reportConfigList, vesselTypeId, modifiedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report part");
                return false;
            }
        }

        public async Task<PartSectionNamesList> GetSectionNamesByPartNameAsync(int vesselTypeId, string partName)
        {
            try
            {
                return await Sp_Get_SectionNamesByPartName(vesselTypeId, partName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting section names by part name");
                return null!;
            }
        }

        public async Task<PartSectionNamesList> GetSectionNamesByPartId(int vesselTypeId, int partId)
        {
            try
            {
                return await Sp_GetVesselTypeSectionNormalMapping(partId, vesselTypeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting section names by part id");
                return null!;
            }
        }

        public async Task<List<ProjectCheckBox>> GetGradingCondition()
        {
            return await Sp_GetGradingCondition();
        }

        public async Task<List<ProjectSubSection>> GetNormalSectionByProjectId(int projectId, int partId)
        {
            return await Sp_GetNormalsectionsByProjectId(projectId, partId);
        }

        public async Task<List<ProjectSubSection>> GetProjectSubsectionBySectionId(Guid sectionId)
        {
            return await Sp_GetProjectSubsectionBySectionId(sectionId);
        }

        public async Task<List<ProjectCheckBox>> GetGradingConditionByProjectId(int projectId)
        {
            return await Sp_GetGradigConditionByProjectId(projectId);
        }

        public async Task<List<ProjectGradingUI>> GetGradingBySectionId(int projectId, Guid sectionId)
        {
            return await Sp_GetProjectSectionGrading(projectId, sectionId);
        }

        public async Task<List<ImageDescriptionUI>> GetImageDescriptionsBySectionId(int projectId, Guid sectionId)
        {
            return await Sp_GetProjectSectionDescription(projectId, sectionId);
        }

        public async Task<List<ImageDescriptionUI>> GetImageDescriptionsBySubSectionId(int projectId, Guid sectionId)
        {
            return await Sp_GetProjectSubSectionDescription(projectId, sectionId);
        }

        public async Task<List<GenericImageCard>> GetGenericImageCard(int projectId, int templateId, Guid sectionId)
        {
            return await Sp_GetGenericImageCard(projectId, templateId, sectionId);
        }

        public async Task<List<ProjectTankUI>> GetTankSectionByProjectId(int projectId, int partId, int imoNumber)
        {
            return await Sp_GetTankSectionsByProjectid(projectId, partId, imoNumber);
        }

        public async Task<List<Core.Models.Survey.Grading>> GetH_IGrading(bool isGasCarrier)
        {
            return await Sp_GetH_IGrading(isGasCarrier);
        }

        public async Task<List<ImageDescriptionUI>> GetImageDescriptionsByTankTypeId(int projectId, int tankTypeId)
        {
            return await Sp_GetImageDescriptionsByTankTypeId(projectId, tankTypeId);
        }

        public async Task<List<ProjectGradingConditionMapping>> GetHandIGradingConditionByProjectId(int projectId)
        {
            return await Sp_GetHandIProjectGradingConditionByProjectId(projectId);
        }

        public async Task<List<ProjectGradingMapping>> GetH_IProjectGradingByProjectId(int projectId)
        {
            return await Sp_GetHandIProjectGradingByProjectId(projectId);
        }

        public async Task<ProjectPart> GetProjectTemplateTitle(int partId, int projectId)
        {
            try
            {
                return await Sp_GetTemplateTitle(partId, projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project template title");
                return null!;
            }
        }

        public async Task<List<ExportPart>> GetVesselTypeExportPartConfiguration(int vesseltypeId)
        {
            try
            {
                return await Sp_GetVesselTypeExportConfigForParts(vesseltypeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vessel type export part configuration");
                return null!;
            }
        }

        public async Task<List<ExportSections>> GetVesselTypeExportSectionConfiguration(int partId, int vesselTypeId)
        {
            try
            {
                return await Sp_GetVesselTypeExportConfigForSections(partId, vesselTypeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vessel type export section configuration");
                return null!;
            }
        }

        public async Task<List<ExportPart>> GetProjectExportPartConfiguration(int projectId)
        {
            try
            {
                return await Sp_GetProjectExportConfigForParts(projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project export part configuration");
                return null!;
            }
        }

        public async Task<List<ExportSections>> GetProjectExportSectionConfiguration(int partId, int projectId)
        {
            try
            {
                return await Sp_GetProjectExportConfigForSections(partId, projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project export section configuration");
                return null!;
            }
        }

        public async Task<bool> CheckToShowSectionExportConfig(int projectId, int imoNumber, int partId)
        {
            return await Sp_CheckToShowSectionExportConfig(projectId, imoNumber, partId);
        }

        #endregion

        #region Stored Procedure Helpers (33 Helpers)

        // Helper 1: Sp_CreateProject_Templates
        internal async Task<bool> Sp_CreateProject_Templates(int vesselTypeId, string userName, int projectId, bool imoExists, int copyprojectId)
        {
            var user50 = userName == null ? null : (userName.Length > 50 ? userName.Substring(0, 50) : userName);
            var rollbackList = new List<(string docId, Func<Task> action)>();

            try
            {
                await Sp_CreateProjectParts(projectId, user50, vesselTypeId, rollbackList);
                await Sp_CreateProjectSections(projectId, user50, vesselTypeId, rollbackList);
                await Sp_CreateProjectTankSections(projectId, user50, vesselTypeId, rollbackList);
                await Sp_CreateProjectSubParts(projectId, user50, vesselTypeId, rollbackList);
                await Sp_CreateProjectSectionDescription(projectId, user50, vesselTypeId, rollbackList);
                await Sp_CreateProjectSubSectionDescription(projectId, user50, vesselTypeId, rollbackList);
                await Sp_CreateProjectSubSectionDescriptionMappping(projectId, user50, vesselTypeId, rollbackList);
                await Sp_CreateProjectSectionGrading(projectId, user50, vesselTypeId, rollbackList);

                return true;
            }
            catch (Exception ex)
            {
                for (int i = rollbackList.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        await rollbackList[i].action();
                    }
                    catch (Exception rbEx)
                    {
                        _logger.LogError(rbEx, "Rollback failed for document {DocId}", rollbackList[i].docId);
                    }
                }
                throw;
            }
        }

        // Helper 2: Sp_CreateProjectParts
        internal async Task Sp_CreateProjectParts(int projectId, string userName, int vesselTypeId, List<(string docId, Func<Task> action)> rollbackList)
        {
            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vtpList = new List<VesselTypePartMappingDocument>();
            using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtpList.AddRange(response);
                }
            }

            var rpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ReportPart);
            var sortedVtp = vtpList.OrderBy(x => x.VesselTypePartMappingId).ToList();

            foreach (var vp in sortedVtp)
            {
                var newPartId = (int)await _sequenceService.GetNextAsync(CosmosContainers.Config_ReportPart);
                var doc = new ReportPartDocument
                {
                    id = newPartId.ToString(),
                    PartId = newPartId,
                    ProjectId = projectId,
                    PartName = vp.PartName,
                    SequenceNo = vp.SequenceNo,
                    IsActive = true,
                    CreatedBy = userName,
                    CreatedOn = DateTime.UtcNow.Date,
                    IsDeleted = false,
                    ModifiedBy = userName,
                    ModifiedOn = DateTime.UtcNow.Date,
                    RequiredInReport = vp.RequiredInReport
                };

                await rpContainer.CreateItemAsync(doc, new PartitionKey((double)projectId));
                rollbackList.Add((doc.id, async () =>
                {
                    try { await rpContainer.DeleteItemAsync<ReportPartDocument>(doc.id, new PartitionKey((double)projectId)); }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
                }));
            }
        }

        // Helper 3: Sp_CreateProjectSections
        internal async Task Sp_CreateProjectSections(int projectId, string userName, int vesselTypeId, List<(string docId, Func<Task> action)> rollbackList)
        {
            var rpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ReportPart);
            var rpQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var rpList = new List<ReportPartDocument>();
            using (var feed = rpContainer.GetItemQueryIterator<ReportPartDocument>(rpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    rpList.AddRange(response);
                }
            }

            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vtpQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vtpList = new List<VesselTypePartMappingDocument>();
            using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(vtpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtpList.AddRange(response);
                }
            }

            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vnsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vnsList = new List<VesselTypeNormalSectionMappingDocument>();
            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(vnsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vnsList.AddRange(response);
                }
            }

            var psmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);

            var matches = (from rp in rpList
                           from vpm in vtpList.Where(v => v.PartName != null && rp.PartName != null
                                                       && string.Equals(v.PartName.TrimEnd(' '), rp.PartName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                                       && v.SequenceNo.HasValue && rp.SequenceNo.HasValue && v.SequenceNo.Value == rp.SequenceNo.Value)
                           from vns in vnsList.Where(s => s.VesselTypePartMappingId == vpm.VesselTypePartMappingId)
                           select new { rp, vns }).ToList();

            foreach (var m in matches)
            {
                var secId = Guid.NewGuid().ToString();
                var doc = new ProjectSectionMappingDocument
                {
                    id = secId,
                    SectionMappingId = secId,
                    ProjectId = projectId,
                    PartId = m.rp.PartId,
                    SectionName = m.vns.SectionName,
                    SubHeader = m.vns.SubHeader,
                    TotalCards = m.vns.TotalCards,
                    PlaceholderCount = m.vns.PlaceholderCount,
                    FileNameCount = m.vns.FileNameCount,
                    IsActive = true,
                    CreatedBy = userName,
                    CreatedOn = DateTime.UtcNow.Date,
                    IsDeleted = false,
                    HasSubSection = m.vns.HasSubSection,
                    Order = m.vns.Order,
                    ModifiedBy = userName,
                    ModifiedOn = DateTime.UtcNow.Date,
                    RequiredInReport = m.vns.RequiredInReport
                };

                await psmContainer.CreateItemAsync(doc, new PartitionKey((double)projectId));
                rollbackList.Add((doc.id, async () =>
                {
                    try { await psmContainer.DeleteItemAsync<ProjectSectionMappingDocument>(doc.id, new PartitionKey((double)projectId)); }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
                }));
            }
        }

        // Helper 4: Sp_CreateProjectTankSections
        internal async Task Sp_CreateProjectTankSections(int projectId, string userName, int vesselTypeId, List<(string docId, Func<Task> action)> rollbackList)
        {
            var rpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ReportPart);
            var rpQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var rpList = new List<ReportPartDocument>();
            using (var feed = rpContainer.GetItemQueryIterator<ReportPartDocument>(rpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    rpList.AddRange(response);
                }
            }

            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vtpQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vtpList = new List<VesselTypePartMappingDocument>();
            using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(vtpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtpList.AddRange(response);
                }
            }

            var vtsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
            var vtsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vtsList = new List<VesselTypeTankSectionMappingDocument>();
            using (var feed = vtsContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(vtsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtsList.AddRange(response);
                }
            }

            var ptsmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectTankSectionMapping);

            var matches = (from rp in rpList
                           from vpm in vtpList.Where(v => v.PartName != null && rp.PartName != null
                                                       && string.Equals(v.PartName.TrimEnd(' '), rp.PartName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                                       && v.SequenceNo.HasValue && rp.SequenceNo.HasValue && v.SequenceNo.Value == rp.SequenceNo.Value)
                           from vts in vtsList.Where(s => s.VesselTypePartMappingId == vpm.VesselTypePartMappingId)
                           select new { rp, vts }).ToList();

            foreach (var m in matches)
            {
                var secId = Guid.NewGuid().ToString();
                var doc = new ProjectTankSectionMappingDocument
                {
                    id = secId,
                    TankSectionMappingId = secId,
                    ProjectId = projectId,
                    PartId = m.rp.PartId,
                    TankTypeId = m.vts.TankTypeId,
                    TotalCards = m.vts.TotalCards,
                    PlaceholderCount = m.vts.PlaceholderCount,
                    FileNameCount = m.vts.FileNameCount,
                    IsActive = true,
                    CreatedBy = userName,
                    CreatedOn = DateTime.UtcNow.Date,
                    IsDeleted = false,
                    ModifiedBy = userName,
                    ModifiedOn = DateTime.UtcNow.Date
                };

                await ptsmContainer.CreateItemAsync(doc, new PartitionKey((double)projectId));
                rollbackList.Add((doc.id, async () =>
                {
                    try { await ptsmContainer.DeleteItemAsync<ProjectTankSectionMappingDocument>(doc.id, new PartitionKey((double)projectId)); }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
                }));
            }
        }

        // Helper 5: Sp_CreateProjectSubParts
        internal async Task Sp_CreateProjectSubParts(int projectId, string userName, int vesselTypeId, List<(string docId, Func<Task> action)> rollbackList)
        {
            var psmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
            var psmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.HasSubSection = true AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var psmList = new List<ProjectSectionMappingDocument>();
            using (var feed = psmContainer.GetItemQueryIterator<ProjectSectionMappingDocument>(psmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    psmList.AddRange(response);
                }
            }

            var rpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ReportPart);
            var rpQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var rpList = new List<ReportPartDocument>();
            using (var feed = rpContainer.GetItemQueryIterator<ReportPartDocument>(rpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    rpList.AddRange(response);
                }
            }

            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vtpQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vtpList = new List<VesselTypePartMappingDocument>();
            using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(vtpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtpList.AddRange(response);
                }
            }

            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vnsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.HasSubSection = true AND c.IsActive = true AND (c.IsDeleted = false OR IS_NULL(c.IsDeleted))")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vnsList = new List<VesselTypeNormalSectionMappingDocument>();
            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(vnsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vnsList.AddRange(response);
                }
            }

            var vnsSecIds = vnsList.Select(n => n.SectionMappingId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var vssList = new List<VesselTypeSubSectionDocument>();
            if (vnsSecIds.Count > 0)
            {
                var vssContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSection);
                var vssQ = new QueryDefinition(
                    "SELECT * FROM c WHERE ARRAY_CONTAINS(@secIds, c.VesselTypeSectionId) AND c.IsActive = true AND (c.IsDeleted = false OR IS_NULL(c.IsDeleted))")
                    .WithParameter("@secIds", vnsSecIds);

                using (var feed = vssContainer.GetItemQueryIterator<VesselTypeSubSectionDocument>(vssQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vssList.AddRange(response);
                    }
                }
            }

            var subContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSubSection);

            var matches = (from psm in psmList
                           from rp in rpList.Where(r => r.PartId == psm.PartId)
                           from vpm in vtpList.Where(v => v.PartName != null && rp.PartName != null && string.Equals(v.PartName.TrimEnd(' '), rp.PartName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                           from vns in vnsList.Where(n => n.VesselTypePartMappingId == vpm.VesselTypePartMappingId && n.SectionName != null && psm.SectionName != null && string.Equals(n.SectionName.TrimEnd(' '), psm.SectionName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                           from vss in vssList.Where(s => !string.IsNullOrEmpty(s.VesselTypeSectionId) && !string.IsNullOrEmpty(vns.SectionMappingId) && string.Equals(s.VesselTypeSectionId, vns.SectionMappingId, StringComparison.OrdinalIgnoreCase))
                           select new { psm, vss }).ToList();

            foreach (var m in matches)
            {
                var subId = Guid.NewGuid().ToString();
                var doc = new ProjectSubSectionDocument
                {
                    id = subId,
                    Id = subId,
                    ProjectSectionId = m.psm.SectionMappingId,
                    SectionName = m.vss.SectionName,
                    Totalcards = m.vss.Totalcards,
                    FileNameCount = m.vss.FileNameCount,
                    PlaceholderCount = m.vss.PlaceholderCount,
                    IsActive = true,
                    CreatedBy = "NVijyakumar@eagle.org",
                    CreatedOn = DateTime.UtcNow.Date,
                    IsDeleted = false,
                    ModifiedOn = DateTime.UtcNow.Date,
                    ModifiedBy = "NVijyakumar@eagle.org",
                    OrderNumber = m.vss.OrderNumber
                };

                await subContainer.CreateItemAsync(doc, new PartitionKey(subId));
                rollbackList.Add((doc.id, async () =>
                {
                    try { await subContainer.DeleteItemAsync<ProjectSubSectionDocument>(doc.id, new PartitionKey(doc.id)); }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
                }));
            }
        }

        // Helper 6: Sp_CreateProjectSectionDescription
        internal async Task Sp_CreateProjectSectionDescription(int projectId, string userName, int vesselTypeId, List<(string docId, Func<Task> action)> rollbackList)
        {
            var psmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
            var psmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var psmList = new List<ProjectSectionMappingDocument>();
            using (var feed = psmContainer.GetItemQueryIterator<ProjectSectionMappingDocument>(psmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    psmList.AddRange(response);
                }
            }

            var rpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ReportPart);
            var rpQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var rpList = new List<ReportPartDocument>();
            using (var feed = rpContainer.GetItemQueryIterator<ReportPartDocument>(rpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    rpList.AddRange(response);
                }
            }

            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vtpQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vtpList = new List<VesselTypePartMappingDocument>();
            using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(vtpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtpList.AddRange(response);
                }
            }

            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vnsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vnsList = new List<VesselTypeNormalSectionMappingDocument>();
            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(vnsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vnsList.AddRange(response);
                }
            }

            var vnsSecIds = vnsList.Select(n => n.SectionMappingId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var vsdList = new List<VesselTypeSectionDescriptionMappingDocument>();
            if (vnsSecIds.Count > 0)
            {
                var vsdContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSectionDescriptionMapping);
                var vsdQ = new QueryDefinition(
                    "SELECT * FROM c WHERE ARRAY_CONTAINS(@secIds, c.SectionId) AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@secIds", vnsSecIds);

                using (var feed = vsdContainer.GetItemQueryIterator<VesselTypeSectionDescriptionMappingDocument>(vsdQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vsdList.AddRange(response);
                    }
                }
            }

            var psdContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionDescriptionMapping);

            var matches = (from psm in psmList
                           from rp in rpList.Where(r => r.PartId == psm.PartId)
                           from vpm in vtpList.Where(v => v.PartName != null && rp.PartName != null && string.Equals(v.PartName.TrimEnd(' '), rp.PartName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                           from vns in vnsList.Where(n => n.SectionName != null && psm.SectionName != null && string.Equals(n.SectionName.TrimEnd(' '), psm.SectionName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase) && n.VesselTypePartMappingId == vpm.VesselTypePartMappingId)
                           from vsd in vsdList.Where(d => !string.IsNullOrEmpty(d.SectionId) && !string.IsNullOrEmpty(vns.SectionMappingId) && string.Equals(d.SectionId, vns.SectionMappingId, StringComparison.OrdinalIgnoreCase))
                           select new { psm, vsd }).ToList();

            var seenKeys = new HashSet<string>();
            foreach (var m in matches)
            {
                if (m.vsd.CategoryId == null)
                {
                    throw new InvalidOperationException("Cannot insert NULL into CategoryId int NOT NULL in ProjectSectionDescriptionMapping");
                }
                var mapId = $"{m.psm.SectionMappingId}|{m.vsd.CategoryId.Value}";
                if (!seenKeys.Add(mapId))
                {
                    throw new InvalidOperationException($"duplicate (<key>,<CategoryId>) in no-PK table Config.ProjectSectionDescriptionMapping: {mapId}");
                }

                var doc = new ProjectSectionDescriptionMappingDocument
                {
                    id = mapId,
                    SectionId = m.psm.SectionMappingId,
                    CategoryId = m.vsd.CategoryId.Value,
                    IsActive = true,
                    CreatedBy = userName,
                    CreatedOn = DateTime.UtcNow.Date,
                    IsDeleted = false,
                    ModifiedOn = DateTime.UtcNow.Date,
                    ModifiedBy = userName
                };

                await psdContainer.CreateItemAsync(doc, new PartitionKey(mapId));
                rollbackList.Add((doc.id, async () =>
                {
                    try { await psdContainer.DeleteItemAsync<ProjectSectionDescriptionMappingDocument>(doc.id, new PartitionKey(doc.id)); }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
                }));
            }
        }

        // Helper 7: Sp_CreateProjectSubSectionDescription
        internal async Task Sp_CreateProjectSubSectionDescription(int projectId, string userName, int vesselTypeId, List<(string docId, Func<Task> action)> rollbackList)
        {
            var psmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
            var psmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.HasSubSection = true AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var psmList = new List<ProjectSectionMappingDocument>();
            using (var feed = psmContainer.GetItemQueryIterator<ProjectSectionMappingDocument>(psmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    psmList.AddRange(response);
                }
            }

            var psmSecIds = psmList.Select(m => m.SectionMappingId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var pssList = new List<ProjectSubSectionDocument>();
            var psdList = new List<ProjectSectionDescriptionMappingDocument>();

            if (psmSecIds.Count > 0)
            {
                var pssContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSubSection);
                var pssQ = new QueryDefinition(
                    "SELECT * FROM c WHERE ARRAY_CONTAINS(@secIds, c.ProjectSectionId) AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@secIds", psmSecIds);

                using (var feed = pssContainer.GetItemQueryIterator<ProjectSubSectionDocument>(pssQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        pssList.AddRange(response);
                    }
                }

                var psdContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionDescriptionMapping);
                var psdQ = new QueryDefinition(
                    "SELECT * FROM c WHERE ARRAY_CONTAINS(@secIds, c.SectionId) AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@secIds", psmSecIds);

                using (var feed = psdContainer.GetItemQueryIterator<ProjectSectionDescriptionMappingDocument>(psdQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        psdList.AddRange(response);
                    }
                }
            }

            var matches = (from pss in pssList
                           from psm in psmList.Where(m => !string.IsNullOrEmpty(m.SectionMappingId) && !string.IsNullOrEmpty(pss.ProjectSectionId) && string.Equals(m.SectionMappingId, pss.ProjectSectionId, StringComparison.OrdinalIgnoreCase))
                           from psd in psdList.Where(d => !string.IsNullOrEmpty(d.SectionId) && !string.IsNullOrEmpty(psm.SectionMappingId) && string.Equals(d.SectionId, psm.SectionMappingId, StringComparison.OrdinalIgnoreCase))
                           select new { pss, psd }).ToList();

            var psdTargetContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionDescriptionMapping);
            var seenKeys = new HashSet<string>();

            foreach (var m in matches)
            {
                var mapId = $"{m.pss.Id}|{m.psd.CategoryId}";
                if (!seenKeys.Add(mapId))
                {
                    throw new InvalidOperationException($"duplicate (<key>,<CategoryId>) in no-PK table Config.ProjectSectionDescriptionMapping: {mapId}");
                }

                var doc = new ProjectSectionDescriptionMappingDocument
                {
                    id = mapId,
                    SectionId = m.pss.Id,
                    CategoryId = m.psd.CategoryId,
                    IsActive = true,
                    CreatedBy = userName,
                    CreatedOn = DateTime.UtcNow.Date,
                    IsDeleted = false,
                    ModifiedOn = DateTime.UtcNow.Date,
                    ModifiedBy = userName
                };

                await psdTargetContainer.CreateItemAsync(doc, new PartitionKey(mapId));
                rollbackList.Add((doc.id, async () =>
                {
                    try { await psdTargetContainer.DeleteItemAsync<ProjectSectionDescriptionMappingDocument>(doc.id, new PartitionKey(doc.id)); }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
                }));
            }
        }

        // Helper 8: Sp_CreateProjectSubSectionDescriptionMappping
        internal async Task Sp_CreateProjectSubSectionDescriptionMappping(int projectId, string userName, int vesselTypeId, List<(string docId, Func<Task> action)> rollbackList)
        {
            var psmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
            var psmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var psmList = new List<ProjectSectionMappingDocument>();
            using (var feed = psmContainer.GetItemQueryIterator<ProjectSectionMappingDocument>(psmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    psmList.AddRange(response);
                }
            }

            var psmSecIds = psmList.Select(m => m.SectionMappingId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var pssList = new List<ProjectSubSectionDocument>();

            if (psmSecIds.Count > 0)
            {
                var pssContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSubSection);
                var pssQ = new QueryDefinition(
                    "SELECT * FROM c WHERE ARRAY_CONTAINS(@secIds, c.ProjectSectionId) AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@secIds", psmSecIds);

                using (var feed = pssContainer.GetItemQueryIterator<ProjectSubSectionDocument>(pssQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        pssList.AddRange(response);
                    }
                }
            }

            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vnsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.HasSubSection = true AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vnsList = new List<VesselTypeNormalSectionMappingDocument>();
            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(vnsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vnsList.AddRange(response);
                }
            }

            var vnsSecIds = vnsList.Select(n => n.SectionMappingId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var vssList = new List<VesselTypeSubSectionDocument>();
            var vtsdList = new List<VesselTypeSubSectionDescriptionMappingDocument>();

            if (vnsSecIds.Count > 0)
            {
                var vssContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSection);
                var vssQ = new QueryDefinition(
                    "SELECT * FROM c WHERE ARRAY_CONTAINS(@secIds, c.VesselTypeSectionId) AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@secIds", vnsSecIds);

                using (var feed = vssContainer.GetItemQueryIterator<VesselTypeSubSectionDocument>(vssQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vssList.AddRange(response);
                    }
                }

                var vssIds = vssList.Select(s => s.Id).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
                if (vssIds.Count > 0)
                {
                    var vtsdContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSectionDescriptionMapping);
                    var vtsdQ = new QueryDefinition(
                        "SELECT * FROM c WHERE ARRAY_CONTAINS(@subIds, c.SubSectionId)")
                        .WithParameter("@subIds", vssIds);

                    using (var feed = vtsdContainer.GetItemQueryIterator<VesselTypeSubSectionDescriptionMappingDocument>(vtsdQ))
                    {
                        while (feed.HasMoreResults)
                        {
                            var response = await feed.ReadNextAsync();
                            vtsdList.AddRange(response);
                        }
                    }
                }
            }

            var v_ss = (from vns in vnsList
                        from vss in vssList.Where(s => !string.IsNullOrEmpty(s.VesselTypeSectionId) && !string.IsNullOrEmpty(vns.SectionMappingId) && string.Equals(s.VesselTypeSectionId, vns.SectionMappingId, StringComparison.OrdinalIgnoreCase))
                        from vtsd in vtsdList.Where(d => !string.IsNullOrEmpty(d.SubSectionId) && !string.IsNullOrEmpty(vss.Id) && string.Equals(d.SubSectionId, vss.Id, StringComparison.OrdinalIgnoreCase))
                        select new { vss.SectionName, vtsd.CategoryId }).ToList();

            var psubContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSubSectionDescriptionMapping);

            var matches = (from pss in pssList
                           from pns in psmList.Where(m => !string.IsNullOrEmpty(m.SectionMappingId) && !string.IsNullOrEmpty(pss.ProjectSectionId) && string.Equals(m.SectionMappingId, pss.ProjectSectionId, StringComparison.OrdinalIgnoreCase))
                           from sub in v_ss.Where(s => s.SectionName != null && pss.SectionName != null && string.Equals(s.SectionName.TrimEnd(' '), pss.SectionName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                           select new { pss, sub.CategoryId }).ToList();

            var seenKeys = new HashSet<string>();
            foreach (var m in matches)
            {
                var mapId = m.CategoryId.HasValue ? $"{m.pss.Id}|{m.CategoryId.Value}" : $"{m.pss.Id}|";
                if (!seenKeys.Add(mapId))
                {
                    throw new InvalidOperationException($"duplicate (<key>,<CategoryId>) in no-PK table Config.ProjectSubSectionDescriptionMapping: {mapId}");
                }

                var doc = new ProjectSubSectionDescriptionMappingDocument
                {
                    id = mapId,
                    SubSectionId = m.pss.Id,
                    CategoryId = m.CategoryId,
                    IsActive = true,
                    CreatedBy = userName,
                    CreatedOn = DateTime.UtcNow.Date,
                    IsDeleted = false,
                    ModifiedBy = userName,
                    ModifiedOn = DateTime.UtcNow.Date
                };

                await psubContainer.CreateItemAsync(doc, new PartitionKey(mapId));
                rollbackList.Add((doc.id, async () =>
                {
                    try { await psubContainer.DeleteItemAsync<ProjectSubSectionDescriptionMappingDocument>(doc.id, new PartitionKey(doc.id)); }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
                }));
            }
        }

        // Helper 9: Sp_CreateProjectSectionGrading
        internal async Task Sp_CreateProjectSectionGrading(int projectId, string userName, int vesselTypeId, List<(string docId, Func<Task> action)> rollbackList)
        {
            var psmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
            var psmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var psmList = new List<ProjectSectionMappingDocument>();
            using (var feed = psmContainer.GetItemQueryIterator<ProjectSectionMappingDocument>(psmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    psmList.AddRange(response);
                }
            }

            var rpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ReportPart);
            var rpQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var rpList = new List<ReportPartDocument>();
            using (var feed = rpContainer.GetItemQueryIterator<ReportPartDocument>(rpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    rpList.AddRange(response);
                }
            }

            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vtpQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vtpList = new List<VesselTypePartMappingDocument>();
            using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(vtpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtpList.AddRange(response);
                }
            }

            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vnsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vnsList = new List<VesselTypeNormalSectionMappingDocument>();
            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(vnsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vnsList.AddRange(response);
                }
            }

            var vnsSecIds = vnsList.Select(n => n.SectionMappingId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var vsgList = new List<VesselTypeSectionGradingMappingDocument>();
            if (vnsSecIds.Count > 0)
            {
                var vsgContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSectionGradingMapping);
                var vsgQ = new QueryDefinition(
                    "SELECT * FROM c WHERE ARRAY_CONTAINS(@secIds, c.SectionId) AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@secIds", vnsSecIds);

                using (var feed = vsgContainer.GetItemQueryIterator<VesselTypeSectionGradingMappingDocument>(vsgQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vsgList.AddRange(response);
                    }
                }
            }

            var psgContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionGradingMapping);

            var matches = (from psm in psmList
                           from rp in rpList.Where(r => r.PartId == psm.PartId)
                           from vpm in vtpList.Where(v => v.PartName != null && rp.PartName != null && string.Equals(v.PartName.TrimEnd(' '), rp.PartName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                           from vns in vnsList.Where(n => n.SectionName != null && psm.SectionName != null && string.Equals(n.SectionName.TrimEnd(' '), psm.SectionName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase) && n.VesselTypePartMappingId == vpm.VesselTypePartMappingId)
                           from vsg in vsgList.Where(g => !string.IsNullOrEmpty(g.SectionId) && !string.IsNullOrEmpty(vns.SectionMappingId) && string.Equals(g.SectionId, vns.SectionMappingId, StringComparison.OrdinalIgnoreCase))
                           select new { psm, vsg }).ToList();

            var seenKeys = new HashSet<string>();
            foreach (var m in matches)
            {
                var mapId = $"{m.psm.SectionMappingId}|{m.vsg.CategoryId}";
                if (!seenKeys.Add(mapId))
                {
                    throw new InvalidOperationException($"duplicate (<key>,<CategoryId>) in no-PK table Config.ProjectSectionGradingMapping: {mapId}");
                }

                var doc = new ProjectSectionGradingMappingDocument
                {
                    id = mapId,
                    SectionId = m.psm.SectionMappingId,
                    CategoryId = m.vsg.CategoryId,
                    IsActive = true,
                    CreatedBy = userName,
                    CreatedOn = DateTime.UtcNow.Date,
                    IsDeleted = false,
                    ModifiedOn = DateTime.UtcNow.Date,
                    ModifiedBy = userName
                };

                await psgContainer.CreateItemAsync(doc, new PartitionKey(mapId));
                rollbackList.Add((doc.id, async () =>
                {
                    try { await psgContainer.DeleteItemAsync<ProjectSectionGradingMappingDocument>(doc.id, new PartitionKey(doc.id)); }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }
                }));
            }
        }

        // Helper 10: Sp_Get_VesselTypes
        internal async Task<ReportConfigDropDownDataList> Sp_Get_VesselTypes()
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_VesselType);
            var q = new QueryDefinition("SELECT * FROM c");

            var types = new List<VesselTypeDocument>();
            using (var feed = container.GetItemQueryIterator<VesselTypeDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    types.AddRange(response);
                }
            }

            var list = new List<VesselTypes>
            {
                new VesselTypes { VesselTypeId = 0, VesselType = "Select" }
            };

            list.AddRange(types.Select(t => new VesselTypes
            {
                VesselTypeId = t.Id,
                VesselType = t.VesselType
            }));

            return new ReportConfigDropDownDataList { vesselTypes = list };
        }

        // Helper 11: Sp_Get_ReportPartsByVesselType
        internal async Task<ReportConfigDropDownDataList> Sp_Get_ReportPartsByVesselType(int vesselTypeId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var items = new List<VesselTypePartMappingDocument>();
            using (var feed = container.GetItemQueryIterator<VesselTypePartMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    items.AddRange(response);
                }
            }

            var templates = items.OrderBy(x => x.SequenceNo.HasValue).ThenBy(x => x.SequenceNo).Select(x => new ReportTemplates
            {
                PartId = x.VesselTypePartMappingId,
                VesselTypeId = x.VesselTypeId ?? 0,
                PartName = x.PartName,
                SequenceNo = x.SequenceNo ?? 0,
                IsActive = x.IsActive == true,
                IsEditable = true
            }).ToList();

            return new ReportConfigDropDownDataList { reportTemplates = templates };
        }

        // Helper 12: Sp_Get_SectionNamesByPartName
        internal async Task<PartSectionNamesList> Sp_Get_SectionNamesByPartName(int vesselTypeId, string partName)
        {
            var pName50 = partName == null ? null : (partName.Length > 50 ? partName.Substring(0, 50) : partName);

            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vtpQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vtpList = new List<VesselTypePartMappingDocument>();
            using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(vtpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtpList.AddRange(response);
                }
            }

            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vnsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vnsList = new List<VesselTypeNormalSectionMappingDocument>();
            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(vnsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vnsList.AddRange(response);
                }
            }

            var normalResult = new List<NormalSectionNames>();

            bool normalExists = (from vtns in vnsList
                                 join vtp in vtpList on vtns.VesselTypePartMappingId equals vtp.VesselTypePartMappingId
                                 where vtp.VesselTypeId == vesselTypeId && pName50 != null && string.Equals(vtp.PartName?.TrimEnd(' '), pName50.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                 select 1).Any();

            if (normalExists)
            {
                var normalMatches = (from vtns in vnsList
                                     join vtp in vtpList on vtns.VesselTypePartMappingId equals vtp.VesselTypePartMappingId
                                     where vtp.VesselTypeId == vesselTypeId
                                        && pName50 != null
                                        && string.Equals(vtp.PartName?.TrimEnd(' '), pName50.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                        && vtp.IsDeleted == false
                                        && vtns.IsDeleted == false
                                     select new NormalSectionNames
                                     {
                                         PartId = vtp.VesselTypePartMappingId,
                                         PartName = vtp.PartName,
                                         VesselTypeId = vtns.VesselTypeId ?? vesselTypeId,
                                         SectionId = Guid.Parse(vtns.SectionMappingId ?? vtns.id),
                                         SectionName = vtns.SectionName,
                                         SubHeader = vtns.SubHeader,
                                         TotalCards = vtns.TotalCards ?? 0,
                                         PlaceholderCount = vtns.PlaceholderCount ?? 0,
                                         FileNameCount = vtns.FileNameCount ?? 0,
                                         IsActive = vtns.IsActive == true
                                     }).ToList();

                normalResult.AddRange(normalMatches);
            }
            else
            {
                var rtContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_ReportTemplate);
                var tsmContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_TemplateSectionMapping);
                var rtsContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_ReportTemplateSection);

                var rtDocs = new List<ReportTemplateDocument>();
                using (var feed = rtContainer.GetItemQueryIterator<ReportTemplateDocument>(new QueryDefinition("SELECT * FROM c")))
                {
                    while (feed.HasMoreResults)
                    {
                        var resp = await feed.ReadNextAsync();
                        rtDocs.AddRange(resp);
                    }
                }

                var matchedTemplates = rtDocs.Where(t => pName50 != null && string.Equals(t.SectionName?.TrimEnd(' '), pName50.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)).ToList();
                if (matchedTemplates.Count > 0)
                {
                    var matchedTemplateIds = matchedTemplates.Select(t => t.Id).ToList();
                    var tsmDocs = new List<TemplateSectionMappingDocument>();
                    var tsmQ = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@tIds, c.TemplateId)").WithParameter("@tIds", matchedTemplateIds);
                    using (var feed = tsmContainer.GetItemQueryIterator<TemplateSectionMappingDocument>(tsmQ))
                    {
                        while (feed.HasMoreResults)
                        {
                            var resp = await feed.ReadNextAsync();
                            tsmDocs.AddRange(resp);
                        }
                    }

                    var secIds = tsmDocs.Select(x => x.SectionId).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
                    var rtsDocs = new List<ReportTemplateSectionDocument>();
                    if (secIds.Count > 0)
                    {
                        using (var feed = rtsContainer.GetItemQueryIterator<ReportTemplateSectionDocument>(new QueryDefinition("SELECT * FROM c")))
                        {
                            while (feed.HasMoreResults)
                            {
                                var resp = await feed.ReadNextAsync();
                                rtsDocs.AddRange(resp);
                            }
                        }
                    }

                    foreach (var tmpl in matchedTemplates)
                    {
                        var tmplTsm = tsmDocs.Where(t => t.TemplateId == tmpl.Id).ToList();
                        foreach (var tsm in tmplTsm)
                        {
                            var s = rtsDocs.FirstOrDefault(x => string.Equals(x.Id, tsm.SectionId, StringComparison.OrdinalIgnoreCase) || string.Equals(x.id, tsm.SectionId, StringComparison.OrdinalIgnoreCase));
                            if (s != null)
                            {
                                normalResult.Add(new NormalSectionNames
                                {
                                    PartId = tmpl.Id,
                                    PartName = tmpl.SectionName,
                                    VesselTypeId = vesselTypeId,
                                    SectionId = Guid.Parse(tsm.SectionId ?? Guid.Empty.ToString()),
                                    SectionName = s.SectionName,
                                    SubHeader = "",
                                    TotalCards = 30,
                                    PlaceholderCount = 6,
                                    FileNameCount = 24,
                                    IsActive = true
                                });
                            }
                        }
                    }
                }
            }

            var vtsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
            var vtsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var vtsList = new List<VesselTypeTankSectionMappingDocument>();
            using (var feed = vtsContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(vtsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtsList.AddRange(response);
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

            var rtList = new List<ReportTemplateDocument>();
            var rtCont = ContainerFactory.GetContainer(CosmosContainers.dbo_ReportTemplate);
            using (var feed = rtCont.GetItemQueryIterator<ReportTemplateDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    rtList.AddRange(response);
                }
            }

            var tankResult = new List<TankSectionNames>();

            bool tankExists = (from vsts in vtsList
                               join vtp in vtpList on vsts.VesselTypePartMappingId equals vtp.VesselTypePartMappingId
                               where vsts.VesselTypeId == vesselTypeId && pName50 != null && string.Equals(vtp.PartName?.TrimEnd(' '), pName50.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                               select 1).Any();

            if (tankExists)
            {
                var mappedTanks = (from vsts in vtsList
                                   join vtp in vtpList on vsts.VesselTypePartMappingId equals vtp.VesselTypePartMappingId
                                   join tt in ttList on vsts.TankTypeId equals tt.Id
                                   where vsts.VesselTypeId == vesselTypeId && pName50 != null && string.Equals(vtp.PartName?.TrimEnd(' '), pName50.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                   select new TankSectionNames
                                   {
                                       PartId = vtp.VesselTypePartMappingId,
                                       PartName = vtp.PartName,
                                       VesselTypeId = vsts.VesselTypeId ?? vesselTypeId,
                                       TankTypeId = vsts.TankTypeId ?? 0,
                                       TankType = tt.TankType,
                                       TotalCards = vsts.TotalCards ?? 0,
                                       PlaceholderCount = vsts.PlaceholderCount ?? 0,
                                       FileNameCount = vsts.FileNameCount ?? 0,
                                       IsActive = vsts.IsActive == true,
                                       IsMapped = true
                                   }).ToList();

                tankResult.AddRange(mappedTanks);

                var excludedTankTypeIds = (from vsts in vtsList
                                           join vtp in vtpList on vsts.VesselTypePartMappingId equals vtp.VesselTypePartMappingId
                                           join tt in ttList on vsts.TankTypeId equals tt.Id
                                           where vsts.VesselTypeId == vesselTypeId && pName50 != null && string.Equals(vtp.PartName?.TrimEnd(' '), pName50.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                           select vsts.TankTypeId).ToHashSet();

                var unmappedTanks = (from tt in ttList
                                     where !excludedTankTypeIds.Contains(tt.Id)
                                     join tmpl in rtList on tt.TemplateId equals tmpl.Id into tmplGroup
                                     from tmpl in tmplGroup.DefaultIfEmpty()
                                     select new TankSectionNames
                                     {
                                         PartId = tt.TemplateId ?? 0,
                                         PartName = tmpl?.SectionName,
                                         VesselTypeId = vesselTypeId,
                                         TankTypeId = tt.Id,
                                         TankType = tt.TankType,
                                         TotalCards = 0,
                                         PlaceholderCount = tt.CurrentconditionPlaceholders,
                                         FileNameCount = 0,
                                         IsActive = true,
                                         IsMapped = pName50 != null && string.Equals(tmpl?.SectionName?.TrimEnd(' '), pName50.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                     }).ToList();

                tankResult.AddRange(unmappedTanks);
            }
            else
            {
                var unmappedTanks = (from tt in ttList
                                     join tmpl in rtList on tt.TemplateId equals tmpl.Id into tmplGroup
                                     from tmpl in tmplGroup.DefaultIfEmpty()
                                     select new TankSectionNames
                                     {
                                         PartId = tt.TemplateId ?? 0,
                                         PartName = tmpl?.SectionName,
                                         VesselTypeId = vesselTypeId,
                                         TankTypeId = tt.Id,
                                         TankType = tt.TankType,
                                         TotalCards = 0,
                                         PlaceholderCount = tt.CurrentconditionPlaceholders,
                                         FileNameCount = 0,
                                         IsActive = true,
                                         IsMapped = pName50 != null && string.Equals(tmpl?.SectionName?.TrimEnd(' '), pName50.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)
                                     }).ToList();

                tankResult.AddRange(unmappedTanks);
            }

            return new PartSectionNamesList
            {
                NormalSections = normalResult,
                TanksSections = tankResult
            };
        }

        // Helper 13: Sp_GetVesselTypeSectionNormalMapping
        internal async Task<PartSectionNamesList> Sp_GetVesselTypeSectionNormalMapping(int partId, int vesselTypeId)
        {
            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vnsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.VesselTypePartMappingId = @partId AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId)
                .WithParameter("@partId", partId);

            var vnsList = new List<VesselTypeNormalSectionMappingDocument>();
            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(vnsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vnsList.AddRange(response);
                }
            }

            var normalResult = vnsList.OrderBy(x => x.Order.HasValue).ThenBy(x => x.Order).Select(vnm => new NormalSectionNames
            {
                PartId = vnm.VesselTypePartMappingId ?? partId,
                PartName = "",
                VesselTypeId = vnm.VesselTypeId ?? 0,
                SectionId = Guid.Parse(vnm.SectionMappingId ?? vnm.id),
                SectionName = vnm.SectionName,
                SubHeader = vnm.SubHeader ?? "",
                TotalCards = vnm.TotalCards ?? 0,
                PlaceholderCount = vnm.PlaceholderCount ?? 0,
                FileNameCount = vnm.FileNameCount ?? 0,
                IsActive = vnm.IsActive == true,
                ReportRequired = false
            }).ToList();

            var sectionIds = vnsList.Select(x => x.SectionMappingId ?? x.id).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

            var vssList = new List<VesselTypeSubSectionDocument>();
            if (sectionIds.Count > 0)
            {
                var vssContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSection);
                var vssQ = new QueryDefinition(
                    "SELECT * FROM c WHERE ARRAY_CONTAINS(@secIds, c.VesselTypeSectionId)")
                    .WithParameter("@secIds", sectionIds);

                using (var feed = vssContainer.GetItemQueryIterator<VesselTypeSubSectionDocument>(vssQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vssList.AddRange(response);
                    }
                }
            }

            var subResult = vssList
                .OrderBy(s => s.OrderNumber.HasValue).ThenBy(s => s.OrderNumber)
                .Select(s => new SubSectionNames
                {
                    PartId = partId,
                    PartName = "",
                    VesselTypeId = 0, // SQL typo alias VeselTypeId keeps default 0
                    SubSectionId = Guid.Parse(s.Id ?? s.id),
                    SectionId = Guid.Parse(s.VesselTypeSectionId ?? Guid.Empty.ToString()),
                    SectionName = s.SectionName,
                    TotalCards = s.Totalcards ?? 0,
                    PlaceholderCount = s.PlaceholderCount ?? 0,
                    FileNameCount = s.FileNameCount ?? 0,
                    IsActive = s.IsActive == true
                }).ToList();

            var vtsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
            var vtsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.VesselTypePartMappingId = @partId AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId)
                .WithParameter("@partId", partId);

            var vtsList = new List<VesselTypeTankSectionMappingDocument>();
            using (var feed = vtsContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(vtsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtsList.AddRange(response);
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

            var tankResult = new List<TankSectionNames>();

            foreach (var vts in vtsList)
            {
                var tt = ttList.FirstOrDefault(t => t.Id == vts.TankTypeId);
                if (tt != null)
                {
                    tankResult.Add(new TankSectionNames
                    {
                        PartId = vts.VesselTypePartMappingId ?? partId,
                        PartName = "",
                        VesselTypeId = vts.VesselTypeId ?? 0,
                        TankTypeId = vts.TankTypeId ?? 0,
                        TankType = tt.TankType,
                        TotalCards = vts.TotalCards ?? 0,
                        PlaceholderCount = vts.PlaceholderCount ?? 0,
                        FileNameCount = vts.FileNameCount ?? 0,
                        IsActive = vts.IsActive == true,
                        IsMapped = true
                    });
                }
            }

            bool hasNullTankTypeId = vtsList.Any(x => x.TankTypeId == null);
            if (!hasNullTankTypeId)
            {
                var mappedTankTypeIds = new HashSet<int>(vtsList.Where(x => x.TankTypeId.HasValue).Select(x => x.TankTypeId!.Value));
                foreach (var tt in ttList.Where(t => !mappedTankTypeIds.Contains(t.Id)))
                {
                    tankResult.Add(new TankSectionNames
                    {
                        PartId = 0,
                        PartName = "",
                        VesselTypeId = vesselTypeId,
                        TankTypeId = tt.Id,
                        TankType = tt.TankType,
                        TotalCards = 32,
                        PlaceholderCount = tt.CurrentconditionPlaceholders,
                        FileNameCount = 32 - tt.CurrentconditionPlaceholders,
                        IsActive = false,
                        IsMapped = false
                    });
                }
            }

            var distinctTankResult = tankResult
                .GroupBy(t => new { t.PartId, t.PartName, t.VesselTypeId, t.TankTypeId, t.TankType, t.TotalCards, t.PlaceholderCount, t.FileNameCount, t.IsActive, t.IsMapped })
                .Select(g => g.First())
                .ToList();

            return new PartSectionNamesList
            {
                NormalSections = normalResult,
                SubSections = subResult,
                TanksSections = distinctTankResult
            };
        }

        private class TvpPartRow
        {
            public int? VesselTypePartMappingId { get; set; }
            public int? VesselTypeId { get; set; }
            public string? PartName { get; set; }
            public int? SequenceNo { get; set; }
            public bool? IsActive { get; set; }
            public string? CreatedBy { get; set; }
            public bool? IsDeleted { get; set; }
            public string? ModifiedBy { get; set; }
        }

        private class TvpNormalSectionRow
        {
            public Guid? VesselTypeNormalSectionMappingId { get; set; }
            public int? VesselTypePartMappingId { get; set; }
            public int? VesselTypeId { get; set; }
            public string? PartName { get; set; }
            public string? SectionName { get; set; }
            public string? SubHeader { get; set; }
            public int? TotalCards { get; set; }
            public int? PlaceholderCount { get; set; }
            public int? FileNameCount { get; set; }
            public bool? IsActive { get; set; }
            public string? CreatedBy { get; set; }
            public bool? IsDeleted { get; set; }
            public string? ModifiedBy { get; set; }
        }

        private class TvpTankSectionRow
        {
            public int? VesselTypePartMappingId { get; set; }
            public int? VesselTypeId { get; set; }
            public string? PartName { get; set; }
            public int? TankTypeId { get; set; }
            public int? TotalCards { get; set; }
            public int? PlaceholderCount { get; set; }
            public int? FileNameCount { get; set; }
            public bool? IsActive { get; set; }
            public string? CreatedBy { get; set; }
            public bool? IsDeleted { get; set; }
            public string? ModifiedBy { get; set; }
        }

        // Helper 14: Sp_Create_VesseltypeReportConfig
        internal async Task<bool> Sp_Create_VesseltypeReportConfig(VesselTypeReportConfigList reportConfigList, int vesselTypeId, string modifiedBy)
        {
            // C1: Build 4 TVP lists
            var tvpParts = new List<TvpPartRow>();
            if (reportConfigList.reportParts != null && reportConfigList.reportParts.Any())
            {
                foreach (var p in reportConfigList.reportParts)
                {
                    tvpParts.Add(new TvpPartRow
                    {
                        VesselTypePartMappingId = p.vesselTypePartMappingId == 0 ? null : p.vesselTypePartMappingId,
                        VesselTypeId = vesselTypeId,
                        PartName = p.PartName,
                        SequenceNo = p.SequenceNo,
                        IsActive = p.IsActive,
                        CreatedBy = string.IsNullOrEmpty(p.CreatedBy) ? modifiedBy : p.CreatedBy,
                        IsDeleted = p.IsDeleted ?? false,
                        ModifiedBy = modifiedBy
                    });
                }
            }

            var tvpNormalSections = new List<TvpNormalSectionRow>();
            if (reportConfigList.normalSectionMappings == null || !reportConfigList.normalSectionMappings.Any())
            {
                tvpNormalSections.Add(new TvpNormalSectionRow());
            }
            else
            {
                foreach (var s in reportConfigList.normalSectionMappings)
                {
                    tvpNormalSections.Add(new TvpNormalSectionRow
                    {
                        VesselTypeNormalSectionMappingId = s.VesselTypeNormalSectionMappingId ?? Guid.Empty,
                        VesselTypePartMappingId = s.VesselTypePartMappingId,
                        VesselTypeId = s.VesselTypeId,
                        PartName = s.PartName,
                        SectionName = s.SectionName,
                        SubHeader = s.SubHeader,
                        TotalCards = s.TotalCards,
                        PlaceholderCount = s.PlaceholderCount,
                        FileNameCount = s.FileNameCount,
                        IsActive = s.IsActive,
                        CreatedBy = s.CreatedBy,
                        IsDeleted = s.IsDeleted,
                        ModifiedBy = modifiedBy
                    });
                }
            }

            var tvpTankSections = new List<TvpTankSectionRow>();
            if (reportConfigList.tankSectionMappings == null || !reportConfigList.tankSectionMappings.Any())
            {
                tvpTankSections.Add(new TvpTankSectionRow());
            }
            else
            {
                foreach (var t in reportConfigList.tankSectionMappings)
                {
                    tvpTankSections.Add(new TvpTankSectionRow
                    {
                        VesselTypePartMappingId = t.VesselTypePartMappingId == 0 ? null : t.VesselTypePartMappingId,
                        VesselTypeId = t.VesselTypeId == 0 ? null : t.VesselTypeId,
                        PartName = string.IsNullOrWhiteSpace(t.PartName) ? null : t.PartName,
                        TankTypeId = t.TankTypeId,
                        TotalCards = t.TotalCards,
                        PlaceholderCount = t.PlaceholderCount,
                        FileNameCount = t.FileNameCount,
                        IsActive = t.IsActive,
                        CreatedBy = t.CreatedBy ?? modifiedBy,
                        IsDeleted = t.IsDeleted,
                        ModifiedBy = modifiedBy
                    });
                }
            }

            // Raw subsections for validation & swapped TVP
            var rawSubSections = reportConfigList.normalSubSectionMappings?.ToList() ?? new List<VesselTypeSubSectionMapping>();
            var tvpSubSections = new List<(Guid? SubSectionId, Guid? SectionId, string? SectionName, int? TotalCards, int? PlaceholderCount, int? FileNameCount, bool? IsActive, string? CreatedBy, bool? IsDeleted, string? ModifiedBy)>();

            if (rawSubSections.Count == 0)
            {
                tvpSubSections.Add((null, null, null, null, null, null, null, null, null, null));
            }
            else
            {
                foreach (var s in rawSubSections)
                {
                    string? tvpCreatedBy = s.IsDeleted ? "1" : "0";
                    bool? tvpIsDeleted = null;
                    if (s.CreatedBy != null)
                    {
                        var trimmed = s.CreatedBy.Trim();
                        if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "1", StringComparison.OrdinalIgnoreCase))
                        {
                            tvpIsDeleted = true;
                        }
                        else if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "0", StringComparison.OrdinalIgnoreCase))
                        {
                            tvpIsDeleted = false;
                        }
                        else
                        {
                            return false; // Trap 10: non-bit text in subsection CreatedBy fails bit conversion!
                        }
                    }

                    tvpSubSections.Add((s.SubSectionId, s.SectionId, s.SectionName, s.TotalCards, s.PlaceholderCount, s.FileNameCount, s.IsActive, tvpCreatedBy, tvpIsDeleted, modifiedBy));
                }
            }

            // C2: Checks BEFORE any write
            // Text length checks
            foreach (var p in tvpParts)
            {
                if (p.PartName != null && p.PartName.Length > 100) return false;
                if (p.CreatedBy != null && p.CreatedBy.Length > 100) return false;
                if (p.ModifiedBy != null && p.ModifiedBy.Length > 100) return false;
            }
            foreach (var s in tvpNormalSections)
            {
                if (s.PartName != null && s.PartName.Length > 50) return false;
                if (s.SectionName != null && s.SectionName.Length > 100) return false;
                if (s.SubHeader != null && s.SubHeader.Length > 100) return false;
                if (s.CreatedBy != null && s.CreatedBy.Length > 100) return false;
                if (s.ModifiedBy != null && s.ModifiedBy.Length > 100) return false;
            }
            foreach (var t in tvpTankSections)
            {
                if (t.PartName != null && t.PartName.Length > 50) return false;
                if (t.CreatedBy != null && t.CreatedBy.Length > 100) return false;
                if (t.ModifiedBy != null && t.ModifiedBy.Length > 100) return false;
            }
            foreach (var sub in tvpSubSections)
            {
                if (sub.SectionName != null && sub.SectionName.Length > 100) return false;
                if (sub.CreatedBy != null && sub.CreatedBy.Length > 100) return false;
                if (sub.ModifiedBy != null && sub.ModifiedBy.Length > 100) return false;
            }

            bool anyError = false;

            // MERGE 1: Parts
            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var existingParts = new List<VesselTypePartMappingDocument>();
            var vtpQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId")
                .WithParameter("@vesselTypeId", vesselTypeId);

            using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(vtpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    existingParts.AddRange(response);
                }
            }

            if (tvpParts.Count > 0)
            {
                bool merge1Error = false;
                var distinctParts = tvpParts
                    .GroupBy(p => new
                    {
                        p.VesselTypePartMappingId,
                        p.VesselTypeId,
                        PartName = p.PartName?.TrimEnd(' ').ToLowerInvariant(),
                        p.SequenceNo,
                        p.IsActive,
                        p.IsDeleted,
                        CreatedBy = p.CreatedBy?.TrimEnd(' ').ToLowerInvariant()
                    })
                    .Select(g => g.First())
                    .ToList();

                // R8a: Duplicate target error happens ONLY when two distinct source rows match the same EXISTING target row
                var matchedPartTargetIds = new List<int>();
                foreach (var p in distinctParts)
                {
                    if (p.VesselTypePartMappingId.HasValue && p.VesselTypeId.HasValue)
                    {
                        var m = existingParts.FirstOrDefault(x => x.VesselTypePartMappingId == p.VesselTypePartMappingId.Value && x.VesselTypeId == p.VesselTypeId.Value);
                        if (m != null)
                        {
                            matchedPartTargetIds.Add(m.VesselTypePartMappingId);
                        }
                    }
                }
                if (matchedPartTargetIds.GroupBy(x => x).Any(g => g.Count() > 1))
                {
                    merge1Error = true;
                }

                // R8b: Length check: ModifiedBy (nvarchar 50) applies only to MATCHED rows; inserted rows check CreatedBy against 100
                foreach (var p in distinctParts)
                {
                    bool isMatched = p.VesselTypePartMappingId.HasValue && p.VesselTypeId.HasValue &&
                        existingParts.Any(x => x.VesselTypePartMappingId == p.VesselTypePartMappingId.Value && x.VesselTypeId == p.VesselTypeId.Value);

                    if (isMatched)
                    {
                        if (p.CreatedBy != null && p.CreatedBy.Length > 50) { merge1Error = true; break; }
                    }
                    else
                    {
                        if (p.CreatedBy != null && p.CreatedBy.Length > 100) { merge1Error = true; break; }
                    }
                    if (p.PartName != null && p.PartName.Length > 100) { merge1Error = true; break; }
                }

                if (merge1Error)
                {
                    anyError = true;
                }
                else
                {
                    foreach (var p in distinctParts)
                    {
                        var match = (p.VesselTypePartMappingId.HasValue && p.VesselTypeId.HasValue)
                            ? existingParts.FirstOrDefault(x => x.VesselTypePartMappingId == p.VesselTypePartMappingId.Value && x.VesselTypeId == p.VesselTypeId.Value)
                            : null;
                        if (match != null)
                        {
                            match.PartName = p.PartName;
                            match.SequenceNo = p.SequenceNo;
                            match.IsActive = p.IsActive;
                            match.IsDeleted = p.IsDeleted;
                            match.ModifiedBy = p.CreatedBy;
                            match.ModifiedOn = DateTime.UtcNow.Date;
                            await vtpContainer.UpsertItemAsync(match, new PartitionKey((double)vesselTypeId));
                        }
                        else
                        {
                            var newId = (int)await _sequenceService.GetNextAsync(CosmosContainers.Config_VesselTypePartMapping);
                            var newPart = new VesselTypePartMappingDocument
                            {
                                id = newId.ToString(),
                                VesselTypePartMappingId = newId,
                                VesselTypeId = p.VesselTypeId,
                                PartName = p.PartName,
                                SequenceNo = p.SequenceNo,
                                IsActive = p.IsActive,
                                CreatedBy = p.CreatedBy,
                                CreatedOn = DateTime.UtcNow.Date,
                                IsDeleted = p.IsDeleted,
                                ModifiedBy = "SYSTEM",
                                ModifiedOn = DateTime.UtcNow.Date,
                                RequiredInReport = true
                            };
                            await vtpContainer.CreateItemAsync(newPart, new PartitionKey((double)vesselTypeId));
                            existingParts.Add(newPart);
                        }
                    }
                }
            }

            // MERGE 2: Normal sections
            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var existingSections = new List<VesselTypeNormalSectionMappingDocument>();
            var vnsQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId")
                .WithParameter("@vesselTypeId", vesselTypeId);

            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(vnsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    existingSections.AddRange(response);
                }
            }

            var updatedNormalSections = (from nsm in tvpNormalSections
                                         where nsm.PartName != null && nsm.VesselTypeId.HasValue
                                         from vtpm in existingParts
                                         where vtpm.PartName != null &&
                                               string.Equals(nsm.PartName.TrimEnd(' '), vtpm.PartName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase) &&
                                               vtpm.VesselTypeId == nsm.VesselTypeId.Value
                                         select new
                                         {
                                             VesselTypeNormalSectionMappingId = nsm.VesselTypeNormalSectionMappingId,
                                             VesselTypePartMappingId = vtpm.VesselTypePartMappingId,
                                             VesselTypeId = nsm.VesselTypeId,
                                             SectionName = nsm.SectionName,
                                             SubHeader = nsm.SubHeader,
                                             TotalCards = nsm.TotalCards,
                                             PlaceholderCount = nsm.PlaceholderCount,
                                             FileNameCount = nsm.FileNameCount,
                                             IsActive = nsm.IsActive,
                                             IsDeleted = nsm.IsDeleted,
                                             CreatedBy = nsm.CreatedBy
                                         })
                                         .GroupBy(s => new
                                         {
                                             s.VesselTypeNormalSectionMappingId,
                                             s.VesselTypePartMappingId,
                                             s.VesselTypeId,
                                             SectionName = s.SectionName?.TrimEnd(' ').ToLowerInvariant(),
                                             SubHeader = s.SubHeader?.TrimEnd(' ').ToLowerInvariant(),
                                             s.TotalCards,
                                             s.PlaceholderCount,
                                             s.FileNameCount,
                                             s.IsActive,
                                             s.IsDeleted,
                                             CreatedBy = s.CreatedBy?.TrimEnd(' ').ToLowerInvariant()
                                         })
                                         .Select(g => g.First())
                                         .ToList();

            bool merge2Error = false;
            // R8a: Duplicate target error only when matching an existing section
            var matchedSectionTargetIds = new List<string>();
            foreach (var s in updatedNormalSections)
            {
                if (s.VesselTypeNormalSectionMappingId.HasValue && s.VesselTypeNormalSectionMappingId.Value != Guid.Empty && s.VesselTypeId.HasValue)
                {
                    var m = existingSections.FirstOrDefault(x =>
                        x.VesselTypePartMappingId == s.VesselTypePartMappingId &&
                        x.VesselTypeId == s.VesselTypeId.Value &&
                        string.Equals(x.SectionMappingId, s.VesselTypeNormalSectionMappingId.Value.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (m != null)
                    {
                        matchedSectionTargetIds.Add(m.SectionMappingId);
                    }
                }
            }
            if (matchedSectionTargetIds.GroupBy(x => x).Any(g => g.Count() > 1))
            {
                merge2Error = true;
            }

            // R8b: ModifiedBy check on matched (<= 50); CreatedBy check on inserted (<= 100); SectionName/SubHeader <= 100
            foreach (var s in updatedNormalSections)
            {
                bool isMatched = s.VesselTypeNormalSectionMappingId.HasValue && s.VesselTypeNormalSectionMappingId.Value != Guid.Empty && s.VesselTypeId.HasValue &&
                    existingSections.Any(x => x.VesselTypePartMappingId == s.VesselTypePartMappingId && x.VesselTypeId == s.VesselTypeId.Value &&
                        string.Equals(x.SectionMappingId, s.VesselTypeNormalSectionMappingId.Value.ToString(), StringComparison.OrdinalIgnoreCase));

                if (isMatched)
                {
                    if (s.CreatedBy != null && s.CreatedBy.Length > 50) { merge2Error = true; break; }
                }
                else
                {
                    if (s.CreatedBy != null && s.CreatedBy.Length > 100) { merge2Error = true; break; }
                }
                if (s.SectionName != null && s.SectionName.Length > 100) { merge2Error = true; break; }
                if (s.SubHeader != null && s.SubHeader.Length > 100) { merge2Error = true; break; }
            }

            if (merge2Error)
            {
                anyError = true;
            }
            else
            {
                // R8e: Compute [Order] count/max once per part BEFORE the loop. When every existing row has NULL [Order], keep NULL
                var distinctPartIds = updatedNormalSections.Select(s => s.VesselTypePartMappingId).Distinct().ToList();
                var partOrderInfo = new Dictionary<int, (int count, int? orderForNew)>();
                foreach (var pid in distinctPartIds)
                {
                    var count = existingSections.Count(s => s.VesselTypeId == vesselTypeId && s.VesselTypePartMappingId == pid);
                    if (count == 0)
                    {
                        partOrderInfo[pid] = (0, 1);
                    }
                    else
                    {
                        var nonNullOrders = existingSections
                            .Where(s => s.VesselTypeId == vesselTypeId && s.VesselTypePartMappingId == pid && s.Order.HasValue)
                            .Select(s => s.Order!.Value)
                            .ToList();
                        int? orderForNew = nonNullOrders.Count == 0 ? (int?)null : nonNullOrders.Max() + 1;
                        partOrderInfo[pid] = (count, orderForNew);
                    }
                }

                foreach (var src in updatedNormalSections)
                {
                    var srcGuidStr = src.VesselTypeNormalSectionMappingId?.ToString();
                    bool isNonEmptyGuid = src.VesselTypeNormalSectionMappingId.HasValue && src.VesselTypeNormalSectionMappingId.Value != Guid.Empty;

                    var match = isNonEmptyGuid ? existingSections.FirstOrDefault(x =>
                        x.VesselTypePartMappingId == src.VesselTypePartMappingId &&
                        x.VesselTypeId == src.VesselTypeId &&
                        string.Equals(x.SectionMappingId, srcGuidStr, StringComparison.OrdinalIgnoreCase)) : null;

                    int pCount = 0;
                    int? pOrderForNew = 1;
                    if (partOrderInfo.ContainsKey(src.VesselTypePartMappingId))
                    {
                        var info = partOrderInfo[src.VesselTypePartMappingId];
                        pCount = info.count;
                        pOrderForNew = info.orderForNew;
                    }

                    if (match != null)
                    {
                        match.SectionName = src.SectionName;
                        match.SubHeader = src.SubHeader;
                        match.TotalCards = src.TotalCards;
                        match.PlaceholderCount = src.PlaceholderCount;
                        match.FileNameCount = src.FileNameCount;
                        match.IsActive = src.IsActive;
                        match.IsDeleted = src.IsDeleted;
                        match.ModifiedBy = src.CreatedBy;
                        match.ModifiedOn = DateTime.UtcNow.Date;
                        match.Order = pCount == 0 ? 1 : match.Order;

                        await vnsContainer.UpsertItemAsync(match, new PartitionKey((double)vesselTypeId));
                    }
                    else
                    {
                        var secId = Guid.NewGuid().ToString();

                        var newSec = new VesselTypeNormalSectionMappingDocument
                        {
                            id = secId,
                            SectionMappingId = secId,
                            VesselTypePartMappingId = src.VesselTypePartMappingId,
                            VesselTypeId = src.VesselTypeId,
                            SectionName = src.SectionName,
                            SubHeader = src.SubHeader,
                            TotalCards = src.TotalCards,
                            PlaceholderCount = src.PlaceholderCount,
                            FileNameCount = src.FileNameCount,
                            IsActive = src.IsActive,
                            CreatedBy = src.CreatedBy,
                            CreatedOn = DateTime.UtcNow.Date,
                            IsDeleted = src.IsDeleted,
                            Order = pOrderForNew,
                            RequiredInReport = true
                        };

                        await vnsContainer.CreateItemAsync(newSec, new PartitionKey((double)vesselTypeId));
                    }
                }
            }

            // MERGE 3: Tank sections
            var vtsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
            var existingTankSections = new List<VesselTypeTankSectionMappingDocument>();
            var vtsQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId")
                .WithParameter("@vesselTypeId", vesselTypeId);

            using (var feed = vtsContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(vtsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    existingTankSections.AddRange(response);
                }
            }

            var updatedTankSections = (from tsm in tvpTankSections
                                       where !string.IsNullOrEmpty(tsm.PartName) && tsm.VesselTypeId.HasValue
                                       from vtpm in existingParts
                                       where !string.IsNullOrEmpty(vtpm.PartName) &&
                                             string.Equals(tsm.PartName.TrimEnd(' '), vtpm.PartName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase) &&
                                             vtpm.VesselTypeId == tsm.VesselTypeId.Value
                                       select new
                                       {
                                           VesselTypePartMappingId = vtpm.VesselTypePartMappingId,
                                           VesselTypeId = tsm.VesselTypeId,
                                           TankTypeId = tsm.TankTypeId,
                                           TotalCards = tsm.TotalCards,
                                           PlaceholderCount = tsm.PlaceholderCount,
                                           FileNameCount = tsm.FileNameCount,
                                           IsActive = tsm.IsActive,
                                           IsDeleted = tsm.IsDeleted,
                                           CreatedBy = tsm.CreatedBy
                                       })
                                       .GroupBy(t => new
                                       {
                                           t.VesselTypePartMappingId,
                                           t.VesselTypeId,
                                           t.TankTypeId,
                                           t.TotalCards,
                                           t.PlaceholderCount,
                                           t.FileNameCount,
                                           t.IsActive,
                                           t.IsDeleted,
                                           CreatedBy = t.CreatedBy?.TrimEnd(' ').ToLowerInvariant()
                                       })
                                       .Select(g => g.First())
                                       .ToList();

            bool merge3Error = false;
            // R8a: Duplicate target error only when two distinct source rows match the same EXISTING tank section
            var matchedTankTargetIds = new List<string>();
            foreach (var t in updatedTankSections)
            {
                if (t.TankTypeId.HasValue)
                {
                    var m = existingTankSections.FirstOrDefault(x =>
                        x.VesselTypePartMappingId.HasValue && x.VesselTypePartMappingId.Value == t.VesselTypePartMappingId &&
                        x.TankTypeId.HasValue && x.TankTypeId.Value == t.TankTypeId.Value);
                    if (m != null)
                    {
                        matchedTankTargetIds.Add(m.VesselTypeTankSectionMappingId ?? m.id);
                    }
                }
            }
            if (matchedTankTargetIds.GroupBy(x => x).Any(g => g.Count() > 1))
            {
                merge3Error = true;
            }

            // R8b: ModifiedBy check on matched (<= 50); CreatedBy check on inserted (<= 100)
            foreach (var t in updatedTankSections)
            {
                bool isMatched = t.TankTypeId.HasValue &&
                    existingTankSections.Any(x => x.VesselTypePartMappingId.HasValue && x.VesselTypePartMappingId.Value == t.VesselTypePartMappingId &&
                                                  x.TankTypeId.HasValue && x.TankTypeId.Value == t.TankTypeId.Value);
                if (isMatched)
                {
                    if (t.CreatedBy != null && t.CreatedBy.Length > 50) { merge3Error = true; break; }
                }
                else
                {
                    if (t.CreatedBy != null && t.CreatedBy.Length > 100) { merge3Error = true; break; }
                }
            }

            if (merge3Error)
            {
                anyError = true;
            }
            else
            {
                foreach (var src in updatedTankSections)
                {
                    // R6a: both TankTypeId and VesselTypePartMappingId must be non-null to match; otherwise INSERT
                    var match = src.TankTypeId.HasValue
                        ? existingTankSections.FirstOrDefault(x =>
                            x.VesselTypePartMappingId.HasValue && x.VesselTypePartMappingId.Value == src.VesselTypePartMappingId &&
                            x.TankTypeId.HasValue && x.TankTypeId.Value == src.TankTypeId.Value)
                        : null;

                    if (match != null)
                    {
                        match.TotalCards = src.TotalCards;
                        match.PlaceholderCount = src.PlaceholderCount;
                        match.FileNameCount = src.FileNameCount;
                        match.IsActive = src.IsActive;
                        match.IsDeleted = src.IsDeleted;
                        match.ModifiedBy = src.CreatedBy;
                        match.ModifiedOn = DateTime.UtcNow.Date;
                        await vtsContainer.UpsertItemAsync(match, new PartitionKey((double)vesselTypeId));
                    }
                    else
                    {
                        var mapId = Guid.NewGuid().ToString();
                        var newTankSec = new VesselTypeTankSectionMappingDocument
                        {
                            id = mapId,
                            VesselTypeTankSectionMappingId = mapId,
                            VesselTypePartMappingId = src.VesselTypePartMappingId,
                            VesselTypeId = src.VesselTypeId,
                            TankTypeId = src.TankTypeId,
                            TotalCards = src.TotalCards,
                            PlaceholderCount = src.PlaceholderCount,
                            FileNameCount = src.FileNameCount,
                            IsActive = src.IsActive,
                            CreatedBy = src.CreatedBy,
                            CreatedOn = DateTime.UtcNow.Date,
                            IsDeleted = src.IsDeleted
                        };
                        await vtsContainer.CreateItemAsync(newTankSec, new PartitionKey((double)vesselTypeId));
                        existingTankSections.Add(newTankSec);
                    }
                }
            }

            // MERGE 4: Subsections
            var distinctSubSections = tvpSubSections
                .Where(s => s.SubSectionId.HasValue && s.SectionId.HasValue)
                .GroupBy(s => new
                {
                    s.SubSectionId,
                    s.SectionId,
                    SectionName = s.SectionName?.TrimEnd(' ').ToLowerInvariant(),
                    s.TotalCards,
                    s.PlaceholderCount,
                    s.FileNameCount,
                    s.IsActive,
                    s.CreatedBy,
                    s.IsDeleted
                })
                .Select(g => g.First())
                .ToList();

            var subIds = distinctSubSections.Select(s => s.SubSectionId!.Value.ToString()).Distinct().ToList();
            if (subIds.Count > 0)
            {
                bool merge4Error = false;
                var vssContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSection);
                var existingSubSections = new List<VesselTypeSubSectionDocument>();
                var vssQ = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@subIds, c.Id)")
                    .WithParameter("@subIds", subIds);

                using (var feed = vssContainer.GetItemQueryIterator<VesselTypeSubSectionDocument>(vssQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        existingSubSections.AddRange(response);
                    }
                }

                // R8a: Duplicate target error only when matching an existing subsection
                var matchedSubTargetIds = new List<string>();
                foreach (var s in distinctSubSections)
                {
                    var subIdStr = s.SubSectionId!.Value.ToString();
                    var secIdStr = s.SectionId!.Value.ToString();
                    var m = existingSubSections.FirstOrDefault(x =>
                        string.Equals(x.Id, subIdStr, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.VesselTypeSectionId, secIdStr, StringComparison.OrdinalIgnoreCase));
                    if (m != null)
                    {
                        matchedSubTargetIds.Add(m.Id ?? m.id);
                    }
                }
                if (matchedSubTargetIds.GroupBy(x => x).Any(g => g.Count() > 1))
                {
                    merge4Error = true;
                }

                // R8c: Subsections: matched rows write SectionName into nvarchar(50) NOT NULL. Over 50 or NULL -> error
                foreach (var s in distinctSubSections)
                {
                    var subIdStr = s.SubSectionId!.Value.ToString();
                    var secIdStr = s.SectionId!.Value.ToString();
                    var m = existingSubSections.FirstOrDefault(x =>
                        string.Equals(x.Id, subIdStr, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.VesselTypeSectionId, secIdStr, StringComparison.OrdinalIgnoreCase));
                    if (m != null)
                    {
                        if (s.SectionName == null || s.SectionName.Length > 50)
                        {
                            merge4Error = true;
                            break;
                        }
                    }
                }

                if (merge4Error)
                {
                    anyError = true;
                }
                else
                {
                    foreach (var src in distinctSubSections)
                    {
                        var subIdStr = src.SubSectionId!.Value.ToString();
                        var secIdStr = src.SectionId!.Value.ToString();

                        var match = existingSubSections.FirstOrDefault(x =>
                            string.Equals(x.Id, subIdStr, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(x.VesselTypeSectionId, secIdStr, StringComparison.OrdinalIgnoreCase));

                        if (match != null)
                        {
                            match.Totalcards = src.TotalCards;
                            match.PlaceholderCount = src.PlaceholderCount;
                            match.FileNameCount = src.FileNameCount;
                            match.SectionName = src.SectionName;
                            match.IsActive = src.IsActive;
                            match.IsDeleted = false;
                            match.ModifiedBy = src.CreatedBy; // '0' or '1' from bool IsDeleted
                            match.ModifiedOn = DateTime.UtcNow.Date;
                            await vssContainer.UpsertItemAsync(match, new PartitionKey(match.id));
                        }
                    }
                }
            }

            return !anyError;
        }

        // Helper 15: Sp_CheckToShowSectionExportConfig
        internal async Task<bool> Sp_CheckToShowSectionExportConfig(int projectId, int imoNumber, int partId)
        {
            var psmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
            var psmQ = new QueryDefinition(
                "SELECT VALUE COUNT(1) FROM c WHERE c.ProjectId = @projectId AND c.PartId = @partId AND c.RequiredInReport = false")
                .WithParameter("@projectId", projectId)
                .WithParameter("@partId", partId);

            int n_c = 0;
            using (var feed = psmContainer.GetItemQueryIterator<int>(psmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    n_c += resp.FirstOrDefault();
                }
            }

            var ptsmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectTankSectionMapping);
            var ptsmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.PartId = @partId")
                .WithParameter("@partId", partId);

            var ptsmList = new List<ProjectTankSectionMappingDocument>();
            using (var feed = ptsmContainer.GetItemQueryIterator<ProjectTankSectionMappingDocument>(ptsmQ))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    ptsmList.AddRange(resp);
                }
            }

            var vtmContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var vtmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.RequiredInReport = false")
                .WithParameter("@projectId", projectId);

            var vtmDocs = new List<VesselTankMappingDocument>();
            using (var feed = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(vtmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    vtmDocs.AddRange(resp);
                }
            }

            int t_c = 0;
            foreach (var vtm in vtmDocs)
            {
                if (vtm.ImoNumber == null)
                {
                    continue; // R4: NULL ImoNumber matches nothing
                }
                long vtmImo = 0;
                if (!string.IsNullOrWhiteSpace(vtm.ImoNumber))
                {
                    if (!long.TryParse(vtm.ImoNumber.Trim(), out vtmImo))
                    {
                        return false;
                    }
                }

                if (vtmImo == (long)imoNumber)
                {
                    t_c += ptsmList.Count(x => x.TankTypeId.HasValue && vtm.TankTypeId.HasValue && x.TankTypeId.Value == vtm.TankTypeId.Value);
                }
            }

            return (n_c + t_c) > 0;
        }

        // Helper 16: Sp_GetGradingCondition
        internal async Task<List<ProjectCheckBox>> Sp_GetGradingCondition()
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_GradingCondition);
            var q = new QueryDefinition("SELECT * FROM c");

            var docs = new List<GradingConditionDocument>();
            using (var feed = container.GetItemQueryIterator<GradingConditionDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    docs.AddRange(resp);
                }
            }

            return docs.Select(d => new ProjectCheckBox
            {
                Id = d.Id,
                Name = d.Condition,
                Value = false,
                GradingId = 0,
                SectionId = Guid.Empty
            }).ToList();
        }

        // Helper 17: Sp_GetNormalsectionsByProjectId
        internal async Task<List<ProjectSubSection>> Sp_GetNormalsectionsByProjectId(int projectId, int partId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.PartId = @partId")
                .WithParameter("@projectId", projectId)
                .WithParameter("@partId", partId);

            var docs = new List<ProjectSectionMappingDocument>();
            using (var feed = container.GetItemQueryIterator<ProjectSectionMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    docs.AddRange(resp);
                }
            }

            return docs.OrderBy(d => d.Order.HasValue).ThenBy(d => d.Order).Select(d => new ProjectSubSection
            {
                SectionMappingId = Guid.Parse(d.SectionMappingId ?? d.id),
                Id = Guid.Empty,
                SectionName = d.SectionName,
                SubHeader = d.SubHeader,
                Totalcards = d.TotalCards ?? 0,
                PlaceholderCount = d.PlaceholderCount ?? 0,
                FileNameCount = d.FileNameCount ?? 0,
                Order = d.Order ?? 0,
                HasSubsection = d.HasSubSection == true,
                IsSubSection = false
            }).ToList();
        }

        // Helper 18: Sp_GetProjectSubsectionBySectionId
        internal async Task<List<ProjectSubSection>> Sp_GetProjectSubsectionBySectionId(Guid sectionId)
        {
            var secStr = sectionId.ToString();
            var pssContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSubSection);
            var pssQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectSectionId = @secId")
                .WithParameter("@secId", secStr);

            var pssList = new List<ProjectSubSectionDocument>();
            using (var feed = pssContainer.GetItemQueryIterator<ProjectSubSectionDocument>(pssQ))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    pssList.AddRange(resp);
                }
            }

            var psmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
            var psmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.SectionMappingId = @secId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@secId", secStr);

            ProjectSectionMappingDocument? psmDoc = null;
            using (var feed = psmContainer.GetItemQueryIterator<ProjectSectionMappingDocument>(psmQ))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    if (psmDoc == null)
                    {
                        psmDoc = resp.FirstOrDefault();
                    }
                }
            }

            if (psmDoc == null)
            {
                return new List<ProjectSubSection>();
            }

            return pssList.OrderBy(x => x.OrderNumber.HasValue).ThenBy(x => x.OrderNumber).Select(sub => new ProjectSubSection
            {
                SectionMappingId = Guid.Parse(sub.Id ?? sub.id),
                Id = Guid.Parse(psmDoc.SectionMappingId ?? psmDoc.id),
                SectionName = sub.SectionName,
                Totalcards = sub.Totalcards ?? 0,
                FileNameCount = sub.FileNameCount ?? 0,
                PlaceholderCount = sub.PlaceholderCount ?? 0,
                Order = psmDoc.Order ?? 0,
                HasSubsection = false,
                IsSubSection = true
            }).ToList();
        }

        // Helper 19: Sp_GetGradigConditionByProjectId
        internal async Task<List<ProjectCheckBox>> Sp_GetGradigConditionByProjectId(int projectId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_ProjectGradingConditionMapping);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var docs = new List<ProjectGradingConditionMappingDocument>();
            using (var feed = container.GetItemQueryIterator<ProjectGradingConditionMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    docs.AddRange(resp);
                }
            }

            return docs.Select(d => new ProjectCheckBox
            {
                Id = d.Id,
                SectionId = !string.IsNullOrEmpty(d.SectionId) && Guid.TryParse(d.SectionId, out var g) ? g : Guid.Empty,
                GradingId = d.GradingId ?? 0,
                Name = d.GradingConditionLabel,
                Value = d.GradingConditionValue == true
            }).ToList();
        }

        // Helper 20: Sp_GetProjectSectionGrading
        internal async Task<List<ProjectGradingUI>> Sp_GetProjectSectionGrading(int projectId, Guid sectionId)
        {
            var secStr = sectionId.ToString();
            var psgContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionGradingMapping);
            var psgQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.SectionId = @secId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@secId", secStr);

            var psgList = new List<ProjectSectionGradingMappingDocument>();
            using (var feed = psgContainer.GetItemQueryIterator<ProjectSectionGradingMappingDocument>(psgQ))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    psgList.AddRange(resp);
                }
            }

            var gradingContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Grading);
            var gradingQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var gradingList = new List<GradingDocument>();
            using (var feed = gradingContainer.GetItemQueryIterator<GradingDocument>(gradingQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    gradingList.AddRange(resp);
                }
            }

            var matches = (from psg in psgList
                           from gr in gradingList.Where(g => g.GradingGroupId.HasValue && g.GradingGroupId.Value == psg.CategoryId)
                           orderby gr.LabelName != null, gr.LabelName
                           select new ProjectGradingUI
                           {
                               Id = gr.Id,
                               Name = gr.LabelName,
                               GradingConditionGroupId = gr.GradingConditionGroupId ?? 0,
                               RequiredInReport = gr.RequiredInReport ?? false
                           }).ToList();

            return matches;
        }

        // Helper 21: Sp_GetProjectSectionDescription
        internal async Task<List<ImageDescriptionUI>> Sp_GetProjectSectionDescription(int projectId, Guid sectionId)
        {
            var secStr = sectionId.ToString();
            var psdContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionDescriptionMapping);
            var psdQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.SectionId = @secId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@secId", secStr);

            var psdList = new List<ProjectSectionDescriptionMappingDocument>();
            using (var feed = psdContainer.GetItemQueryIterator<ProjectSectionDescriptionMappingDocument>(psdQ))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    psdList.AddRange(resp);
                }
            }

            var descContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_ImageDescriptions);
            var descQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var descList = new List<ImageDescriptionDocument>();
            using (var feed = descContainer.GetItemQueryIterator<ImageDescriptionDocument>(descQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    descList.AddRange(resp);
                }
            }

            var matches = (from psd in psdList
                           join d in descList on (long)psd.CategoryId equals d.CategoryId
                           select new ImageDescriptionUI
                           {
                               Id = d.Id,
                               Description = d.Description
                           }).ToList();

            return matches;
        }

        // Helper 22: Sp_GetProjectSubSectionDescription
        internal async Task<List<ImageDescriptionUI>> Sp_GetProjectSubSectionDescription(int projectId, Guid sectionId)
        {
            var secStr = sectionId.ToString();
            var pssdContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSubSectionDescriptionMapping);
            var pssdQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.SubSectionId = @secId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@secId", secStr);

            var pssdList = new List<ProjectSubSectionDescriptionMappingDocument>();
            using (var feed = pssdContainer.GetItemQueryIterator<ProjectSubSectionDescriptionMappingDocument>(pssdQ))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    pssdList.AddRange(resp);
                }
            }

            var descContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_ImageDescriptions);
            var descQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var descList = new List<ImageDescriptionDocument>();
            using (var feed = descContainer.GetItemQueryIterator<ImageDescriptionDocument>(descQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    descList.AddRange(resp);
                }
            }

            var matches = (from pssd in pssdList
                           where pssd.CategoryId.HasValue
                           join d in descList on (long)pssd.CategoryId.Value equals d.CategoryId
                           select new ImageDescriptionUI
                           {
                               Id = d.Id,
                               Description = d.Description
                           }).ToList();

            return matches;
        }

        // Helper 23: Sp_GetGenericImageCard
        internal async Task<List<GenericImageCard>> Sp_GetGenericImageCard(int? projectId, int? templateId, Guid? sectionId)
        {
            var secStr = sectionId?.ToString();
            var cardContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_GenericImageCard);

            QueryDefinition q;
            QueryRequestOptions? options = null;

            if (projectId.HasValue)
            {
                q = new QueryDefinition(
                    "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId AND c.SectionId = @sectionId AND c.IsActive = true AND (c.IsDeleted = false OR IS_NULL(c.IsDeleted))")
                    .WithParameter("@projectId", projectId.Value)
                    .WithParameter("@templateId", templateId)
                    .WithParameter("@sectionId", secStr);
                options = new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId.Value) };
            }
            else
            {
                q = new QueryDefinition(
                    "SELECT * FROM c WHERE c.TemplateId = @templateId AND c.SectionId = @sectionId AND c.IsActive = true AND (c.IsDeleted = false OR IS_NULL(c.IsDeleted))")
                    .WithParameter("@templateId", templateId)
                    .WithParameter("@sectionId", secStr);
            }

            var cards = new List<GenericImageCardDocument>();
            using (var feed = cardContainer.GetItemQueryIterator<GenericImageCardDocument>(q, requestOptions: options))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    cards.AddRange(resp);
                }
            }

            var imgContainer = ContainerFactory.GetContainer(CosmosContainers.upskill_ImageData);
            var imageIds = cards.Where(c => c.ImageId.HasValue).Select(c => c.ImageId!.Value).Distinct().ToList();

            var imgList = new List<ImageDataDocument>();
            if (imageIds.Count > 0)
            {
                var imgQ = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@imgIds, c.ImageId)").WithParameter("@imgIds", imageIds);
                using (var feed = imgContainer.GetItemQueryIterator<ImageDataDocument>(imgQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var resp = await feed.ReadNextAsync();
                        imgList.AddRange(resp);
                    }
                }
            }

            var result = new List<GenericImageCard>();
            foreach (var c in cards)
            {
                if (c.ImageId.HasValue)
                {
                    var matchingImages = imgList.Where(img => img.ImageId == c.ImageId.Value).ToList();
                    if (matchingImages.Count > 0)
                    {
                        foreach (var img in matchingImages)
                        {
                            result.Add(new GenericImageCard
                            {
                                Id = c.Id,
                                CardName = c.CardName,
                                CardNumber = c.CardNumber ?? 0,
                                DescriptionId = c.DescriptionId ?? 0,
                                AdditionalDescription = c.AdditionalDescription,
                                CurrentCondition = c.CurrentCondition,
                                ImageId = c.ImageId ?? 0,
                                IsActive = c.IsActive == true,
                                src = img.Base64String
                            });
                        }
                    }
                    else
                    {
                        result.Add(new GenericImageCard
                        {
                            Id = c.Id,
                            CardName = c.CardName,
                            CardNumber = c.CardNumber ?? 0,
                            DescriptionId = c.DescriptionId ?? 0,
                            AdditionalDescription = c.AdditionalDescription,
                            CurrentCondition = c.CurrentCondition,
                            ImageId = c.ImageId ?? 0,
                            IsActive = c.IsActive == true,
                            src = null!
                        });
                    }
                }
                else
                {
                    result.Add(new GenericImageCard
                    {
                        Id = c.Id,
                        CardName = c.CardName,
                        CardNumber = c.CardNumber ?? 0,
                        DescriptionId = c.DescriptionId ?? 0,
                        AdditionalDescription = c.AdditionalDescription,
                        CurrentCondition = c.CurrentCondition,
                        ImageId = 0,
                        IsActive = c.IsActive == true,
                        src = null!
                    });
                }
            }

            return result;
        }

        // Helper 24: Sp_GetTankSectionsByProjectid
        internal async Task<List<ProjectTankUI>> Sp_GetTankSectionsByProjectid(int projectId, int partId, int imoNumber)
        {
            var vtmContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var vtmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsDeleted = false AND c.IsActive = true")
                .WithParameter("@projectId", projectId);

            var vtmList = new List<VesselTankMappingDocument>();
            using (var feed = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(vtmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    vtmList.AddRange(resp);
                }
            }

            var ttContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_TankType);
            var ttList = new List<TankTypeDocument>();
            using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    ttList.AddRange(resp);
                }
            }

            var ptsmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectTankSectionMapping);
            var ptsmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.PartId = @partId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId)
                .WithParameter("@partId", partId);

            var ptsmList = new List<ProjectTankSectionMappingDocument>();
            using (var feed = ptsmContainer.GetItemQueryIterator<ProjectTankSectionMappingDocument>(ptsmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    ptsmList.AddRange(resp);
                }
            }

            var filteredVtm = new List<VesselTankMappingDocument>();
            foreach (var vtm in vtmList)
            {
                if (vtm.ImoNumber == null)
                {
                    continue; // R4: NULL ImoNumber matches nothing
                }
                long vtmImo = 0;
                if (!string.IsNullOrWhiteSpace(vtm.ImoNumber))
                {
                    vtmImo = long.Parse(vtm.ImoNumber.Trim());
                }

                if (vtmImo == (long)imoNumber)
                {
                    filteredVtm.Add(vtm);
                }
            }

            var matches = (from vtm in filteredVtm
                           join tt in ttList on vtm.TankTypeId equals tt.Id
                           join ptsm in ptsmList on vtm.TankTypeId equals ptsm.TankTypeId
                           orderby vtm.TankName != null, vtm.TankName, vtm.TankTypeId.HasValue, vtm.TankTypeId, vtm.CreatedDttm.HasValue, vtm.CreatedDttm
                           select new ProjectTankUI
                           {
                               Id = Guid.Parse(vtm.Id ?? vtm.id),
                               SectionMappingId = Guid.Parse(ptsm.TankSectionMappingId ?? ptsm.id),
                               TankTypeId = vtm.TankTypeId ?? 0,
                               TankName = vtm.TankName,
                               Totalcards = ptsm.TotalCards ?? 0,
                               PlaceholderCount = ptsm.PlaceholderCount ?? 0,
                               FileNameCount = ptsm.FileNameCount ?? 0,
                               Subheader = vtm.Subheader
                           }).ToList();

            return matches;
        }

        // Helper 25: Sp_GetH_IGrading
        internal async Task<List<Core.Models.Survey.Grading>> Sp_GetH_IGrading(bool isGasCarrier)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.Config_H_I_Grading);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.SpecialGasGrading = @gas")
                .WithParameter("@gas", isGasCarrier);

            var docs = new List<HIGradingDocument>();
            using (var feed = container.GetItemQueryIterator<HIGradingDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    docs.AddRange(resp);
                }
            }

            return docs.Select(d => new Core.Models.Survey.Grading
            {
                Id = d.Id,
                Name = d.LabelName
            }).ToList();
        }

        // Helper 26: Sp_GetImageDescriptionsByTankTypeId
        internal async Task<List<ImageDescriptionUI>> Sp_GetImageDescriptionsByTankTypeId(int projectId, int tankTypeId)
        {
            int? targetTankTypeId = tankTypeId == 0 ? (int?)null : tankTypeId;

            var descContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_ImageDescriptions);
            var descQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true")
                .WithParameter("@projectId", projectId);

            var descList = new List<ImageDescriptionDocument>();
            using (var feed = descContainer.GetItemQueryIterator<ImageDescriptionDocument>(descQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    descList.AddRange(resp);
                }
            }

            var mapContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Tank_ImageDescriptionMapping);
            var mapDocs = new List<TankImageDescriptionMappingDocument>();
            using (var feed = mapContainer.GetItemQueryIterator<TankImageDescriptionMappingDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    mapDocs.AddRange(resp);
                }
            }

            var ttContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_TankType);
            var ttDocs = new List<TankTypeDocument>();
            using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    ttDocs.AddRange(resp);
                }
            }

            var matches = (from descr in descList
                           where descr.CategoryId.HasValue
                           from tank in mapDocs
                           where tank.CategoryId.HasValue && descr.CategoryId.Value == (long)tank.CategoryId.Value
                           join tt in ttDocs on tank.TankTypeId equals tt.Id
                           where (targetTankTypeId == null ? tank.TankTypeId != null : tank.TankTypeId == targetTankTypeId)
                           orderby descr.Id
                           select new ImageDescriptionUI
                           {
                               Id = descr.Id,
                               Description = descr.Description,
                               TankTypeId = tank.TankTypeId ?? 0
                           }).ToList();

            return matches;
        }

        // Helper 27: Sp_GetHandIProjectGradingConditionByProjectId
        internal async Task<List<ProjectGradingConditionMapping>> Sp_GetHandIProjectGradingConditionByProjectId(int projectId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_HandIProjectGradingConditions);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var docs = new List<HandIProjectGradingConditionsDocument>();
            using (var feed = container.GetItemQueryIterator<HandIProjectGradingConditionsDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    docs.AddRange(resp);
                }
            }

            return docs.Select(d => new ProjectGradingConditionMapping
            {
                Id = d.Id,
                ProjectId = d.ProjectId ?? projectId,
                GradingId = d.GradingID ?? 0,
                GradingConditionLabel = d.ConditionLabel,
                GradingConditionValue = d.Value == true
            }).ToList();
        }

        // Helper 28: Sp_GetHandIProjectGradingByProjectId
        internal async Task<List<ProjectGradingMapping>> Sp_GetHandIProjectGradingByProjectId(int projectId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_HandIProjectGrading);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectID = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var docs = new List<HandIProjectGradingDocument>();
            using (var feed = container.GetItemQueryIterator<HandIProjectGradingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    docs.AddRange(resp);
                }
            }

            return docs.Select(d => new ProjectGradingMapping
            {
                Id = d.Id,
                ProjectId = d.ProjectID ?? projectId,
                GradingId = d.GradingId ?? 0,
                Value = d.Value
            }).ToList();
        }

        // Helper 29: Sp_GetTemplateTitle
        internal async Task<ProjectPart> Sp_GetTemplateTitle(int partId, int projectId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.Config_ReportPart);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.PartId = @partId")
                .WithParameter("@projectId", projectId)
                .WithParameter("@partId", partId);

            using (var feed = container.GetItemQueryIterator<ReportPartDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    var match = resp.FirstOrDefault();
                    if (match != null)
                    {
                        return new ProjectPart
                        {
                            PartId = match.PartId,
                            PartName = match.PartName,
                            SequenceNo = match.SequenceNo ?? 0
                        };
                    }
                }
            }

            if (partId == 1)
            {
                return new ProjectPart
                {
                    PartId = 1,
                    PartName = "Vessel Main Data",
                    SequenceNo = 0
                };
            }

            if (partId == 2)
            {
                return new ProjectPart
                {
                    PartId = 2,
                    PartName = "Part H&I",
                    SequenceNo = 20
                };
            }

            return null!;
        }

        // Helper 30: Sp_GetVesselTypeExportConfigForParts
        internal async Task<List<ExportPart>> Sp_GetVesselTypeExportConfigForParts(int vesselTypeId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId);

            var docs = new List<VesselTypePartMappingDocument>();
            using (var feed = container.GetItemQueryIterator<VesselTypePartMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    docs.AddRange(resp);
                }
            }

            return docs.OrderBy(d => d.SequenceNo.HasValue).ThenBy(d => d.SequenceNo).Select(d => new ExportPart
            {
                PartId = d.VesselTypePartMappingId,
                PartName = d.PartName,
                RequiredInReport = d.RequiredInReport ?? false,
                IncludeInReport = true
            }).ToList();
        }

        // Helper 31: Sp_GetVesselTypeExportConfigForSections
        internal async Task<List<ExportSections>> Sp_GetVesselTypeExportConfigForSections(int partId, int vesselTypeId)
        {
            var vtContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_VesselType);
            var vtQ = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", vesselTypeId);
            string? vesselTypeName = null;
            using (var feed = vtContainer.GetItemQueryIterator<VesselTypeDocument>(vtQ))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    if (vesselTypeName == null)
                    {
                        vesselTypeName = resp.FirstOrDefault()?.VesselType;
                    }
                }
            }

            var vtName50 = vesselTypeName == null ? null : (vesselTypeName.Length > 50 ? vesselTypeName.Substring(0, 50) : vesselTypeName);

            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vnsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.VesselTypePartMappingId = @partId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId)
                .WithParameter("@partId", partId);

            var vnsDocs = new List<VesselTypeNormalSectionMappingDocument>();
            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(vnsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    vnsDocs.AddRange(resp);
                }
            }

            var vtsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
            var vtsQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vesselTypeId AND c.VesselTypePartMappingId = @partId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vesselTypeId", vesselTypeId)
                .WithParameter("@partId", partId);

            var vtsDocs = new List<VesselTypeTankSectionMappingDocument>();
            using (var feed = vtsContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(vtsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    vtsDocs.AddRange(resp);
                }
            }

            var validTankTypeIds = new HashSet<int?>(vtsDocs.Select(x => x.TankTypeId));

            var ttContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_TankType);
            var ttDocs = new List<TankTypeDocument>();
            using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    ttDocs.AddRange(resp);
                }
            }

            var vtmDocs = new List<VesselTankMappingDocument>();
            if (vtName50 != null)
            {
                var vtmContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
                // R9d: Query without case-sensitive VesselType filter, then compare in memory with OrdinalIgnoreCase + TrimEnd(' ')
                var vtmQ = new QueryDefinition(
                    "SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false AND (NOT IS_DEFINED(c.ImoNumber) OR IS_NULL(c.ImoNumber))");

                using (var feed = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(vtmQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var resp = await feed.ReadNextAsync();
                        vtmDocs.AddRange(resp.Where(x => validTankTypeIds.Contains(x.TankTypeId) &&
                            x.VesselType != null &&
                            string.Equals(x.VesselType.TrimEnd(' '), vtName50.TrimEnd(' '), StringComparison.OrdinalIgnoreCase)));
                    }
                }
            }

            var combined = new List<(ExportSections sec, int? order)>();

            foreach (var s in vnsDocs)
            {
                combined.Add((new ExportSections
                {
                    SectionId = Guid.Parse(s.SectionMappingId ?? s.id),
                    SectionName = s.SectionName,
                    RequiredInReport = s.RequiredInReport ?? false,
                    IncludeInReport = true,
                    TankTypeId = 0,
                    TankType = null!,
                    IsSubSection = false,
                    ParentId = Guid.Empty
                }, s.Order));
            }

            foreach (var t in vtmDocs)
            {
                var tt = ttDocs.FirstOrDefault(x => x.Id == t.TankTypeId);
                if (tt == null) continue; // R9a: INNER JOIN drops tanks with no TankType row

                combined.Add((new ExportSections
                {
                    SectionId = Guid.Parse(t.Id ?? t.id),
                    SectionName = t.TankName,
                    RequiredInReport = t.RequiredInReport ?? false,
                    IncludeInReport = true,
                    TankTypeId = t.TankTypeId ?? 0,
                    TankType = tt.TankType,
                    IsSubSection = false,
                    ParentId = Guid.Empty
                }, 999));
            }

            // R9c: sort the combined list by SQL ORDER BY (NULL first)
            var result = combined
                .OrderBy(x => x.order.HasValue).ThenBy(x => x.order)
                .Select(x => x.sec)
                .ToList();

            return result;
        }

        // Helper 32: Sp_GetProjectExportConfigForParts
        internal async Task<List<ExportPart>> Sp_GetProjectExportConfigForParts(int projectId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.Config_ReportPart);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var docs = new List<ReportPartDocument>();
            using (var feed = container.GetItemQueryIterator<ReportPartDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    docs.AddRange(resp);
                }
            }

            return docs.OrderBy(d => d.SequenceNo.HasValue).ThenBy(d => d.SequenceNo).Select(d => new ExportPart
            {
                PartId = d.PartId,
                PartName = d.PartName,
                RequiredInReport = d.RequiredInReport ?? false,
                IncludeInReport = true
            }).ToList();
        }

        // Helper 33: Sp_GetProjectExportConfigForSections
        internal async Task<List<ExportSections>> Sp_GetProjectExportConfigForSections(int partId, int projectId)
        {
            var psmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
            var psmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.PartId = @partId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId)
                .WithParameter("@partId", partId);

            var psmDocs = new List<ProjectSectionMappingDocument>();
            using (var feed = psmContainer.GetItemQueryIterator<ProjectSectionMappingDocument>(psmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    psmDocs.AddRange(resp);
                }
            }

            var subSectionsWithParent = new List<(ProjectSubSectionDocument sub, ProjectSectionMappingDocument parent)>();
            var psmWithSub = psmDocs.Where(s => s.HasSubSection == true).ToList();
            if (psmWithSub.Count > 0)
            {
                var psmSubSecIds = psmWithSub.Select(s => s.SectionMappingId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
                var pssContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSubSection);
                var pssQ = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(@secIds, c.ProjectSectionId) AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@secIds", psmSubSecIds);

                var allSub = new List<ProjectSubSectionDocument>();
                using (var feed = pssContainer.GetItemQueryIterator<ProjectSubSectionDocument>(pssQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var resp = await feed.ReadNextAsync();
                        allSub.AddRange(resp);
                    }
                }

                foreach (var parent in psmWithSub)
                {
                    var subs = allSub.Where(s => string.Equals(s.ProjectSectionId, parent.SectionMappingId, StringComparison.OrdinalIgnoreCase)).ToList();
                    foreach (var sub in subs)
                    {
                        subSectionsWithParent.Add((sub, parent));
                    }
                }
            }

            var sectionOrders = psmDocs.Where(s => s.Order.HasValue).Select(s => s.Order!.Value)
                .Concat(subSectionsWithParent.Where(sp => sp.parent.Order.HasValue).Select(sp => sp.parent.Order!.Value));
            int maxOrder = sectionOrders.DefaultIfEmpty(0).Max();

            var ptsmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectTankSectionMapping);
            var ptsmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.PartId = @partId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId)
                .WithParameter("@partId", partId);

            var ptsmDocs = new List<ProjectTankSectionMappingDocument>();
            using (var feed = ptsmContainer.GetItemQueryIterator<ProjectTankSectionMappingDocument>(ptsmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    ptsmDocs.AddRange(resp);
                }
            }

            var ttContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_TankType);
            var ttDocs = new List<TankTypeDocument>();
            using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    ttDocs.AddRange(resp);
                }
            }

            var vtmContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var vtmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@projectId", projectId);

            var vtmDocs = new List<VesselTankMappingDocument>();
            using (var feed = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(vtmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var resp = await feed.ReadNextAsync();
                    vtmDocs.AddRange(resp);
                }
            }

            var combined = new List<(ExportSections sec, int? order, DateTime? createdDttm)>();

            foreach (var main in psmDocs)
            {
                combined.Add((new ExportSections
                {
                    SectionId = Guid.Parse(main.SectionMappingId ?? main.id),
                    SectionName = main.SectionName,
                    RequiredInReport = main.RequiredInReport ?? false,
                    IncludeInReport = true,
                    TankTypeId = 0,
                    TankType = null!,
                    ParentId = Guid.Empty,
                    IsSubSection = false
                }, main.Order, null));
            }

            foreach (var sp in subSectionsWithParent)
            {
                combined.Add((new ExportSections
                {
                    SectionId = Guid.Parse(sp.sub.Id ?? sp.sub.id),
                    SectionName = sp.sub.SectionName,
                    RequiredInReport = sp.parent.RequiredInReport ?? false,
                    IncludeInReport = true,
                    TankTypeId = 0,
                    TankType = null!,
                    ParentId = Guid.Parse(sp.parent.SectionMappingId ?? sp.parent.id),
                    IsSubSection = true
                }, sp.parent.Order, null));
            }

            var tankJoinList = (from vtm in vtmDocs
                                join psm in ptsmDocs on vtm.TankTypeId equals psm.TankTypeId
                                join tt in ttDocs on vtm.TankTypeId equals tt.Id
                                orderby tt.Id, vtm.TankName != null, vtm.TankName, vtm.CreatedDttm.HasValue, vtm.CreatedDttm
                                select new { vtm, tt }).ToList();

            int rowNumber = 1;
            foreach (var item in tankJoinList)
            {
                int tankOrder = (maxOrder + 999) + rowNumber;
                rowNumber++;

                combined.Add((new ExportSections
                {
                    SectionId = Guid.Parse(item.vtm.Id ?? item.vtm.id),
                    SectionName = item.vtm.TankName,
                    RequiredInReport = item.vtm.RequiredInReport ?? false,
                    IncludeInReport = true,
                    TankTypeId = item.tt.Id,
                    TankType = item.tt.TankType,
                    ParentId = Guid.Empty,
                    IsSubSection = false
                }, tankOrder, item.vtm.CreatedDttm));
            }

            var finalResult = combined
                .OrderBy(x => x.order.HasValue).ThenBy(x => x.order)
                .ThenBy(x => x.createdDttm.HasValue).ThenBy(x => x.createdDttm)
                .Select(x => x.sec)
                .ToList();

            return finalResult;
        }

        #endregion
    }
}
