using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models.ReportConfig;
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
    public class CosmosProjectReportRepository : CosmosRepositoryBase<GenericImageCardDocument>, IProjectReportRepository
    {
        private static readonly DateTime SqlMinDate = new DateTime(1753, 1, 1);
        private readonly ISequenceService _sequenceService;

        public CosmosProjectReportRepository(
            ICosmosContainerFactory containerFactory,
            ISequenceService sequenceService,
            ILogger<CosmosProjectReportRepository> logger)
            : base(containerFactory, CosmosContainers.dbo_GenericImageCard, logger)
        {
            _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
        }

        public void Commit() { }

        public async Task<GenericImageCard> GetGenericImagCardByName(int projectId, int templateId, Guid sectionId, int cardNumber)
        {
            try
            {
                var result = await Sp_GetGenericImagecardByName(projectId, templateId, sectionId, cardNumber);
                return result.ToList().FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdateGenericImageCard(GenericImageCard model)
        {
            try
            {
                return await Sp_UpdateGenericImageCard(model);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CreateGenericImageCard(GenericImageCard model)
        {
            try
            {
                return await Sp_CreateGenericImageCard(model);
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ProjectPart>> GetProjectTemplatebyProjectId(int projectId)
        {
            var result = await Sp_GetReportPartsByProjectId(projectId);
            return result.ToList();
        }

        #region Stored Procedure Helpers

        private async Task<List<GenericImageCard>> Sp_GetGenericImagecardByName(int projectId, int templateId, Guid sectionId, int cardNumber)
        {
            // WHERE [ProjectId]=@ProjectId and [TemplateId]=@TemplateId and [SectionId]=@SectionId and [CardNumber]=@CardNumber and IsActive=1 and (IsDeleted=0 or IsDeleted is null)
            var q = new QueryDefinition(
                "SELECT * FROM c WHERE c.ProjectId = @projectId AND c.TemplateId = @templateId AND c.CardNumber = @cardNumber AND c.IsActive = true AND (c.IsDeleted = false OR IS_NULL(c.IsDeleted) OR NOT IS_DEFINED(c.IsDeleted))")
                .WithParameter("@projectId", projectId)
                .WithParameter("@templateId", templateId)
                .WithParameter("@cardNumber", cardNumber);

            var docs = await QueryAsync(q, new PartitionKey((double)projectId));
            var secStr = sectionId.ToString();
            var matched = docs.Where(d => string.Equals(d.SectionId, secStr, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.Id)
                .Select(MapToModel)
                .ToList();

            return matched;
        }

        private async Task<bool> Sp_UpdateGenericImageCard(GenericImageCard model)
        {
            if (model.CreatedDttm < SqlMinDate || model.UpdatedDttm < SqlMinDate)
            {
                throw new ArgumentOutOfRangeException(model.CreatedDttm < SqlMinDate ? nameof(model.CreatedDttm) : nameof(model.UpdatedDttm), "SqlDateTime overflow");
            }

            GenericImageCardDocument? existing = null;
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
            existing.ProjectId = model.ProjectId;
            existing.TemplateId = model.TemplateId;
            existing.SectionId = model.SectionId.ToString();
            existing.CardNumber = model.CardNumber;
            existing.DescriptionId = model.DescriptionId;
            existing.AdditionalDescription = model.AdditionalDescription;
            existing.CurrentCondition = model.CurrentCondition;
            existing.ImageId = model.ImageId;
            existing.CreatedDttm = model.CreatedDttm;
            existing.UpdatedDttm = model.UpdatedDttm;
            existing.IsActive = model.IsActive;
            existing.IsDeleted = model.IsDeleted;
            existing.CardName = model.CardName;

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

        private async Task<bool> Sp_CreateGenericImageCard(GenericImageCard model)
        {
            if (model.CreatedDttm < SqlMinDate || model.UpdatedDttm < SqlMinDate)
            {
                throw new ArgumentOutOfRangeException(model.CreatedDttm < SqlMinDate ? nameof(model.CreatedDttm) : nameof(model.UpdatedDttm), "SqlDateTime overflow");
            }

            var newId = await _sequenceService.GetNextAsync(CosmosContainers.dbo_GenericImageCard);
            var doc = new GenericImageCardDocument
            {
                id = newId.ToString(),
                Id = newId,
                ProjectId = model.ProjectId,
                TemplateId = model.TemplateId,
                SectionId = model.SectionId.ToString(),
                CardNumber = model.CardNumber,
                DescriptionId = model.DescriptionId,
                AdditionalDescription = model.AdditionalDescription,
                CurrentCondition = model.CurrentCondition,
                ImageId = model.ImageId,
                CreatedDttm = model.CreatedDttm,
                UpdatedDttm = model.UpdatedDttm,
                IsActive = model.IsActive,
                IsDeleted = model.IsDeleted,
                CardName = model.CardName
            };

            PartitionKey pk = new PartitionKey((double)model.ProjectId);
            await CreateAsync(doc, pk);
            return true;
        }

        private async Task<List<ProjectPart>> Sp_GetReportPartsByProjectId(int projectId)
        {
            var reportPartContainer = ContainerFactory.GetContainer(CosmosContainers.Config_ReportPart);
            var q = new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @pid")
                .WithParameter("@pid", projectId);

            var items = new List<(int PartId, string? PartName, int? SequenceNo)>();
            using (var feed = reportPartContainer.GetItemQueryIterator<ReportPartDocument>(q, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey((double)projectId) }))
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var doc in response)
                    {
                        items.Add((doc.PartId, doc.PartName, doc.SequenceNo));
                    }
                }
            }

            items.Add((1, "Vessel Main Data", 0));
            items.Add((2, "Part H&I", 20));

            // In SQL Server, ORDER BY SequenceNo places NULL first
            var ordered = items
                .OrderBy(p => p.SequenceNo.HasValue)
                .ThenBy(p => p.SequenceNo)
                .Select(p => new ProjectPart
                {
                    PartId = p.PartId,
                    PartName = p.PartName,
                    SequenceNo = p.SequenceNo ?? 0
                })
                .ToList();

            return ordered;
        }

        private static GenericImageCard MapToModel(GenericImageCardDocument doc)
        {
            return new GenericImageCard
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
                CreatedDttm = doc.CreatedDttm ?? DateTime.MinValue,
                UpdatedDttm = doc.UpdatedDttm ?? DateTime.MinValue,
                IsActive = doc.IsActive ?? false,
                CardName = doc.CardName
            };
        }

        #endregion
    }
}
