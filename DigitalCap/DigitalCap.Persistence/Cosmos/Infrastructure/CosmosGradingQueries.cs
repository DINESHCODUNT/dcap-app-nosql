using DigitalCap.Core.Models.Grading;
using DigitalCap.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    public static class CosmosGradingQueries
    {
        public static async Task<List<GradingTemplate>> Sp_GetGradingTemplate(ICosmosContainerFactory containerFactory, string? vesselType)
        {
            vesselType = vesselType?.Length > 100 ? vesselType.Substring(0, 100) : vesselType;

            if (vesselType == null)
            {
                // select Id as TemplateId, [Section Name] as TemplateName from ReportTemplate Where Id !=0 and Id !=8
                var rtContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportTemplate);
                var q = new QueryDefinition("SELECT * FROM c WHERE c.Id != 0 AND c.Id != 8");
                var list = new List<GradingTemplate>();
                using (var feed = rtContainer.GetItemQueryIterator<ReportTemplateDocument>(q))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var doc in response)
                        {
                            list.Add(new GradingTemplate
                            {
                                TemplateId = doc.Id,
                                TemplateName = doc.SectionName
                            });
                        }
                    }
                }
                return list;
            }
            else
            {
                // select VesselTypePartMappingId as TemplateId, PM.PartName as TemplateName
                // from [Config].[VesselTypePartMapping] AS PM 
                // INNER JOIN VesselType as VT ON VT.Id = PM.VesselTypeId
                // where PM.IsActive=1 and PM.IsDeleted=0 
                // AND VT.VesselType = @VesselType
                // order by SequenceNo
                var vtContainer = containerFactory.GetContainer(CosmosContainers.dbo_VesselType);
                var vtList = new List<VesselTypeDocument>();
                using (var feed = vtContainer.GetItemQueryIterator<VesselTypeDocument>(new QueryDefinition("SELECT * FROM c")))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vtList.AddRange(response);
                    }
                }
                // Join EVERY VesselType row with that name
                var matchingVtIds = vtList
                    .Where(v => v.VesselType != null && string.Equals(v.VesselType.TrimEnd(' '), vesselType.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                    .Select(v => v.Id)
                    .ToHashSet();

                var vtpContainer = containerFactory.GetContainer(CosmosContainers.Config_VesselTypePartMapping);
                var vtpList = new List<VesselTypePartMappingDocument>();
                foreach (var vtid in matchingVtIds)
                {
                    var vtpQ = new QueryDefinition("SELECT * FROM c WHERE c.VesselTypeId = @vtid AND c.IsActive = true AND c.IsDeleted = false")
                        .WithParameter("@vtid", vtid);

                    using var feed = vtpContainer.GetItemQueryIterator<VesselTypePartMappingDocument>(vtpQ, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)vtid) });
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        vtpList.AddRange(response);
                    }
                }

                // In SQL Server, ORDER BY SequenceNo sorts NULL first
                return vtpList
                    .OrderBy(p => p.SequenceNo.HasValue)
                    .ThenBy(p => p.SequenceNo)
                    .Select(p => new GradingTemplate
                    {
                        TemplateId = p.VesselTypePartMappingId,
                        TemplateName = p.PartName
                    })
                    .ToList();
            }
        }

        public static async Task<List<GradingSection>> Sp_GetGradingSections(ICosmosContainerFactory containerFactory, int templateId, string? vesselType)
        {
            var results = new List<GradingSection>();

            // 1. Default Templates:
            // ReportTemplate rt INNER JOIN TemplateSectionMapping tsm ON tsm.TemplateId = rt.Id
            // INNER JOIN ReportTemplateSection rts ON tsm.SectionId = rts.Id
            // WHERE rt.Id != 0 AND rt.Id != 8 AND rts.HasSubSection = 0
            var rtContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportTemplate);
            var validRtIds = new HashSet<int>();
            using (var feed = rtContainer.GetItemQueryIterator<ReportTemplateDocument>(new QueryDefinition("SELECT * FROM c WHERE c.Id != 0 AND c.Id != 8")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var rt in response) validRtIds.Add(rt.Id);
                }
            }

            var tsmContainer = containerFactory.GetContainer(CosmosContainers.dbo_TemplateSectionMapping);
            var tsmList = new List<TemplateSectionMappingDocument>();
            using (var feed = tsmContainer.GetItemQueryIterator<TemplateSectionMappingDocument>(new QueryDefinition("SELECT * FROM c WHERE c.TemplateId != 0 AND c.TemplateId != 8")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var tsm in response)
                    {
                        if (tsm.TemplateId.HasValue && validRtIds.Contains(tsm.TemplateId.Value))
                        {
                            tsmList.Add(tsm);
                        }
                    }
                }
            }

            var rtsContainer = containerFactory.GetContainer(CosmosContainers.dbo_ReportTemplateSection);
            var rtsList = new List<ReportTemplateSectionDocument>();
            using (var feed = rtsContainer.GetItemQueryIterator<ReportTemplateSectionDocument>(new QueryDefinition("SELECT * FROM c WHERE c.HasSubSection = false")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    rtsList.AddRange(response);
                }
            }
            var rtsMap = rtsList.Where(r => !string.IsNullOrEmpty(r.Id)).ToDictionary(r => r.Id!, r => r.SectionName, StringComparer.OrdinalIgnoreCase);

            foreach (var tsm in tsmList)
            {
                if (!string.IsNullOrEmpty(tsm.SectionId) && rtsMap.TryGetValue(tsm.SectionId, out var secName))
                {
                    var secGuid = Guid.TryParse(tsm.SectionId, out var g) ? g : Guid.Empty;
                    results.Add(new GradingSection
                    {
                        SectionId = secGuid,
                        TanktypeId = 0,
                        TemplateId = tsm.TemplateId ?? 0,
                        SectionName = secName
                    });
                }
            }

            // 2. Tank Templates:
            // Vessel_TankMapping: WHERE IsActive = 1 AND VesselType = IIF(@VesselType IS NOT NULL, @VesselType, VesselType) AND TankTypeId != 0
            // Note: when @VesselType is null, VesselType = VesselType excludes NULL VesselType
            var vtmContainer = containerFactory.GetContainer(CosmosContainers.dbo_Vessel_TankMapping);
            var vtmQ = new QueryDefinition("SELECT * FROM c WHERE c.IsActive = true");
            var distinctTankTypeIds = new HashSet<int>();
            using (var feed = vtmContainer.GetItemQueryIterator<VesselTankMappingDocument>(vtmQ))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        if (doc.TankTypeId.HasValue && doc.TankTypeId.Value != 0)
                        {
                            if (vesselType != null)
                            {
                                if (doc.VesselType != null && string.Equals(doc.VesselType.TrimEnd(' '), vesselType.TrimEnd(' '), StringComparison.OrdinalIgnoreCase))
                                {
                                    distinctTankTypeIds.Add(doc.TankTypeId.Value);
                                }
                            }
                            else
                            {
                                // @VesselType IS NULL -> VesselType = VesselType excludes NULL VesselType
                                if (doc.VesselType != null)
                                {
                                    distinctTankTypeIds.Add(doc.TankTypeId.Value);
                                }
                            }
                        }
                    }
                }
            }

            // TankType: INNER JOIN TankType tty ON rt.Id = tty.TemplateId AND tty.Id = vt.TankTypeId WHERE rt.Id != 0 AND rt.Id != 8
            var ttContainer = containerFactory.GetContainer(CosmosContainers.dbo_TankType);
            var ttList = new List<TankTypeDocument>();
            using (var feed = ttContainer.GetItemQueryIterator<TankTypeDocument>(new QueryDefinition("SELECT * FROM c WHERE c.TemplateId != 0 AND c.TemplateId != 8")))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    ttList.AddRange(response);
                }
            }

            foreach (var tt in ttList)
            {
                if (distinctTankTypeIds.Contains(tt.Id) && tt.TemplateId.HasValue && validRtIds.Contains(tt.TemplateId.Value))
                {
                    results.Add(new GradingSection
                    {
                        SectionId = Guid.Empty,
                        TanktypeId = tt.Id,
                        TemplateId = tt.TemplateId.Value,
                        SectionName = tt.TankType
                    });
                }
            }

            return results
                .Where(r => templateId == 0 || r.TemplateId == templateId)
                .OrderBy(r => r.TemplateId)
                .ToList();
        }
    }
}
