using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ApiBaseController
    {
        private readonly ICommentService _commentService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommentController(ICommentService commentService, UserManager<ApplicationUser> userManager)
        {
            _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <summary>
        /// Gets user identification using UserManager to validate and fetch user ID from AspNetUsers table.
        /// Returns tuple: (userId, userEmail)
        /// </summary>
        private async Task<(string userId, string userEmail)> GetUserIdentificationAsync()
        {
            var userEmail = CurrentUsername;

            if (string.IsNullOrEmpty(userEmail))
            {
                return (string.Empty, string.Empty);
            }

            // Use UserManager to find user by email and get their ID from AspNetUsers
            var user = await _userManager.FindByEmailAsync(userEmail);

            if (user != null)
            {
                // User found in AspNetUsers table - return their ID and email
                return (user.Id, userEmail);
            }

            // User not found, return empty ID but keep email
            return (string.Empty, userEmail);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetComments([FromQuery] int projectId, [FromQuery] int? projectTaskId, [FromQuery] string? notes)
        {
            try
            {
                var (userId, userEmail) = await GetUserIdentificationAsync();

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "User not authenticated" });

                var comments = await _commentService.GetComments(projectId, projectTaskId);

                if (comments.Count() == 0 && !string.IsNullOrEmpty(notes))
                {
                    comments = await _commentService.TransferExistingNotesToComment(projectId, projectTaskId, userId, notes);
                }

                if (comments.Count() != 0)
                {
                    comments = _commentService.SetCommentsEditableBasedOnUser(comments, userId);
                }

                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving comments", error = ex.Message });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequest request)
        {
            try
            {
                var (userId, userEmail) = await GetUserIdentificationAsync();

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var comments = await _commentService.CreateComment(
                    request.ProjectId,
                    request.ProjectTaskId == 0 ? null : request.ProjectTaskId,
                    userId,
                    request.Comment);

                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    new
                    {
                        message = "Error creating comment",
                        error = ex.Message
                    });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateComment([FromBody] UpdateCommentRequest request)
        {
            try
            {
                var (userId, userEmail) = await GetUserIdentificationAsync();

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        message = "User not authenticated"
                    });
                }

                var taskComments = await _commentService.UpdateComment(
                    request.Id,
                    request.ProjectId,
                    request.Comment,
                    userId);

                return Ok(taskComments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error updating comment",
                    error = ex.Message
                });
            }
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> DeleteComment([FromBody] UpdateCommentRequest request)
        {
            try
            {
                var (userId, userEmail) = await GetUserIdentificationAsync();

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new
                    {
                        message = "User not authenticated"
                    });
                }

                var taskComments = await _commentService.DeleteComment(
                    request.Id,
                    request.ProjectId,
                    request.Comment,
                    userId);

                return Ok(taskComments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error deleting comment",
                    error = ex.Message
                });
            }
        }
    }
}
