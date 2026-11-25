using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Security;
using DigitalCap.Infrastructure.Service;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using DigitalCap.Persistence.Cosmos.Repositories;
using DigitalCap.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Persistence.Extensions
{
    public static class DependencyInjectionService
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCosmosInfrastructure(configuration);

            // Repositories that do not use a database
            services.AddScoped<IBlobStorageRepository, BlobStorageRepository>();
            services.AddScoped<IFreedomAPIRepository, FreedomAPIRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IEmailRepository, EmailRepository>();
            services.AddScoped<IHostPathProvider, HostPathProvider>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITemplateReader, TemplateReader>();

            // Cosmos helpers used by the Cosmos repositories
            services.AddScoped<CosmosActivityWriter>();
            services.AddScoped<ProjectStatusRecalculator>();

            // Cosmos repositories (the only data access)
            services.AddScoped<IClassRepository, CosmosClassRepository>();
            services.AddScoped<IPortRepository, CosmosPortRepository>();
            services.AddScoped<IClientRepository, CosmosClientRepository>();
            services.AddScoped<IClientProfileRepository, CosmosClientProfileRepository>();
            services.AddScoped<ISecurityClientRepository, CosmosSecurityClientRepository>();
            services.AddScoped<IUserAccountRepository, CosmosUserAccountRepository>();
            services.AddScoped<IUserPermissionRepository, CosmosUserPermissionRepository>();
            services.AddScoped<IActivityRepository, CosmosActivityRepository>();
            services.AddScoped<ICommentRepository, CosmosCommentRepository>();
            services.AddScoped<IGradeRepository, CosmosGradeRepository>();
            services.AddScoped<IFileStorageRepository, CosmosFileStorageRepository>();
            services.AddScoped<ITaskRepository, CosmosTaskRepository>();
            services.AddScoped<ICapRepository, CosmosCapRepository>();
            services.AddScoped<IVesselRepository, CosmosVesselRepository>();
            services.AddScoped<IProjectRepository, CosmosProjectRepository>();
            services.AddScoped<ITemplateRepository, CosmosTemplateRepository>();
            services.AddScoped<IProjectReportRepository, CosmosProjectReportRepository>();
            services.AddScoped<IExportRepository, CosmosExportRepository>();
            services.AddScoped<IDescriptionRepository, CosmosDescriptionRepository>();
            services.AddScoped<IGradingRepository, CosmosGradingRepository>();
            services.AddScoped<ITankRepository, CosmosTankRepository>();
            services.AddScoped<IReportPartRepository, CosmosReportPartRepository>();
            services.AddScoped<ISurveyReportRepository, CosmosSurveyReportRepository>();
            services.AddScoped<ISyncRepository, CosmosSyncRepository>();
            services.AddScoped<IPlatformUserRepository, CosmosPlatformUserRepository>();
            services.AddScoped<ITransferDataOnlinetoOfflineRepository, CosmosTransferDataOnlinetoOfflineRepository>();
            services.AddHttpClient(); // Add this
            services.AddScoped<IHttpService, HttpService>();
            // Services
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IGradingService, GradingService>();
            services.AddScoped<ISecurityService, SecurityService>();
            services.AddScoped<ISurveyReportService, SurveyReportService>();
            services.AddScoped<IVesselService, VesselService>();
            services.AddScoped<ISecurityClientService, SecurityClientService>();
            services.AddScoped<IUserAccountService, UserAccountService>();
            services.AddScoped<ITankService, TankService>();
            services.AddScoped<IDescriptionService, DescriptionService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IReportPartService, ReportPartService>();
            services.AddScoped<IProjectSurveyService, ProjectSurveyService>();
            services.AddScoped<IClientProfileService, ClientProfileService>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IUserPermissionService, UserPermissionService>();
            services.AddScoped<IActivityService, ActivityService>();
            services.AddScoped<ICapService, CapService>();
            services.AddScoped<IClassService, ClassService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IManageAbsAdminService, ManageAbsAdminService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<PermissionAuthorizationFilter>();
            services.AddScoped<IAsposePdfService, AsposePdfService>();
            services.AddScoped<IProjectReportService, ProjectReportService>();
            services.AddScoped<IConfigurationService, ConfigurationService>();
            services.AddScoped<IPlatformUserService, PlatformUserService>();
            services.AddScoped<IPortService, PortService>();

            // Register IUser factory - provides IUser from current HttpContext
            // Note: Using GetAwaiter().GetResult() is acceptable in DI registration context
            services.AddScoped<IUser>(serviceProvider =>
            {
                try
                {
                    var securityService = serviceProvider.GetRequiredService<ISecurityService>();
                    var result = securityService.GetCurrentUserAsync().GetAwaiter().GetResult();
                    if (result.IsSuccess && result.Data != null)
                    {
                        return result.Data;
                    }
                }
                catch
                {
                    // If user cannot be resolved, return anonymous user
                }
                // Return anonymous user if not authenticated or error occurs
                return ApplicationDto.Anonymous;
            });

            return services;
        }
    }
}
