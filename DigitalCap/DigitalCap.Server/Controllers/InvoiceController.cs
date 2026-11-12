using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace DigitalCap.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ApiBaseController
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;

        public InvoiceController(ITaskService taskService, IProjectService projectService)
        {
            _taskService = taskService;
            _projectService = projectService;
        }

        /// <summary>
        /// Invoice index: invoicing-category tasks grouped by type + project name.
        /// </summary>
        [HttpGet("[action]")]
        public async Task<IActionResult> Index(int? id)
        {
            if (!id.HasValue)
                return BadRequest(new { message = "Project ID is required" });

            var cap2Result = await _taskService.GetCap2Tasks(id.Value, type: null);
            if (!cap2Result.IsSuccess)
                return BadRequest(new { message = cap2Result.ErrorMessage });

            var tasks = cap2Result.Data?
                .Where(x => x.Category == CategoryNames.Invoicing)
                ?? Enumerable.Empty<SingleTask>();

            var model = new TasksViewModel(id, tasks)
            {
                ProjectId = id
            };

            var nameResult = await _projectService.GetProjectName(id.Value);
            if (nameResult.IsSuccess)
                model.ProjectName = nameResult.Data;

            return Ok(model);
        }
    }
}
