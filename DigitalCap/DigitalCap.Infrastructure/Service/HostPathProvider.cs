using DigitalCap.Core.Interfaces.Service;
using Microsoft.AspNetCore.Hosting;

namespace DigitalCap.Infrastructure.Service
{
    public class HostPathProvider : IHostPathProvider
    {
        private readonly IWebHostEnvironment _environment;

        public HostPathProvider(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public string Path => _environment.ContentRootPath;
    }
}
