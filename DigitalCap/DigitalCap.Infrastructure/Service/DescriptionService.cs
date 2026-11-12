using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.ViewModels;
using DigitalCap.Core.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Telerik.Windows.Documents.Fixed.Model.ColorSpaces;
using static System.Collections.Specialized.BitVector32;

namespace DigitalCap.Infrastructure.Service
{
    public class DescriptionService : IDescriptionService
    {
        private readonly IDescriptionRepository _descriptionRepository;
        private readonly IGradingRepository _gradingRepository;
        private readonly ITankRepository _tankRepository;
        private readonly IUserPermissionRepository _userPermissionRepository;


        public DescriptionService(IDescriptionRepository descriptionRepository, IGradingRepository gradingRepository, ITankRepository tankRepository, IUserPermissionRepository userPermissionRepository)
        {
            _descriptionRepository = descriptionRepository;
            _gradingRepository = gradingRepository;
            _tankRepository = tankRepository;
            _userPermissionRepository = userPermissionRepository;
        }


        public async Task<ServiceResult<PagedResult<ImageDescriptions>>> GetAllAsync(int pageNumber, int pageSize)
        {
            try
            {
                var result = await _descriptionRepository.GetAllDescription(pageNumber, pageSize);

                return ServiceResult<PagedResult<ImageDescriptions>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<ImageDescriptions>>.Failure("Failed to retrieve descriptions");
            }
        }

        public async Task<ServiceResult<bool>> CreateAsync(ImageDescriptionDTO model)
        {
            if (string.IsNullOrWhiteSpace(model.Description))
                return ServiceResult<bool>.Failure("Description required");

            var sections = await _descriptionRepository.GetDescriptionSectionNamesByTemplateNameAndVesselType(
            model.TemplateName, model.VesselType);

            var section = sections.FirstOrDefault(x => x.SectionName == model.SectionName);
            if (section == null)
                return ServiceResult<bool>.Failure("Invalid Section");

              model.SectionId = section.SectionId;
              model.TankTypeId = section.TanktypeId;
              model.CreatedDttm = DateTime.UtcNow;
              model.UpdatedDttm = DateTime.UtcNow;

            var exists = await _descriptionRepository.CheckImageDescriptionExistsOrNot(
                        model.VesselType,
                        model.SectionName,
                        model.TemplateName,
                        model.Description);

            if (exists.Any())
                return ServiceResult<bool>.Failure("Already exists");

            var entity = new ImageDescriptions
            {
                
                TemplateName = model.TemplateName,
                VesselType = model.VesselType,
                SectionName = model.SectionName,
                Description = model.Description,
                ProjectId = 0,
                IsActive = model.IsActive ?? true,
                IsDeleted = false,
            };

            await _descriptionRepository.CreateImageDescription(entity);

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<AddDescriptionMetaDTO>>GetAddDescriptionMetadataAsync(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return ServiceResult<AddDescriptionMetaDTO>.Failure("Unauthorized");

            var permission = await _userPermissionRepository.GetRolePermissionByUserName(userName,
            EnumExtensions.GetDescription(ManagePages.ManageDescription));

            if (permission?.Edit != true)
                return ServiceResult<AddDescriptionMetaDTO>.Failure("Access denied");

            var templates = await _gradingRepository.GetGradingTemplates();
            var sections = await _gradingRepository.GetGradingSections(0, null);
            var vesselTypes = await _tankRepository.GetShipTypes();

            var result = new AddDescriptionMetaDTO
            {
                Templates = templates,
                Sections = sections,
                VesselTypes = vesselTypes
            };

            return ServiceResult<AddDescriptionMetaDTO>.Success(result);
        }

        public async Task<ServiceResult<bool>> AddNewDescriptionAsync(ImageDescriptionViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.VesselType))
                    return ServiceResult<bool>.Failure("Please select valid Vessel Type");

                if (string.IsNullOrWhiteSpace(model.TemplateName))
                    return ServiceResult<bool>.Failure("Please select valid Template Name");

                if (string.IsNullOrWhiteSpace(model.SectionName))
                    return ServiceResult<bool>.Failure("Please select valid Section Name");

                if (string.IsNullOrWhiteSpace(model.DescriptionName))
                    return ServiceResult<bool>.Failure("Description Name Required");

               var existing = await _descriptionRepository.CheckImageDescriptionExistsOrNot(
                    model.VesselType,
                    model.SectionName,
                    model.TemplateName,
                    model.DescriptionName
                );

                if (existing != null && existing.Any())
                    return ServiceResult<bool>.Failure(
                        "The description is already available in the section");

