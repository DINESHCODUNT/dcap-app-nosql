using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Service
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IFileStorageRepository _fileStorageRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public FileStorageService(
            IFileStorageRepository fileStorageRepository,
            IBlobStorageRepository blobStorageRepository,
            IUnitOfWork unitOfWork,
            IEmailService emailService)
        {
            _fileStorageRepository = fileStorageRepository;
            _blobStorageRepository = blobStorageRepository;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<IEnumerable<ProjectFile>> GetProjectFiles(int projectId)
        {
            return await _fileStorageRepository.GetProjectFiles(projectId);
        }

        public async Task<IEnumerable<ProjectFile>> GetTaskFiles(int taskId)
        {
            return await _fileStorageRepository.GetTaskFiles(taskId);
        }

        public async Task<ProjectFile> SaveProjectFile(IFormFile uploadedFile, FileDataType dataType, int projectId, string currentUser)
        {
            if (uploadedFile == null)
                throw new ArgumentNullException(nameof(uploadedFile));

            var file = new ProjectFile(uploadedFile)
            {
                ProjectId = projectId,
                DataType = dataType
            };

            var storageKey = await _blobStorageRepository.SaveFile(uploadedFile);
            var savedFile = await _fileStorageRepository.SaveProjectFile(file, storageKey, currentUser);
            return savedFile;
        }

        public async Task<ProjectFile> SaveTaskFile(IFormFile uploadedFile, FileDataType dataType, int projectId, int taskId, string currentUser)
        {
            if (uploadedFile == null)
                throw new ArgumentNullException(nameof(uploadedFile));

            var file = new ProjectFile(uploadedFile)
            {
                ProjectId = projectId,
                DataType = dataType
            };

            var storageKey = await _blobStorageRepository.SaveFile(uploadedFile);
            var savedFile = await _fileStorageRepository.SaveTaskFile(file, storageKey, taskId, currentUser);
            return savedFile;
        }

        public async Task DeleteFile(int fileId, string currentUser)
        {
            var file = await _fileStorageRepository.GetFile<ProjectFile>(fileId);

            if (file != null && !string.IsNullOrEmpty(file.StorageKey))
            {
                await _blobStorageRepository.DeleteFile(file.StorageKey);
            }

            await _fileStorageRepository.DeleteFile(fileId, currentUser);
        }

        public async Task<string> GetFileDownloadUrl(int fileId)
        {
            var file = await _fileStorageRepository.GetFile<ProjectFile>(fileId);
            if (file == null || string.IsNullOrEmpty(file.StorageKey))
                return null;

            return await _blobStorageRepository.GetFileDownloadUrl(file.StorageKey);
        }

        public async Task<T> GetFile<T>(int fileId) where T : ProjectFile, new()
        {
            return await _fileStorageRepository.GetFile<T>(fileId);
        }

        public async Task MoveFile(int fileId, FileDataType fileDataType, string currentUser)
        {
            await _fileStorageRepository.MoveFile(fileId, fileDataType, currentUser);
        }

        public async Task UpdateFileMetadata(int id, FileDataType dataType, string name, bool availableToClient, string currentUser)
        {
            await _fileStorageRepository.UpdateFileMetadata(id, dataType, name, availableToClient, currentUser);
        }
    }
}
