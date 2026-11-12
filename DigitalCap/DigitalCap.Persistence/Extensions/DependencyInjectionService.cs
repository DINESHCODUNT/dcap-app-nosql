using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Security;
using DigitalCap.Infrastructure.Service;
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
