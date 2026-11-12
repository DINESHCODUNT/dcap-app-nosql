using DigitalCap.Core.DTO;
using DigitalCap.Core.Interfaces.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PortController : ApiBaseController
    {
        private readonly IPortService _portService;

        public PortController(IPortService portService)
        {
            _portService = portService ?? throw new ArgumentNullException(nameof(portService));
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Search([FromQuery] string? text)
        {
            try
            {
                var result = await _portService.Search(text ?? string.Empty);

                if (!result.IsSuccess)
                {
                    return BadRequest(result.Message);
                }

                var portResult = result.Data
                    .Select(x => new PortDTO
                    {
                        PortId = x.Id,
                        PortCode = x.Code,
                        PortName = x.Name
                    })
                    .OrderBy(x => x.PortName);

                return Ok(portResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching ports", error = ex.Message });
            }
        }
    }
}
