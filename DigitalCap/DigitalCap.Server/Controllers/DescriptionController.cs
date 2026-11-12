using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.ViewModels;
using DigitalCap.Infrastructure.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DescriptionController : ApiBaseController
    {
        private readonly IDescriptionService _descriptionService;
        private readonly IGradingService _gradingService;
        public DescriptionController(IDescriptionService descriptionService, IGradingService gradingService)
        {
            _descriptionService = descriptionService;
            _gradingService = gradingService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllDescription(int pageNumber = 1, int pageSize = 50)
        {
            var result = await _descriptionService.GetAllAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AddNewDescription([FromBody] ImageDescriptionViewModel model)
        {
            var result = await _descriptionService.AddNewDescriptionAsync(model);

            if (!result.IsSuccess)
                return BadRequest(result.Message ?? result.ErrorMessage);

            return Ok(true);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UpdateImageDescription([FromBody] ImageDescriptionViewModel model)
        {
            if (model == null)
                return BadRequest("Invalid request");

            var result = await _descriptionService.UpdateAsync(model);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetSectionNameByTemplateNameAndVesselType([FromQuery] string templateName, [FromQuery] string vesselType)
        {
            var result = await _descriptionService.GetSectionNamesByTemplateNameAndVesselTypeAsync(templateName, vesselType);

            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
    }
}

