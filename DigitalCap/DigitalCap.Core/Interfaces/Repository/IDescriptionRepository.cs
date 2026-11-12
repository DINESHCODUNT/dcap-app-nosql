using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.Models.ImageDescription;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface IDescriptionRepository
    {
        Task<bool> CreateProjectImageDescription(int projectId, string vesseltype);
        Task<List<ImageDescriptions>> GetImageDescriptionsByProjectId(int projectId);

        
        Task<bool> UpdateImageDescription(ImageDescriptions model);
        Task<IEnumerable<ImageDescriptions>> CheckImageDescriptionExistsOrNot(string vesselType, string sectionName, string templateName, string description);
        
        Task<PagedResult<ImageDescriptions>> GetAllDescription(int pageNumber, int pageSize);
        
        Task<List<GradingSection>> GetDescriptionSectionNamesByTemplateNameAndVesselType(string templateName, string vesselType);
        Task<List<GradingTemplate>> GetGradingTemplates();
        Task<List<GradingSection>> GetGradingSections(int templateId, string vesselType);
        Task<ImageDescriptions> GetImageDescriptionById(int id);

        Task<bool> CreateImageDescription(ImageDescriptions description);
        
    }


}
