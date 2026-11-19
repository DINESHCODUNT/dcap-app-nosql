using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.ImageDescription;
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
    public class CosmosDescriptionRepository : CosmosRepositoryBase<ImageDescriptionDocument>, IDescriptionRepository
    {
        private static readonly DateTime SqlMinDate = new DateTime(1753, 1, 1);
        private readonly ISequenceService _sequenceService;

        public CosmosDescriptionRepository(
            ICosmosContainerFactory containerFactory,
            ISequenceService sequenceService,
            ILogger<CosmosDescriptionRepository> logger)
            : base(containerFactory, CosmosContainers.dbo_ImageDescriptions, logger)
        {
            _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
        }

        public void Commit() { }

        public async Task<bool> CreateProjectImageDescription(int projectId, string vesseltype)
        {
            try
            {
                await Sp_CreateProjectSectionImageDescriptions(projectId, vesseltype);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<ImageDescriptions>> GetImageDescriptionsByProjectId(int projectId)
        {
            try
            {
                var result = await Sp_GetImageDescriptionsByProjectId(projectId);
                return result.ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> CreateImageDescription(ImageDescriptions description)
        {
            return await Sp_CreateImageDescription(description);
        }

        public async Task<bool> UpdateImageDescription(ImageDescriptions model)
        {
            try
            {
                return await Sp_UpdateImageDescription(model);
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<ImageDescriptions>> CheckImageDescriptionExistsOrNot(string vesselType, string sectionName, string templateName, string description)
        {
            return await Sp_CheckImageDescriptionExistsOrNot(vesselType, sectionName, templateName, description?.Trim());
        }

        public async Task<PagedResult<ImageDescriptions>> GetAllDescription(int pageNumber, int pageSize)
        {
            try
            {
                return await Sp_GetAllDescriptions(pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<GradingSection>> GetDescriptionSectionNamesByTemplateNameAndVesselType(string templateName, string vesselType)
        {
            var result = await Sp_GetDescriptionSectionsByTemplateNameAndVesselType(templateName, vesselType);
            return result.ToList();
        }

        public async Task<List<GradingTemplate>> GetGradingTemplates()
        {
            var result = await CosmosGradingQueries.Sp_GetGradingTemplate(ContainerFactory, null);
            return result.ToList();
        }

        public async Task<List<GradingSection>> GetGradingSections(int templateId, string vesselType)
        {
            var result = await CosmosGradingQueries.Sp_GetGradingSections(ContainerFactory, templateId, vesselType);
            return result.ToList();
        }

        public async Task<ImageDescriptions> GetImageDescriptionById(int id)
        {
            var result = await Sp_GetImageDescriptionById(id);
            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<GradingSection>> GetSectionNamesByTemplateAndVesselAsync(string templateName, string vesselType)
        {
            var result = await Sp_GetDescriptionSectionsByTemplateNameAndVesselType(templateName, vesselType);
            return result;
        }

        #region Stored Procedure Helpers

        private async Task Sp_CreateProjectSectionImageDescriptions(int projectId, string vesselType)
        {
            var cutVesselType = vesselType?.Length > 50 ? vesselType.Substring(0, 50) : vesselType;
            // WHERE VesselType=@VesselType and IsActive=1 and IsDeleted=0 and (ProjectId is null or ProjectId=0)
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false AND (IS_NULL(c.ProjectId) OR c.ProjectId = 0 OR NOT IS_DEFINED(c.ProjectId))");

            var templates = await QueryAsync(q);
            var matchedTemplates = templates
                .Where(t => string.Equals(t.VesselType?.TrimEnd(), cutVesselType?.TrimEnd(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Id)
                .ToList();

            var container = ContainerFactory.GetContainer(CosmosContainers.dbo_ImageDescriptions);
            foreach (var t in matchedTemplates)
            {
                var newId = await _sequenceService.GetNextAsync(CosmosContainers.dbo_ImageDescriptions);
                var newDoc = new ImageDescriptionDocument
                {
                    id = newId.ToString(),
                    Id = newId,
                    Description = t.Description,
                    CategoryId = t.CategoryId,
                    ProjectId = projectId,
                    IsActive = t.IsActive,
                    IsDeleted = t.IsDeleted,
                    CreatedDttm = DateTime.UtcNow,
                    UpdatedDttm = null,
                    VesselType = t.VesselType
                };
                await container.CreateItemAsync(newDoc, new PartitionKey((double)projectId));
            }
        }

        private async Task<List<ImageDescriptions>> Sp_GetImageDescriptionsByProjectId(int projectId)
        {
            // where ProjectId=@projectId and IsActive=1
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @pid AND c.IsActive = true")
                .WithParameter("@pid", projectId);

            var descriptions = await QueryAsync(q, new PartitionKey((double)projectId));

            // Section mappings lookup
            var secMapContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Section_ImageDescriptionMapping);
            var secMapQ = new QueryDefinition("SELECT * FROM c");
            var secMappings = new List<SectionImageDescriptionMappingDocument>();
            using (var feed = secMapContainer.GetItemQueryIterator<SectionImageDescriptionMappingDocument>(secMapQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    secMappings.AddRange(response);
                }
            }

            var rtsContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_ReportTemplateSection);
            var rtsQ = new QueryDefinition("SELECT * FROM c");
            var rtsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var feed = rtsContainer.GetItemQueryIterator<ReportTemplateSectionDocument>(rtsQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (!string.IsNullOrEmpty(doc.Id) && doc.SectionName != null)
                        {
                            rtsMap[doc.Id] = doc.SectionName;
                        }
                    }
                }
            }

            // Tank mappings lookup
            var tankMapContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Tank_ImageDescriptionMapping);
            var tankMapQ = new QueryDefinition("SELECT * FROM c");
            var tankMappings = new List<TankImageDescriptionMappingDocument>();
            using (var feed = tankMapContainer.GetItemQueryIterator<TankImageDescriptionMappingDocument>(tankMapQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    tankMappings.AddRange(response);
                }
            }

            var ttContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_TankType);
            var ttQ = new QueryDefinition("SELECT * FROM c");
            var tankTypeMap = new Dictionary<int, string>();
            using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(ttQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (doc.TankType != null)
                        {
                            tankTypeMap[doc.Id] = doc.TankType;
                        }
                    }
                }
            }

            var results = new List<ImageDescriptions>();

            // SectionGrading branch
            foreach (var descr in descriptions)
            {
                var matchingSecMaps = secMappings.Where(sm => sm.CategoryId == descr.CategoryId).ToList();
                foreach (var secimg in matchingSecMaps)
                {
                    if (!string.IsNullOrEmpty(secimg.SectionId) && rtsMap.TryGetValue(secimg.SectionId, out var sectionName))
                    {
                        var secGuid = Guid.TryParse(secimg.SectionId, out var g) ? g : Guid.Empty;
                        results.Add(new ImageDescriptions
                        {
                            Id = descr.Id,
                            Description = descr.Description,
                            CategoryId = (int)(descr.CategoryId ?? 0),
                            ProjectId = descr.ProjectId ?? 0,
                            IsActive = descr.IsActive ?? true,
                            IsDeleted = descr.IsDeleted ?? false,
                            SectionId = secGuid,
                            SectionName = sectionName,
                            CreatedDttm = descr.CreatedDttm ?? DateTime.MinValue,
                            UpdatedDttm = descr.UpdatedDttm ?? DateTime.MinValue,
                            TankTypeId = null
                        });
                    }
                }
            }

            // TankGrading branch
            foreach (var descr in descriptions)
            {
                var matchingTankMaps = tankMappings.Where(tm => tm.CategoryId == descr.CategoryId).ToList();
                foreach (var tank in matchingTankMaps)
                {
                    if (tank.TankTypeId.HasValue && tankTypeMap.TryGetValue(tank.TankTypeId.Value, out var tankTypeName))
                    {
                        results.Add(new ImageDescriptions
                        {
                            Id = descr.Id,
                            Description = descr.Description,
                            CategoryId = (int)(descr.CategoryId ?? 0),
                            ProjectId = descr.ProjectId ?? 0,
                            IsActive = descr.IsActive ?? true,
                            IsDeleted = descr.IsDeleted ?? false,
                            SectionId = null,
                            SectionName = tankTypeName,
                            CreatedDttm = descr.CreatedDttm ?? DateTime.MinValue,
                            UpdatedDttm = descr.UpdatedDttm ?? DateTime.MinValue,
                            TankTypeId = tank.TankTypeId.Value
                        });
                    }
                }
            }

            return results.OrderBy(r => r.Id).ToList();
        }

        private async Task<bool> Sp_CreateImageDescription(ImageDescriptions description)
        {
            if (description.CreatedDttm < SqlMinDate || description.UpdatedDttm < SqlMinDate)
            {
                throw new ArgumentOutOfRangeException(description.CreatedDttm < SqlMinDate ? nameof(description.CreatedDttm) : nameof(description.UpdatedDttm), "SqlDateTime overflow");
            }

            string? sectionIdStr = description.SectionId.HasValue ? description.SectionId.Value.ToString() : null;
            long? categoryId = description.CategoryId;

            // 1. Check Normal Section
            string? checkNormalSection = null;
            if (sectionIdStr != null)
            {
                var normContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
                var normQ = new QueryDefinition(
                    "SELECT * FROM c WHERE c.SectionMappingId = @secId AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@secId", sectionIdStr);

                var normList = new List<VesselTypeNormalSectionMappingDocument>();
                using (var feed = normContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(normQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        normList.AddRange(response);
                    }
                }
                checkNormalSection = CosmosLastRowHelper.GetLastRowValue(normList, d => d.id, d => d.SectionMappingId);
            }

            // 2. Check Sub Section
            string? checkSubSection = null;
            if (sectionIdStr != null)
            {
                var subContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSection);
                var subQ = new QueryDefinition(
                    "SELECT * FROM c WHERE c.Id = @secId AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@secId", sectionIdStr);

                var subList = new List<VesselTypeSubSectionDocument>();
                using (var feed = subContainer.GetItemQueryIterator<VesselTypeSubSectionDocument>(subQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        subList.AddRange(response);
                    }
                }
                checkSubSection = CosmosLastRowHelper.GetLastRowValue(subList, d => d.id, d => d.Id);
            }

            if (checkNormalSection != null && sectionIdStr != null)
            {
                var mappingContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSectionDescriptionMapping);
                var mapQ = new QueryDefinition(
                    "SELECT * FROM c WHERE c.SectionId = @secId")
                    .WithParameter("@secId", sectionIdStr);

                var mapList = new List<VesselTypeSectionDescriptionMappingDocument>();
                using (var feed = mappingContainer.GetItemQueryIterator<VesselTypeSectionDescriptionMappingDocument>(mapQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        mapList.AddRange(response);
                    }
                }

                if (mapList.Count == 0)
                {
                    // Find max(CategoryId) + 1 from ImageDescriptions where IsActive = 1 and IsDeleted = 0
                    var maxCatQ = new QueryDefinition(
                        "SELECT VALUE MAX(c.CategoryId) FROM c WHERE c.IsActive = true AND c.IsDeleted = false");
                    long? maxVal = null;
                    using (var feed = Container.GetItemQueryIterator<long?>(maxCatQ))
                    {
                        if (feed.HasMoreResults)
                        {
                            var response = await feed.ReadNextAsync();
                            maxVal = response.FirstOrDefault();
                        }
                    }

                    if (!maxVal.HasValue)
                    {
                        throw new InvalidOperationException("Cannot determine max CategoryId from active ImageDescriptions");
                    }

                    long newCatId = maxVal.Value + 1;
                    var mapId = $"{sectionIdStr}|{newCatId}";
                    var newMap = new VesselTypeSectionDescriptionMappingDocument
                    {
                        id = mapId,
                        SectionId = sectionIdStr,
                        CategoryId = (int)newCatId,
                        IsActive = true,
                        CreatedBy = "System",
                        CreatedOn = DateTime.UtcNow.Date,
                        IsDeleted = false
                    };
                    await mappingContainer.CreateItemAsync(newMap, new PartitionKey(mapId));
                    mapList.Add(newMap);
                }

                categoryId = CosmosLastRowHelper.GetLastRowValue(mapList, d => d.id, d => (long?)d.CategoryId, categoryId);
            }

            if (checkSubSection != null && sectionIdStr != null)
            {
                var subMapContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSectionDescriptionMapping);
                var subMapQ = new QueryDefinition(
                    "SELECT * FROM c WHERE c.SubSectionId = @secId")
                    .WithParameter("@secId", sectionIdStr);

                var subMapList = new List<VesselTypeSubSectionDescriptionMappingDocument>();
                using (var feed = subMapContainer.GetItemQueryIterator<VesselTypeSubSectionDescriptionMappingDocument>(subMapQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        subMapList.AddRange(response);
                    }
                }
                categoryId = CosmosLastRowHelper.GetLastRowValue(subMapList, d => d.id, d => (long?)d.CategoryId, categoryId);
            }

            if (description.TankTypeId.HasValue && description.TankTypeId.Value != 0)
            {
                var tankMapContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Tank_ImageDescriptionMapping);
                var tankMapQ = new QueryDefinition(
                    "SELECT * FROM c WHERE c.TankTypeId = @ttId")
                    .WithParameter("@ttId", description.TankTypeId.Value);

                var tankMapList = new List<TankImageDescriptionMappingDocument>();
                using (var feed = tankMapContainer.GetItemQueryIterator<TankImageDescriptionMappingDocument>(tankMapQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        tankMapList.AddRange(response);
                    }
                }
                categoryId = CosmosLastRowHelper.GetLastRowValue(tankMapList, d => d.id, d => (long?)d.CategoryId, categoryId);
            }

            var newId = await _sequenceService.GetNextAsync(CosmosContainers.dbo_ImageDescriptions);
            var newDoc = new ImageDescriptionDocument
            {
                id = newId.ToString(),
                Id = newId,
                Description = description.Description?.Trim(' '),
                CategoryId = categoryId,
                ProjectId = description.ProjectId,
                VesselType = description.VesselType,
                IsActive = description.IsActive,
                IsDeleted = description.IsDeleted,
                CreatedDttm = description.CreatedDttm,
                UpdatedDttm = description.UpdatedDttm
            };

            PartitionKey pk = new PartitionKey((double)description.ProjectId);
            await CreateAsync(newDoc, pk);
            return true;
        }

        private async Task<bool> Sp_UpdateImageDescription(ImageDescriptions model)
        {
            long? categoryId = model.CategoryId;
            if (categoryId == 0)
            {
                if (model.SectionId.HasValue)
                {
                    var secStr = model.SectionId.Value.ToString();
                    var secMapContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Section_ImageDescriptionMapping);
                    var secMapQ = new QueryDefinition("SELECT * FROM c WHERE c.SectionId = @secId").WithParameter("@secId", secStr);
                    var secMapList = new List<SectionImageDescriptionMappingDocument>();
                    using (var feed = secMapContainer.GetItemQueryIterator<SectionImageDescriptionMappingDocument>(secMapQ))
                    {
                        while (feed.HasMoreResults)
                        {
                            var response = await feed.ReadNextAsync();
                            secMapList.AddRange(response);
                        }
                    }
                    categoryId = CosmosLastRowHelper.GetLastRowValue(secMapList, d => d.id, d => d.CategoryId, categoryId);
                }

                if (model.TankTypeId.HasValue && model.TankTypeId.Value != 0)
                {
                    var tankMapContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Tank_ImageDescriptionMapping);
                    var tankMapQ = new QueryDefinition("SELECT * FROM c WHERE c.TankTypeId = @ttId").WithParameter("@ttId", model.TankTypeId.Value);
                    var tankMapList = new List<TankImageDescriptionMappingDocument>();
                    using (var feed = tankMapContainer.GetItemQueryIterator<TankImageDescriptionMappingDocument>(tankMapQ))
                    {
                        while (feed.HasMoreResults)
                        {
                            var response = await feed.ReadNextAsync();
                            tankMapList.AddRange(response);
                        }
                    }
                    categoryId = CosmosLastRowHelper.GetLastRowValue(tankMapList, d => d.id, d => (long?)d.CategoryId, categoryId);
                }
            }

            // Find existing document by Id (last row if multiple)
            ImageDescriptionDocument? existing = null;
            if (model.ProjectId != 0)
            {
                var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", model.Id);
                var list = await QueryAsync(q, new PartitionKey((double)model.ProjectId));
                existing = list.OrderBy(d => d.Id).LastOrDefault();
            }

            if (existing == null)
            {
                var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", model.Id);
                var list = await QueryAsync(q);
                existing = list.OrderBy(d => d.Id).LastOrDefault();
            }

            if (existing == null)
            {
                return true;
            }

            int? oldProjectId = existing.ProjectId;
            existing.Description = model.Description; // NOT trimmed
            existing.CategoryId = categoryId;
            existing.ProjectId = model.ProjectId;
            existing.VesselType = model.VesselType;
            existing.IsActive = model.IsActive;
            existing.IsDeleted = model.IsDeleted;
            existing.CreatedDttm = DateTime.Now;
            existing.UpdatedDttm = DateTime.Now;

            if (oldProjectId != model.ProjectId)
            {
                PartitionKey newPk = new PartitionKey((double)model.ProjectId);
                PartitionKey oldPk = oldProjectId.HasValue ? new PartitionKey((double)oldProjectId.Value) : PartitionKey.Null;
                await CreateAsync(existing, newPk);
                try
                {
                    await DeleteAsync(existing.id, oldPk);
                }
                catch
                {
                    await DeleteAsync(existing.id, newPk);
                    throw;
                }
            }
            else
            {
                PartitionKey pk = new PartitionKey((double)model.ProjectId);
                await UpsertAsync(existing, pk);
            }

            return true;
        }

        private async Task<List<ImageDescriptions>> Sp_CheckImageDescriptionExistsOrNot(string? vesselType, string? sectionName, string? partName, string? description)
        {
            vesselType = vesselType?.Length > 250 ? vesselType.Substring(0, 250) : vesselType;
            sectionName = sectionName?.Length > 250 ? sectionName.Substring(0, 250) : sectionName;
            partName = partName?.Length > 50 ? partName.Substring(0, 50) : partName;
            description = description?.Length > 500 ? description.Substring(0, 500) : description;

            // 1. VesselTypeId: select @VesselTypeId=Id from VesselType Where VesselType=@VesselType
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
            var matchingVt = vtList.Where(v => string.Equals(v.VesselType?.TrimEnd(), vesselType?.TrimEnd(), StringComparison.OrdinalIgnoreCase));
            int? vesselTypeId = CosmosLastRowHelper.GetLastRowValue(matchingVt, v => v.Id, v => (int?)v.Id, null);

            // 2. PartId: select @PartId=VesselTypePartMappingId from config.VesselTypePartMapping where IsDeleted=0 and IsActive=1 and VesselTypeId=@VesselTypeId and PartName=@PartName
            int? partId = null;
            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vtpList = new List<VesselTypePartMappingDocument>();
            if (vesselTypeId.HasValue)
            {
                var vtpQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@vtid", vesselTypeId.Value);
                using var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(vtpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId.Value) });
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtpList.AddRange(response);
                }
                var matchingVtp = vtpList.Where(p => string.Equals(p.PartName?.TrimEnd(), partName?.TrimEnd(), StringComparison.OrdinalIgnoreCase));
                partId = CosmosLastRowHelper.GetLastRowValue(matchingVtp, p => p.VesselTypePartMappingId, p => (int?)p.VesselTypePartMappingId, null);
            }

            // 3. SectionId:
            // select @SectionId=SectionMappingId from config.VesselTypeNormalSectionMapping vns
            // inner join config.VesselTypePartMapping vtp on vtp.VesselTypePartMappingId = vns.VesselTypePartMappingId and vns.VesselTypeId = vtp.VesselTypeId
            // where vns.IsDeleted=0 and vns.IsActive=1 and SectionName =@SectionName and vns.VesselTypePartMappingId= @PartId
            // and vtp.IsActive=1 and vtp.IsDeleted=0
            string? sectionIdStr = null;
            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vnsList = new List<VesselTypeNormalSectionMappingDocument>();
            if (vesselTypeId.HasValue && partId.HasValue)
            {
                var vnsQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.VesselTypePartMappingId = @partId AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@vtid", vesselTypeId.Value)
                    .WithParameter("@partId", partId.Value);
                using var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(vnsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId.Value) });
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vnsList.AddRange(response);
                }
                var matchingVns = vnsList.Where(s => string.Equals(s.SectionName?.TrimEnd(), sectionName?.TrimEnd(), StringComparison.OrdinalIgnoreCase));
                sectionIdStr = CosmosLastRowHelper.GetLastRowValue(matchingVns, s => s.id, s => s.SectionMappingId, null);
            }

            // 4. SubSectionId:
            // select @SubSectionId=Id from config.VesselTypeSubSection vss
            // inner join config.VesselTypeNormalSectionMapping as VNS on VNS.SectionMappingId = vss.VesselTypeSectionId and vns.HasSubSection=1
            // inner join config.VesselTypePartMapping vtp on vtp.VesselTypePartMappingId = vns.VesselTypePartMappingId and vns.VesselTypeId = vtp.VesselTypeId
            // where vss.IsDeleted=0 and vss.IsActive=1 and vss.SectionName =@SectionName
            // and vns.IsDeleted=0 and vns.IsActive=1 and vns.VesselTypePartMappingId= @PartId
            // and vtp.IsActive=1 and vtp.IsDeleted=0
            string? subSectionIdStr = null;
            if (vesselTypeId.HasValue && partId.HasValue)
            {
                var vssContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSection);
                var vssList = new List<VesselTypeSubSectionDocument>();
                using (var feed = vssContainer.GetItemQueryIterator<VesselTypeSubSectionDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false")))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vssList.AddRange(response);
                    }
                }

                var vnsWithSub = vnsList.Where(s => s.HasSubSection == true && !string.IsNullOrEmpty(s.SectionMappingId)).ToDictionary(s => s.SectionMappingId!, StringComparer.OrdinalIgnoreCase);
                var matchingVss = vssList.Where(sub => !string.IsNullOrEmpty(sub.VesselTypeSectionId) && vnsWithSub.ContainsKey(sub.VesselTypeSectionId) && string.Equals(sub.SectionName?.TrimEnd(), sectionName?.TrimEnd(), StringComparison.OrdinalIgnoreCase));
                subSectionIdStr = CosmosLastRowHelper.GetLastRowValue(matchingVss, sub => sub.id, sub => sub.Id, null);
            }

            // 5. CategoryId:
            int? categoryId = null;
            if (sectionIdStr != null)
            {
                var vsdContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSectionDescriptionMapping);
                var vsdQ = new QueryDefinition("SELECT * FROM c WHERE c.SectionId = @secId").WithParameter("@secId", sectionIdStr);
                var vsdList = new List<VesselTypeSectionDescriptionMappingDocument>();
                using (var feed = vsdContainer.GetItemQueryIterator<VesselTypeSectionDescriptionMappingDocument>(vsdQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vsdList.AddRange(response);
                    }
                }
                categoryId = CosmosLastRowHelper.GetLastRowValue(vsdList, m => m.id, m => m.CategoryId, null);
            }
            else if (subSectionIdStr != null)
            {
                var vssdContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSectionDescriptionMapping);
                var vssdQ = new QueryDefinition("SELECT * FROM c WHERE c.SubSectionId = @subSecId").WithParameter("@subSecId", subSectionIdStr);
                var vssdList = new List<VesselTypeSubSectionDescriptionMappingDocument>();
                using (var feed = vssdContainer.GetItemQueryIterator<VesselTypeSubSectionDescriptionMappingDocument>(vssdQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vssdList.AddRange(response);
                    }
                }
                categoryId = CosmosLastRowHelper.GetLastRowValue(vssdList, m => m.id, m => m.CategoryId, null);
            }
            else
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
                var matchingTt = ttList.Where(t => string.Equals(t.TankType?.TrimEnd(), sectionName?.TrimEnd(), StringComparison.OrdinalIgnoreCase));
                int? tankTypeId = CosmosLastRowHelper.GetLastRowValue(matchingTt, t => t.Id, t => (int?)t.Id, null);

                if (tankTypeId.HasValue)
                {
                    var tidContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Tank_ImageDescriptionMapping);
                    var tidQ = new QueryDefinition("SELECT * FROM c WHERE c.TankTypeId = @ttId").WithParameter("@ttId", tankTypeId.Value);
                    var tidList = new List<TankImageDescriptionMappingDocument>();
                    using (var feed = tidContainer.GetItemQueryIterator<TankImageDescriptionMappingDocument>(tidQ))
                    {
                        while (feed.HasMoreResults)
                        {
                            var response = await feed.ReadNextAsync();
                            tidList.AddRange(response);
                        }
                    }
                    categoryId = CosmosLastRowHelper.GetLastRowValue(tidList, m => m.id, m => m.CategoryId);
                }
            }

            // In SQL: if @CategoryId is null, WHERE CategoryId = @CategoryId produces 0 rows
            if (!categoryId.HasValue)
            {
                return new List<ImageDescriptions>();
            }

            // 6. Query ImageDescriptions:
            // WHERE VesselType= @VesselType and (ProjectId =0) and CategoryId=@CategoryId and IsDeleted=0 and Description= Trim(@Description)
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = 0 AND c.CategoryId = @catId AND c.IsDeleted = false")
                .WithParameter("@catId", categoryId.Value);

            var descriptions = await QueryAsync(q, new PartitionKey(0.0));
            var trimmedDesc = description?.Trim();
            var targetSecGuid = Guid.TryParse(sectionIdStr, out var sg) ? sg : (Guid?)null;

            var results = descriptions
                .Where(d => string.Equals(d.VesselType?.TrimEnd(), vesselType?.TrimEnd(), StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(d.Description?.TrimEnd(), trimmedDesc, StringComparison.OrdinalIgnoreCase))
                .Select(d => new ImageDescriptions
                {
                    Id = d.Id,
                    VesselType = vesselType,
                    SectionId = targetSecGuid,
                    CategoryId = categoryId.Value,
                    Description = d.Description,
                    TemplateId = 0 // Dapper produces 0 because ImageDescriptions model has no PartId property
                })
                .GroupBy(r => (r.Id, r.VesselType, r.SectionId, r.CategoryId, r.Description, r.TemplateId))
                .Select(g => g.First())
                .ToList();

            return results;
        }

        private async Task<PagedResult<ImageDescriptions>> Sp_GetAllDescriptions(int pageNumber, int pageSize)
        {
            int startRow = (pageNumber - 1) * pageSize + 1;
            int endRow = pageNumber * pageSize;

            // Load vessel types
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
            var vtById = vtList.ToDictionary(v => v.Id, v => v.VesselType);

            // Load ImageDescriptions: Set 1 requires ProjectId = 0; Sets 2 & 3 require (ProjectId = 0 OR ProjectId IS NULL)
            var descQ = new QueryDefinition("SELECT * FROM c WHERE (c.ProjectId = 0 OR IS_NULL(c.ProjectId) OR NOT IS_DEFINED(c.ProjectId)) AND c.IsDeleted = false");
            var allDescriptions = await QueryAsync(descQ);

            // Load config.VesselTypePartMapping (IsDeleted == 0)
            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vtpList = new List<VesselTypePartMappingDocument>();
            using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsDeleted = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtpList.AddRange(response);
                }
            }

            // Load config.VesselTypeNormalSectionMapping (IsDeleted == 0)
            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vnsList = new List<VesselTypeNormalSectionMappingDocument>();
            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsDeleted = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vnsList.AddRange(response);
                }
            }

            // Load config.VesselTypeSectionDescriptionMapping (IsDeleted == 0)
            var vsdContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSectionDescriptionMapping);
            var vsdList = new List<VesselTypeSectionDescriptionMappingDocument>();
            using (var feed = vsdContainer.GetItemQueryIterator<VesselTypeSectionDescriptionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsDeleted = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vsdList.AddRange(response);
                }
            }

            // Load config.VesselTypeTankSectionMapping (IsActive == 1 and IsDeleted == 0)
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

            // Load Tank_ImageDescriptionMapping
            var tidContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Tank_ImageDescriptionMapping);
            var tidList = new List<TankImageDescriptionMappingDocument>();
            using (var feed = tidContainer.GetItemQueryIterator<TankImageDescriptionMappingDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    tidList.AddRange(response);
                }
            }

            // Load TankType
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
            var tankTypeMap = ttList.ToDictionary(t => t.Id, t => t.TankType);

            // Load config.VesselTypeSubSectionDescriptionMapping
            var vssdContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSectionDescriptionMapping);
            var vssdList = new List<VesselTypeSubSectionDescriptionMappingDocument>();
            using (var feed = vssdContainer.GetItemQueryIterator<VesselTypeSubSectionDescriptionMappingDocument>(new QueryDefinition("SELECT * FROM c")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vssdList.AddRange(response);
                }
            }

            // Load config.VesselTypeSubSection (IsActive == 1 and IsDeleted == 0)
            var vtssContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSection);
            var vtssList = new List<VesselTypeSubSectionDocument>();
            using (var feed = vtssContainer.GetItemQueryIterator<VesselTypeSubSectionDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtssList.AddRange(response);
                }
            }

            var combinedRows = new List<(ImageDescriptions Item, int? SequenceNo)>();

            // --- Set 1: Normal sections ---
            // descr.ProjectId = 0 ONLY!
            foreach (var vns in vnsList)
            {
                if (!vns.VesselTypeId.HasValue || string.IsNullOrEmpty(vns.SectionMappingId)) continue;
                if (!vtById.TryGetValue(vns.VesselTypeId.Value, out var vtName) || vtName == null) continue;

                var matchingVsd = vsdList.Where(v => string.Equals(v.SectionId, vns.SectionMappingId, StringComparison.OrdinalIgnoreCase)).ToList();
                var matchingVtp = vtpList.Where(p => p.VesselTypeId == vns.VesselTypeId.Value && p.VesselTypePartMappingId == vns.VesselTypePartMappingId).ToList();

                foreach (var vsd in matchingVsd)
                {
                    var matchingDescr = allDescriptions.Where(d => d.CategoryId == vsd.CategoryId && d.ProjectId == 0 && string.Equals(d.VesselType?.TrimEnd(), vtName.TrimEnd(), StringComparison.OrdinalIgnoreCase)).ToList();
                    foreach (var descr in matchingDescr)
                    {
                        foreach (var vtp in matchingVtp)
                        {
                            var secGuid = Guid.TryParse(vns.SectionMappingId, out var g) ? g : (Guid?)null;
                            combinedRows.Add((new ImageDescriptions
                            {
                                Id = descr.Id,
                                Description = descr.Description,
                                CategoryId = (int)(descr.CategoryId ?? 0),
                                ProjectId = descr.ProjectId ?? 0,
                                VesselType = descr.VesselType,
                                TemplateId = vtp.VesselTypePartMappingId,
                                TemplateName = vtp.PartName,
                                IsActive = descr.IsActive ?? true,
                                IsDeleted = descr.IsDeleted ?? false,
                                SectionId = secGuid,
                                SectionName = vns.SectionName,
                                CreatedDttm = descr.CreatedDttm ?? DateTime.MinValue,
                                UpdatedDttm = descr.UpdatedDttm ?? DateTime.MinValue,
                                TankTypeId = null
                            }, vtp.SequenceNo));
                        }
                    }
                }
            }

            // --- Set 2: Tank sections ---
            // (descr.ProjectId = 0 OR descr.ProjectId IS NULL)
            foreach (var vts in vtsList)
            {
                if (!vts.VesselTypeId.HasValue || !vts.TankTypeId.HasValue) continue;
                if (!vtById.TryGetValue(vts.VesselTypeId.Value, out var vtName) || vtName == null) continue;
                if (!tankTypeMap.TryGetValue(vts.TankTypeId.Value, out var ttName)) continue;

                var matchingTid = tidList.Where(t => t.TankTypeId == vts.TankTypeId.Value).ToList();
                var matchingVtp = vtpList.Where(p => p.VesselTypePartMappingId == vts.VesselTypePartMappingId && p.IsActive == true && p.IsDeleted == false).ToList();

                foreach (var tid in matchingTid)
                {
                    var matchingDescr = allDescriptions.Where(d => d.CategoryId == tid.CategoryId && (d.ProjectId == 0 || d.ProjectId == null) && string.Equals(d.VesselType?.TrimEnd(), vtName.TrimEnd(), StringComparison.OrdinalIgnoreCase)).ToList();
                    foreach (var descr in matchingDescr)
                    {
                        foreach (var vtp in matchingVtp)
                        {
                            combinedRows.Add((new ImageDescriptions
                            {
                                Id = descr.Id,
                                Description = descr.Description,
                                CategoryId = (int)(descr.CategoryId ?? 0),
                                ProjectId = descr.ProjectId ?? 0,
                                VesselType = descr.VesselType,
                                TemplateId = vtp.VesselTypePartMappingId,
                                TemplateName = vtp.PartName,
                                IsActive = descr.IsActive ?? true,
                                IsDeleted = descr.IsDeleted ?? false,
                                SectionId = null,
                                SectionName = ttName,
                                CreatedDttm = descr.CreatedDttm ?? DateTime.MinValue,
                                UpdatedDttm = descr.UpdatedDttm ?? DateTime.MinValue,
                                TankTypeId = vts.TankTypeId.Value
                            }, vtp.SequenceNo));
                        }
                    }
                }
            }

            // --- Set 3: SubSections ---
            // (descr.ProjectId = 0 OR descr.ProjectId IS NULL)
            foreach (var vtss in vtssList)
            {
                if (string.IsNullOrEmpty(vtss.Id) || string.IsNullOrEmpty(vtss.VesselTypeSectionId)) continue;
                var matchingVssd = vssdList.Where(m => string.Equals(m.SubSectionId, vtss.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                var matchingVtsm = vnsList.Where(m => string.Equals(m.SectionMappingId, vtss.VesselTypeSectionId, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var vtsm in matchingVtsm)
                {
                    if (!vtsm.VesselTypeId.HasValue) continue;
                    if (!vtById.TryGetValue(vtsm.VesselTypeId.Value, out var vtName) || vtName == null) continue;
                    var matchingVpm = vtpList.Where(p => p.VesselTypePartMappingId == vtsm.VesselTypePartMappingId).ToList();

                    foreach (var vssd in matchingVssd)
                    {
                        var matchingDescr = allDescriptions.Where(d => d.CategoryId == vssd.CategoryId && (d.ProjectId == 0 || d.ProjectId == null) && string.Equals(d.VesselType?.TrimEnd(), vtName.TrimEnd(), StringComparison.OrdinalIgnoreCase)).ToList();
                        foreach (var descr in matchingDescr)
                        {
                            foreach (var vpm in matchingVpm)
                            {
                                var secGuid = Guid.TryParse(vtss.Id, out var g) ? g : (Guid?)null;
                                combinedRows.Add((new ImageDescriptions
                                {
                                    Id = descr.Id,
                                    Description = descr.Description,
                                    CategoryId = (int)(descr.CategoryId ?? 0),
                                    ProjectId = descr.ProjectId ?? 0,
                                    VesselType = descr.VesselType,
                                    TemplateId = vpm.VesselTypePartMappingId,
                                    TemplateName = vpm.PartName,
                                    IsActive = descr.IsActive ?? true,
                                    IsDeleted = descr.IsDeleted ?? false,
                                    SectionId = secGuid,
                                    SectionName = vtss.SectionName,
                                    CreatedDttm = descr.CreatedDttm ?? DateTime.MinValue,
                                    UpdatedDttm = descr.UpdatedDttm ?? DateTime.MinValue,
                                    TankTypeId = null
                                }, vtss.OrderNumber));
                            }
                        }
                    }
                }
            }

            // DISTINCT and ORDER BY SequenceNo (NULL first), Id
            var distinctList = combinedRows
                .GroupBy(r => (r.Item.Id, r.Item.Description, r.Item.CategoryId, r.Item.ProjectId, r.Item.VesselType, r.Item.TemplateId, r.Item.TemplateName, r.Item.IsActive, r.Item.IsDeleted, r.Item.SectionId, r.Item.SectionName, r.Item.CreatedDttm, r.Item.UpdatedDttm, r.SequenceNo, r.Item.TankTypeId))
                .Select(g => g.First())
                .OrderBy(r => r.SequenceNo.HasValue)
                .ThenBy(r => r.SequenceNo)
                .ThenBy(r => r.Item.Id)
                .ToList();

            int total = distinctList.Count;
            List<ImageDescriptions> pageData;
            if (pageSize == 0)
            {
                pageData = distinctList.Select(r => r.Item).ToList();
            }
            else
            {
                pageData = distinctList
                    .Select((r, idx) => new { RowNum = idx + 1, r.Item })
                    .Where(x => x.RowNum >= startRow && x.RowNum <= endRow)
                    .Select(x => x.Item)
                    .ToList();
            }

            return new PagedResult<ImageDescriptions>
            {
                Data = pageData,
                Total = total
            };
        }

        private async Task<List<GradingSection>> Sp_GetDescriptionSectionsByTemplateNameAndVesselType(string? templateName, string? vesselType)
        {
            templateName = templateName?.Length > 200 ? templateName.Substring(0, 200) : templateName;
            vesselType = vesselType?.Length > 200 ? vesselType.Substring(0, 200) : vesselType;

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
            var matchingVt = vtList.Where(v => string.Equals(v.VesselType?.TrimEnd(), vesselType?.TrimEnd(), StringComparison.OrdinalIgnoreCase));
            int vesselTypeId = CosmosLastRowHelper.GetLastRowValue(matchingVt, v => v.Id, v => v.Id, 0);

            var vtpContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var vtpList = new List<VesselTypePartMappingDocument>();
            // Look up TemplateId even when VesselTypeId == 0
            var vtpQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid").WithParameter("@vtid", vesselTypeId);
            using (var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(vtpQ, requestOptions: vesselTypeId != 0 ? new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) } : null))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtpList.AddRange(response);
                }
            }
            var matchingVtp = vtpList.Where(p => string.Equals(p.PartName?.TrimEnd(), templateName?.TrimEnd(), StringComparison.OrdinalIgnoreCase));
            int templateId = CosmosLastRowHelper.GetLastRowValue(matchingVtp, p => p.VesselTypePartMappingId, p => p.VesselTypePartMappingId, 0);

            if (templateId == 0 || vesselType == null)
            {
                return new List<GradingSection>();
            }

            var results = new List<(GradingSection Section, int? Order)>();

            // 1. Normal sections:
            // inner join config.VesselTypePartMapping vtp on vtp.VesselTypePartMappingId = nsm.VesselTypePartMappingId and nsm.VesselTypeId = vtp.VesselTypeId
            // Where vtp.VesselTypeId=@VesselTypeId and vtp.IsActive=1 and vtp.IsDeleted=0 and nsm.VesselTypePartMappingId= @TemplateId and NSM.IsDeleted=0 and nsm.IsActive=1
            var vnsContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var vnsQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.VesselTypePartMappingId = @tid AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vtid", vesselTypeId)
                .WithParameter("@tid", templateId);

            var activeVtp = vtpList.Where(p => p.VesselTypeId == vesselTypeId && p.IsActive == true && p.IsDeleted == false).ToDictionary(p => p.VesselTypePartMappingId);

            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(vnsQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (doc.VesselTypePartMappingId.HasValue && activeVtp.ContainsKey(doc.VesselTypePartMappingId.Value))
                        {
                            var secId = Guid.TryParse(doc.SectionMappingId, out var g) ? g : Guid.Empty;
                            results.Add((new GradingSection
                            {
                                SectionId = secId,
                                TanktypeId = 0,
                                TemplateId = doc.VesselTypePartMappingId.Value,
                                SectionName = doc.SectionName
                            }, doc.Order));
                        }
                    }
                }
            }

            // 2. Tank sections:
            // inner join config.VesselTypePartMapping vtp on vtp.VesselTypePartMappingId = vtt.VesselTypePartMappingId and vtt.VesselTypeId = vtp.VesselTypeId
            // INNER JOIN TankType TT ON TT.Id = vtt.TankTypeId
            // WHERE vtt.VesselTypeId=@VesselTypeId and vtt.VesselTypePartMappingId= @TemplateId and vtt.IsDeleted=0 and vtt.IsActive=1 and vtp.IsActive=1 and vtp.IsDeleted=0
            var vttContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
            var vttQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.VesselTypePartMappingId = @tid AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vtid", vesselTypeId)
                .WithParameter("@tid", templateId);

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
            var tankTypeMap = ttList.ToDictionary(t => t.Id, t => t.TankType);

            using (var feed = vttContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(vttQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (doc.VesselTypePartMappingId.HasValue && activeVtp.ContainsKey(doc.VesselTypePartMappingId.Value) &&
                            doc.TankTypeId.HasValue && tankTypeMap.TryGetValue(doc.TankTypeId.Value, out var ttName))
                        {
                            results.Add((new GradingSection
                            {
                                SectionId = Guid.Empty,
                                TanktypeId = doc.TankTypeId.Value,
                                TemplateId = doc.VesselTypePartMappingId.Value,
                                SectionName = ttName
                            }, 999));
                        }
                    }
                }
            }

            // 3. SubSection:
            // FROM [Config].VesselTypeSubSection AS VS
            // inner join config.VesselTypeNormalSectionMapping as VNS on VNS.SectionMappingId = vs.VesselTypeSectionId and vns.HasSubSection=1
            // inner join config.VesselTypePartMapping vtp on vtp.VesselTypePartMappingId = vns.VesselTypePartMappingId and vns.VesselTypeId = vtp.VesselTypeId
            // Where vns.VesselTypeId=@VesselTypeId and vns.VesselTypePartMappingId= @TemplateId and VS.IsDeleted=0 and Vs.IsActive=1
            // and VNs.IsActive =1 and vns.IsDeleted =0 and vtp.IsActive =1 and vtp.IsDeleted =0
            var vssContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeSubSection);
            var vssList = new List<VesselTypeSubSectionDocument>();
            using (var feed = vssContainer.GetItemQueryIterator<VesselTypeSubSectionDocument>(new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vssList.AddRange(response);
                }
            }

            var normalWithSub = new List<VesselTypeNormalSectionMappingDocument>();
            var normSubQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.VesselTypePartMappingId = @tid AND c.HasSubSection = true AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vtid", vesselTypeId)
                .WithParameter("@tid", templateId);
            using (var feed = vnsContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(normSubQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    normalWithSub.AddRange(response);
                }
            }

            var normSubMap = normalWithSub.Where(n => !string.IsNullOrEmpty(n.SectionMappingId)).ToDictionary(n => n.SectionMappingId!, n => n, StringComparer.OrdinalIgnoreCase);
            foreach (var vs in vssList)
            {
                if (!string.IsNullOrEmpty(vs.VesselTypeSectionId) && normSubMap.TryGetValue(vs.VesselTypeSectionId, out var normParent))
                {
                    if (normParent.VesselTypePartMappingId.HasValue && activeVtp.ContainsKey(normParent.VesselTypePartMappingId.Value))
                    {
                        var subId = Guid.TryParse(vs.Id, out var g) ? g : Guid.Empty;
                        results.Add((new GradingSection
                        {
                            SectionId = subId,
                            TanktypeId = 0,
                            TemplateId = normParent.VesselTypePartMappingId.Value,
                            SectionName = vs.SectionName
                        }, vs.OrderNumber));
                    }
                }
            }

            // ORDER BY [Order] (NULL first)
            return results
                .OrderBy(r => r.Order.HasValue)
                .ThenBy(r => r.Order)
                .Select(r => r.Section)
                .ToList();
        }

        private async Task<List<ImageDescriptions>> Sp_GetImageDescriptionById(int id)
        {
            var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", id);
            var docs = await QueryAsync(q);
            return docs.Select(d => new ImageDescriptions
            {
                Id = d.Id,
                Description = d.Description,
                CategoryId = (int)(d.CategoryId ?? 0),
                ProjectId = d.ProjectId ?? 0,
                IsActive = d.IsActive ?? true,
                IsDeleted = d.IsDeleted ?? false,
                CreatedDttm = d.CreatedDttm ?? DateTime.MinValue,
                UpdatedDttm = d.UpdatedDttm ?? DateTime.MinValue,
                VesselType = d.VesselType
            }).ToList();
        }

        #endregion
    }
}
