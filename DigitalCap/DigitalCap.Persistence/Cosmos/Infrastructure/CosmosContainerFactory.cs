using DigitalCap.Persistence.Cosmos.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    public class CosmosContainerFactory : ICosmosContainerFactory
    {
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseName;
        private readonly ConcurrentDictionary<string, Container> _containers = new();

        public CosmosContainerFactory(CosmosClient cosmosClient, IOptions<CosmosDbOptions> options)
        {
            _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
            _databaseName = !string.IsNullOrWhiteSpace(options?.Value?.DatabaseName)
                ? options.Value.DatabaseName
                : "DigitalCap";
        }

        public Container GetContainer(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));
            }

            if (!CosmosContainers.PartitionKeys.ContainsKey(containerName))
            {
                throw new ArgumentException($"Container '{containerName}' is not defined in CosmosContainers.", nameof(containerName));
            }

            return _containers.GetOrAdd(containerName, name => _cosmosClient.GetContainer(_databaseName, name));
        }
    }
}
