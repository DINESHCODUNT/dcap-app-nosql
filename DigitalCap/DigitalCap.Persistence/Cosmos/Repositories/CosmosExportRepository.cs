using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models.ExportConfig;
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
    internal static class CosmosLastRowHelper
    {
        public static TValue GetLastRowValue<TDoc, TKey, TValue>(
            IEnumerable<TDoc> docs,
            Func<TDoc, TKey> keySelector,
            Func<TDoc, TValue> valueSelector,
            TValue defaultValue = default!) where TKey : IComparable<TKey>
        {
            var ordered = docs.OrderBy(keySelector).ToList();
            if (ordered.Count == 0)
            {
                return defaultValue;
            }
            return valueSelector(ordered[^1]);
        }

        public static TValue GetLastRowValue<TDoc, TValue>(
            IEnumerable<TDoc> docs,
            Func<TDoc, TValue> selector,
            TValue defaultValue = default!) where TValue : IComparable<TValue>
        {
            var ordered = docs.OrderBy(selector).ToList();
            if (ordered.Count == 0)
            {
                return defaultValue;
            }
            return selector(ordered[^1]);
        }
    }

    public class CosmosExportRepository : CosmosRepositoryBase<ReportPartDocument>, IExportRepository
    {
        public CosmosExportRepository(
            ICosmosContainerFactory containerFactory,
            ILogger<CosmosExportRepository> logger)
            : base(containerFactory, CosmosContainers.Config_ReportPart, logger)
        {
        }

        public void Commit() { }

        public async Task<List<ExportPart>> GetVesselTypeExportPartConfiguration(int vesselTypeId)
        {
            try
            {
                return (await Sp_GetVesselTypeExportConfigForParts(vesselTypeId)).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<ExportSections>> GetVesselTypeExportSectionConfiguration(int partId, int vesselTypeId)
        {
            try
            {
                return (await Sp_GetVesselTypeExportConfigForSections(partId, vesselTypeId)).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<ExportPart>> GetProjectExportPartConfiguration(int projectId)
        {
            try
            {
                return (await Sp_GetProjectExportConfigForParts(projectId)).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<ExportSections>> GetProjectExportSectionConfiguration(int partId, int projectId)
        {
            try
            {
                return (await Sp_GetProjectExportConfigForSections(partId, projectId)).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdatePartRequiredInReportAsync(bool requiredInReport, int partId, int projectId, int vesselTypeId, string updatedBy)
        {
            try
            {
                await Sp_UpdatePartRequiredInReport(requiredInReport, partId, vesselTypeId, projectId, updatedBy);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateTankSectionRequiredInReportAsync(bool requiredInReport, Guid sectionId, int projectId, string updatedBy)
        {
            try
            {
                await Sp_UpdateTankSectionRequiredInReport(requiredInReport, sectionId, projectId, updatedBy);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateSectionRequiredInReportAsync(bool requiredInReport, Guid sectionId, int projectId, int vesselTypeId, string updatedBy)
        {
            try
            {
                await Sp_UpdateSectionRequiredInReport(requiredInReport, sectionId, vesselTypeId, projectId, updatedBy);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Stored Procedure Helpers

        private async Task<List<ExportPart>> Sp_GetVesselTypeExportConfigForParts(int vesselTypeId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vtid", vesselTypeId);

            var items = new List<VesselTypePartMappingDocument>();
            using var feed = container.GetItemQueryIterator<VesselTypePartMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) });
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                items.AddRange(response);
            }

            return items
                .OrderBy(d => d.SequenceNo.HasValue)
                .ThenBy(d => d.SequenceNo)
                .Select(d => new ExportPart
                {
                    PartId = d.VesselTypePartMappingId,
                    PartName = d.PartName,
                    RequiredInReport = d.RequiredInReport ?? false
                })
                .ToList();
        }

        private async Task<List<ExportSections>> Sp_GetVesselTypeExportConfigForSections(int partId, int vesselTypeId)
        {
            // 1. Get VesselType string from VesselType container (last row wins)
            var vtContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_VesselType);
            var vtQ = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", vesselTypeId);
            var vtList = new List<VesselTypeDocument>();
            using (var feed = vtContainer.GetItemQueryIterator<VesselTypeDocument>(vtQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    vtList.AddRange(response);
                }
            }

            string? vesselTypeStr = CosmosLastRowHelper.GetLastRowValue(vtList, d => d.Id, d => d.VesselType);
            if (vesselTypeStr != null && vesselTypeStr.Length > 50)
            {
                vesselTypeStr = vesselTypeStr.Substring(0, 50);
            }

            var results = new List<(ExportSections Section, int? Order)>();

            // 2. Normal sections
            var normalContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
            var normalQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.VesselTypePartMappingId = @partId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@vtid", vesselTypeId)
                .WithParameter("@partId", partId);

            using (var feed = normalContainer.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(normalQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        var secId = Guid.TryParse(doc.SectionMappingId, out var g) ? g : Guid.Empty;
                        results.Add((new ExportSections
                        {
                            SectionId = secId,
                            SectionName = doc.SectionName,
                            RequiredInReport = doc.RequiredInReport ?? false,
                            TankTypeId = 0,
                            TankType = null,
                            IsSubSection = false,
                            ParentId = Guid.Empty
                        }, doc.Order));
                    }
                }
            }

            // 3. Tank sections (only if vesselTypeStr is NOT NULL; if null, SQL WHERE VesselType = NULL yields 0 rows)
            if (vesselTypeStr != null)
            {
                var tankMappingContainer = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeTankSectionMapping);
                var tankMappingQ = new QueryDefinition(
                    "SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.VesselTypePartMappingId = @partId AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@vtid", vesselTypeId)
                    .WithParameter("@partId", partId);

                var allowedTankTypeIds = new HashSet<int>();
                using (var feed = tankMappingContainer.GetItemQueryIterator<VesselTypeTankSectionMappingDocument>(tankMappingQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) }))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var doc in response)
                        {
                            if (doc.TankTypeId.HasValue)
                            {
                                allowedTankTypeIds.Add(doc.TankTypeId.Value);
                            }
                        }
                    }
                }

                if (allowedTankTypeIds.Count > 0)
                {
                    // TankType lookup dictionary (INNER JOIN: drop rows without TankType match)
                    var ttContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_TankType);
                    var tankTypeNames = new Dictionary<int, string>();
                    var ttQ = new QueryDefinition("SELECT * FROM c");
                    using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(ttQ))
                    {
                        while (feed.HasMoreResults)
                        {
                            var response = await feed.ReadNextAsync();
                            foreach (var tt in response)
                            {
                                if (tt.TankType != null)
                                {
                                    tankTypeNames[tt.Id] = tt.TankType;
                                }
                            }
                        }
                    }

                    // Vessel_TankMapping: where IsActive=1 and IsDeleted=0 and ImoNumber is null and VesselType=@VesselType and TankTypeId in allowedTankTypeIds
                    var vtmContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
                    var vtmQ = new QueryDefinition(
                        "SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false AND (IS_NULL(c.ImoNumber) OR NOT IS_DEFINED(c.ImoNumber))");

                    using (var feed = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(vtmQ))
                    {
                        while (feed.HasMoreResults)
                        {
                            var response = await feed.ReadNextAsync();
                            foreach (var doc in response)
                            {
                                if (doc.VesselType != null && string.Equals(doc.VesselType.TrimEnd(' '), vesselTypeStr.TrimEnd(' '), StringComparison.OrdinalIgnoreCase) &&
                                    doc.TankTypeId.HasValue && allowedTankTypeIds.Contains(doc.TankTypeId.Value))
                                {
                                    // INNER JOIN TankType: drop if not found
                                    if (tankTypeNames.TryGetValue(doc.TankTypeId.Value, out var ttName))
                                    {
                                        var secId = Guid.TryParse(doc.Id, out var g) ? g : Guid.Empty;
                                        results.Add((new ExportSections
                                        {
                                            SectionId = secId,
                                            SectionName = doc.TankName,
                                            RequiredInReport = doc.RequiredInReport ?? false,
                                            TankTypeId = doc.TankTypeId.Value,
                                            TankType = ttName,
                                            IsSubSection = false,
                                            ParentId = Guid.Empty
                                        }, 999));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // In SQL Server, ORDER BY [Order] places NULL first
            return results
                .OrderBy(r => r.Order.HasValue)
                .ThenBy(r => r.Order)
                .Select(r => r.Section)
                .ToList();
        }

        private async Task<List<ExportPart>> Sp_GetProjectExportConfigForParts(int projectId)
        {
            var container = ContainerFactory.GetContainer(CosmosContainers.Config_ReportPart);
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @pid AND c.IsDeleted = false")
                .WithParameter("@pid", projectId);

            var items = new List<ReportPartDocument>();
            using var feed = container.GetItemQueryIterator<ReportPartDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) });
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                items.AddRange(response);
            }

            return items
                .OrderBy(d => d.SequenceNo.HasValue)
                .ThenBy(d => d.SequenceNo)
                .Select(d => new ExportPart
                {
                    PartId = d.PartId,
                    PartName = d.PartName,
                    RequiredInReport = d.RequiredInReport ?? false
                })
                .ToList();
        }

        private async Task<List<ExportSections>> Sp_GetProjectExportConfigForSections(int partId, int projectId)
        {
            // 1. Main Sections
            var psmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
            var psmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @pid AND c.PartId = @partId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@pid", projectId)
                .WithParameter("@partId", partId);

            var sections = new List<ProjectSectionMappingDocument>();
            using (var feed = psmContainer.GetItemQueryIterator<ProjectSectionMappingDocument>(psmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    sections.AddRange(response);
                }
            }

            // 2. Sub Sections
            var subSectionsByParent = new Dictionary<string, List<ProjectSubSectionDocument>>(StringComparer.OrdinalIgnoreCase);
            var sectionsWithSub = sections.Where(s => s.HasSubSection == true && !string.IsNullOrEmpty(s.SectionMappingId)).ToList();
            if (sectionsWithSub.Count > 0)
            {
                var subContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSubSection);
                var subQ = new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true AND c.IsDeleted = false");
                using (var feed = subContainer.GetItemQueryIterator<ProjectSubSectionDocument>(subQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var sub in response)
                        {
                            if (!string.IsNullOrEmpty(sub.ProjectSectionId))
                            {
                                if (!subSectionsByParent.TryGetValue(sub.ProjectSectionId, out var subList))
                                {
                                    subList = new List<ProjectSubSectionDocument>();
                                    subSectionsByParent[sub.ProjectSectionId] = subList;
                                }
                                subList.Add(sub);
                            }
                        }
                    }
                }
            }

            // 3. Compute maxOrder from ProjectSectionMapping (including subsections)
            var orders = sections.Where(s => s.Order.HasValue).Select(s => s.Order!.Value).ToList();
            foreach (var s in sectionsWithSub)
            {
                if (s.Order.HasValue && subSectionsByParent.TryGetValue(s.SectionMappingId!, out var subList) && subList.Count > 0)
                {
                    orders.AddRange(Enumerable.Repeat(s.Order.Value, subList.Count));
                }
            }
            int maxOrder = orders.Count > 0 ? orders.Max() : 0;

            var combinedList = new List<(ExportSections Item, int? Order, DateTime? CreatedDttm)>();

            // Add Main Sections
            foreach (var s in sections)
            {
                var secId = Guid.TryParse(s.SectionMappingId, out var g) ? g : Guid.Empty;
                combinedList.Add((new ExportSections
                {
                    SectionId = secId,
                    SectionName = s.SectionName,
                    RequiredInReport = s.RequiredInReport ?? false,
                    TankTypeId = 0,
                    TankType = null,
                    ParentId = Guid.Empty,
                    IsSubSection = false
                }, s.Order, null));
            }

            // Add Sub Sections
            foreach (var s in sectionsWithSub)
            {
                if (subSectionsByParent.TryGetValue(s.SectionMappingId!, out var subList))
                {
                    var parentGuid = Guid.TryParse(s.SectionMappingId, out var pg) ? pg : Guid.Empty;
                    foreach (var sub in subList)
                    {
                        var subId = Guid.TryParse(sub.Id, out var sg) ? sg : Guid.Empty;
                        combinedList.Add((new ExportSections
                        {
                            SectionId = subId,
                            SectionName = sub.SectionName,
                            RequiredInReport = s.RequiredInReport ?? false,
                            TankTypeId = 0,
                            TankType = null,
                            ParentId = parentGuid,
                            IsSubSection = true
                        }, s.Order, null));
                    }
                }
            }

            // 4. Vessel Tank Mapping
            var ptsmContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectTankSectionMapping);
            var ptsmQ = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @pid AND c.PartId = @partId AND c.IsActive = true AND c.IsDeleted = false")
                .WithParameter("@pid", projectId)
                .WithParameter("@partId", partId);

            var psmList = new List<ProjectTankSectionMappingDocument>();
            using (var feed = ptsmContainer.GetItemQueryIterator<ProjectTankSectionMappingDocument>(ptsmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    psmList.AddRange(response);
                }
            }

            if (psmList.Count > 0)
            {
                var ttContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_TankType);
                var tankTypeMap = new Dictionary<int, string>();
                var ttQ = new QueryDefinition("SELECT * FROM c");
                using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(ttQ))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var tt in response)
                        {
                            if (tt.TankType != null)
                            {
                                tankTypeMap[tt.Id] = tt.TankType;
                            }
                        }
                    }
                }

                var vtmContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
                var vtmQ = new QueryDefinition(
                    "SELECT * FROM c WHERE c.ProjectId = @pid AND c.IsActive = true AND c.IsDeleted = false")
                    .WithParameter("@pid", projectId);

                var vtmList = new List<VesselTankMappingDocument>();
                using (var feed = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(vtmQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vtmList.AddRange(response);
                    }
                }

                // Inner join vtm and psm on TankTypeId, and inner join tt on tt.Id = vtm.TankTypeId
                // 1 row per (vtm x matching psm row)
                var joinedTankRows = new List<(VesselTankMappingDocument Vtm, string TankType, int TankTypeId)>();
                foreach (var vtm in vtmList)
                {
                    if (vtm.TankTypeId.HasValue)
                    {
                        int ttId = vtm.TankTypeId.Value;
                        if (tankTypeMap.TryGetValue(ttId, out var ttName))
                        {
                            foreach (var psmDoc in psmList)
                            {
                                if (psmDoc.TankTypeId.HasValue && psmDoc.TankTypeId.Value == ttId)
                                {
                                    joinedTankRows.Add((vtm, ttName, ttId));
                                }
                            }
                        }
                    }
                }

                // Number by tt.Id, TankName, CreatedDttm (NULL CreatedDttm first)
                var orderedTanks = joinedTankRows
                    .OrderBy(r => r.TankTypeId)
                    .ThenBy(r => r.Vtm.TankName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(r => r.Vtm.CreatedDttm.HasValue)
                    .ThenBy(r => r.Vtm.CreatedDttm)
                    .ToList();

                var rowNum = 1;
                foreach (var row in orderedTanks)
                {
                    var secId = Guid.TryParse(row.Vtm.Id, out var g) ? g : Guid.Empty;
                    var computedOrder = (maxOrder + 999) + rowNum;
                    rowNum++;

                    combinedList.Add((new ExportSections
                    {
                        SectionId = secId,
                        SectionName = row.Vtm.TankName,
                        RequiredInReport = row.Vtm.RequiredInReport ?? false,
                        TankTypeId = row.TankTypeId,
                        TankType = row.TankType,
                        ParentId = Guid.Empty,
                        IsSubSection = false
                    }, computedOrder, row.Vtm.CreatedDttm));
                }
            }

            // Final order: Order (NULL first), then CreatedDttm (NULL first)
            return combinedList
                .OrderBy(c => c.Order.HasValue)
                .ThenBy(c => c.Order)
                .ThenBy(c => c.CreatedDttm.HasValue)
                .ThenBy(c => c.CreatedDttm)
                .Select(c => c.Item)
                .ToList();
        }

        private async Task Sp_UpdatePartRequiredInReport(bool requiredInReport, int partId, int vesselTypeId, int projectId, string updatedBy)
        {
            var cutUpdatedBy = updatedBy?.Length > 50 ? updatedBy.Substring(0, 50) : updatedBy;

            if (vesselTypeId != 0)
            {
                var container = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
                var q = new QueryDefinition(
                    "SELECT * FROM c WHERE c.VesselTypePartMappingId = @partId AND c.VesselTypeId = @vtid")
                    .WithParameter("@partId", partId)
                    .WithParameter("@vtid", vesselTypeId);

                using var feed = container.GetItemQueryIterator<VesselTypePartMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vesselTypeId) });
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        doc.RequiredInReport = requiredInReport;
                        doc.ModifiedBy = cutUpdatedBy;
                        doc.ModifiedOn = DateTime.UtcNow.Date;
                        await container.UpsertItemAsync(doc, new PartitionKey((double)vesselTypeId));
                    }
                }
            }
            else
            {
                var container = ContainerFactory.GetContainer(CosmosContainers.Config_ReportPart);
                var q = new QueryDefinition(
                    "SELECT * FROM c WHERE c.PartId = @partId AND c.ProjectId = @pid")
                    .WithParameter("@partId", partId)
                    .WithParameter("@pid", projectId);

                using var feed = container.GetItemQueryIterator<ReportPartDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) });
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        doc.RequiredInReport = requiredInReport;
                        doc.ModifiedBy = cutUpdatedBy;
                        // SQL does NOT set ModifiedOn here
                        await container.UpsertItemAsync(doc, new PartitionKey((double)projectId));
                    }
                }
            }
        }

        private async Task Sp_UpdateTankSectionRequiredInReport(bool requiredInReport, Guid sectionId, int projectId, string updatedBy)
        {
            var vtmContainer = ContainerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var secStr = sectionId.ToString();
            var cutUpdatedBy = updatedBy?.Length > 50 ? updatedBy.Substring(0, 50) : updatedBy;

            if (projectId == 0)
            {
                // WHERE Id = @SectionId
                var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", secStr);
                using var feed = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(q);
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        doc.RequiredInReport = requiredInReport;
                        doc.UpdateDttm = DateTime.UtcNow;
                        doc.UpdatedBy = cutUpdatedBy;
                        PartitionKey pk = doc.ProjectId.HasValue ? new PartitionKey((double)doc.ProjectId.Value) : PartitionKey.Null;
                        await vtmContainer.UpsertItemAsync(doc, pk);
                    }
                }
            }
            else
            {
                // SELECT @Imo=Imo FROM cap.Projects WHERE Id=@ProjectId
                var projContainer = ContainerFactory.GetContainer(CosmosContainers.CAP_Projects);
                var projQ = new QueryDefinition("SELECT * FROM c WHERE c.Id = @pid OR c.ID = @pid").WithParameter("@pid", projectId);

                int? imo = null;
                using (var feed = projContainer.GetItemQueryIterator<ProjectDocument>(projQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(projectId.ToString()) }))
                {
                    var projList = new List<ProjectDocument>();
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        projList.AddRange(response);
                    }
                    var proj = projList.OrderBy(p => p.EffectiveId).LastOrDefault();
                    imo = proj?.IMO;
                }

                // If project not found or IMO is null, 0 rows match WHERE ImoNumber = @Imo
                if (!imo.HasValue)
                {
                    return;
                }

                // In SQL: update Vessel_TankMapping WHERE Id = @sectionId AND ImoNumber = @Imo (no ProjectId filter on vtm)
                var q = new QueryDefinition("SELECT * FROM c WHERE c.Id = @id").WithParameter("@id", secStr);
                using var feed2 = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(q);
                while (feed2.HasMoreResults)
                {
                    var response = await feed2.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (doc.ImoNumber != null)
                        {
                            // SQL converts ''/spaces to 0; non-numeric still throws
                            var trimmedImo = doc.ImoNumber.Trim(' ');
                            int docImo = trimmedImo.Length == 0 ? 0 : int.Parse(trimmedImo);
                            if (docImo == imo.Value)
                            {
                                doc.RequiredInReport = requiredInReport;
                                doc.UpdatedBy = cutUpdatedBy;
                                doc.UpdateDttm = DateTime.UtcNow;
                                PartitionKey pk = doc.ProjectId.HasValue ? new PartitionKey((double)doc.ProjectId.Value) : PartitionKey.Null;
                                await vtmContainer.UpsertItemAsync(doc, pk);
                            }
                        }
                    }
                }
            }
        }

        private async Task Sp_UpdateSectionRequiredInReport(bool requiredInReport, Guid sectionId, int vesselTypeId, int projectId, string updatedBy)
        {
            var secStr = sectionId.ToString();
            var cutUpdatedBy = updatedBy?.Length > 50 ? updatedBy.Substring(0, 50) : updatedBy;

            if (vesselTypeId != 0)
            {
                // WHERE SectionMappingId = @SectionId (cross-partition query, no VesselTypeId filter in SQL WHERE)
                var container = ContainerFactory.GetContainer(CosmosContainers.Config_VesselTypeNormalSectionMapping);
                var q = new QueryDefinition("SELECT * FROM c WHERE c.SectionMappingId = @secId").WithParameter("@secId", secStr);

                using var feed = container.GetItemQueryIterator<VesselTypeNormalSectionMappingDocument>(q);
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        doc.RequiredInReport = requiredInReport;
                        doc.ModifiedBy = cutUpdatedBy;
                        doc.ModifiedOn = DateTime.UtcNow.Date;
                        PartitionKey pk = doc.VesselTypeId.HasValue ? new PartitionKey((double)doc.VesselTypeId.Value) : PartitionKey.Null;
                        await container.UpsertItemAsync(doc, pk);
                    }
                }
            }
            else
            {
                // WHERE SectionMappingId = @SectionId AND ProjectId = @projectId
                var container = ContainerFactory.GetContainer(CosmosContainers.Config_ProjectSectionMapping);
                var q = new QueryDefinition(
                    "SELECT * FROM c WHERE c.ProjectId = @pid AND c.SectionMappingId = @secId")
                    .WithParameter("@pid", projectId)
                    .WithParameter("@secId", secStr);

                using var feed = container.GetItemQueryIterator<ProjectSectionMappingDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) });
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        doc.RequiredInReport = requiredInReport;
                        doc.ModifiedBy = cutUpdatedBy;
                        doc.ModifiedOn = DateTime.UtcNow.Date;
                        await container.UpsertItemAsync(doc, new PartitionKey((double)projectId));
                    }
                }
            }
        }

        #endregion
    }
}
