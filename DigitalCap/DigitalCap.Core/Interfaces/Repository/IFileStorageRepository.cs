using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IFileStorageRepository
    {
        Task<IEnumerable<ProjectFile>> GetProjectFiles(int projectId);
        Task<IEnumerable<ProjectFile>> GetTaskFiles(int taskId);
        Task<ProjectFile> SaveProjectFile(ProjectFile file, string storageKey, string currentUser);
        Task<ProjectFile> SaveTaskFile(ProjectFile file, string storageKey, int taskId, string currentUser);
        Task DeleteFile(int fileId, string currentUser);
        Task<byte[]> GetFileContent(int id);
        Task<T> GetFile<T>(int id) where T : ProjectFile, new();
        Task MoveFile(int fileId, FileDataType fileDataType, string currentUser);
        Task UpdateFileMetadata(int id, FileDataType dataType, string name, bool availableToClient, string currentUser);
    }
}
