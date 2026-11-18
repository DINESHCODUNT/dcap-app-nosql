using System.Collections.Generic;

namespace DigitalCap.Persistence.Cosmos.Configuration
{
    public sealed class DataProviderOptions
    {
        public const string SectionName = "DataProvider";
        public string Default { get; set; } = "Sql";            // "Sql" or "Cosmos"
        public Dictionary<string, string> Overrides { get; set; } = new();   // NOT used (global switch only)
    }
}
