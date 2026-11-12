using Azure.Core;
using DigitalCap.Core.Configuration;
using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.Security;
using DigitalCap.Core.Security.Extensions;
using DigitalCap.Core.ViewModels.AccountViewModels;
using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DigitalCap.Infrastructure.Service
{
    public class UserAccountService : IUserAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly ISecurityService _securityService;
        private readonly ISecurityClientService _securityClientService;
        private readonly ISecurityClientRepository _securityClientRepository;
        private readonly IPlatformUserService _platformUserService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserAccountService> _logger;

        public UserAccountService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserAccountRepository userAccountRepository,
            ISecurityService securityService,
            ISecurityClientService securityClientService,
            ISecurityClientRepository securityClientRepository,
            IPlatformUserService platformUserService,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<UserAccountService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userAccountRepository = userAccountRepository;
            _securityService = securityService;
            _securityClientService = securityClientService;
            _securityClientRepository = securityClientRepository;
            _platformUserService = platformUserService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        // ---------------- AUTH ----------------

        public async Task<ServiceResult<LoginResult>> LoginAsync(LoginViewModel model)
        {
            if (model == null)
                return ServiceResult<LoginResult>.Failure("Invalid request");

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return ServiceResult<LoginResult>.Failure("Invalid login attempt");

            var account = await _userAccountRepository.GetByAspNetIdAsync(user.Id);
            if (account == null || account.Deleted)
                return ServiceResult<LoginResult>.Failure("Invalid login attempt");

            var signInResult = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (signInResult.Succeeded)
            {
                _logger.LogInformation("User logged in");

                return ServiceResult<LoginResult>.Success(new LoginResult
                {
                    Success = true,
                    Message = "Login successful"
                });
            }

            if (signInResult.IsLockedOut)
            {
                return ServiceResult<LoginResult>.Failure("User account is locked");
            }

            if (signInResult.IsNotAllowed)
            {
                return ServiceResult<LoginResult>.Failure("Login not allowed");
            }

            return ServiceResult<LoginResult>.Failure("Invalid login attempt");
        }
        //  EMAIL / PASSWORD 
        public async Task<ServiceResult<string>> ConfirmEmailAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return ServiceResult<string>.Failure("User not found");

            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                return ServiceResult<string>.Success("Email confirmed");
            }

            return ServiceResult<string>.Failure("Email confirmation failed");
        }

        public async Task<ServiceResult<string>> ForgotPasswordAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<string>.Failure("Email is required");

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                var u = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    NormalizedEmail = email.ToUpper(),
                    NormalizedUserName = email.ToUpper(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                var result = await _userManager.CreateAsync(u);
                
                if (!result.Succeeded)
                {
                    return ServiceResult<string>.Failure("Failed to create provisional account.");
                }

                user = await _userManager.FindByEmailAsync(email);
            }

            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                return ServiceResult<string>.Success(
                    "If the email is registered, a password reset link has been sent.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var url =
                $"{_configuration["Frontend:ResetPasswordUrl"]}" +
                $"?email={Uri.EscapeDataString(email)}" +
                $"&token={Uri.EscapeDataString(token)}";

            try
            {
                await _emailService.SendEmailAsync(
                    email,
                    "Reset Password",
                    $"Reset your password using this link:\n{url}");
            }
            catch (Exception ex)
            {
               
                _logger.LogError(ex, "Failed to send forgot password email to {Email}", email);
            }

            return ServiceResult<string>.Success(
                "If the email is registered, a password reset link has been sent.");
        }

        public async Task<ServiceResult<string>> ResetPasswordAsync(string email, string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return ServiceResult<string>.Failure("Invalid reset request");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return ServiceResult<string>.Failure("Invalid reset request");
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                return ServiceResult<string>.Success("Password reset successful");
            }

            var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
            return ServiceResult<string>.Failure(errors);
        }

        public async Task RequestAccessAsync(string email)
        {
            string to = _configuration.GetRequestAccessEmail() + "," + email;

            await _emailService.SendEmailAsync(
                to,
                "Grant User Access to CAP",
                "User is requesting access"
            );
        }

        public Task<UserAccountModel?> GetByAspNetIdAsync(string aspNetUserId)
        {
            return _userAccountRepository.GetByAspNetIdAsync(aspNetUserId);
        }

        public Task<UserAccountModel?> GetByAspNetIdIncludingDeletedAsync(string aspNetUserId)
            => _userAccountRepository.GetByAspNetIdIncludingDeletedAsync(aspNetUserId);

        public Task<IEnumerable<UserAccountModel>> GetUsersInRoleAsync(string roleName)
            => _userAccountRepository.GetUsersInRoleAsync(roleName);

        public Task<IEnumerable<UserAccountModel>> GetUsersInRoleForClientAsync(string roleName, Guid clientId)
            => _userAccountRepository.GetUsersInRoleForClientAsync(roleName, clientId);

        public Task<IEnumerable<UserAccountModel>> GetUsersWithPermissionAsync(Permission permission)
            => _userAccountRepository.GetUsersWithPermissionAsync(permission);

        public Task<IEnumerable<CapUser>> GetAbsUsersAsync()
            => _userAccountRepository.GetAbsUsersAsync();

        public Task<IEnumerable<CapUser>> GetClientUsersAsync(string clientId)
            => _userAccountRepository.GetClientUsersAsync(clientId);

        public async Task CommitAsync()
        {
            await Task.Run(() => _userAccountRepository.Commit());
        }

        public async Task<ServiceResult<bool>> DeleteAsync(string aspNetUserId)
        {
            try
            {
                var user = await _userAccountRepository.GetByAspNetIdAsync(aspNetUserId);

                if (user == null)
                {
                    return ServiceResult<bool>.Failure("User not found.");
                }

                await _userAccountRepository.DeleteAsync(user.Id); 
                _userAccountRepository.Commit();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Error deleting user: {ex.Message}");
            }
        }

        public async Task<ServiceResult<CapUsersViewModel>> ManageAbsUsers(bool userUpdated = false, bool userCreated = false)
        {
            try
            {
                var capUsersViewModel = new CapUsersViewModel
                {
                    CapUsers = await _userAccountRepository.GetAbsUsersAsync(),
                    UserUpdated = userUpdated,
                    UserCreated = userCreated,
                    Company = DigitalCap.Core.Constants.AppConstants.ABS_Company_Name,
                    IsClientUsers = false
                };

                return ServiceResult<CapUsersViewModel>.Success(capUsersViewModel);
            }
            catch (Exception ex)
            {
                return ServiceResult<CapUsersViewModel>.Failure($"Internal server error: {ex.Message}");
            }
        }

        public async Task<ServiceResult<CapUsersViewModel>> ManageClientUsersAsync(bool userUpdated = false, bool userCreated = false)
        {
            try
            {
                var currentUser = await _securityService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return ServiceResult<CapUsersViewModel>.Failure("Current user not found");
                }

                var currentUserAccount = await _userAccountRepository.GetByAspNetIdAsync(currentUser.Data.Id);
                if (currentUserAccount == null)
                {
                    return ServiceResult<CapUsersViewModel>.Failure("User account not found");
                }

                var clientUsers = await _userAccountRepository.GetClientUsersAsync(currentUserAccount.ClientId.ToString());
                var capUsersViewModel = new CapUsersViewModel
                {
                    CapUsers = clientUsers,
                    UserUpdated = userUpdated,
                    UserCreated = userCreated,
                    IsClientUsers = true,
                    Company = clientUsers.FirstOrDefault()?.Company ?? string.Empty,
                    ClientId = currentUserAccount.ClientId
                };

                return ServiceResult<CapUsersViewModel>.Success(capUsersViewModel);
            }
            catch (Exception ex)
            {
                return ServiceResult<CapUsersViewModel>.Failure($"Internal server error: {ex.Message}");
            }
        }
    }
}

