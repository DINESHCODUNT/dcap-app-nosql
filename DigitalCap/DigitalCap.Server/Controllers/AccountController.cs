using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.ViewModels.AccountViewModels;
using DigitalCap.Infrastructure.Service;
using DigitalCap.WebApi.Core;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using DigitalCap.WebApi.Controllers;

namespace DigitalCap.API.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController : ApiBaseController
    {
        private readonly IUserAccountService _userAccountService;
        private readonly ISecurityService _securityService;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        public AccountController(IUserAccountService userAccountService, ISecurityService securityService, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
        {
            _userAccountService = userAccountService;
            _securityService = securityService;
            _userManager = userManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var result = await _userAccountService.LoginAsync(model);
            return Ok(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            await _userAccountService.ForgotPasswordAsync(model.Email);
            return Ok();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            await _userAccountService.ResetPasswordAsync(model.Email, model.Code, model.Password);
            return Ok();
        }

        [HttpPost("RequestAccess")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestAccess([FromBody] RequestAccessDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new
                {
                    message = "Email is required",
                    error = "EMAIL_REQUIRED"
                });
            }

            try
            {
                await _userAccountService.RequestAccessAsync(dto.Email.Trim());
                return Ok(new
                {
                    message = "Access request sent successfully",
                    success = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while processing your request",
                    error = ex.Message
                });
            }
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var currentUserResponse = await _securityService.GetCurrentUserAsync();

                if (currentUserResponse?.Data == null)
                {
                    return Unauthorized(new { message = "User not found." });
                }

                var applicationUser = await _userManager.FindByNameAsync(currentUserResponse.Data.UserName);

                if (applicationUser == null)
                {
                    return Unauthorized(new { message = "User not found." });
                }

                var result = await _userManager.ChangePasswordAsync(
                    applicationUser,
                    model.OldPassword,
                    model.NewPassword);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        errors = result.Errors.Select(e => e.Description)
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Password changed successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "An unexpected error occurred."
                });
            }
        }
    }
}
