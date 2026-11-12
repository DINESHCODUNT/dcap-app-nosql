using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ExportConfig;
using DigitalCap.Core.Models.Permissions;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class ExportService : IExportService
    {
        private readonly IExportRepository _exportRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ISecurityService _securityService;
        private readonly IUserPermissionRepository _userPermissionRepository;

        public ExportService(
            IExportRepository exportRepository,
            IProjectRepository projectRepository,
            ISecurityService securityService,
            IUserPermissionRepository userPermissionRepository)
        {
            _exportRepository = exportRepository;
            _projectRepository = projectRepository;
            _securityService = securityService;
            _userPermissionRepository = userPermissionRepository;
        }

        public async Task<ServiceResult<ExportSettings>> GetExportPartsByVesselTypeAsync(int vesselTypeId)
        {
            try
            {
                var settings = new ExportSettings();

                // Get Parts
                settings.exportParts =
                    await _exportRepository.GetVesselTypeExportPartConfiguration(vesselTypeId);

                // Get Sections for each Part
                foreach (var part in settings.exportParts)
                {
                    part.sections = await _exportRepository.GetVesselTypeExportSectionConfiguration(part.PartId, vesselTypeId);
                }

                return ServiceResult<ExportSettings>.Success(settings);
            }
            catch (Exception ex)
            {
                // Log exception here if you have logging

                return ServiceResult<ExportSettings>.Failure(
                    $"Failed to load export configuration. {ex.Message}"
                );
            }
        }

        public async Task<ServiceResult<bool>> SaveRequiredInExportAsync(ExportSettingsRequest settings,string username)
        {
            try
            {
                var permission = await _userPermissionRepository.GetRolePermissionByUserName(username, EnumExtensions.GetDescription(ManagePages.ManageExport));
                if (permission == null || !permission.Edit)
                {
                    return ServiceResult<bool>.Failure("You do not have permission to edit export settings.");
                }

                foreach (var part in settings.exportParts)
                {
                    var partUpdated = await _exportRepository.UpdatePartRequiredInReportAsync(
                        part.RequiredInReport,
                        part.PartId,
                        settings.SelectedImoNumber,
                        settings.SelectedVesselTypeId, username);

                    if (!partUpdated)
                    {
                        return ServiceResult<bool>.Failure("Failed to save export settings");
                    }

                    foreach (var section in part.sections)
                    {
                        if (section.TankTypeId != 0)
                        {
                            var tankUpdated = await _exportRepository.UpdateTankSectionRequiredInReportAsync(
                                section.RequiredInReport,
                                section.SectionId,
                                settings.SelectedImoNumber, username);

                            if (!tankUpdated)
                            {
                                return ServiceResult<bool>.Failure("Failed to save export settings");
                            }
                        }
                        else
                        {
                            var sectionUpdated = await _exportRepository.UpdateSectionRequiredInReportAsync(
                                section.RequiredInReport,
                                section.SectionId,
                                settings.SelectedImoNumber,
                                settings.SelectedVesselTypeId, username);

                            if (!sectionUpdated)
                            {
                                return ServiceResult<bool>.Failure("Failed to save export settings");
                            }
                        }
                    }
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                // optionally log ex
                return ServiceResult<bool>.Failure("Failed to save export settings");
            }
        }
    }
}
