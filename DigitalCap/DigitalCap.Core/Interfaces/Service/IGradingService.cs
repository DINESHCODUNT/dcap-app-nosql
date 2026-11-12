using DigitalCap.Core.DTO;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface IGradingService
    {
        Task<ServiceResult<bool>> PopulateGrading(string vesselType, int projectId);

        Task<ServiceResult<List<Core.Models.Grading.Grading>>> GetAllGradingsAsync();
        Task<ServiceResult<bool>> CreateGradingAsync(CreateGradingRequest model);
        Task<ServiceResult<bool>> UpdateGradingAsync(CreateGradingRequest model);
        Task<ServiceResult<bool>> DeleteGradingAsync(int gradingId, int tankId);
        Task<ServiceResult<List<GradingTemplate>>> GetTemplateName([FromQuery] string vesselType = null);
        Task<ServiceResult<List<GradingSection>>> GetGradingSections(int templateId, string vesselType);
        Task<ServiceResult<List<GradingSection>>> GetSectionNameByTemplateNameAndVesselType(string templateName, string vesselType);

        Task<ServiceResult<PagedResult<Core.Models.Grading.Grading>>> GetAllGrading_Pagination(int pageNumber, int pageSize);
    }
}
