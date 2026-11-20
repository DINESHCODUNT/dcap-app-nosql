using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.Tank;
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
    public class CosmosGradingRepository : CosmosRepositoryBase<GradingDocument>, IGradingRepository
    {
        private readonly ISequenceService _sequenceService;
        private readonly ILogger<CosmosGradingRepository> _logger;

        public CosmosGradingRepository(
            ICosmosContainerFactory containerFactory,
            ISequenceService sequenceService,
            ILogger<CosmosGradingRepository> logger)
            : base(containerFactory, CosmosContainers.dbo_Grading, logger)
        {
            _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Commit()
        {
            // No-op for Cosmos DB repository
        }

        public async Task<int> CreateProjectSectionGrading(int projectId, string vesselType)
        {
            return await Sp_CreateProjectSectionGrading(projectId, vesselType);
        }

        public async Task<bool> CreateVessel_Grading(VesselTankGrading vesselTankGrading)
        {
            try
            {
                return await Sp_Create_Vessel_Grading(vesselTankGrading);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vessel grading");
                return false;
            }
        }

        public async Task<List<Grading>> GetAllGradingAsync()
        {
            return await Sp_GetAllGrading();
        }

        public Task<PagedResult<Grading>> GetAllGrading_Pagination(int pageNumber, int pageSize)
        {
            throw new NotSupportedException("dbo.GetAllGrading_Pagination");
        }

        public async Task<bool> CreateTankGradingAsync(Grading grading)
        {
            return await Sp_CreateTankGrading(grading);
        }

        public async Task<bool> CreateSectionGradingAsync(Grading grading)
        {
            return await Sp_CreateSectionGrading(grading);
        }

        public async Task<bool> UpdateTankGradingAsync(Grading grading)
        {
            return await Sp_UpdateTankGrading(grading);
        }

        public async Task<bool> UpdateSectionGradingAsync(Grading grading)
        {
            return await Sp_UpdateSectionGrading(grading);
        }

        public async Task<List<GradingTemplate>> GetGradingTemplates()
        {
            try
            {
                var result = await CosmosGradingQueries.Sp_GetGradingTemplate(ContainerFactory, null);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grading templates");
                return null!;
            }
        }

        public async Task<bool> DeleteGradingAsync(int gradingId, int tankId)
        {
            return await Sp_DeleteGrading(gradingId, tankId);
        }

        public async Task<List<GradingTemplate>> GetGradingTemplatesByVesselType(string Vesseltype)
        {
            try
            {
                var result = await CosmosGradingQueries.Sp_GetGradingTemplate(ContainerFactory, Vesseltype);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grading templates by vessel type");
                return null!;
            }
        }

        public async Task<List<GradingSection>> GetGradingSectionNamesByTemplateNameAndVesselType(string templateName, string vesseltype)
        {
            try
            {
                var result = await Sp_GetGradingSectionsByTemplateNameAndVesselType(templateName, vesseltype);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grading section names by template name and vessel type");
                return null!;
            }
        }

        public async Task<List<GradingSection>> GetGradingSections(int templateId, string vesseltype)
        {
            try
            {
                var result = await CosmosGradingQueries.Sp_GetGradingSections(ContainerFactory, templateId, vesseltype);
                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grading sections");
                return null!;
            }
        }

        #region Stored Procedure Implementations

        private async Task<int> Sp_CreateProjectSectionGrading(int projectId, string vesselType)
        {
            vesselType = vesselType?.Length > 50 ? vesselType.Substring(0, 50) : vesselType;

            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Grading);
            var q = new QueryDefinition("SELECT * FROM c WHERE (NOT IS_DEFINED(c.ProjectId) OR IS_NULL(c.ProjectId) OR c.ProjectId = 0) AND c.IsActive = true AND c.IsDeleted = false");
            var templateRows = new List<GradingDocument>();

            using (var feed = container.GetItemQueryIterator<GradingDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (vesselType != null && doc.VesselType != null && string.Equals(doc.VesselType.TrimEnd(' '), vesselType.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                        {
                            templateRows.Add(doc);
                        }
                    }
                }
            }

            int count = 0;
            foreach (var src in templateRows.OrderBy(r => r.Id))
            {
                var newId = (int)await _sequenceService.GetNextAsync(CosmosContainers.dbo_Grading);
                var newDoc = new GradingDocument
                {
                    id = newId.ToString(),
                    Id = newId,
                    LabelName = src.LabelName,
                    GradingGroupId = src.GradingGroupId,
                    GradingConditionGroupId = src.GradingConditionGroupId,
                    IsActive = src.IsActive,
                    IsDeleted = src.IsDeleted,
                    VesselType = src.VesselType,
                    ProjectId = projectId,
                    RequiredInReport = src.RequiredInReport
                };

                await container.CreateItemAsync(newDoc, new PartitionKey((double)projectId));
                count++;
            }

            return count;
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
            {
                throw new ArgumentOutOfRangeException(nameof(vesselTankGrading.CreatedDttm), "SqlDateTime overflow: must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");
            }
            if (vesselTankGrading.UpdateDttm < minSqlDate)
            {
                throw new ArgumentOutOfRangeException(nameof(vesselTankGrading.UpdateDttm), "SqlDateTime overflow: must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");
            }

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

        private async Task<List<Grading>> Sp_GetAllGrading()
        {
            var results = new List<(Grading Item, int? Order, int? SequenceNo)>();

            // Query VesselTypes
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

            // Query PartMappings (active, not deleted)
            var vpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vpList = new List<VesselTypePartMappingDocument>();
            using (var feed = vpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vpList.AddRange(response);
                }
            }

            // PART 1: Section gradings
            var gradingContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Grading);
            var gradingList = new List<GradingDocument>();
            var gradingQ = new QueryDefinition("SELECT * FROM c WHERE (NOT IS_DEFINED(c.ProjectId) OR IS_NULL(c.ProjectId) OR c.ProjectId = 0) AND IS_DEFINED(c.VesselType) AND NOT IS_NULL(c.VesselType) AND c.IsDeleted = false");
            using (var feed = gradingContainer.GetItemQueryIterator<GradingDocument>(gradingQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    gradingList.AddRange(response);
                }
            }

            var vsgContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSectionGradingMapping);
            var vsgList = new List<VesselTypeSectionGradingMappingDocument>();
            using (var feed = vsgContainer.GetItemQueryIterator<VesselTypeSectionGradingMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vsgList.AddRange(response);
                }
            }

            var vtsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vtsList = new List<VesselTypeNormalSectionMappingDocument>();
            using (var feed = vtsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtsList.AddRange(response);
                }
            }

            foreach (var g in gradingList)
            {
                // inner join vesselType v_type on grading.VesselType = v_type.VesselType
                var matchingVt = vtList.Where(vt => g.VesselType != null && vt.VesselType != null && string.Equals(g.VesselType.TrimEnd(' '), vt.VesselType.TrimEnd(' '), StringComparison.OrdinalIgnoreCase));
                foreach (var vt in matchingVt)
                {
                    // inner join Config.VesselTypeSectionGradingMapping VSG on grading.GradingGroupId = VSG.CategoryId
                    var matchingVsg = vsgList.Where(vsg => g.GradingGroupId.HasValue && vsg.CategoryId == g.GradingGroupId.Value);
                    foreach (var vsg in matchingVsg)
                    {
                        // inner join config.VesselTypeNormalSectionMapping VTS on VSG.SectionId = VTS.SectionMappingId and v_type.Id = VTS.VesselTypeId
                        var matchingVts = vtsList.Where(vts => string.Equals(vsg.SectionId, vts.SectionMappingId, StringComparison.OrdinalIgnoreCase) && vts.VesselTypeId == vt.Id);
                        foreach (var vts in matchingVts)
                        {
                            // inner join config.VesselTypePartMapping VP on VTS.VesselTypePartMappingId = VP.VesselTypePartMappingId and v_type.Id = VP.VesselTypeId
                            var matchingVp = vpList.Where(vp => vts.VesselTypePartMappingId.HasValue && vp.VesselTypePartMappingId == vts.VesselTypePartMappingId.Value && vp.VesselTypeId == vt.Id);
                            foreach (var vp in matchingVp)
                            {
                                var secGuid = Guid.TryParse(vts.SectionMappingId, out var parsedGuid) ? parsedGuid : Guid.Empty;
                                var item = new Grading
                                {
                                    SectionId = secGuid,
                                    TanktypeId = 0,
                                    TemplateId = vp.VesselTypePartMappingId,
                                    TemplateName = vp.PartName!,
                                    SectionName = vts.SectionName!,
                                    GradingId = g.Id,
                                    GradingName = g.LabelName!,
                                    VesselType = g.VesselType!,
                                    IsActive = g.IsActive ?? true,
                                    IsDeleted = g.IsDeleted ?? false,
                                    ProjectId = 0,
                                    RequiredInReport = g.RequiredInReport ?? false
                                };
                                results.Add((item, vts.Order, vp.SequenceNo));
                            }
                        }
                    }
                }
            }

            // PART 2: Tank gradings
            var vgmContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_GradingMapping);
            var vgmList = new List<VesselGradingMappingDocument>();
            var vgmQ = new QueryDefinition("SELECT * FROM c WHERE (NOT IS_DEFINED(c.ProjectId) OR IS_NULL(c.ProjectId) OR c.ProjectId = 0) AND IS_DEFINED(c.VesselType) AND NOT IS_NULL(c.VesselType) AND c.IsDeleted = false");
            using (var feed = vgmContainer.GetItemQueryIterator<VesselGradingMappingDocument>(vgmQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vgmList.AddRange(response);
                }
            }

            var vtgContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankGradingMapping);
            var vtgList = new List<VesselTypeTankGradingMappingDocument>();
            using (var feed = vtgContainer.GetItemQueryIterator<VesselTypeTankGradingMappingDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtgList.AddRange(response);
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

            foreach (var g in vgmList)
            {
                // inner join vesselType v_type on grading.VesselType = v_type.VesselType
                var matchingVt = vtList.Where(vt => g.VesselType != null && vt.VesselType != null && string.Equals(g.VesselType.TrimEnd(' '), vt.VesselType.TrimEnd(' '), StringComparison.OrdinalIgnoreCase));
                foreach (var vt in matchingVt)
                {
                    // inner join Config.VesselTypeTankGradingMapping VSG on grading.GradingGroupId = VSG.CategoryId and grading.TankTypeId = VSG.TankTypeId
                    var matchingVtg = vtgList.Where(vtg => g.GradingGroupId.HasValue && vtg.CategoryId == g.GradingGroupId.Value && g.TankTypeId.HasValue && vtg.TankTypeId == g.TankTypeId.Value);
                    foreach (var vtg in matchingVtg)
                    {
                        // inner join config.VesselTypeTankSectionMapping VTS on VSG.TankTypeId = VTS.TankTypeId and v_type.Id = VTS.VesselTypeId and VTs.IsActive=1 and VTS.IsDeleted=0
                        var matchingVtt = vttList.Where(vtt => vtg.TankTypeId.HasValue && vtt.TankTypeId == vtg.TankTypeId.Value && vtt.VesselTypeId == vt.Id);
                        foreach (var vtt in matchingVtt)
                        {
                            // inner join TankType tt on VTS.TankTypeId = tt.Id
                            var matchingTt = ttList.Where(tt => vtt.TankTypeId.HasValue && tt.Id == vtt.TankTypeId.Value);
                            foreach (var tt in matchingTt)
                            {
                                // inner join Config.VesselTypePartMapping VP on VP.VesselTypePartMappingId = VTS.VesselTypePartMappingId and VP.IsDeleted=0 and Vp.IsActive=1
                                var matchingVp = vpList.Where(vp => vtt.VesselTypePartMappingId.HasValue && vp.VesselTypePartMappingId == vtt.VesselTypePartMappingId.Value);
                                foreach (var vp in matchingVp)
                                {
                                    var secGuid = Guid.TryParse(vtt.VesselTypeTankSectionMappingId, out var parsedGuid) ? parsedGuid : Guid.Empty;
                                    var item = new Grading
                                    {
                                        SectionId = secGuid,
                                        TanktypeId = g.TankTypeId ?? 0,
                                        TemplateId = vp.VesselTypePartMappingId,
                                        TemplateName = vp.PartName!,
                                        SectionName = tt.TankType!,
                                        GradingId = g.Id,
                                        GradingName = g.GradingName!,
                                        VesselType = g.VesselType!,
                                        IsActive = g.IsActive ?? true,
                                        IsDeleted = g.IsDeleted ?? false,
                                        ProjectId = 0,
                                        RequiredInReport = g.RequiredInReport ?? false
                                    };
                                    results.Add((item, 999, vp.SequenceNo));
                                }
                            }
                        }
                    }
                }
            }

            // ORDER BY [Order], SequenceNo (NULL first)
            return results
                .OrderBy(r => r.Order.HasValue)
                .ThenBy(r => r.Order)
                .ThenBy(r => r.SequenceNo.HasValue)
                .ThenBy(r => r.SequenceNo)
                .Select(r => r.Item)
                .ToList();
        }

        private async Task<bool> Sp_CreateTankGrading(Grading grading)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_GradingMapping);

            int? gradingGroupId = 0;
            var q = new QueryDefinition("SELECT * FROM c WHERE c.TankTypeId = @tankTypeId")
                .WithParameter("@tankTypeId", grading.TanktypeId);

            var matches = new List<VesselGradingMappingDocument>();
            using (var feed = container.GetItemQueryIterator<VesselGradingMappingDocument>(q))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    matches.AddRange(response);
                }
            }

            // SELECT TOP(1) @GradingGroupId = GradingGroupId ... without ORDER BY: take lowest Id
            var topMatch = matches.OrderBy(m => m.Id).FirstOrDefault();
            if (topMatch != null)
            {
                gradingGroupId = topMatch.GradingGroupId;
            }

            var newId = (int)await _sequenceService.GetNextAsync(CosmosContainers.dbo_Vessel_GradingMapping);
            int? projectId = grading.ProjectId;

            var newDoc = new VesselGradingMappingDocument
            {
                id = newId.ToString(),
                Id = newId,
                VesselType = grading.VesselType,
                TankTypeId = grading.TanktypeId,
                GradingName = grading.GradingName?.Trim(' '),
                GradingGroupId = gradingGroupId,
                GradingConditionGroupId = 1,
                IsActive = grading.IsActive,
                CreatedDttm = null,
                UpdateDttm = null,
                IsDeleted = false,
                ProjectId = projectId,
                RequiredInReport = grading.RequiredInReport
            };

            PartitionKey pk = projectId.HasValue ? new PartitionKey((double)projectId.Value) : PartitionKey.Null;
            await container.CreateItemAsync(newDoc, pk);
            return true;
        }

        private async Task<bool> Sp_CreateSectionGrading(Grading grading)
        {
            var mapContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSectionGradingMapping);
            string secIdStr = grading.SectionId.ToString();

            var mapQ = new QueryDefinition("SELECT * FROM c WHERE c.SectionId = @secId").WithParameter("@secId", secIdStr);
            var mapDocs = new List<VesselTypeSectionGradingMappingDocument>();
            using (var feed = mapContainer.GetItemQueryIterator<VesselTypeSectionGradingMappingDocument>(mapQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    mapDocs.AddRange(response);
                }
            }

            int gradingGroupId;
            if (mapDocs.Count == 0)
            {
                // SELECT @GradingGroupId = max(GradingGroupId) +1 from Grading where IsActive = 1 and IsDeleted = 0
                var gradingContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Grading);
                var activeQ = new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false");
                var activeDocs = new List<GradingDocument>();
                using (var feed = gradingContainer.GetItemQueryIterator<GradingDocument>(activeQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        activeDocs.AddRange(response);
                    }
                }

                var groupIds = activeDocs.Where(d => d.GradingGroupId.HasValue).Select(d => d.GradingGroupId.Value).ToList();
                if (groupIds.Count == 0)
                {
                    throw new InvalidOperationException("Cannot compute max(GradingGroupId) from active grading documents; CategoryId cannot be null.");
                }

                gradingGroupId = groupIds.Max() + 1;
                var mapId = $"{secIdStr}|{gradingGroupId}";

                var newMap = new VesselTypeSectionGradingMappingDocument
                {
                    id = mapId,
                    SectionId = secIdStr,
                    CategoryId = gradingGroupId,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedOn = DateTime.UtcNow.Date,
                    IsDeleted = false,
                    ModifiedBy = null,
                    ModifiedOn = null
                };

                await mapContainer.CreateItemAsync(newMap, new PartitionKey(mapId));
            }
            else
            {
                // select @GradingGroupId = CategoryId from config.VesselTypeSectionGradingMapping where SectionId = @SectionId (last row wins)
                gradingGroupId = mapDocs.Last().CategoryId;
            }

            // GradingConditionGroupId = IIF(@LabelName = 'Last Visit Date for Under Water Body Survey', NULL, 1)
            // Compare ignoring case and trailing spaces on the untrimmed parameter
            int? conditionGroupId = string.Equals(grading.GradingName?.TrimEnd(' '), "Last Visit Date for Under Water Body Survey", StringComparison.OrdinalIgnoreCase)
                ? (int?)null
                : 1;

            var newId = (int)await _sequenceService.GetNextAsync(CosmosContainers.dbo_Grading);
            int? projectId = grading.ProjectId;

            var newDoc = new GradingDocument
            {
                id = newId.ToString(),
                Id = newId,
                LabelName = grading.GradingName?.Trim(' '),
                GradingGroupId = gradingGroupId,
                GradingConditionGroupId = conditionGroupId,
                IsActive = grading.IsActive,
                IsDeleted = grading.IsDeleted,
                VesselType = grading.VesselType,
                ProjectId = projectId,
                RequiredInReport = grading.RequiredInReport
            };

            var finalGradingContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Grading);
            PartitionKey pk = projectId.HasValue ? new PartitionKey((double)projectId.Value) : PartitionKey.Null;
            await finalGradingContainer.CreateItemAsync(newDoc, pk);
            return true;
        }

        private async Task<bool> Sp_UpdateTankGrading(Grading grading)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_GradingMapping);

            // Find existing row across all partitions by Id
            var findQ = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", grading.GradingId);
            var existingDocs = new List<VesselGradingMappingDocument>();
            using (var feed = container.GetItemQueryIterator<VesselGradingMappingDocument>(findQ))
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

            // select TOP(1) @GradingGroupId = GradingGroupId from Vessel_GradingMapping Where TankTypeId = @TankTypeId
            int? gradingGroupId = 0;
            var tankTypeQ = new QueryDefinition("SELECT * FROM c WHERE c.TankTypeId = @tankTypeId").WithParameter("@tankTypeId", grading.TanktypeId);
            var tankTypeDocs = new List<VesselGradingMappingDocument>();
            using (var feed = container.GetItemQueryIterator<VesselGradingMappingDocument>(tankTypeQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    tankTypeDocs.AddRange(response);
                }
            }
            var topMatch = tankTypeDocs.OrderBy(m => m.Id).FirstOrDefault();
            if (topMatch != null)
            {
                gradingGroupId = topMatch.GradingGroupId;
            }

            int? oldProjectId = existing.ProjectId;
            int? newProjectId = grading.ProjectId;

            existing.VesselType = grading.VesselType;
            existing.TankTypeId = grading.TanktypeId;
            existing.GradingName = grading.GradingName; // NOT trimmed
            existing.GradingGroupId = gradingGroupId;
            existing.IsActive = grading.IsActive;
            existing.UpdateDttm = null; // SQL bug kept: @UpdateDttm not passed by C#, set to NULL
            existing.IsDeleted = false;
            existing.RequiredInReport = grading.RequiredInReport;

            if (oldProjectId != newProjectId)
            {
                // Partition move: create in new partition first, then delete old
                existing.ProjectId = newProjectId;
                PartitionKey newPk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                PartitionKey oldPk = oldProjectId.HasValue ? new PartitionKey((double)oldProjectId.Value) : PartitionKey.Null;

                await container.CreateItemAsync(existing, newPk);
                try
                {
                    await container.DeleteItemAsync<VesselGradingMappingDocument>(existing.id, oldPk);
                }
                catch
                {
                    try
                    {
                        await container.DeleteItemAsync<VesselGradingMappingDocument>(existing.id, newPk);
                    }
                    catch
                    {
                        // best-effort rollback
                    }
                    throw;
                }
            }
            else
            {
                PartitionKey pk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                await container.UpsertItemAsync(existing, pk);
            }

            return true;
        }

        private async Task<bool> Sp_UpdateSectionGrading(Grading grading)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Grading);

            var findQ = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", grading.GradingId);
            var existingDocs = new List<GradingDocument>();
            using (var feed = container.GetItemQueryIterator<GradingDocument>(findQ))
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

            int? oldProjectId = existing.ProjectId;
            int? newProjectId = grading.ProjectId;

            existing.LabelName = grading.GradingName; // NOT trimmed
            existing.GradingConditionGroupId = 1; // Always 1
            existing.IsActive = grading.IsActive;
            existing.IsDeleted = grading.IsDeleted;
            existing.VesselType = grading.VesselType;
            existing.RequiredInReport = grading.RequiredInReport;

            if (oldProjectId != newProjectId)
            {
                // Partition move: create in new partition first, then delete old
                existing.ProjectId = newProjectId;
                PartitionKey newPk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                PartitionKey oldPk = oldProjectId.HasValue ? new PartitionKey((double)oldProjectId.Value) : PartitionKey.Null;

                await container.CreateItemAsync(existing, newPk);
                try
                {
                    await container.DeleteItemAsync<GradingDocument>(existing.id, oldPk);
                }
                catch
                {
                    try
                    {
                        await container.DeleteItemAsync<GradingDocument>(existing.id, newPk);
                    }
                    catch
                    {
                        // best-effort rollback
                    }
                    throw;
                }
            }
            else
            {
                PartitionKey pk = newProjectId.HasValue ? new PartitionKey((double)newProjectId.Value) : PartitionKey.Null;
                await container.UpsertItemAsync(existing, pk);
            }

            return true;
        }

        private async Task<bool> Sp_DeleteGrading(int gradingId, int tankId)
        {
            if (tankId == 0)
            {
                var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Grading);
                var findQ = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", gradingId);
                var existingDocs = new List<GradingDocument>();
                using (var feed = container.GetItemQueryIterator<GradingDocument>(findQ))
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

                existing.IsDeleted = true;
                PartitionKey pk = existing.ProjectId.HasValue ? new PartitionKey((double)existing.ProjectId.Value) : PartitionKey.Null;
                await container.UpsertItemAsync(existing, pk);
                return true;
            }
            else
            {
                var container = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_GradingMapping);
                var findQ = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id AND c.TankTypeId = @tankId")
                    .WithParameter("@id", gradingId)
                    .WithParameter("@tankId", tankId);

                var existingDocs = new List<VesselGradingMappingDocument>();
                using (var feed = container.GetItemQueryIterator<VesselGradingMappingDocument>(findQ))
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

                existing.IsDeleted = true;
                PartitionKey pk = existing.ProjectId.HasValue ? new PartitionKey((double)existing.ProjectId.Value) : PartitionKey.Null;
                await container.UpsertItemAsync(existing, pk);
                return true;
            }
        }

        private async Task<List<GradingSection>> Sp_GetGradingSectionsByTemplateNameAndVesselType(string templateName, string vesselType)
        {
            if (templateName == null)
            {
                return new List<GradingSection>();
            }

            templateName = templateName.Length > 200 ? templateName.Substring(0, 200) : templateName;
            vesselType = vesselType?.Length > 200 ? vesselType.Substring(0, 200) : vesselType;

            // SELECT @VesselTypeId = Id from VesselType where VesselType = @VesselType (last row wins, ordered by Id)
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

            int vesselTypeId = 0;
            if (vesselType != null)
            {
                var matchingVt = vtList.Where(v => string.Equals(v.VesselType?.TrimEnd(' '), vesselType.TrimEnd(' '), StringComparison.OrdinalIgnoreCase));
                vesselTypeId = CosmosLastRowHelper.GetLastRowValue(matchingVt, v => v.Id, v => v.Id, 0);
            }

            // SELECT @TemplateId = PM.VesselTypePartMappingId FROM [Config].[VesselTypePartMapping] PM WHERE PM.PartName = @TemplateName AND PM.VesselTypeId = @VesselTypeId (last row wins, ordered by VesselTypePartMappingId)
            int templateId = 0;
            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vtpQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid")
                .WithParameter("@vtid", vesselTypeId);

            var vtpMatches = new List<VesselTypePartMappingDocument>();
            using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(vtpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtpMatches.AddRange(response);
                }
            }

            var matchingVtp = vtpMatches.Where(doc => string.Equals(doc.PartName?.TrimEnd(' '), templateName.TrimEnd(' '), StringComparison.OrdinalIgnoreCase));
            templateId = CosmosLastRowHelper.GetLastRowValue(matchingVtp, doc => doc.VesselTypePartMappingId, doc => doc.VesselTypePartMappingId, 0);

            if (templateId == 0 || vesselType == null)
            {
                return new List<GradingSection>();
            }

            var results = new List<(GradingSection Item, int? Order)>();

            // 1. Normal Sections:
            // WHERE VesselTypeId = @VesselTypeId and VesselTypePartMappingId = @TemplateId and NSM.IsDeleted = 0 and IsActive = 1
            var nsmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var nsmQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.VesselTypePartMappingId = @templateId AND c.IsDeleted = false AND c.IsActive = true")
                .WithParameter("@vtid", vesselTypeId)
                .WithParameter("@templateId", templateId);

            using (var feed = nsmContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(nsmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var nsm in response)
                    {
                        var secGuid = Guid.TryParse(nsm.SectionMappingId, out var g) ? g : Guid.Empty;
                        results.Add((new GradingSection
                        {
                            SectionId = secGuid,
                            TanktypeId = 0,
                            TemplateId = nsm.VesselTypePartMappingId ?? 0,
                            SectionName = nsm.SectionName!
                        }, nsm.Order));
                    }
                }
            }

            // 2. Tank Sections:
            // FROM [Config].[VesselTypeTankSectionMapping] TSM INNER JOIN TankType TT ON TT.Id = TSM.TankTypeId
            // WHERE VesselTypeId = @VesselTypeId and VesselTypePartMappingId = @TemplateId and TSM.IsDeleted = 0 and IsActive = 1
            var tsmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
            var tsmQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.VesselTypePartMappingId = @templateId AND c.IsDeleted = false AND c.IsActive = true")
                .WithParameter("@vtid", vesselTypeId)
                .WithParameter("@templateId", templateId);

            var tsmList = new List<VesselTypeTankSectionMappingDocument>();
            using (var feed = tsmContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(tsmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    tsmList.AddRange(response);
                }
            }

            if (tsmList.Count > 0)
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

                var ttMap = ttList.ToDictionary(t => t.Id, t => t.TankType);
                foreach (var tsm in tsmList)
                {
                    if (tsm.TankTypeId.HasValue && ttMap.TryGetValue(tsm.TankTypeId.Value, out var tankTypeName))
                    {
                        results.Add((new GradingSection
                        {
                            SectionId = Guid.Empty,
                            TanktypeId = tsm.TankTypeId.Value,
                            TemplateId = tsm.VesselTypePartMappingId ?? 0,
                            SectionName = tankTypeName!
                        }, 999));
                    }
                }
            }

            return results
                .Where(r => r.Item.TemplateId == templateId)
                .OrderBy(r => r.Order.HasValue)
                .ThenBy(r => r.Order)
                .Select(r => r.Item)
                .ToList();
        }

        #endregion
    }
}
