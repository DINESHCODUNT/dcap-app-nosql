using DigitalCap.Core.Configuration;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Repositories
{
    public class BlobStorageRepository : IBlobStorageRepository
    {
        private readonly TimeSpan _shareValidityDuration;
        protected CloudBlobContainer BlobContainer { get; private set; }

        public BlobStorageRepository(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            // SAS link lifetime for GetFileDownloadUrl (override via Blob:ShareValidityHours if needed)
            var shareHours = configuration["Blob:ShareValidityHours"];
            _shareValidityDuration = int.TryParse(shareHours, out var h) && h > 0
                ? TimeSpan.FromHours(h)
                : TimeSpan.FromHours(1);

            var connectionString = configuration.GetBlobStorageConnectionString();
            var containerName = configuration.GetBlobStorageContainerName();

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Blob storage is not configured: set 'Blob:ConnectionString' in appsettings (see ConfigurationConstants.ConfigurationKeys.BlobStorage).");
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new InvalidOperationException(
                    "Blob storage is not configured: set 'Blob:ContainerName' in appsettings.");
            }

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            BlobContainer = blobClient.GetContainerReference(containerName);
        }

        public async Task<string> SaveFile(IFormFile file, string id = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                id = Guid.NewGuid().ToString();

            await BlobContainer.CreateIfNotExistsAsync();

            var blockBlob = BlobContainer.GetBlockBlobReference(id);
            blockBlob.Properties.ContentDisposition = file.ContentDisposition;

            await using var fileStream = file.OpenReadStream();
            await blockBlob.UploadFromStreamAsync(fileStream);

            return id;
        }
        public async Task<string> GetFileDownloadUrl(string id)
        {
            var blockBlob = BlobContainer.GetBlockBlobReference(id);

            if (!await blockBlob.ExistsAsync())
                return null;

            var token = blockBlob.GetSharedAccessSignature(
                new SharedAccessBlobPolicy
                {
                    SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-1),
                    SharedAccessExpiryTime = DateTime.UtcNow.Add(_shareValidityDuration),
                    Permissions = SharedAccessBlobPermissions.Read
                });

            return blockBlob.Uri + token;
        }
        public async Task DeleteFile(string id)
        {
            var blockBlob = BlobContainer.GetBlockBlobReference(id);
            await blockBlob.DeleteIfExistsAsync();
        }

        public async Task<byte[]> GetContent(string id)
        {
            var blockBlob = BlobContainer.GetBlockBlobReference(id);

            if (!await blockBlob.ExistsAsync())
                return Array.Empty<byte>();

            await blockBlob.FetchAttributesAsync();

            var content = new byte[blockBlob.Properties.Length];
            await blockBlob.DownloadToByteArrayAsync(content, 0);

            return content;
        }
    }
}
