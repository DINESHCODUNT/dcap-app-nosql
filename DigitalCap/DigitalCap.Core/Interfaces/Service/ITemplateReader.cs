using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface ITemplateReader
    {
        Task<string> ReadTemplateAsync(string templateName, object model);

        public Task<string> ReadTemplate(string configurationPath);

    }
}
