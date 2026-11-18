using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosFileStorageRepository : CosmosRepositoryBase<FileDocument>, IFileStorageRepository
    {
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly CosmosActivityWriter _activityWriter;
        private readonly ISequenceService _sequenceService;

        public CosmosFileStorageRepository(
            ICosmosContainerFactory containerFactory,
            IBlobStorageRepository blobStorageRepository,
            CosmosActivityWriter activityWriter,
            ISequenceService sequenceService,
            ILogger<CosmosFileStorageRepository> logger)
            : base(containerFactory, CosmosContainers.CAP_Files, logger)
        {
            _blobStorageRepository = blobStorageRepository ?? throw new ArgumentNullException(nameof(blobStorageRepository));
            _activityWriter = activityWriter;
            _sequenceService = sequenceService;
        }

        public void Commit() { }

        public async Task<IEnumerable<ProjectFile>> GetProjectFiles(int projectId)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.Projectid = @p AND NOT (c.SurveyImage = true)")
                    .WithParameter("@p", projectId);

                var items = await QueryAsync(query);
                return items.Select(MapToFileModel<ProjectFile>).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving project files for project ID {projectId}: {ex.Message}", ex);
            }
        }

        public async Task<T> GetFile<T>(int id) where T : ProjectFile, new()
        {
            try
            {
                var idStr = id.ToString();
                try
                {
                    var response = await Container.ReadItemAsync<FileDocument>(idStr, new PartitionKey(idStr));
                    if (response.Resource != null)
                    {
                        return MapToFileModel<T>(response.Resource);
                    }
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null!;
                }

                return null!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving file with ID {id}: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GetFileContent(int id)
        {
            var file = await GetFile<ProjectFile>(id);
            return await GetFileContent(file);
        }

        public async Task<byte[]> GetFileContent(ProjectFile file)
        {
            if (file == null)
            {
                return null!;
            }

            return await _blobStorageRepository.GetContent(file.StorageKey);
        }

        public async Task<IEnumerable<ProjectFile>> GetTaskFiles(int taskId)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.TaskId = @taskId")
                    .WithParameter("@taskId", taskId);

                var items = await QueryAsync(query);
                return items.Select(MapToFileModel<ProjectFile>).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving task files for task ID {taskId}: {ex.Message}", ex);
            }
        }

        public async Task<ProjectFile> SaveProjectFile(ProjectFile file, string storageKey, string currentUser)
        {
            try
            {
                file.StorageKey = storageKey;
                file.UploadedBy = currentUser;
                file.LastModifiedBy = currentUser;
                file.LastModifiedDate = DateTime.UtcNow;

                var newId = await _sequenceService.GetNextAsync(CosmosContainers.CAP_Files);
                file.Id = newId;

                var cutName = file.Name != null && file.Name.Length > 500 ? file.Name[..500] : file.Name;
                var cutExt = file.Extension != null && file.Extension.Length > 25 ? file.Extension[..25] : file.Extension;
                var cutMime = file.MimeType != null && file.MimeType.Length > 150 ? file.MimeType[..150] : file.MimeType;
                var cutStorage = storageKey != null && storageKey.Length > 50 ? storageKey[..50] : storageKey;
                var cutUser = currentUser != null && currentUser.Length > 150 ? currentUser[..150] : currentUser;

                var doc = new FileDocument
                {
                    id = newId.ToString(),
                    Id = newId,
                    Projectid = file.ProjectId,
                    TaskId = file.TaskId,
                    DataType = (int)file.DataType,
                    Name = cutName,
                    Extension = cutExt,
                    MimeType = cutMime,
                    StorageKey = cutStorage,
                    UploadedBy = cutUser,
                    LastModifiedBy = cutUser,
                    LastModifiedDate = DateTime.UtcNow,
                    AvailableToClient = null,
                    SurveyImage = file.SurveyImage
                };

                await CreateAsync(doc, new PartitionKey(doc.id));

                var actText = "Uploaded file " + cutName;
                if (actText.Length > 500) actText = actText[..500];
                await _activityWriter.CreateActivityAsync(file.ProjectId, actText, currentUser);

                return file;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving project file: {ex.Message}", ex);
            }
        }

        public async Task<ProjectFile> SaveTaskFile(ProjectFile file, string storageKey, int taskId, string currentUser)
        {
            try
            {
                file.StorageKey = storageKey;
                file.TaskId = taskId;
                file.UploadedBy = currentUser;
                file.LastModifiedBy = currentUser;
                file.LastModifiedDate = DateTime.UtcNow;

                var newId = await _sequenceService.GetNextAsync(CosmosContainers.CAP_Files);
                file.Id = newId;

                var cutName = file.Name != null && file.Name.Length > 500 ? file.Name[..500] : file.Name;
                var cutExt = file.Extension != null && file.Extension.Length > 25 ? file.Extension[..25] : file.Extension;
                var cutMime = file.MimeType != null && file.MimeType.Length > 150 ? file.MimeType[..150] : file.MimeType;
                var cutStorage = storageKey != null && storageKey.Length > 50 ? storageKey[..50] : storageKey;
                var cutUser = currentUser != null && currentUser.Length > 150 ? currentUser[..150] : currentUser;

                var doc = new FileDocument
                {
                    id = newId.ToString(),
                    Id = newId,
                    Projectid = file.ProjectId,
                    TaskId = taskId,
                    DataType = (int)file.DataType,
                    Name = cutName,
                    Extension = cutExt,
                    MimeType = cutMime,
                    StorageKey = cutStorage,
                    UploadedBy = cutUser,
                    LastModifiedBy = cutUser,
                    LastModifiedDate = DateTime.UtcNow,
                    AvailableToClient = null,
                    SurveyImage = file.SurveyImage
                };

                await CreateAsync(doc, new PartitionKey(doc.id));

                var actText = "Uploaded file " + cutName;
                if (actText.Length > 500) actText = actText[..500];
                await _activityWriter.CreateActivityAsync(file.ProjectId, actText, currentUser);

                return file;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving task file: {ex.Message}", ex);
            }
        }

        public async Task DeleteFile(int fileId, string currentUser)
        {
            if (string.IsNullOrWhiteSpace(currentUser))
            {
                throw new ArgumentException("Current user cannot be null or empty.", nameof(currentUser));
            }

            try
            {
                var idStr = fileId.ToString();
                FileDocument? file = null;
                try
                {
                    var response = await Container.ReadItemAsync<FileDocument>(idStr, new PartitionKey(idStr));
                    file = response.Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                }

                if (file != null)
                {
                    await DeleteAsync(idStr, new PartitionKey(idStr));

                    var actText = "Deleted file " + file.Name;
                    if (actText.Length > 500) actText = actText[..500];
                    await _activityWriter.CreateActivityAsync(file.Projectid, actText, currentUser);
                }
                else
                {
                    // SQL SP CAP.Delete_File writes activity with ProjectId null and null text when file is missing
                    await _activityWriter.CreateActivityAsync(null, null, currentUser);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting file with ID {fileId}: {ex.Message}", ex);
            }
        }

        public async Task UpdateFileMetadata(int id, FileDataType dataType, string name, bool availableToClient, string currentUser)
        {
            var file = await GetFile<ProjectFile>(id);
            if (file == null)
            {
                throw new ApplicationException($"Could not find file (id: {id})");
            }

            try
            {
                var user = currentUser ?? "SYSTEM";
                var actText = "Updated file " + name;
                if (actText.Length > 500) actText = actText[..500];

                // StorageKey: column is 50, parameter is 150. A value longer than 50 makes SQL UPDATE fail,
                // but statement error continues the SP, so activity is still written, then caller gets truncation error.
                if (file.StorageKey != null && file.StorageKey.Length > 50)
                {
                    await _activityWriter.CreateActivityAsync(file.ProjectId, actText, user);
                    throw new InvalidOperationException($"Error updating file metadata for ID {id}: String or binary data would be truncated.\r\nThe statement has been terminated.");
                }

                var cutName = name != null && name.Length > 500 ? name[..500] : name;
                var cutExt = file.Extension != null && file.Extension.Length > 25 ? file.Extension[..25] : file.Extension;
                var cutMime = file.MimeType != null && file.MimeType.Length > 50 ? file.MimeType[..50] : file.MimeType;
                var cutStorage = file.StorageKey != null && file.StorageKey.Length > 50 ? file.StorageKey[..50] : file.StorageKey;
                var cutLastModBy = user.Length > 150 ? user[..150] : user;

                var idStr = id.ToString();
                var pk = new PartitionKey(idStr);

                var docResponse = await Container.ReadItemAsync<FileDocument>(idStr, pk);
                var doc = docResponse.Resource;

                doc.DataType = (int)dataType;
                doc.Name = cutName;
                doc.Extension = cutExt;
                doc.MimeType = cutMime;
                doc.StorageKey = cutStorage;
                doc.AvailableToClient = availableToClient;
                doc.LastModifiedBy = cutLastModBy;
                // Note: LastModifiedDate is NOT changed in SQL CAP.Update_File

                await ReplaceAsync(doc, idStr, pk);
                await _activityWriter.CreateActivityAsync(file.ProjectId, actText, user);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating file metadata for ID {id}: {ex.Message}", ex);
            }
        }

        public Task MoveFile(int fileId, FileDataType fileDataType, string currentUser)
        {
            throw new NotSupportedException("CAP.Move_File does not exist in the database and is not used.");
        }

        private static T MapToFileModel<T>(FileDocument doc) where T : ProjectFile, new()
        {
            return new T
            {
                Id = doc.Id,
                ProjectId = doc.Projectid ?? 0,
                TaskId = doc.TaskId,
                DataType = (FileDataType)(doc.DataType ?? 0),
                Name = doc.Name!,
                Extension = doc.Extension!,
                MimeType = doc.MimeType!,
                StorageKey = doc.StorageKey!,
                UploadedBy = doc.UploadedBy!,
                LastModifiedBy = doc.LastModifiedBy!,
                LastModifiedDate = doc.LastModifiedDate ?? default,
                AvailableToClient = doc.AvailableToClient ?? false,
                SurveyImage = doc.SurveyImage ?? false
            };
        }
    }
}
