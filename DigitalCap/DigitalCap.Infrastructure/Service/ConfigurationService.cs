using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        public ConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public AppConfiguration GetConfiguration()
        {
            return new AppConfiguration
            {
                CapPortalUrl = _configuration["CapPortalUrl"]
            };
        }
    }
}
