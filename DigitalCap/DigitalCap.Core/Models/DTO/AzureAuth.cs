using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.DTO
{
    public class AzureAuth
    {
        public string clientId { get; set; } = string.Empty;
        public string authority { get; set; } = string.Empty;
        public List<string> knownAuthorities { get; set; } = new List<string>();
        public string redirectUri { get; set; } = string.Empty;
        public string postLogoutRedirectUri { get; set; } = string.Empty;
        public bool navigateToLoginRequestUrl { get; set; } = false;
    }

    public class AzureCache
    {
        public string cacheLocation { get; set; } = string.Empty;
        public bool storeAuthStateInCookie { get; set; } = false;
    }

    public class AzureAuthConfigSection
    {
        public AzureAuthConfigSection()
        {
            auth = new AzureAuth();
            cache = new AzureCache();
        }

        public AzureAuth auth { get; set; }
        public AzureCache cache { get; set; }
        public List<string> scopes { get; set; } = new List<string>();
    }

    public class MsalConfigResponse
    {
        public MsalConfigResponse()
        {
            azureAdConfig = new AzureAuthConfigSection();
            azureAdMsalConfig = new AzureAuthConfigSection();
        }

        public AzureAuthConfigSection azureAdConfig { get; set; }
        public AzureAuthConfigSection azureAdMsalConfig { get; set; }
    }
}
