using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IFileStorageService
    {
        Task<IEnumerable<ProjectFile>> GetProjectFiles(int projectId);
        Task<IEnumerable<ProjectFile>> GetTaskFiles(int taskId);
        Task<ProjectFile> SaveProjectFile(IFormFile uploadedFile, FileDataType dataType, int projectId, string currentUser);
        Task<ProjectFile> SaveTaskFile(IFormFile uploadedFile, FileDataType dataType, int projectId, int taskId, string currentUser);
        Task DeleteFile(int fileId, string currentUser);
        Task<string> GetFileDownloadUrl(int fileId);
        Task<T> GetFile<T>(int fileId) where T : ProjectFile, new();
        Task MoveFile(int fileId, FileDataType fileDataType, string currentUser);
        Task UpdateFileMetadata(int id, FileDataType dataType, string name, bool availableToClient, string currentUser);
    }
}
