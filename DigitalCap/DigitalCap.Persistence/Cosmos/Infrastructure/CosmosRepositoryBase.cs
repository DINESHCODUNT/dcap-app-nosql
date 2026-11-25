using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    public abstract class CosmosRepositoryBase<T> where T : class
    {
        protected readonly ICosmosContainerFactory ContainerFactory;
        protected readonly string ContainerName;
        protected readonly ILogger Logger;
        protected readonly Container Container;

        protected CosmosRepositoryBase(ICosmosContainerFactory containerFactory, string containerName, ILogger logger)
        {
            ContainerFactory = containerFactory ?? throw new ArgumentNullException(nameof(containerFactory));
            ContainerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Container = containerFactory.GetContainer(containerName);
        }

        protected async Task<T?> ReadAsync(string id, PartitionKey pk, CancellationToken ct = default)
        {
            try
            {
                var response = await Container.ReadItemAsync<T>(id, pk, cancellationToken: ct);
                LogResponse(nameof(ReadAsync), response.RequestCharge, (int)response.StatusCode, response.ActivityId);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                LogResponse(nameof(ReadAsync), ex.RequestCharge, (int)ex.StatusCode, ex.ActivityId);
                return null;
            }
            catch (CosmosException ex)
            {
                LogException(nameof(ReadAsync), ex);
                throw;
            }
        }

        protected async Task<List<T>> QueryAsync(QueryDefinition q, PartitionKey? pk = null, CancellationToken ct = default)
        {
            var options = new QueryRequestOptions();
            if (pk.HasValue)
            {
                options.PartitionKey = pk.Value;
            }

            var items = new List<T>();
            using var feed = Container.GetItemQueryIterator<T>(q, requestOptions: options);
            try
            {
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync(ct);
                    LogResponse(nameof(QueryAsync), response.RequestCharge, (int)response.StatusCode, response.ActivityId);
                    items.AddRange(response);
                }
                return items;
            }
            catch (CosmosException ex)
            {
                LogException(nameof(QueryAsync), ex);
                throw;
            }
        }

        protected async Task<(List<T> Items, string? ContinuationToken)> QueryPageAsync(
            QueryDefinition q,
            PartitionKey? pk,
            int pageSize,
            string? continuationToken,
            CancellationToken ct = default)
        {
            var options = new QueryRequestOptions
            {
                MaxItemCount = pageSize
            };
            if (pk.HasValue)
            {
                options.PartitionKey = pk.Value;
            }

            using var feed = Container.GetItemQueryIterator<T>(q, continuationToken: continuationToken, requestOptions: options);
            try
            {
                if (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync(ct);
                    LogResponse(nameof(QueryPageAsync), response.RequestCharge, (int)response.StatusCode, response.ActivityId);
                    return (response.ToList(), response.ContinuationToken);
                }
                return (new List<T>(), null);
            }
            catch (CosmosException ex)
            {
                LogException(nameof(QueryPageAsync), ex);
                throw;
            }
        }

        protected async Task<T> CreateAsync(T item, PartitionKey pk, CancellationToken ct = default)
        {
            try
            {
                var response = await Container.CreateItemAsync(item, pk, cancellationToken: ct);
                LogResponse(nameof(CreateAsync), response.RequestCharge, (int)response.StatusCode, response.ActivityId);
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                LogException(nameof(CreateAsync), ex);
                throw;
            }
        }

        protected async Task<T> UpsertAsync(T item, PartitionKey pk, CancellationToken ct = default)
        {
            try
            {
                var response = await Container.UpsertItemAsync(item, pk, cancellationToken: ct);
                LogResponse(nameof(UpsertAsync), response.RequestCharge, (int)response.StatusCode, response.ActivityId);
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                LogException(nameof(UpsertAsync), ex);
                throw;
            }
        }

        protected async Task<T?> ReplaceAsync(T item, string id, PartitionKey pk, string? ifMatchEtag = null, CancellationToken ct = default)
        {
            var options = !string.IsNullOrEmpty(ifMatchEtag)
                ? new ItemRequestOptions { IfMatchEtag = ifMatchEtag }
                : null;

            try
            {
                var response = await Container.ReplaceItemAsync(item, id, pk, requestOptions: options, cancellationToken: ct);
                LogResponse(nameof(ReplaceAsync), response.RequestCharge, (int)response.StatusCode, response.ActivityId);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                LogResponse(nameof(ReplaceAsync), ex.RequestCharge, (int)ex.StatusCode, ex.ActivityId);
                return null;
            }
            catch (CosmosException ex)
            {
                LogException(nameof(ReplaceAsync), ex);
                throw;
            }
        }

        protected async Task<T?> PatchAsync(string id, PartitionKey pk, IReadOnlyList<PatchOperation> ops, CancellationToken ct = default)
        {
            try
            {
                var response = await Container.PatchItemAsync<T>(id, pk, ops, cancellationToken: ct);
                LogResponse(nameof(PatchAsync), response.RequestCharge, (int)response.StatusCode, response.ActivityId);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                LogResponse(nameof(PatchAsync), ex.RequestCharge, (int)ex.StatusCode, ex.ActivityId);
                return null;
            }
            catch (CosmosException ex)
            {
                LogException(nameof(PatchAsync), ex);
                throw;
            }
        }

        protected async Task<bool> DeleteAsync(string id, PartitionKey pk, CancellationToken ct = default)
        {
            try
            {
                var response = await Container.DeleteItemAsync<T>(id, pk, cancellationToken: ct);
                LogResponse(nameof(DeleteAsync), response.RequestCharge, (int)response.StatusCode, response.ActivityId);
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                LogResponse(nameof(DeleteAsync), ex.RequestCharge, (int)ex.StatusCode, ex.ActivityId);
                return false;
            }
            catch (CosmosException ex)
            {
                LogException(nameof(DeleteAsync), ex);
                throw;
            }
        }

        protected async Task<TransactionalBatchResponse> ExecuteBatchAsync(PartitionKey pk, Action<TransactionalBatch> build, CancellationToken ct = default)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));

            var batch = Container.CreateTransactionalBatch(pk);
            build(batch);

            try
            {
                var response = await batch.ExecuteAsync(ct);
                LogResponse(nameof(ExecuteBatchAsync), response.RequestCharge, (int)response.StatusCode, response.ActivityId);
                if (!response.IsSuccessStatusCode)
                {
                    throw new CosmosException(
                        $"Transactional batch in container '{ContainerName}' failed with status code {response.StatusCode}.",
                        response.StatusCode,
                        0,
                        response.ActivityId,
                        response.RequestCharge);
                }
                return response;
            }
            catch (CosmosException ex)
            {
                LogException(nameof(ExecuteBatchAsync), ex);
                throw;
            }
        }

        private void LogResponse(string operation, double requestCharge, int statusCode, string activityId)
        {
            Logger.LogDebug("{Operation} in {Container}. RU: {RequestCharge}, Status: {StatusCode}, ActivityId: {ActivityId}",
                operation, ContainerName, requestCharge, statusCode, activityId);
        }

        private void LogException(string operation, CosmosException ex)
        {
            if ((int)ex.StatusCode == 429 || (int)ex.StatusCode == 503)
            {
                Logger.LogWarning(ex, "{Operation} in {Container} received transient status {StatusCode}. ActivityId: {ActivityId}, RU: {RequestCharge}",
                    operation, ContainerName, ex.StatusCode, ex.ActivityId, ex.RequestCharge);
            }
            else
            {
                Logger.LogError(ex, "{Operation} in {Container} failed with status {StatusCode}. ActivityId: {ActivityId}, RU: {RequestCharge}",
                    operation, ContainerName, ex.StatusCode, ex.ActivityId, ex.RequestCharge);
            }
        }
    }
}
