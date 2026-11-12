using DigitalCap.Core.Constants;
using DigitalCap.Core.DTO;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.Security;
using DigitalCap.Core.ViewModels;
using DigitalCap.WebApi.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Text;
using static DigitalCap.Core.DTO.ClientProfileDTO;

namespace DigitalCap.Infrastructure.Service
{
    public class ClientProfileService : IClientProfileService                                                                                                                                          
    {
        private readonly IClientProfileRepository _clientProfileRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ISecurityService _securityService;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ClientProfileService> _logger;
        private readonly ITemplateReader _templateReader;
        private readonly RoleManager<IdentityRole> _roleManager;


        public ClientProfileService(IClientProfileRepository clientProfileRepository,
            IProjectRepository projectRepository, ISecurityService securityService,
            IUserAccountRepository userAccountRepository, IEmailService emailService,
            UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
            ILogger<ClientProfileService> logger, ITemplateReader templateReader)
        {
            _clientProfileRepository = clientProfileRepository;
            _projectRepository = projectRepository;

            _securityService = securityService;
            _userAccountRepository = userAccountRepository;
            _emailService = emailService;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _templateReader = templateReader;
        }


        public async Task<ServiceResult<List<ClientSummaryDto>>> GetClientSummariesAsync()
        {
            var clients = await _clientProfileRepository.GetAllClientsAsync();

            var projectCounts = await _projectRepository.GetProjectCountGroupedByClientIdAsync();

            var projectCountMap = projectCounts
            .ToDictionary(x => x.ClientId, x => (uint)x.ProjectCount);

            var result = clients.Select(client => new ClientSummaryDto
            {
                Id = client.Id,
                CompanyName = client.CompanyName,
                Wcn = uint.TryParse(client.Wcn, out var wcn) ? wcn.ToString() : null,
                CompanyAddress = client.CompanyAddress,
                CompanyBillingEmail = client.CompanyBillingEmail,
                CompanyBillingSystemUrl = client.CompanyBillingSystemUrl,
                ProjectCount = (int)(projectCountMap.TryGetValue(client.Id, out var count)
                    ? count
                    : 0)
            }).ToList();

            return ServiceResult<List<ClientSummaryDto>>.Success(result);
        }


        public async Task<ServiceResult<ClientProfileViewModel>> CreateClientProfileAsync(ClientProfileViewModel model)
        {
            var client = new Client
            {
                Id = Guid.NewGuid(),
                CompanyName = model.CompanyName,
                CompanyAddress = model.CompanyAddress ?? string.Empty,
                CompanyBillingEmail = model.CompanyBillingEmail ?? string.Empty,
                CompanyBillingSystemUrl = model.CompanyBillingSystemUrl ?? string.Empty,
                Wcn = model.Wcn?.ToString()
            };

            await _clientProfileRepository.AddAsync(client);

            var projectCounts = await _projectRepository.GetProjectCountGroupedByClientIdAsync();
            var projectCountMap = projectCounts.ToDictionary(x => x.ClientId, x => (uint)x.ProjectCount);

            var projectCount = projectCountMap.TryGetValue(client.Id, out var count) ? (int)count : 0;

            var result = new ClientProfileViewModel
            {
                Id = client.Id,
                CompanyName = client.CompanyName,
                CompanyAddress = client.CompanyAddress,
                CompanyBillingEmail = client.CompanyBillingEmail,
                CompanyBillingSystemUrl = client.CompanyBillingSystemUrl,
                Wcn = client.Wcn,
                ProjectCount = projectCount
            };

            return ServiceResult<ClientProfileViewModel>.Success(result);
        }


