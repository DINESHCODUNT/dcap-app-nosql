using DigitalCap.Core.Constants;
using DigitalCap.Core.DTO;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.Security;


using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Cryptography;

namespace DigitalCap.Infrastructure.Service
{
    public class ManageAbsAdminService : IManageAbsAdminService
    {
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly ISecurityService _securityService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ITemplateReader _templateReader;

        public ManageAbsAdminService(
            IUserAccountRepository userAccountRepository,
            ISecurityService securityService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ITemplateReader templateReader)
        {
            _userAccountRepository = userAccountRepository;
            _securityService = securityService;
            _userManager = userManager;
            _emailService = emailService;
            _templateReader = templateReader;
        }

        public async Task<ServiceResult<AccountViewModel>> GetAbsUserAsync(Guid userId)
        {
            var aspNetUser = await _securityService.GetUser(userId);
            if (aspNetUser == null)
                return ServiceResult<AccountViewModel>.Failure("User not found");

            var userAccount = await _userAccountRepository.GetByAspNetIdAsync(userId.ToString());

            var model = new AccountViewModel
            {
                UserAccount = userAccount,
                AspNetUser = aspNetUser.ToString(),
                Company = userAccount?.Company,
                AbsUser = true,
                UpdatedRole = aspNetUser.DisplayRole
            };

            return ServiceResult<AccountViewModel>.Success(model);
        }

        private async Task SendNewUserEmailAsync(
            ApplicationDto user,
            UserAccountModel account,
            UserAccountModel adminUser,
            AccountViewModel model,
            string subject,
            string password,
            string ccEmail)
        {
            try
            {
                var template = await _templateReader.ReadTemplate(DigitalCap.Core.Constants.Constants.EmailTemplates.ClientCreated);
                string body = template
                    .Replace("{userEmail}", user.Email)
                    .Replace("{userName}", $"{account.FirstName} {account.LastName}")
                    .Replace("{companyName}", model.Company)
                    .Replace("{accountCreator}", $"{adminUser?.FirstName} {adminUser?.LastName}")
                    .Replace("{accountEmail}", user.Email)
                    .Replace("{newPassword}", password)
                    .Replace("{appLink}", ""); // App link can be configured via settings

                if (string.IsNullOrEmpty(ccEmail))
                {
                    await _emailService.SendEmailAsync(user.Email, subject, "", body);
                }
                else
                {
                    await _emailService.SendEmailAsyncWithCC(user.Email, ccEmail, subject, "", body);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the operation
                System.Diagnostics.Debug.WriteLine($"Failed to send confirmation email to user. Details: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> StoreAbsUserAsync(AccountViewModel model)
        {
            try
            {
                if (model == null)
                    return ServiceResult<bool>.Failure("Invalid request.");

                string password = string.Empty;

                var currentUserResult = await _securityService.GetCurrentUserAsync();

                if (!currentUserResult.IsSuccess || currentUserResult.Data == null)
                    return ServiceResult<bool>.Failure("Unauthorized");

                var currentUser = currentUserResult.Data;


                if (FormValues.ABSRoles.Contains(model.UpdatedRole) &&
                    !(currentUser.Permissions?.Contains(Permission.ManageAbsUsers) ?? false))
                {
                    return ServiceResult<bool>.Failure("You do not have permission to assign this role.");
                }

                var adminUser = await _userAccountRepository.GetByAspNetIdAsync(currentUser.Id);

                var account = model.UserAccount;
                account.IsAgreeToPrivacy = true;

                var existingIdentityUser = await _userManager.FindByEmailAsync(model.Email);

                bool isEdit = !model.NewUser && (!string.IsNullOrEmpty(model.AspNetUser) || !string.IsNullOrEmpty(account?.AspNetUserId));

                ApplicationDto userDto = null;

                if (existingIdentityUser != null)
                {
                    if (!isEdit || (existingIdentityUser.Id != model.AspNetUser && existingIdentityUser.Id != account?.AspNetUserId))
                    {
                        return ServiceResult<bool>.Failure("This email address already exists. Please use a different email address.");
                    }

                    if (Guid.TryParse(existingIdentityUser.Id, out var existingUserId))
                    {
                        userDto = await _securityService.GetUser(existingUserId);
                    }

                    // enable if disabled
                    if (!existingIdentityUser.IsEnabled &&
                        Guid.TryParse(existingIdentityUser.Id, out var disabledUserId))
                    {
                        await _securityService.ToggleUserIsEnabled(disabledUserId, true);
                    }

                    account.AspNetUserId = existingIdentityUser.Id;
                }
                else
                {
                    // 6️⃣ Create new identity user
                    password = GenerateSecurePassword();
                    var newUser = new ApplicationUser
                    {
                        Email = model.Email,
                        UserName = model.Email,
                        EmailConfirmed = true,
                        IsEnabled = true
                    };

                    var createResult = await _userManager.CreateAsync(newUser, password);

                    if (!createResult.Succeeded)
                    {
                        var error = string.Join(". ", createResult.Errors.Select(x => x.Description));
                        return ServiceResult<bool>.Failure($"User creation failed: {error}");
                    }

                    account.AspNetUserId = newUser.Id;

                    userDto = await _securityService.GetUser(Guid.Parse(newUser.Id));
                }

                if (userDto == null)
                    return ServiceResult<bool>.Failure("User loading failed.");

                userDto.EmailConfirmed = true;

                // 7️⃣ Insert / Update account
                bool sendEmail = false;

                if (existingIdentityUser == null)
                {
                    _userAccountRepository.Insert(account);
                    sendEmail = true;
                }
                else
                {
                    var existingAccount =
                        await _userAccountRepository.GetByAspNetId_ActiveOrDeleted(existingIdentityUser.Id);

                    if (existingAccount == null)
                    {
                        _userAccountRepository.Insert(account);
                        sendEmail = true;
                    }
                    else
                    {
                        account.Id = existingAccount.Id;
                        _userAccountRepository.Update(account);
                    }
                }

                // 8️⃣ Role assignment
                if (model.AbsUser || model.ClientUser)
                {
                    await _securityService.UpdateRolesForUser(userDto, model.UpdatedRole);
                }
                else
                {
                    await _securityService.UpdateRolesForUser(userDto, AppConstants.Roles.ClientAdmin);
                }

                // 9️⃣ Commit after role update
                _userAccountRepository.Commit();

                // 🔟 Email
                if (sendEmail)
                {
                    if (model.AbsUser)
                        password = "N/A";

                    await SendNewUserEmailAsync(
                        userDto,
                        account,
                        adminUser,
                        model,
                        model.AbsUser || model.ClientUser
                            ? "Access Granted"
                            : "CAP Client Admin Created",
                        password,
                        currentUser.Email);
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Error saving ABS user: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates a secure random password with uppercase, lowercase, numbers, and special characters.
        /// Password length: 8 characters
        /// </summary>
        /// <returns>A secure random password string</returns>
        private string GenerateSecurePassword()
        {
            // Character set includes: uppercase, lowercase, numbers, and special characters
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var bytes = new byte[8];
            
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }

    }
}
