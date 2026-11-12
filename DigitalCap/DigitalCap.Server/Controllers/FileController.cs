using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Core.Security;
using DigitalCap.Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ApiBaseController
    {
        private readonly IProjectService _projectService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ISecurityService _securityService;
        private readonly IUserPermissionRepository _userPermissionRepository;

        public FileController(
            IProjectService projectService,
            IFileStorageService fileStorageService,
            ISecurityService securityService,
            IUserPermissionRepository userPermissionRepository)
        {
            _projectService = projectService ;
            _fileStorageService = fileStorageService ;
            _securityService = securityService ;
            _userPermissionRepository = userPermissionRepository;
        }

        [HttpGet("[action]")]
        public IActionResult GetDataTypes()
        {
            try
            {
                var result = FormValues.AdditionalFilesDataTypes.Select(x => new { Value = x.Value, Text = x.Text });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving data types", error = ex.Message });
            }
        }

        [HttpPost("[action]")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadProjectFile([FromForm] IFormFile uploadedFile, [FromForm] FileDataType dataType, [FromForm] int projectId)
        {
            try
            {
                if (uploadedFile == null)
                    return BadRequest(new { message = "File is required" });

                var currentUser = CurrentUsername.ToLower();

                if (string.IsNullOrEmpty(currentUser))
                    return Unauthorized(new { message = "User not authenticated" });

                var storedFile = await _fileStorageService.SaveProjectFile(uploadedFile, dataType, projectId, currentUser);

                return Ok(new { Id = storedFile.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error uploading project file", error = ex.Message });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UploadTaskFile([FromForm] IFormFile uploadedFile, [FromForm] FileDataType dataType, [FromForm] int projectId, [FromForm] int taskId)
        {
            try
            {
                if (uploadedFile == null)
                    return BadRequest(new { message = "File is required" });

                var currentUser = CurrentUsername.ToLower();

                if (string.IsNullOrEmpty(currentUser))
                    return Unauthorized(new { message = "User not authenticated" });

                var storedFile = await _fileStorageService.SaveTaskFile(uploadedFile, dataType, projectId, taskId, currentUser);

                return Ok(new { Id = storedFile.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error uploading task file", error = ex.Message });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Delete([FromQuery] int? fileId)
        {
            try
            {
                if (!fileId.HasValue)
                    return BadRequest(new { message = "File ID is required" });

                var currentUser = CurrentUsername.ToLower();
                if (string.IsNullOrEmpty(currentUser))
                    return Unauthorized(new { message = "User not authenticated" });

                await _fileStorageService.DeleteFile(fileId.Value, currentUser);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting file", error = ex.Message });
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetProjectFiles([FromQuery] int projectId)
        {
            try
            {
                var result = await _fileStorageService.GetProjectFiles(projectId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving project files", error = ex.Message });
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetTaskFiles([FromQuery] int taskId)
        {
            try
            {
                var result = await _fileStorageService.GetTaskFiles(taskId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving task files", error = ex.Message });
            }
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetDownloadUrl([FromQuery] int fileId)
        {
            try
            {
                var result = await _fileStorageService.GetFileDownloadUrl(fileId);
                if (string.IsNullOrEmpty(result))
                    return NotFound(new { message = "File not found" });

                return Ok(new { url = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving download URL", error = ex.Message });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateFileMetadata([FromQuery] string id, [FromQuery] string name, [FromQuery] string dataType, [FromQuery] string availableToClient)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || !int.TryParse(id, out var fileId))
                    return BadRequest(new { message = "Invalid file ID" });

                if (string.IsNullOrWhiteSpace(dataType) || !int.TryParse(dataType, out var dataTypeInt))
                    return BadRequest(new { message = "Invalid data type" });

                if (string.IsNullOrWhiteSpace(availableToClient) || !bool.TryParse(availableToClient, out var availableToClientBool))
                    return BadRequest(new { message = "Invalid available to client value" });

                var currentUser = CurrentUsername.ToLower(); ;
                if (string.IsNullOrEmpty(currentUser))
                    return Unauthorized(new { message = "User not authenticated" });

                await _fileStorageService.UpdateFileMetadata(fileId, (FileDataType)dataTypeInt, name, availableToClientBool, currentUser);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating file metadata", error = ex.Message });
            }
        }
    }
}
