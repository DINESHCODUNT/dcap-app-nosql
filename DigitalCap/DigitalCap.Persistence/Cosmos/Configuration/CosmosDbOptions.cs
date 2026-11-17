namespace DigitalCap.Persistence.Cosmos.Configuration
{
    public class CosmosDbOptions
    {
        public const string SectionName = "CosmosDb";

        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = "DigitalCap";
        public bool UseEmulator { get; set; } = false;
        public bool UseManagedIdentity { get; set; } = false;
        public int MaxRetryAttemptsOnRateLimitedRequests { get; set; } = 9;
        public int MaxRetryWaitTimeInSeconds { get; set; } = 30;
        public string? ConsistencyLevel { get; set; }
    }
}
