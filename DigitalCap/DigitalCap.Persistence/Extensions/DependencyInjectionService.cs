using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Security;
using DigitalCap.Infrastructure.Service;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using DigitalCap.Persistence.Cosmos.Repositories;
using DigitalCap.Persistence.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNet.Identity.EntityFramework;
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

            // UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Repositories
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IBlobStorageRepository, BlobStorageRepository>();
            services.AddScoped<IClassRepository, ClassRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IDescriptionRepository, DescriptionRepository>();
            services.AddScoped<IFileStorageRepository, FileStorageRepository>();
            services.AddScoped<IFreedomAPIRepository, FreedomAPIRepository>();
            services.AddScoped<IGradeRepository, GradeRepository>();
            services.AddScoped<IGradingRepository, GradingRepository>();
            services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
            services.AddScoped<IPortRepository, PortRepository>();
            services.AddScoped<IReportPartRepository, ReportPartRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();
            services.AddScoped<ISurveyReportRepository, SurveyReportRepository>();
            services.AddScoped<ISyncRepository, SyncRepository>();
            services.AddScoped<ITankRepository, TankRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<ITransferDataOnlinetoOfflineRepository, TransferDataOnlinetoOfflineRepository>();
            services.AddScoped<IDataTransferDevRepository, DataTransferDevRepository>();
            services.AddScoped<IUserAccountRepository, UserAccountRepository>();
            services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IVesselRepository, VesselRepository>();
            services.AddScoped<IProjectReportRepository, ProjectReportRepository>();
            services.AddScoped<IUserAccountRepository, UserAccountRepository>();
            services.AddScoped<IEmailRepository, EmailRepository>();
            services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
            services.AddScoped<ISecurityClientRepository, SecurityClientRepository>();
            services.AddScoped<IExportRepository, ExportRepository>();
            services.AddScoped<IClientProfileRepository, ClientProfileRepository>();
            services.AddScoped<IHostPathProvider, HostPathProvider>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailRepository, EmailRepository>();
            services.AddScoped<ITemplateReader, TemplateReader>();
            services.AddScoped<IActivityRepository, ActivityRepository>();
            services.AddScoped<ICapRepository, CapRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<ITemplateReader, TemplateReader>();
            services.AddScoped<IManageAbsAdminRepository, ManageAbsAdminRepository>();
            services.AddScoped<ITemplateRepository, TemplateRepository>();

            var dataProviderDefault = configuration["DataProvider:Default"] ?? "Sql";
            if (!string.Equals(dataProviderDefault, "Sql", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(dataProviderDefault, "Cosmos", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"DataProvider:Default must be 'Sql' or 'Cosmos' (found '{dataProviderDefault}').");
            }

            services.AddScoped<ClassRepository>();
            services.AddScoped<PortRepository>();
            services.AddScoped<ClientRepository>();
            services.AddScoped<CosmosClassRepository>();
            services.AddScoped<CosmosPortRepository>();
            services.AddScoped<CosmosClientRepository>();
            services.AddScoped<DigitalCap.Persistence.Cosmos.Tools.SqlToCosmosCopier>();
            services.AddScoped<ClientProfileRepository>();
            services.AddScoped<SecurityClientRepository>();
            services.AddScoped<CosmosClientProfileRepository>();
            services.AddScoped<CosmosSecurityClientRepository>();
            services.AddScoped<UserAccountRepository>();
            services.AddScoped<ManageAbsAdminRepository>();
            services.AddScoped<UserPermissionRepository>();
            services.AddScoped<CosmosUserAccountRepository>();
            services.AddScoped<CosmosManageAbsAdminRepository>();
            services.AddScoped<CosmosUserPermissionRepository>();
            services.AddScoped<ActivityRepository>();
            services.AddScoped<CommentRepository>();
            services.AddScoped<GradeRepository>();
            services.AddScoped<FileStorageRepository>();
            services.AddScoped<TaskRepository>();
            services.AddScoped<CosmosActivityRepository>();
            services.AddScoped<CosmosCommentRepository>();
            services.AddScoped<CosmosGradeRepository>();
            services.AddScoped<CosmosFileStorageRepository>();
            services.AddScoped<CosmosTaskRepository>();
            services.AddScoped<CosmosActivityWriter>();
            services.AddScoped<ProjectStatusRecalculator>();
            services.AddScoped<CapRepository>();
            services.AddScoped<VesselRepository>();
            services.AddScoped<ProjectRepository>();
            services.AddScoped<CosmosCapRepository>();
            services.AddScoped<CosmosVesselRepository>();
            services.AddScoped<CosmosProjectRepository>();
            services.AddScoped<CosmosTemplateRepository>();
            services.AddScoped<CosmosProjectReportRepository>();
            services.AddScoped<CosmosExportRepository>();
            services.AddScoped<CosmosDescriptionRepository>();
            services.AddScoped<GradingRepository>();
            services.AddScoped<TankRepository>();
            services.AddScoped<CosmosGradingRepository>();
            services.AddScoped<CosmosTankRepository>();
            services.AddScoped<ReportPartRepository>();
            services.AddScoped<CosmosReportPartRepository>();

            if (string.Equals(dataProviderDefault, "Cosmos", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IClassRepository, CosmosClassRepository>();
                services.AddScoped<IPortRepository, CosmosPortRepository>();
                services.AddScoped<IClientRepository, CosmosClientRepository>();
                services.AddScoped<IClientProfileRepository, CosmosClientProfileRepository>();
                services.AddScoped<ISecurityClientRepository, CosmosSecurityClientRepository>();
                services.AddScoped<IUserAccountRepository, CosmosUserAccountRepository>();
                services.AddScoped<IManageAbsAdminRepository, CosmosManageAbsAdminRepository>();
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
            }
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
            services.AddScoped<IUserAccountService, UserAccountService>();
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
            services.AddScoped<IUser, CurrentUser>();
            services.AddScoped<PermissionAuthorizationFilter>();
            services.AddScoped<IAsposePdfService, AsposePdfService>();
            services.AddScoped<IProjectReportService, ProjectReportService>();
            services.AddScoped<IConfigurationService, ConfigurationService>();
            services.AddScoped<IUserAccountService, UserAccountService>();
            services.AddScoped<ISecurityClientService, SecurityClientService>();
            services.AddScoped<IPlatformUserService, PlatformUserService>();
            services.AddScoped<IPortService, PortService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IManageAbsAdminService, ManageAbsAdminService>();
            services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
            // Register PermissionAuthorizationFilter - it will get IUser from DI
            services.AddScoped<PermissionAuthorizationFilter>();

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
