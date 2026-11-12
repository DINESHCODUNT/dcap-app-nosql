using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Permissions;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.Models.VesselModel;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DigitalCap.Core.Security.Extensions;

namespace DigitalCap.Infrastructure.Service
{
    public class TankService : ITankService
    {
        private readonly ITankRepository _tankRepository;
        private readonly ISurveyReportRepository _surveyReportRepository;
        private readonly ISurveyReportService _surveyReportService;
        private readonly IConfiguration _configuration;
        private readonly IProjectRepository _projectRepository;
        private readonly ISecurityService _securityService;
        private readonly IUserPermissionRepository _userPermissionRepository;
        private readonly Microsoft.AspNetCore.Identity.UserManager<DigitalCap.WebApi.Core.ApplicationUser> _userManager;

        public TankService(ITankRepository tankRepository, ISurveyReportRepository surveyReportRepository
            , ISurveyReportService surveyReportService, IConfiguration configuration,
            IProjectRepository projectRepository, ISecurityService securityService,
            IUserPermissionRepository userPermissionRepository,
            Microsoft.AspNetCore.Identity.UserManager<DigitalCap.WebApi.Core.ApplicationUser> userManager)
        {
            _tankRepository = tankRepository;
            _surveyReportRepository = surveyReportRepository;
            _surveyReportService = surveyReportService;
            _configuration = configuration;
            _projectRepository = projectRepository;
            _securityService = securityService;
            _userPermissionRepository = userPermissionRepository;
            _userManager = userManager;
        }

