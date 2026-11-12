using Dapper;
using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Transactions;

namespace DigitalCap.Persistence.Repositories
{
    public class FileStorageRepository : IFileStorageRepository
    {
        private readonly IBlobStorageRepository _blobStorageRepository;
        protected readonly IUnitOfWork _unitOfWork;

        public FileStorageRepository(IUnitOfWork unitOfWork, IBlobStorageRepository blobStorageRepository)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _blobStorageRepository = blobStorageRepository ?? throw new ArgumentNullException(nameof(blobStorageRepository));
        }

        public void Commit()
        {
            _unitOfWork?.Commit();
        }

        public async Task<IEnumerable<ProjectFile>> GetProjectFiles(int projectId)
        {
            // Capture connection at method start
            var connection = _unitOfWork?.Connection;
            if (connection == null)
                throw new InvalidOperationException("Database connection is not initialized.");

            // Ensure connection is open
            if (connection.State != ConnectionState.Open)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                    throw new InvalidOperationException($"Database connection is in an invalid state: {connection.State}");
            }

            // Get transaction - ensure it exists
            var transaction = _unitOfWork?.Transaction;

            // If transaction is null or disposed, start a new one
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;

                if (transaction == null)
                {
                    throw new InvalidOperationException("Failed to start a new transaction for read operation.");
                }
            }

            try
            {
                var result = await connection.QueryAsync<ProjectFile>(
                    sql: "CAP.Read_Files_ByProjectId",
                    param: new
                    {
                        ProjectId = projectId
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving project files for project ID {projectId}: {ex.Message}", ex);
            }
        }

        public async Task DeleteFile(int fileId, string currentUser)
        {
            if (string.IsNullOrWhiteSpace(currentUser))
                throw new ArgumentException("Current user cannot be null or empty.", nameof(currentUser));

            // Capture connection and transaction
            var connection = _unitOfWork?.Connection;
            if (connection == null)
                throw new InvalidOperationException("Database connection is not initialized.");

            // Ensure connection is open
            if (connection.State != ConnectionState.Open)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                    throw new InvalidOperationException($"Database connection is in an invalid state: {connection.State}");
            }

            // Get transaction - ensure it exists
            var transaction = _unitOfWork?.Transaction;

            // If transaction is null or disposed, start a new one
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;

                if (transaction == null)
                {
                    throw new InvalidOperationException("Failed to start a new transaction for delete operation.");
                }
            }

            try
            {
                await connection.ExecuteAsync(
                    sql: "CAP.Delete_File",
                    param: new
                    {
                        Id = fileId,
                        User = currentUser
                    },
                    commandType: CommandType.StoredProcedure,
                    transaction: transaction,
                    commandTimeout: 30
                );
                this.Commit();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting file with ID {fileId}: {ex.Message}", ex);
            }
        }

        public async Task<T> GetFile<T>(int id)
            where T : ProjectFile, new()
        {
            // Capture connection at method start
            var connection = _unitOfWork?.Connection;
            if (connection == null)
                throw new InvalidOperationException("Database connection is not initialized.");

            // Ensure connection is open
            if (connection.State != ConnectionState.Open)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                    throw new InvalidOperationException($"Database connection is in an invalid state: {connection.State}");
            }

            // Get transaction - ensure it exists
            var transaction = _unitOfWork?.Transaction;

            // If transaction is null or disposed, start a new one
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;

                if (transaction == null)
                {
                    throw new InvalidOperationException("Failed to start a new transaction for read operation.");
                }
            }

            try
            {
                var file = (await connection.QueryAsync<T>(
                    "CAP.Read_File_ById",
                    new { Id = id },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                )).SingleOrDefault();

                return file;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving file with ID {id}: {ex.Message}", ex);
            }
        }
        public async Task<byte[]> GetFileContent(int id)
        {
            var file = await GetFile<ProjectFile>(id);
            var content = await GetFileContent(file);

            return content;
        }

        public async Task<byte[]> GetFileContent(ProjectFile file)
        {
            if (file == null)
                return null;

            var content = await _blobStorageRepository.GetContent(file.StorageKey);

            return content;
        }

        public async Task<IEnumerable<ProjectFile>> GetTaskFiles(int taskId)
        {
            var connection = _unitOfWork?.Connection;
            if (connection == null)
                throw new InvalidOperationException("Database connection is not initialized.");

            if (connection.State != ConnectionState.Open)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                    throw new InvalidOperationException($"Database connection is in an invalid state: {connection.State}");
            }

            var transaction = _unitOfWork?.Transaction;
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;
                if (transaction == null)
                    throw new InvalidOperationException("Failed to start a new transaction for read operation.");
            }

            try
            {
                var result = await connection.QueryAsync<ProjectFile>(
                    sql: "CAP.Read_Files_ByTaskId",
                    param: new { TaskId = taskId },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving task files for task ID {taskId}: {ex.Message}", ex);
            }
        }

        public async Task<ProjectFile> SaveProjectFile(ProjectFile file, string storageKey, string currentUser)
        {
            var connection = _unitOfWork?.Connection;
            if (connection == null)
                throw new InvalidOperationException("Database connection is not initialized.");

            if (connection.State != ConnectionState.Open)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                    throw new InvalidOperationException($"Database connection is in an invalid state: {connection.State}");
            }

            var transaction = _unitOfWork?.Transaction;
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;
                if (transaction == null)
                    throw new InvalidOperationException("Failed to start a new transaction for save operation.");
            }

            try
            {
                file.StorageKey = storageKey;
                file.UploadedBy = currentUser;
                file.LastModifiedBy = currentUser;
                file.LastModifiedDate = DateTime.UtcNow;

                // CAP.Create_File: @ProjectId, @TaskId, @DataType, @Name, @Extension, @MimeType, @StorageKey, @UploadedBy, @SurveyImage
                // Returns SELECT SCOPE_IDENTITY() — not a row; LastModifiedBy in DB is set from @UploadedBy in the proc.
                var identity = await connection.ExecuteScalarAsync<decimal?>(
                    sql: "CAP.Create_File",
                    param: new
                    {
                        ProjectId = file.ProjectId,
                        TaskId = file.TaskId,
                        DataType = (int)file.DataType,
                        Name = file.Name,
                        Extension = file.Extension,
                        MimeType = file.MimeType,
                        StorageKey = file.StorageKey,
                        UploadedBy = file.UploadedBy,
                        SurveyImage = file.SurveyImage
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);

                if (identity.HasValue)
                    file.Id = (int)identity.Value;

                Commit();
                return file;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving project file: {ex.Message}", ex);
            }
        }

        public async Task<ProjectFile> SaveTaskFile(ProjectFile file, string storageKey, int taskId, string currentUser)
        {
            var connection = _unitOfWork?.Connection;
            if (connection == null)
                throw new InvalidOperationException("Database connection is not initialized.");

            if (connection.State != ConnectionState.Open)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                    throw new InvalidOperationException($"Database connection is in an invalid state: {connection.State}");
            }

            var transaction = _unitOfWork?.Transaction;
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;
                if (transaction == null)
                    throw new InvalidOperationException("Failed to start a new transaction for save operation.");
            }

            try
            {
                file.StorageKey = storageKey;
                file.TaskId = taskId;
                file.UploadedBy = currentUser;
                file.LastModifiedBy = currentUser;
                file.LastModifiedDate = DateTime.UtcNow;

                var identity = await connection.ExecuteScalarAsync<decimal?>(
                    sql: "CAP.Create_File",
                    param: new
                    {
                        ProjectId = file.ProjectId,
                        TaskId = taskId,
                        DataType = (int)file.DataType,
                        Name = file.Name,
                        Extension = file.Extension,
                        MimeType = file.MimeType,
                        StorageKey = file.StorageKey,
                        UploadedBy = file.UploadedBy,
                        SurveyImage = file.SurveyImage
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);

                if (identity.HasValue)
                    file.Id = (int)identity.Value;

                Commit();
                return file;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving task file: {ex.Message}", ex);
            }
        }

        public async Task MoveFile(int fileId, FileDataType fileDataType, string currentUser)
        {
            var connection = _unitOfWork?.Connection;
            if (connection == null)
                throw new InvalidOperationException("Database connection is not initialized.");

            if (connection.State != ConnectionState.Open)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                    throw new InvalidOperationException($"Database connection is in an invalid state: {connection.State}");
            }

            var transaction = _unitOfWork?.Transaction;
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;
                if (transaction == null)
                    throw new InvalidOperationException("Failed to start a new transaction for move operation.");
            }

            try
            {
                await connection.ExecuteAsync(
                    sql: "CAP.Move_File",
                    param: new
                    {
                        Id = fileId,
                        DataType = (int)fileDataType,
                        User = currentUser
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                Commit();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error moving file with ID {fileId}: {ex.Message}", ex);
            }
        }

        public async Task UpdateFileMetadata(int id, FileDataType dataType, string name, bool availableToClient, string currentUser)
        {
            var file = await GetFile<ProjectFile>(id);
            if (file == null)
                throw new ApplicationException($"Could not find file (id: {id})");

            file.DataType = dataType;
            file.Name = name;
            file.AvailableToClient = availableToClient;
            file.LastModifiedBy = currentUser;

            var connection = _unitOfWork?.Connection;
            if (connection == null)
                throw new InvalidOperationException("Database connection is not initialized.");

            if (connection.State != ConnectionState.Open)
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                    throw new InvalidOperationException($"Database connection is in an invalid state: {connection.State}");
            }

            var transaction = _unitOfWork?.Transaction;
            if (transaction == null || transaction.Connection == null)
            {
                _unitOfWork?.Begin();
                transaction = _unitOfWork?.Transaction;
                if (transaction == null)
                    throw new InvalidOperationException("Failed to start a new transaction for update operation.");
            }

            try
            {
                await connection.ExecuteAsync(
                    sql: "CAP.Update_File",
                    param: new
                    {
                        Id = file.Id,
                        DataType = (int)file.DataType,
                        Name = file.Name,
                        Extension = file.Extension,
                        AvailableToClient = file.AvailableToClient,
                        MimeType = file.MimeType,
                        StorageKey = file.StorageKey,
                        LastModifiedBy = file.LastModifiedBy
                    },
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30
                );

                Commit();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error updating file metadata for ID {id}: {ex.Message}", ex);
            }
        }
    }
}
