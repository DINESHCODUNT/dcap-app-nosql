using DigitalCap.Core.DTO;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IDescriptionService
    {
        Task<ServiceResult<PagedResult<ImageDescriptions>>> GetAllAsync(int pageNumber, int pageSize);
        Task<ServiceResult<bool>> CreateAsync(ImageDescriptionDTO model);
        Task<ServiceResult<ImageDescriptionViewModel>> UpdateAsync(ImageDescriptionViewModel model);
        Task<ServiceResult<List<GradingSection>>> GetSectionNamesByTemplateNameAndVesselTypeAsync(string templateName, string vesselType);
        Task<ServiceResult<bool>> AddNewDescriptionAsync(ImageDescriptionViewModel model);
        Task<ServiceResult<AddDescriptionMetaDTO>>GetAddDescriptionMetadataAsync(string userName);
    }
}
