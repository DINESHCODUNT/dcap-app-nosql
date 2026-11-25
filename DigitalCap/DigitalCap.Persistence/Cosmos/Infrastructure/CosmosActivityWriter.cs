using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Persistence.Cosmos.Documents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    public class CosmosActivityWriter
    {
        private readonly Container _container;
        private readonly ISequenceService _sequenceService;
        private readonly ILogger<CosmosActivityWriter> _logger;

        public CosmosActivityWriter(
            ICosmosContainerFactory containerFactory,
            ISequenceService sequenceService,
            ILogger<CosmosActivityWriter> logger)
        {
            if (containerFactory == null) throw new ArgumentNullException(nameof(containerFactory));
            _sequenceService = sequenceService ?? throw new ArgumentNullException(nameof(sequenceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _container = containerFactory.GetContainer(CosmosContainers.CAP_Activities);
        }

        public async Task<int> CreateActivityAsync(int? projectId, string? activity, string? createdBy, CancellationToken ct = default)
        {
            var text = activity != null && activity.Length > 3000 ? activity[..3000] : activity;
            var user = createdBy != null && createdBy.Length > 50 ? createdBy[..50] : createdBy;

            var id = await _sequenceService.GetNextAsync(CosmosContainers.CAP_Activities, ct);

            var doc = new ActivityDocument
            {
                id = id.ToString(),
                ID = id,
                Text = text,
                CreateBy = user,
                CreateDttm = DateTime.UtcNow,
                ProjectId = projectId,
                LinkText = null,
                LinkHref = null,
                Icon = null,
                IsDeleted = null,
                IsRead = null,
                ReadBy = null,
                ReadDttm = null
            };

            var pk = projectId.HasValue ? new PartitionKey((double)projectId.Value) : PartitionKey.Null;
            await _container.CreateItemAsync(doc, pk, cancellationToken: ct);

            return id;
        }
    }
}
