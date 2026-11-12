using DigitalCap.Core.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet("[action]")]
        public IActionResult GetAuthConfig()
        {
            try
            {
                bool adNavigateToLoginRequestUrl = false;
                bool adStoreAuthStateInCookie = false;
                List<string> adScopes = new List<string>();
                bool.TryParse(_configuration["AzureAd:NavigateToLoginRequestUrl"], out adNavigateToLoginRequestUrl);
                bool.TryParse(_configuration["AzureAd:StoreAuthStateInCookie"], out adStoreAuthStateInCookie);
                string adScopesStr = _configuration["AzureAd:Scopes"];
                if (!string.IsNullOrEmpty(adScopesStr)) adScopes.AddRange(adScopesStr.Split(","));

                // Build redirect URIs dynamically from the current request host so
                // the same deployment works on localhost and Azure App Service without config changes.
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var azureAd = new AzureAuthConfigSection()
                {
                    auth = new AzureAuth()
                    {
                        clientId = _configuration["AzureAd:Audience"],
                        authority = _configuration["AzureAd:Authority"],
                        redirectUri = $"{baseUrl}/success",
                        navigateToLoginRequestUrl = adNavigateToLoginRequestUrl,
                        postLogoutRedirectUri = $"{baseUrl}/signout"
                    },
                    cache = new AzureCache()
                    {
                        cacheLocation = _configuration["AzureAd:CacheLocation"],
                        storeAuthStateInCookie = adStoreAuthStateInCookie
                    },
                    scopes = adScopes
                };
                               
              
                return Ok(new MsalConfigResponse()
                {
                    azureAdConfig = azureAd
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
