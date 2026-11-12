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
    public class ClassController : ApiBaseController
    {
        private readonly IClassService _classService;

        public ClassController(IClassService classService)
        {
            _classService = classService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Search()
        {
            try
            {
                var result = await _classService.Search();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching classes", error = ex.Message });
            }
        }
    }
}
