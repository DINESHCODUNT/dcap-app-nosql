using DigitalCap.Core.Interfaces.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DigitalCap.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;

        public ConfigurationController(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        [HttpGet("[action]")]
        public IActionResult GetConfiguration()
        {
            var appConfiguration = _configurationService.GetConfiguration();
            return Ok(appConfiguration);
        }
    }
}