        public async Task<ServiceResult<Guid>> EditClientProfileAsync(ClientProfileViewModel model)
        {
            try
            {
                if (model == null)
                    return ServiceResult<Guid>.Failure("Invalid request");


                if (model.Id == Guid.Empty)
                    return ServiceResult<Guid>.Failure("Invalid client ID for edit operation");

                var clients = await _clientProfileRepository.GetAllClientsAsync();

                bool duplicateExists = !string.IsNullOrWhiteSpace(model.Wcn) && clients.Any(c => c.Wcn == model.Wcn && c.Id != model.Id);
                if (duplicateExists)
                    return ServiceResult<Guid>.Failure("DuplicateWCN");

                var client = await _clientProfileRepository.GetClientAsync(model.Id);

                if (client == null)
                    return ServiceResult<Guid>.Failure("Client not found");

                var originalId = client.Id;

                client.CompanyName = model.CompanyName;
                client.CompanyAddress = model.CompanyAddress ?? string.Empty;
                client.CompanyBillingEmail = model.CompanyBillingEmail ?? string.Empty;
                client.CompanyBillingSystemUrl = model.CompanyBillingSystemUrl ?? string.Empty;
                client.Wcn = model.Wcn;

                if (client.Id == Guid.Empty)
                {

                    client.Id = originalId;
                }
                var savedId = await _clientProfileRepository.SaveClientProfileAsync(client);

                return ServiceResult<Guid>.Success(savedId);
            }
            catch (Exception ex)
            {
                return ServiceResult<Guid>.Failure($"Error saving client profile: {ex.Message}");
            }
        }

        //for Project history
        public async Task<ServiceResult<ClientProfileDetailDto>> GetClientProfileWithDetailsAsync(Guid clientId, bool freshmeat = false)
        {
            try
            {
                if (clientId == Guid.Empty)
                    return ServiceResult<ClientProfileDetailDto>.Failure("Invalid client ID");

                var client = await _clientProfileRepository.GetClientAsync(clientId);

                if (client == null)
                    return ServiceResult<ClientProfileDetailDto>.Failure("Client not found");

                List<ProjectSummaryDto> projects;
                if (freshmeat)
                {
                    projects = new List<ProjectSummaryDto>();
                }
                else
                {
                    var projectsData = await _projectRepository.GetProjectsByClientIdAsync(clientId);
                    projects = projectsData.Select(p => new ProjectSummaryDto(p)).ToList();
                }


                var adminsData = await _clientProfileRepository.GetClientAdminsAsync(clientId);
var admins = adminsData.ToList();

                var result = new ClientProfileDetailDto
                {
                    Id = client.Id,
                    Wcn = client.Wcn,
                    CompanyName = client.CompanyName,
                    CompanyAddress = client.CompanyAddress,
                    CompanyBillingEmail = client.CompanyBillingEmail,
                    CompanyBillingSystemUrl = client.CompanyBillingSystemUrl,
                    Projects = projects,
                    ClientAdmins = admins,
                    Freshmeat = freshmeat
                };

                _logger.LogInformation($"Retrieved client profile for {client.CompanyName} with {projects.Count} projects and {admins.Count} admins");

                return ServiceResult<ClientProfileDetailDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetClientProfileWithDetailsAsync for clientId: {clientId}");
                return ServiceResult<ClientProfileDetailDto>.Failure($"Error retrieving client profile: {ex.Message}");
            }
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
                // You might want to add logging here
                System.Diagnostics.Debug.WriteLine($"Failed to send confirmation email to user. Details: {ex.Message}");
            }
        }

        public async Task<ServiceResult<Guid>> SaveClientProfileAsync(ClientProfileViewModel model, bool freshmeat)
        {
            try
            {
                if (model == null)
                    return ServiceResult<Guid>.Failure("Invalid request");

                var clientList = await _clientProfileRepository.GetAllClients();

                if (model.Id == Guid.Empty &&
                    !string.IsNullOrWhiteSpace(model.Wcn) &&
                    clientList.Any(c => c.Wcn == model.Wcn.ToString()))
                {
                    return ServiceResult<Guid>.Failure("DuplicateWCN");
                }
                var client = new Client
                {
                    Id = model.Id,
                    CompanyName = model.CompanyName,
                    Wcn = model.Wcn?.ToString(),
                    // map remaining properties
                };

                var clientId = await _clientProfileRepository.SaveClientProfileAsync(client);


                return ServiceResult<Guid>.Success(clientId);
            }
            catch (Exception)
            {
                return ServiceResult<Guid>.Failure("Error storing client profile");
            }
        }



        public async Task<ServiceResult<bool>> DeleteClientProfileAsync(Guid Id)
        {
            try
            {
                var client = await _clientProfileRepository.GetClientAsync(Id);

                if (client == null)
                    return ServiceResult<bool>.Failure("Client not found");

                await _clientProfileRepository.DeleteClientAsync(Id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"Error deleting client profile: {ex.Message}");  // Return here
            }

        }
    }
}
