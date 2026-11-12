using DigitalCap.Core.Interfaces.Service;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class TemplateReader : ITemplateReader
    {
        private string _basePath;
        private IConfiguration _configuration;

        public TemplateReader(IHostPathProvider hostPathProvider, IConfiguration configuration)
        {
            _basePath = hostPathProvider.Path;
            _configuration = configuration;
        }

        public async Task<string> ReadTemplate(string configurationPath)
        {
            var path = Path.Combine(_basePath, "emailtemplates", _configuration.GetValue<string>(configurationPath));
            var text = await File.ReadAllTextAsync(path);
            return text;
        }
        public async Task<string> ReadTemplateAsync(string templateName, object model)
        {
            return await Task.FromResult(" ");
        }
    }
}
