using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiagController : ControllerBase
    {
        private readonly IConfiguration _config;

        public DiagController(IConfiguration config)
        {
            _config = config;
        }
    }
}
