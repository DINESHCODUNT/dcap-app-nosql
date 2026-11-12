using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class GradingService : IGradingService
    {

        private readonly IGradingRepository _gradingRepository;
        private readonly ITankRepository _tankRepository;
        private readonly IUserPermissionRepository _userPermissionRepository;

        public GradingService(IGradingRepository gradingRepository, ITankRepository tankRepository, IUserPermissionRepository userPermissionRepository)
        {
            _gradingRepository = gradingRepository;
            _tankRepository = tankRepository;
            _userPermissionRepository = userPermissionRepository;
        }
        public async Task<ServiceResult<bool>> PopulateGrading(string vesselType, int projectId)
        {
            try
            {
                // 1. Create section grading
                await _gradingRepository.CreateProjectSectionGrading(projectId, vesselType);

                // 2. Get tank grading template
                var tankGradings =
                    await _tankRepository.GetVessel_GradingByVesselType(vesselType);

                // 3. Populate grading
                foreach (var grading in tankGradings)
                {
                    grading.ProjectId = projectId;
                    grading.CreatedDttm = DateTime.Now;
                    grading.UpdateDttm = DateTime.Now;

                    await _gradingRepository.CreateVessel_Grading(grading);
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> CreateGradingAsync(Core.Models.Grading.CreateGradingRequest model)
        {
            // Get sections by template and vessel type
            var sections = await _gradingRepository.GetGradingSectionNamesByTemplateNameAndVesselType(
                model.TemplateName, model.VesselType);

            if (sections == null || !sections.Any())
                return ServiceResult<bool>.Failure("Invalid section");

            var section = sections.FirstOrDefault(s => s.SectionName == model.SectionName);

            if (section == null || section.SectionId == Guid.Empty)
                return ServiceResult<bool>.Failure("Please select valid Section");


            var grading = new Core.Models.Grading.Grading
            {
                GradingName = model.GradingName,
                VesselType = model.VesselType,
                SectionId = section.SectionId,
                TanktypeId = section.TanktypeId,
                IsActive = model.IsActive,
                IsDeleted = false,
                RequiredInReport = model.RequiredInReport
            };

            if (grading.TanktypeId == 0)
                await _gradingRepository.CreateSectionGradingAsync(grading);
            else
                await _gradingRepository.CreateTankGradingAsync(grading);

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> UpdateGradingAsync(CreateGradingRequest model)
        {
            var grading = new Core.Models.Grading.Grading
            {
                GradingId = model.GradingId,
                GradingName = model.GradingName,
                IsActive = model.IsActive,
                RequiredInReport = model.RequiredInReport,
                TanktypeId = model.TanktypeId,
                VesselType = model.VesselType,
                SectionId = model.SectionId
            };

            if (grading.TanktypeId == 0)
                await _gradingRepository.UpdateSectionGradingAsync(grading);
            else
                await _gradingRepository.UpdateTankGradingAsync(grading);

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteGradingAsync(int gradingId, int tankId)
        {
            await _gradingRepository.DeleteGradingAsync(gradingId, tankId);
            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<List<GradingTemplate>>> GetTemplateName([FromQuery] string vesselType = null)
        {
            try
            {
                var templates = await _gradingRepository.GetGradingTemplatesByVesselType(vesselType);
                return ServiceResult<List<GradingTemplate>>.Success(templates);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<GradingTemplate>>.Failure("Error retrieving templates");
            }
        }

        public async Task<ServiceResult<List<GradingSection>>> GetSectionNameByTemplateNameAndVesselType(string templateName, string vesselType)
        {
            try
            {
                var sections = await _gradingRepository.GetGradingSectionNamesByTemplateNameAndVesselType(templateName, vesselType);
                return ServiceResult<List<GradingSection>>.Success(sections);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<GradingSection>>.Failure("Error retrieving sections");
            }
        }
        public async Task<ServiceResult<List<GradingSection>>> GetGradingSections(int templateId, string vesselType)
        {
            try
            {
                var sections = await _gradingRepository.GetGradingSections(templateId, vesselType);
                return ServiceResult<List<GradingSection>>.Success(sections);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<GradingSection>>.Failure("Error retrieving sections");
            }
        }

        public async Task<ServiceResult<List<Core.Models.Grading.Grading>>> GetAllGradingsAsync()
        {
            var all = await _gradingRepository.GetAllGradingAsync();
            return ServiceResult<List<Core.Models.Grading.Grading>>.Success(all);
        }
        public async Task<ServiceResult<PagedResult<Core.Models.Grading.Grading>>> GetAllGrading_Pagination(int pageNumber, int pageSize)
        {
            var result = await _gradingRepository.GetAllGrading_Pagination(pageNumber, pageSize);
            return ServiceResult<PagedResult<Core.Models.Grading.Grading>>.Success(result);
        }
    }
}
