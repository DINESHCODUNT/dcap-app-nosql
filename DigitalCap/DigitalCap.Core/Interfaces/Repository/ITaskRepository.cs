using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Interfaces.Repository
{
    public interface ITaskRepository 
    {
        Task<byte> UpdateProjectPercentComplete(int projectId);
        Task<IEnumerable<SingleTask>> GetCap2Tasks(int projectId);
        Task<List<TaskStatusGroup>> GetStatusGroups(string category);
        Task<IEnumerable<SingleTask>> GetTasks(int projectId);
        Task<SingleTask> GetTask(int id);
        Task<SingleTask> GetCap2Task(int id);
        Task<IEnumerable<SingleTask>> GetIncompleteTasks(int projectId, string category);
        Task<bool> UpdateTask(SingleTask task, string currentUser);
        Task<bool> CloseTechnicalTasks(int projectId);
        Task<bool> CloseInvoiceTasks(int projectId);
        Task<IEnumerable<Status>> GetStatuses();
    }
}
