using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DigitalCap.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ApiBaseController : ControllerBase
    {
        /// <summary>
        /// Returns the authenticated user's email/UPN from whichever claim Azure AD
        /// includes in the token (v2: preferred_username, v1: unique_name / upn).
        /// Returns null if no matching claim is present.
        /// </summary>
        protected string? CurrentUsername =>
            User.FindFirstValue("preferred_username")
            ?? User.FindFirstValue("unique_name")
            ?? User.FindFirstValue("upn")
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name;
    }
}
