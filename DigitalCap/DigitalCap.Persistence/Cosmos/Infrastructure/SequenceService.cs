using DigitalCap.Core.Interfaces.Service;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    public class SequenceService : ISequenceService
    {
        private readonly Container _container;
        private readonly ILogger<SequenceService> _logger;
        private const int MaxRetries = 5;

        public SequenceService(ICosmosContainerFactory containerFactory, ILogger<SequenceService> logger)
        {
            if (containerFactory == null) throw new ArgumentNullException(nameof(containerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _container = containerFactory.GetContainer(CosmosContainers.sys_Counters);
        }

        public async Task<int> GetNextAsync(string containerName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));
            }

            var partitionKey = new PartitionKey(containerName);

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    ItemResponse<CounterDocument> response;
                    try
                    {
                        response = await _container.ReadItemAsync<CounterDocument>(
                            containerName,
                            partitionKey,
                            cancellationToken: ct);
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        // If document does not exist, create it with Current = 1
                        try
                        {
                            var newDoc = new CounterDocument
                            {
                                Id = containerName,
                                Current = 1
                            };
                            await _container.CreateItemAsync(newDoc, partitionKey, cancellationToken: ct);
                            return 1;
                        }
                        catch (CosmosException createEx) when (IsTransient(createEx))
                        {
                            if (attempt == MaxRetries) throw;
                            _logger.LogWarning(createEx, "Transient CosmosException ({StatusCode}) creating counter '{ContainerName}' on attempt {Attempt}. Retrying...",
                                createEx.StatusCode, containerName, attempt);
                            await Task.Delay(50 * attempt, ct);
                            continue;
                        }
                        catch (CosmosException createEx)
                        {
                            _logger.LogError(createEx, "Non-transient CosmosException ({StatusCode}) creating counter '{ContainerName}'.",
                                createEx.StatusCode, containerName);
                            throw;
                        }
                    }

                    var doc = response.Resource;
                    string etag = response.ETag;
                    int nextVal = doc.Current + 1;
                    doc.Current = nextVal;

                    var replaceOptions = new ItemRequestOptions
                    {
                        IfMatchEtag = etag
                    };

                    await _container.ReplaceItemAsync(
                        doc,
                        containerName,
                        partitionKey,
                        requestOptions: replaceOptions,
                        cancellationToken: ct);

                    return nextVal;
                }
                catch (CosmosException ex) when (IsTransient(ex))
                {
                    if (attempt == MaxRetries) throw;
                    _logger.LogWarning(ex, "Transient CosmosException ({StatusCode}) updating counter '{ContainerName}' on attempt {Attempt}. Retrying...",
                        ex.StatusCode, containerName, attempt);
                    await Task.Delay(50 * attempt, ct);
                }
                catch (CosmosException ex)
                {
                    _logger.LogError(ex, "Non-transient CosmosException ({StatusCode}) for counter '{ContainerName}'.",
                        ex.StatusCode, containerName);
                    throw;
                }
            }

            throw new InvalidOperationException($"Failed to acquire next sequence number for container '{containerName}' after {MaxRetries} retries due to concurrency conflicts.");
        }

        private static bool IsTransient(CosmosException ex)
        {
            return ex.StatusCode == HttpStatusCode.Conflict // 409
                || ex.StatusCode == HttpStatusCode.PreconditionFailed // 412
                || (int)ex.StatusCode == 429 // TooManyRequests
                || ex.StatusCode == HttpStatusCode.ServiceUnavailable; // 503
        }

        private class CounterDocument
        {
            [Newtonsoft.Json.JsonProperty("id")]
            public string Id { get; set; } = string.Empty;

            [Newtonsoft.Json.JsonProperty("Current")]
            public int Current { get; set; }
        }
    }
}