        public async Task<ServiceResult<bool>> UpdateTankImageDescriptionDropdownCard(int templateId, int projectId, Guid sectionId, int cardNumber, int value, string cardName)
        {
            try
            {
                cardName = cardName.Trim();
                var imageCard = _tankRepository.GetProjectTankImagCardByName(projectId, templateId, sectionId, cardNumber).Result;
                if (imageCard != null && imageCard.Id != 0)
                {
                    imageCard.DescriptionId = value;
                    imageCard.AdditionalDescription = null;
                    imageCard.UpdatedDttm = DateTime.Now;
                    imageCard.CardName = cardName;
                    var result = _tankRepository.UpdateProjectTankImageCard(imageCard).Result;
                }
                else
                {
                    TankImageCard tankImageCard = new TankImageCard();
                    tankImageCard.ProjectId = projectId;
                    tankImageCard.TemplateId = templateId;
                    tankImageCard.SectionId = sectionId;
                    tankImageCard.CardNumber = cardNumber;
                    tankImageCard.DescriptionId = value;
                    tankImageCard.CreatedDttm = DateTime.Now;
                    tankImageCard.UpdatedDttm = DateTime.Now;
                    tankImageCard.IsActive = true;
                    tankImageCard.CardName = cardName;
                    tankImageCard.AdditionalDescription = null;
                    var result = await _tankRepository.CreateProjectTankImageCard(tankImageCard);

                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("false");
            }
        }

        public async Task<ServiceResult<bool>> UpdateTankCardAdditionalDescription(int templateId, int projectId, Guid sectionId, int cardNumber, string value, string cardName)
        {
            try
            {
                cardName = cardName.Trim();
                var imageCard = _tankRepository.GetProjectTankImagCardByName(projectId, templateId, sectionId, cardNumber).Result;
                if (imageCard != null && imageCard.Id != 0)
                {
                    imageCard.AdditionalDescription = value;
                    imageCard.UpdatedDttm = DateTime.Now;
                    imageCard.CardName = cardName;
                    var result = _tankRepository.UpdateProjectTankImageCard(imageCard).Result;
                }
                else
                {
                    TankImageCard tankImageCard = new TankImageCard();
                    tankImageCard.ProjectId = projectId;
                    tankImageCard.TemplateId = templateId;
                    tankImageCard.SectionId = sectionId;
                    tankImageCard.CardNumber = cardNumber;
                    tankImageCard.AdditionalDescription = value;
                    tankImageCard.CreatedDttm = DateTime.Now;
                    tankImageCard.UpdatedDttm = DateTime.Now;
                    tankImageCard.IsActive = true;
                    tankImageCard.CardName = cardName;
                    var result = await _tankRepository.CreateProjectTankImageCard(tankImageCard);

                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("false");
            }
        }

        public async Task<ServiceResult<bool>> UpdateTankCardCurrentCondition(int templateId, int projectId, Guid sectionId, int cardNumber, int value, string cardName)
        {
            try
            {
                cardName = cardName.Trim();
                var imageCard = _tankRepository.GetProjectTankImagCardByName(projectId, templateId, sectionId, cardNumber).Result;
                if (imageCard != null && imageCard.Id != 0)
                {
                    imageCard.CurrentCondition = value;
                    imageCard.UpdatedDttm = DateTime.Now;
                    imageCard.CardName = cardName;
                    var result = _tankRepository.UpdateProjectTankImageCard(imageCard).Result;
                }
                else
                {
                    TankImageCard projectCardMapping = new TankImageCard();
                    projectCardMapping.ProjectId = projectId;
                    projectCardMapping.TemplateId = templateId;
                    projectCardMapping.SectionId = sectionId;
                    projectCardMapping.CardNumber = cardNumber;
                    projectCardMapping.CurrentCondition = value;
                    projectCardMapping.CreatedDttm = DateTime.Now;
                    projectCardMapping.UpdatedDttm = DateTime.Now;
                    projectCardMapping.CardName = cardName;
                    projectCardMapping.IsActive = true;
                    var result = await _tankRepository.CreateProjectTankImageCard(projectCardMapping);

                }
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure("false");
            }
        }
        public async Task<ServiceResult<UploadPhotoResultDto>> UploadTankCardImage(IFormFile tankfile, int templateId, int projectId, Guid sectionId, int cardNumber, int imageId, bool replaceImage = false, string cardName = null)
        {
            cardName = cardName.Trim();
            string resizedImage = "";
            string newfileId = Guid.NewGuid().ToString();
            if (tankfile != null && tankfile.Length > 0)
            {
                var fileContent = ContentDispositionHeaderValue.Parse(tankfile.ContentDisposition);

                using (var ms = new MemoryStream())
                {
                    tankfile.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    resizedImage = _surveyReportService.ResizeImage(fileBytes);
                    var imgId = await _surveyReportRepository.UploadImage("unknown", newfileId);
                    await _surveyReportRepository.AddEditImageNew(imgId, resizedImage);
                    var imageCard = _tankRepository.GetProjectTankImagCardByName(projectId, templateId, sectionId, cardNumber).Result;
                    if (imageCard == null || imageCard.Id == 0)
                    {
                        TankImageCard projectCardMapping = new TankImageCard();

                        projectCardMapping.ProjectId = projectId;
                        projectCardMapping.TemplateId = templateId;
                        projectCardMapping.SectionId = sectionId;
                        projectCardMapping.CardNumber = cardNumber;
                        projectCardMapping.ImageId = imgId;
                        projectCardMapping.CurrentCondition = 0;
                        projectCardMapping.DescriptionId = 0;
                        projectCardMapping.AdditionalDescription = null;
                        projectCardMapping.IsActive = true;
                        projectCardMapping.CreatedDttm = DateTime.Now;
                        projectCardMapping.UpdatedDttm = DateTime.Now;
                        projectCardMapping.CardName = cardName;
                        var result = await _tankRepository.CreateProjectTankImageCard(projectCardMapping);
                    }
                    else
                    {
                        imageCard.UpdatedDttm = DateTime.Now;
                        imageCard.CreatedDttm = DateTime.Now;
                        imageCard.ImageId = imgId;
                        imageCard.CardName = cardName;
                        var result = await _tankRepository.UpdateProjectTankImageCard(imageCard);
                    }

                }
            }
            return ServiceResult<UploadPhotoResultDto>.Success(
                   new UploadPhotoResultDto
                   {
                       FileId = newfileId,
                       Image = resizedImage
                   });
        }

        public async Task<ServiceResult<bool>> DeleteTankCardImage(int templateId, int projectId, Guid sectionId, int cardNumber, int imageId)
        {
            var imageCard = _tankRepository.GetProjectTankImagCardByName(projectId, templateId, sectionId, cardNumber).Result;
            if (imageCard.ImageId != 0)
            {
                imageCard.ImageId = 0;
                imageCard.UpdatedDttm = DateTime.Now;
                var result = _tankRepository.UpdateProjectTankImageCard(imageCard).Result;
            }
            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<List<TankTypes>>> GetTankTypes()
        {
            var tanktypes = _tankRepository.GetTankTypes().Result;
            return ServiceResult<List<TankTypes>>.Success(tanktypes);
        }
        public async Task<ServiceResult<List<TankTypes>>> GetMappedTankTypes(int projectId, string? vesseltype)
        {
            List<TankTypes> tankTypes;

            if (projectId != 0 && projectId < _configuration.GetValue<int>("DefaultTemplateProjectId"))
            {
                // Get vessel type from project
                vesseltype = await _projectRepository.GetProjectVesselType(projectId);

                // Load default tank types
                tankTypes = await _tankRepository.GetTankTypes(0, vesseltype);
            }
            else
            {
                tankTypes = await _tankRepository.GetTankTypes(projectId, vesseltype);
            }

            return ServiceResult<List<TankTypes>>.Success(tankTypes);

        }
        public async Task<ServiceResult<List<VesselDetails>>> GetVesselDetails()
        {
            var vesseltypes = new List<VesselDetails>();
            vesseltypes = _tankRepository.GetVesselIMONo().Result;
            return ServiceResult<List<VesselDetails>>.Success(vesseltypes);
        }

        public async Task<ServiceResult<bool>> UpdateTank(string tankId, string vesseltype, string vesselname, string imonumber, string tanktype, string tankname, bool isActive)
        {
            var tanktypes = _tankRepository.GetTankTypes().Result;

            if (string.IsNullOrEmpty(tankId))
            {

                var tanktypeId = tanktypes.Where(x => x.TankName == tanktype).Select(x => x.Id).FirstOrDefault();
                VesselTank vesselTank = new VesselTank();
                vesselTank.Id = Guid.NewGuid();
                vesselTank.TankTypeId = tanktypeId;
                vesselTank.TankName = tankname;
                if (!string.IsNullOrEmpty(vesseltype))
                {

                    vesselTank.VesselType = vesseltype;
                    vesselTank.ImoNumber = null;
                }
                else
                {
                    vesselTank.VesselType = null;
                    vesselTank.ImoNumber = imonumber;
                }

                vesselTank.CreatedDttm = DateTime.Now;
                vesselTank.IsActive = isActive;
                vesselTank.UpdateDttm = DateTime.Now;
                var result = _tankRepository.CreateTank(vesselTank).Result;
            }
            else
            {

                VesselTank vesselTank = _tankRepository.GetTanks_VesselById(Guid.Parse(tankId), null).Result;
                vesselTank.TankTypeId = tanktypes.Where(x => x.TankName == tanktype).Select(x => x.Id).FirstOrDefault();
                vesselTank.TankName = tankname;
                vesselTank.IsActive = isActive;
                vesselTank.UpdateDttm = DateTime.Now;
                var result = _tankRepository.UpdateTank(vesselTank).Result;
            }

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<List<Core.Models.View.Admin.VesselTypes>>> GetVesselType()
        {
            var vesseltypes = await _tankRepository.GetVesselType();

            return ServiceResult<List<Core.Models.View.Admin.VesselTypes>>.Success(vesseltypes);
        }

        public async Task<ServiceResult<List<ShipType>>> GetShipType()
        {
            var shiptypes = await _tankRepository.GetShipType();
            return ServiceResult<List<ShipType>>.Success(shiptypes);
        }
        public async Task<ServiceResult<List<VesselTankDetails>>> GetVesselTypeList()
        {
            var vesseltypes = new List<Core.Models.VesselModel.VesselTankDetails>();
            vesseltypes = _tankRepository.GetVesselTypeList().Result;
            return ServiceResult<List<VesselTankDetails>>.Success(vesseltypes);
        }
        public async Task<ServiceResult<List<Core.Models.View.Admin.Tank>>> FilterMenuCustomization_Read()
        {
            var all = await _tankRepository.GetTanks_Vessel();
            return ServiceResult<List<Core.Models.View.Admin.Tank>>.Success(all);
        }

        public async Task<ServiceResult<List<string>>> ManageTankFilter_TankName(string IMO)
        {
            var tanks = await _tankRepository.GetTanks_Vessel();

            var tankNames = tanks.Where(x => x.TankName != null).Select(x => x.TankName).Distinct().ToList();

            if (!string.IsNullOrWhiteSpace(IMO))
            {
                tankNames = tanks.Where(x => x.TankName != null && x.IMONumber == IMO).Select(x => x.TankName).Distinct().ToList();
            }

            return ServiceResult<List<string>>.Success(tankNames);
        }

        public async Task<ServiceResult<List<string>>> ManageTankFilter_TankType(string IMO)
        {
            var tanks = await _tankRepository.GetTanks_Vessel();
            var TankTypes = tanks.Where(x => x.TankType != null).Select(e => e.TankType).Distinct().ToList();
            if (!string.IsNullOrEmpty(IMO))
            {
                TankTypes = tanks.Where(x => x.TankType != null && x.IMONumber == IMO).Select(e => e.TankType).Distinct().ToList();
            }
            return ServiceResult<List<string>>.Success(TankTypes);
        }

        public async Task<ServiceResult<List<IMOTankFilterModel>>> ManageTankFilter_Project(string IMO)
        {
            var projectnames = _tankRepository.GetProjectNames(IMO).Result;
            return ServiceResult<List<IMOTankFilterModel>>.Success(projectnames);
        }

        public async Task<ServiceResult<TanksViewModel>> ManageTankActiveCheckBox(List<Guid> data, bool status, string? IMO)
        {
            await _tankRepository.UpdateStatus(data, status);

            var tankViewModel = new TanksViewModel
            {
                IsActive = false,
                Tanks = await _tankRepository.GetTanks_Vessel()
            };

            var currentUser = await _securityService.GetCurrentUserAsync();

            if (currentUser.Data.IsABSAdministrator())
            {
                tankViewModel.Tanks = tankViewModel.Tanks
                    .Where(x => x.IMONumber != null || x.VesselType != null)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(IMO))
            {
                tankViewModel.Tanks = tankViewModel.Tanks
                    .Where(x => x.IMONumber == IMO)
                    .ToList();
            }

            return ServiceResult<TanksViewModel>.Success(tankViewModel);
        }

        public async Task<ServiceResult<List<Core.Models.View.Admin.Tank>>> DeleteTanks(Guid tankId, string IMO, int ProjectId)
        {
            var data = await _tankRepository.GetTanks_VesselById(tankId, ProjectId);
            data.IsDeleted = true;
            data.IsActive = false;
            var result = await _tankRepository.UpdateTank(data);

            var tanks = await _tankRepository.GetTanks_Vessel();

            if (!string.IsNullOrEmpty(IMO))
            {
                var filteredTanks = tanks.Where(t => t.IMONumber == IMO).ToList();
                return ServiceResult<List<Core.Models.View.Admin.Tank>>.Success(filteredTanks);
            }

            return ServiceResult<List<Core.Models.View.Admin.Tank>>.Success(tanks);
        }

        public async Task<ServiceResult<ManageTankResponse>> ManageTankAsync(string username, bool isActive, string imo, string projectId, int tankRestoreFilter, int searchRestoreFilter)
        {
            // Permission check

            var permission = await _userPermissionRepository.GetRolePermissionByUserName(username,
                EnumExtensions.GetDescription(ManagePages.ManageTank));

            if (permission == null || (!Convert.ToBoolean(permission?.Edit) && !Convert.ToBoolean(permission?.Read) && !Convert.ToBoolean(permission?.Delete)))
            {
                return ServiceResult<ManageTankResponse>.Failure("AccessDenied");
            }

            // Get current user for admin check
            var currentUserResult = await _securityService.GetCurrentUserAsync();
            var currentUser = currentUserResult.Data;
            var isAdmin = await _userManager.GetRolesAsync(await _userManager.FindByNameAsync(username));
            var isAdminUser = isAdmin.Contains("Admin (CAP HQ)");

            // Load tanks
            List<Tank> tanks;
            if (isAdminUser)
            {
                tanks = await _tankRepository.GetTanks_Vessel();
            }
            else
            {
                tanks = await _tankRepository.GetTanks_Vessel(username);
                tanks = tanks.Where(x => x.IMONumber != null || x.VesselType != null).ToList();
            }

            // Filter by IMO and ProjectId if provided
            bool isDownload = false;
            if (!string.IsNullOrEmpty(imo) && !string.IsNullOrEmpty(projectId))
            {
                var projectName = await _projectRepository.GetProjectName(Convert.ToInt32(projectId));

                if (!string.IsNullOrEmpty(projectName))
                {
                    projectName = projectName.ToLower();

                    tanks = tanks.Where(x => x.IMONumber == imo && x.ProjectName?.ToLower() == projectName).ToList();
                    isDownload = tanks.Select(x => x.IsDownload).FirstOrDefault();
                }
            }
            // Build filter options
            var validTanks = tanks.Where(x => x.IMONumber != null && x.VesselType != null).ToList();

            var imoOptions = validTanks
                .Select(x => new TankFilterModel { Text = x.IMONumber + "-" + x.VesselName, Value = x.IMONumber })
                .GroupBy(x => new { x.Value })
                .Distinct()
                .Select(x => x.First())
                .ToList();

            var tankTypeOptions = validTanks
                .Select(x => new TankFilterModel { Text = x.TankType, Value = x.TankType })
                .GroupBy(x => new { x.Text, x.Value })
                .Select(x => x.First())
                .ToList();

            var tankNameOptions = validTanks
                .Select(x => new TankFilterModel { Text = x.TankName, Value = x.TankName })
                .GroupBy(x => new { x.Text, x.Value })
                .Select(x => x.First())
                .ToList();

            var statusOptions = validTanks
                .Select(x => new TankFilterModel { Text = x.Status ? "Active" : "InActive", Value = x.Status ? "Active" : "InActive" })
                .GroupBy(x => new { x.Text, x.Value })
                .Select(x => x.First())
                .ToList();

            var response = new ManageTankResponse
            {
                IsActive = isActive,
                Editable = Convert.ToBoolean(permission?.Edit),
                CanDelete = Convert.ToBoolean(permission?.Delete),
                IsAdmin = isAdminUser,
                IsDownload = isDownload,
                IMO = imo,
                ProjectId = projectId,
                TankRestoreFilter = tankRestoreFilter == 1 ? 1 : 0,
                SearchRestoreFilter = searchRestoreFilter == 1 ? 1 : 0,
                IMONumberOptions = imoOptions,
                TankTypeOptions = tankTypeOptions,
                TankNameOptions = tankNameOptions,
                TankStatusOptions = statusOptions,
            };

            return ServiceResult<ManageTankResponse>.Success(response);
        }
        public async Task<ServiceResult<ManageTankResponse>> GetVesselTankbyImo(string username, string imo)
        {
            var permission = await _userPermissionRepository.GetRolePermissionByUserName(username,
                EnumExtensions.GetDescription(ManagePages.ManageTank));

            // Get current user for admin check
            var currentUserResult = await _securityService.GetCurrentUserAsync();
            var currentUser = currentUserResult.Data;
            var isAdmin = await _userManager.GetRolesAsync(await _userManager.FindByNameAsync(username));
            var isAdminUser = isAdmin.Contains("Admin (CAP HQ)");

            // Load tanks
            List<Tank> tanks;
            if (isAdminUser)
            {
                tanks = await _tankRepository.GetTanks_Vessel();
            }
            else
            {
                tanks = await _tankRepository.GetTanks_Vessel(username);
                tanks = tanks.Where(x => x.IMONumber != null || x.VesselType != null).ToList();
            }

            var validTanks = tanks.Where(x => x.IMONumber != null && x.VesselType != null).ToList();

            var tanklist = tanks.Where(x => x.IMONumber == imo && x.IsDeleted == false).ToList();

            var response = new ManageTankResponse
            {

                Editable = Convert.ToBoolean(permission?.Edit),
                CanDelete = Convert.ToBoolean(permission?.Delete),
                IsAdmin = isAdminUser,
                IMO = imo,
                Tanks = tanklist
            };

            return ServiceResult<ManageTankResponse>.Success(response);
        }

        public async Task<ServiceResult<bool>> CreateTankAsync(CreateTankRequest model, string username)
        {
            var permission = await _userPermissionRepository
                .GetRolePermissionByUserName(username,
                EnumExtensions.GetDescription(ManagePages.ManageTank));

            var vesselTank = new VesselTank
            {
                Id = Guid.NewGuid(),
                TankTypeId = model.TankTypeId,
                TankName = model.TankName,
                Subheader = model.Subheader,
                IsActive = model.Status,
                IsDeleted = false,
                CreatedDttm = DateTime.Now,
                UpdateDttm = DateTime.Now
            };

            if (!string.IsNullOrEmpty(model.VesselType))
            {
                vesselTank.VesselType = model.VesselType;
                vesselTank.ImoNumber = null;
            }
            else
            {
                vesselTank.ImoNumber = model.IMONumber;

                // set project details
                vesselTank.ProjectId = Convert.ToInt32(model.ProjectId);

                // get vessel type from project
                vesselTank.VesselType = await _projectRepository
                    .GetProjectVesselType(vesselTank.ProjectId.Value);

                // IMO set
                vesselTank.ImoNumber = model.IMONumber;

            }

            await _tankRepository.CreateTank(vesselTank);
            return ServiceResult<bool>.Success(true);
        }
        public async Task<ServiceResult<bool>> UpdateTankAsync(CreateTankRequest model, string username)
        {
            // Permission check
            var permission = await _userPermissionRepository
                .GetRolePermissionByUserName(
                    username,
                EnumExtensions.GetDescription(ManagePages.ManageTank));

            if (string.IsNullOrWhiteSpace(model.TankName))
                return ServiceResult<bool>.Failure("Tank Name is required");

            // Get existing tank
            VesselTank vesselTank;

            if (model.ProjectId == 0)
            {
                vesselTank = await _tankRepository
                    .GetTanks_VesselById(model.TankId, null);
            }
            else
            {
                vesselTank = await _tankRepository
                    .GetTanks_VesselById(model.TankId, model.ProjectId);
            }

            if (vesselTank == null)
                return ServiceResult<bool>.Failure("Tank not found");

            // Duplicate check
            var tanks = await _tankRepository.GetTanks_Vessel();
            var tankTypes = await _tankRepository.GetTankTypes();

            var tankTypeName = tankTypes
                .FirstOrDefault(x => x.Id == vesselTank.TankTypeId)
                ?.TankName;

            var exists = tanks.Any(x =>
                x.TankType == tankTypeName &&
                x.TankName == model.TankName &&
                x.IMONumber == vesselTank.ImoNumber &&
                x.VesselType == vesselTank.VesselType &&
                x.TankId != model.TankId);

            if (exists)
                return ServiceResult<bool>.Failure(
                    "This tank is already available in the section");

            // Update allowed fields only
            vesselTank.Subheader = model.Subheader;
            vesselTank.TankName = model.TankName;
            vesselTank.IsActive = model.Status;
            vesselTank.UpdateDttm = DateTime.Now;

            await _tankRepository.UpdateTank(vesselTank);

            return ServiceResult<bool>.Success(true);
        }

    }
}
