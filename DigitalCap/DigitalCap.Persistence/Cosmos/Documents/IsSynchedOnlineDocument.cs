using Newtonsoft.Json;
using System;

namespace DigitalCap.Persistence.Cosmos.Documents
{
    // Exactly the columns of dbo.IsSynchedOnline (no primary key: the copied "id" is the Id column).
    public class IsSynchedOnlineDocument
    {
        [JsonProperty("id")]
        public string id { get; set; } = string.Empty;

        [JsonProperty("Id")]
        public int? Id { get; set; }

        [JsonProperty("ProjectId")]
        public int? ProjectId { get; set; }

        [JsonProperty("DownloadOfflineDate")]
        public DateTime? DownloadOfflineDate { get; set; }

        [JsonProperty("SynchedDate")]
        public DateTime? SynchedDate { get; set; }

        [JsonProperty("IsSynched")]
        public bool? IsSynched { get; set; }

        [JsonProperty("IsReadOnly")]
        public bool? IsReadOnly { get; set; }

        [JsonProperty("UserId")]
        public string? UserId { get; set; }

        [JsonProperty("EmailId")]
        public string? EmailId { get; set; }
    }
}
