using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Interfaces.Service;
using DigitalCap.Core.Models;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Infrastructure.Service
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IEmailService _emailService;
        private readonly IProjectRepository _projectRepository;

        public TaskService(ITaskRepository taskRepository, IEmailService emailService, IProjectRepository projectRepository)
        {
            _taskRepository = taskRepository;
            _emailService = emailService;
            _projectRepository = projectRepository;
        }

        public async Task<ServiceResult<IEnumerable<SingleTask>>> GetTasks(int projectId, string type)
        {
            try
            {
                var tasks = await _taskRepository.GetTasks(projectId);
                var filteredTasks = string.IsNullOrEmpty(type)
                    ? tasks
                    : tasks.Where(t => t.Type == type);

                return ServiceResult<IEnumerable<SingleTask>>.Success(filteredTasks);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<SingleTask>>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<IEnumerable<SingleTask>>> GetCap2Tasks(int projectId, string type)
        {
            try
            {
                var tasks = await _taskRepository.GetCap2Tasks(projectId);
                var filteredTasks = string.IsNullOrEmpty(type)
                    ? tasks
                    : tasks.Where(t => t.Type == type);

                return ServiceResult<IEnumerable<SingleTask>>.Success(filteredTasks);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<SingleTask>>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<IEnumerable<Status>>> GetStatuses()
        {
            try
            {
                var statuses = await _taskRepository.GetStatuses();
                return ServiceResult<IEnumerable<Status>>.Success(statuses);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<Status>>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<SingleTask>> GetTask(int id)
        {
            try
            {
                var task = await _taskRepository.GetTask(id);
                if (task == null)
                    return ServiceResult<SingleTask>.Failure("Task not found");

                return ServiceResult<SingleTask>.Success(task);
            }
            catch (Exception ex)
            {
                return ServiceResult<SingleTask>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<SingleTask>> GetCap2Task(int id)
        {
            try
            {
                var task = await _taskRepository.GetCap2Task(id);
                if (task == null)
                    return ServiceResult<SingleTask>.Failure("Task not found");

                return ServiceResult<SingleTask>.Success(task);
            }
            catch (Exception ex)
            {
                return ServiceResult<SingleTask>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<IEnumerable<SingleTask>>> GetIncompleteTasks(int projectId, string category)
        {
            try
            {
                var tasks = await _taskRepository.GetIncompleteTasks(projectId, category);
                return ServiceResult<IEnumerable<SingleTask>>.Success(tasks);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<SingleTask>>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> Update(SingleTask task, string currentUser)
        {
            try
            {
                if (task == null)
                    return ServiceResult<bool>.Failure("Task cannot be null");

                var result = await _taskRepository.UpdateTask(task, currentUser);
                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<byte>> UpdateProjectPercentComplete(int projectId)
        {
            try
            {
                var percentComplete = await _taskRepository.UpdateProjectPercentComplete(projectId);
                return ServiceResult<byte>.Success(percentComplete);
            }
            catch (Exception ex)
            {
                return ServiceResult<byte>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<object>> Grading(int projectId)
        {
            try
            {
                // This method appears to return project grading information
                // For now, returning a placeholder - you may need to implement based on actual requirements
                var projectGrading = new { ProjectId = projectId };
                return ServiceResult<object>.Success(projectGrading);
            }
            catch (Exception ex)
            {
                return ServiceResult<object>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> CloseTechnicalTasks(int projectId)
        {
            try
            {
                var result = await _taskRepository.CloseTechnicalTasks(projectId);
                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure(ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> CloseInvoiceTasks(int projectId)
        {
            try
            {
                var result = await _taskRepository.CloseInvoiceTasks(projectId);
                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure(ex.Message);
            }
        }
    }
}
