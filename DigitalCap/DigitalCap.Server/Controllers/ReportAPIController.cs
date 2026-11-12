using DigitalCap.Core.Interfaces.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DigitalCap.WebApi.Controllers;

namespace DigitalCAP.APIController
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportAPIController : ApiBaseController
    {
        private readonly IReportAPIService _reportAPIService;
        private readonly IProjectAPIService _projectAPIService;
        private readonly ITemplateAPIService _templateAPIService;

        public ReportAPIController(
            IReportAPIService reportAPIService,
            IProjectAPIService projectAPIService,
            ITemplateAPIService templateAPIService)
        {
            _reportAPIService = reportAPIService;
            _projectAPIService = projectAPIService;
            _templateAPIService = templateAPIService;
        }
    }
}
