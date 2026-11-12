using DigitalCap.Core.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IBlobStorageRepository
    {
        Task<string> SaveFile(IFormFile file, string id = null);
        Task<string> GetFileDownloadUrl(string id);
        Task DeleteFile(string id);
        Task<byte[]> GetContent(string id);
    }
}
