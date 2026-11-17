using Azure.Identity;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Persistence.Cosmos.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;

namespace DigitalCap.Persistence.Cosmos.Infrastructure
{
    public static class CosmosServiceExtensions
    {
        public static IServiceCollection AddCosmosInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CosmosDbOptions>(configuration.GetSection(CosmosDbOptions.SectionName));

            services.AddSingleton<CosmosClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

                if (string.IsNullOrWhiteSpace(options.Endpoint))
                {
                    throw new InvalidOperationException("Cosmos DB configuration error: 'CosmosDb:Endpoint' is required but not configured.");
                }

                if (!options.UseManagedIdentity && string.IsNullOrWhiteSpace(options.Key))
                {
                    throw new InvalidOperationException("Cosmos DB configuration error: 'CosmosDb:Key' is required when UseManagedIdentity is false.");
                }

                if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var uri))
                {
                    throw new InvalidOperationException($"Cosmos DB configuration error: 'CosmosDb:Endpoint' '{options.Endpoint}' is not a valid absolute URI.");
                }

                var clientOptions = new CosmosClientOptions
                {
                    ApplicationName = "DigitalCap",
                    ConnectionMode = options.UseEmulator ? ConnectionMode.Gateway : ConnectionMode.Direct,
                    LimitToEndpoint = options.UseEmulator,
                    MaxRetryAttemptsOnRateLimitedRequests = options.MaxRetryAttemptsOnRateLimitedRequests,
                    MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(options.MaxRetryWaitTimeInSeconds),
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.Default,
                        IgnoreNullValues = false
                    }
                };

                if (!string.IsNullOrWhiteSpace(options.ConsistencyLevel) &&
                    Enum.TryParse<ConsistencyLevel>(options.ConsistencyLevel, true, out var consistencyLevel))
                {
                    clientOptions.ConsistencyLevel = consistencyLevel;
                }

                if (options.UseEmulator &&
                    (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
                {
                    clientOptions.HttpClientFactory = () =>
                    {
                        var handler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                        };
                        return new HttpClient(handler);
                    };
                }

                if (options.UseManagedIdentity)
                {
                    return new CosmosClient(options.Endpoint, new DefaultAzureCredential(), clientOptions);
                }

                return new CosmosClient(options.Endpoint, options.Key, clientOptions);
            });

            services.AddSingleton<ICosmosContainerFactory, CosmosContainerFactory>();
            services.AddScoped<ISequenceService, SequenceService>();

            return services;
        }
    }
}
