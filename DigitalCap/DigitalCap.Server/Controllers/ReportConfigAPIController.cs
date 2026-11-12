using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Infrastructure.Service;
using DigitalCap.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalCap.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportConfigAPIController : Controller
    {
        private readonly IConfiguration _configData;
        private readonly IReportPartService _reportPartService;
        private readonly ISecurityService _securityService;
        public IWebHostEnvironment WebHostEnvironment { get; set; }
        public ReportConfigAPIController(IReportPartService reportPartService, IConfiguration configData,
            ISecurityService securityService)
        {
            _reportPartService = reportPartService;

            _configData = configData;
            _securityService = securityService;
        }
        [HttpGet("[action]")]
        public async Task<IActionResult> GetReportPartsByVesselType(int vesselTypeId)
        {
            if (vesselTypeId == 0)
            {
                return BadRequest("Invalid Vessel Type ID.");
            }

            // Call your service or data layer to get the report parts
            var reportParts = await _reportPartService.GetReportPartsByVesselType(vesselTypeId);

            if (reportParts == null)
            {
                return NotFound("No report parts found for the specified Vessel Type.");
            }

            return Ok(reportParts.Data.reportTemplates);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetSectionNamesByPartId(int vesselTypeId, int partId)
        {
            if (vesselTypeId == 0)
            {
                return BadRequest("Invalid Vessel Type ID.");
            }

            // Call your service or data layer to get the report parts
            var reportParts = await _reportPartService.GetSectionNamesByPartIdAsync(vesselTypeId, partId);

            if (reportParts == null)
            {
                return NotFound("No report parts found for the specified Vessel Type.");
            }

            return Ok(reportParts.Data);
        }
        [HttpPost("[action]")]
        public async Task<ActionResult> CreateReportPartConfig(VesselTypeReportConfigList reportConfigList, [FromQuery] int vesselTypeId)
        {
            var userResult = await _securityService.GetCurrentUserAsync();
            var modifiedBy = userResult.Data.UserName;

            var result = await _reportPartService.CreateReportPart(reportConfigList, vesselTypeId, modifiedBy);
            return Ok(result);
        }
    }
}
