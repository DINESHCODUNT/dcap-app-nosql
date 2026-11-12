using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models.Tank;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VesselController : ApiBaseController
    {
        private readonly IVesselService _vesselService;
        private readonly IClassService _classService;
        private readonly IFreedomAPIRepository _freedomApiRepository;
        private readonly ILogger<VesselController> _logger;
        private readonly ITankService _tankService;
        private readonly IProjectService _projectService;

        public VesselController(
            IVesselService vesselService,
            IClassService classService,
            IFreedomAPIRepository freedomAPIRepository,
            ILogger<VesselController> logger,
            ITankService tankService,
            IProjectService projectService)
        {
            _vesselService = vesselService;
            _classService = classService;
            _freedomApiRepository = freedomAPIRepository;
            _logger = logger;
            _tankService = tankService;
            _projectService = projectService;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> LookUp([FromQuery] string classNumber)
        {
            var viewModel = await _vesselService.LookUp(classNumber);

            return Ok(viewModel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> LookUpByImo([FromQuery] int imoNumber)
        {
            var vessel = await _vesselService.LookUpByImo(imoNumber);
            return Ok(vessel.Data);
        }
    }

}
