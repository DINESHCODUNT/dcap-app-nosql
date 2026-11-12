using DigitalCap.Core.Models;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DigitalCap.Core.Interfaces.Service
{
    public interface ITaskService
    {
        Task<ServiceResult<IEnumerable<SingleTask>>> GetTasks(int projectId, string type);
        Task<ServiceResult<IEnumerable<SingleTask>>> GetCap2Tasks(int projectId, string type);
        Task<ServiceResult<SingleTask>> GetTask(int id);
        Task<ServiceResult<SingleTask>> GetCap2Task(int id);
        Task<ServiceResult<IEnumerable<SingleTask>>> GetIncompleteTasks(int projectId, string category);
        Task<ServiceResult<bool>> Update(SingleTask task, string currentUser);
        Task<ServiceResult<byte>> UpdateProjectPercentComplete(int projectId);
        Task<ServiceResult<object>> Grading(int projectId);
        Task<ServiceResult<bool>> CloseTechnicalTasks(int projectId);
        Task<ServiceResult<bool>> CloseInvoiceTasks(int projectId);

        Task<ServiceResult<IEnumerable<Status>>> GetStatuses();
    }
}