                var sections = await _descriptionRepository.GetDescriptionSectionNamesByTemplateNameAndVesselType(
                    model.TemplateName,model.VesselType);

                var section = sections?.FirstOrDefault(x => x.SectionName == model.SectionName);

                if (section == null)
                    return ServiceResult<bool>.Failure("Invalid Section selected");

                var entity = new ImageDescriptions
                {
                    Description = model.DescriptionName.Trim(),
                    VesselType = model.VesselType,
                    SectionId = section.TanktypeId == 0
                    ? section.SectionId: null,

                    TankTypeId = section.TanktypeId != 0
                    ? section.TanktypeId: null,

                    IsActive = model.isActive,
                    IsDeleted = false,
                    ProjectId = 0,
                    CreatedDttm = DateTime.UtcNow,
                    UpdatedDttm = DateTime.UtcNow
                };

                await _descriptionRepository.CreateImageDescription(entity);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("Internal server error");
            }
        }


        

        public async Task<ServiceResult<ImageDescriptionViewModel>> UpdateAsync(ImageDescriptionViewModel model)
        {
            var entity = await _descriptionRepository.GetImageDescriptionById(model.Id);
            if (entity == null)
                return ServiceResult<ImageDescriptionViewModel>.Failure("Not found");

            var exists = await _descriptionRepository.CheckImageDescriptionExistsOrNot(
                        model.VesselType,
                        model.SectionName,
                        model.TemplateName,
                        model.DescriptionName);

            if (exists.Any(x => x.Id != model.Id))
                return ServiceResult<ImageDescriptionViewModel>.Failure("Duplicate");

            entity.Description = model.DescriptionName;
            entity.IsActive = model.isActive;
            entity.UpdatedDttm = DateTime.UtcNow;

            await _descriptionRepository.UpdateImageDescription(entity);
            return ServiceResult<ImageDescriptionViewModel>.Success(model);
        }


        public async Task<ServiceResult<List<GradingSection>>> GetSectionNamesByTemplateNameAndVesselTypeAsync(string templateName, string vesselType)
        {

            if (string.IsNullOrWhiteSpace(templateName))
                return ServiceResult<List<GradingSection>>
                    .Failure("Template name is required");

            if (string.IsNullOrWhiteSpace(vesselType))
                return ServiceResult<List<GradingSection>>
                    .Failure("Vessel type is required");
            try
            {
                var sections = await _descriptionRepository.GetDescriptionSectionNamesByTemplateNameAndVesselType(templateName, vesselType);

                var list = sections?.ToList() ?? new List<GradingSection>();

                return ServiceResult<List<GradingSection>>.Success(list);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<GradingSection>>
                    .Failure("Failed to load section names");
            }
        }

        public async Task<ServiceResult<GetDescriptionForEditResponse>>GetDescriptionForEditAsync(int descriptionId,int tankTypeId,Guid sectionId,string username, int pageNumber, int pageSize)
        {
            var permission = await _userPermissionRepository.GetRolePermissionByUserName(username,
                EnumExtensions.GetDescription(ManagePages.ManageDescription));

            if (permission?.Edit != true)
                return ServiceResult<GetDescriptionForEditResponse>.Failure("AccessDenied");

            var pagedDescriptions = await _descriptionRepository.GetAllDescription(pageNumber, pageSize);
            var descriptions = pagedDescriptions.Data;

            if (!descriptions.Any())
                return ServiceResult<GetDescriptionForEditResponse>.Failure("NotFound");

            ImageDescriptions result;

            if (tankTypeId == 0 && sectionId == Guid.Empty)
            {
                result = descriptions.FirstOrDefault();
            }
            else if (tankTypeId != 0)
            {
                result = descriptions.FirstOrDefault(x => x.TankTypeId == tankTypeId);
            }
            else
            {
                result = descriptions.FirstOrDefault(x => x.SectionId == sectionId);
            }

            if (result == null)
                return ServiceResult<GetDescriptionForEditResponse>.Failure("NotFound");

            var templates = await _gradingRepository.GetGradingTemplates();
            var sections = await _gradingRepository.GetGradingSections(0, null);
            var vesselTypes = await _tankRepository.GetShipType();

            var response = new GetDescriptionForEditResponse
            {
                Id = result.Id,
                TemplateName = result.TemplateName,
                VesselType = result.VesselType,
                SectionName = result.SectionName,
                DescriptionName = result.Description,
                TankTypeId = result.TankTypeId ?? 0,
                Status = result.IsActive,
                SectionId = result.SectionId,
                Templates = templates,
                Sections = sections,
                VesselTypes = vesselTypes
            };

            return ServiceResult<GetDescriptionForEditResponse>.Success(response);
        }
    }
}
