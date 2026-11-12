using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Mvc;
using DomainConstants = DigitalCap.Core.Constants.DomainConstants;

namespace DigitalCap.WebApi.Controllers.Extensions
{
    public static class ControllerExtensions
    {
        public static async Task SendNewUserEmail(this ControllerBase controller, ITemplateReader templateReader, IEmailService emailService,
            AccountViewModel model, UserAccountModel adminUser, UserAccountModel account, ApplicationUser user, string subject, string password = "", string ccEmail = "")
        {
            try
            {
                var template = await templateReader.ReadTemplate(DigitalCap.Core.Constants.Constants.EmailTemplates.ClientCreated);
                string body = template
                    .Replace("{userEmail}", user.Email)
                    .Replace("{userName}", $"{account.FirstName} {account.LastName}")
                    .Replace("{companyName}", model.Company)
                    .Replace("{accountCreator}", $"{adminUser.FirstName} {adminUser.LastName}")
                    .Replace("{accountEmail}", user.Email)
                    .Replace("{newPassword}", password)
                    .Replace("{appLink}", $"{controller.Request.Scheme}://{controller.Request.Host}/");

                if (string.IsNullOrEmpty(ccEmail))
                {
                    await emailService.SendEmailAsync(user.Email, subject, "", body);
                }
                else
                {
                    await emailService.SendEmailAsyncWithCC(user.Email, ccEmail, subject, "", body);
                }

            }
            catch (Exception ex)
            {
                controller.ModelState.AddModelError(DomainConstants.Errors.Views.GeneralErrorFieldName, $"Failed to send confirmation email to user. Details: {ex.Message}");
            }
        }

    }
}
