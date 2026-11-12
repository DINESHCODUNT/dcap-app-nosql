using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Infrastructure.Service
{
    public class TemplateAPIService : ITemplateAPIService
    {
        private readonly ITemplateService _templateService;

        public TemplateAPIService(ITemplateService templateService)
        {
            _templateService = templateService;
        }
    }
}
