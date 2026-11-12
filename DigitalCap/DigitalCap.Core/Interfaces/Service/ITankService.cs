using DigitalCap.Core.DTO;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.Models.VesselModel;
using DigitalCap.Core.Models.View.Admin;
using DigitalCap.Core.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface ITankService
    {
        Task<ServiceResult<bool>> UpdateTankImageDescriptionDropdownCard(int templateId, int projectId, Guid sectionId, int cardNumber, int value, string cardName);
        Task<ServiceResult<bool>> UpdateTankCardAdditionalDescription(int templateId, int projectId, Guid sectionId, int cardNumber, string value, string cardName);
        Task<ServiceResult<bool>> UpdateTankCardCurrentCondition(int templateId, int projectId, Guid sectionId, int cardNumber, int value, string cardName);
        Task<ServiceResult<UploadPhotoResultDto>> UploadTankCardImage(IFormFile tankfile, int templateId, int projectId, Guid sectionId, int cardNumber, int imageId, bool replaceImage = false, string cardName = null);
        Task<ServiceResult<bool>> DeleteTankCardImage(int templateId, int projectId, Guid sectionId, int cardNumber, int imageId);
        Task<ServiceResult<List<TankTypes>>> GetTankTypes();
        Task<ServiceResult<List<TankTypes>>> GetMappedTankTypes(int projectId, string vesseltype);
        Task<ServiceResult<List<VesselDetails>>> GetVesselDetails();
        Task<ServiceResult<bool>> UpdateTank(string tankId, string vesseltype, string vesselname, string imonumber, string tanktype, string tankname, bool isActive);
        Task<ServiceResult<List<Core.Models.View.Admin.VesselTypes>>> GetVesselType();
        Task<ServiceResult<List<ShipType>>> GetShipType();
        Task<ServiceResult<List<VesselTankDetails>>> GetVesselTypeList();
        Task<ServiceResult<List<string>>> ManageTankFilter_TankName(string IMO);
        Task<ServiceResult<List<string>>> ManageTankFilter_TankType(string IMO);
        Task<ServiceResult<List<IMOTankFilterModel>>> ManageTankFilter_Project(string IMO);
        Task<ServiceResult<List<Core.Models.View.Admin.Tank>>> DeleteTanks(Guid tankId, string IMO, int ProjectId);
        // New API methods
        Task<ServiceResult<ManageTankResponse>> ManageTankAsync(string username, bool isActive, string imo, string projectId, int tankRestoreFilter, int searchRestoreFilter);
        Task<ServiceResult<bool>> CreateTankAsync(CreateTankRequest model, string username);
        Task<ServiceResult<bool>> UpdateTankAsync(CreateTankRequest model, string username);
        Task<ServiceResult<List<Core.Models.View.Admin.Tank>>> FilterMenuCustomization_Read();
        Task<ServiceResult<TanksViewModel>> ManageTankActiveCheckBox(List<Guid> data, bool status, string? IMO);

        Task<ServiceResult<ManageTankResponse>> GetVesselTankbyImo(string username, string imo);
    }


}
