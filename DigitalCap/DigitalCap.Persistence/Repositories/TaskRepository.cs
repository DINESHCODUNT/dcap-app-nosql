using Dapper;
using DapperExtensions;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.ViewModels;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Transactions;

namespace DigitalCap.Persistence.Repositories
{
    public class TaskRepository :  ITaskRepository
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected IDbConnection Connection => _unitOfWork?.Connection!;
        protected IDbTransaction Transaction => _unitOfWork?.Transaction!;

        public TaskRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public void Commit()
        {
            _unitOfWork?.Commit();
        }

        public async Task<byte> UpdateProjectPercentComplete(int projectId)
        {
            var tasks = await GetCap2Tasks(projectId);
            var projectTasks = tasks.Where(x => x.Category == CategoryNames.Project);
            var taskViewModel = new TasksViewModel(projectId, projectTasks);
            const int STARTING_PROGRESS = 4;
            var percentComplete = (byte)(STARTING_PROGRESS + taskViewModel.Sections.Sum(x => x.Tasks.Where(x => x.PercentageComplete > 0 && x.Status == CapTaskStatus.Completed.ToString()).Sum(x => x.PercentageComplete)));

            return percentComplete;
        }

        public async Task<IEnumerable<SingleTask>> GetCap2Tasks(int projectId)
        {
            try
            {
                var result = await Connection.QueryAsync<SingleTask>(
                    sql: "[CAP].[Read_Cap2Tasks_ByProjectId]",
                    new { ID = projectId },
                    transaction: Transaction,
                    commandType: CommandType.StoredProcedure);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<TaskStatusGroup>> GetStatusGroups(string category)
        {
            var sql = @"
        SELECT *
        FROM CAP.TaskStatusGroups
        WHERE Category = @Category
        ORDER BY Id
    ";

            var result = await Connection.QueryAsync<TaskStatusGroup>(
                sql,
                new { Category = category },
                transaction: Transaction
            );

            return result.ToList();
        }
        public async Task<IEnumerable<SingleTask>> GetTasks(int projectId)
        {
            var result = await Connection.QueryAsync<SingleTask>(
                sql: "CAP.Read_Tasks_ByProjectId",
                new { ID = projectId },
                transaction: Transaction,
                commandType: CommandType.StoredProcedure);

            return result;
        }

        public async Task<SingleTask> GetTask(int id)
        {
            var result = await Connection.QueryFirstOrDefaultAsync<SingleTask>(
                sql: "CAP.Read_Task_ById",
                new { ID = id },
                transaction: Transaction,
                commandType: CommandType.StoredProcedure);

            return result;
        }

        public async Task<SingleTask> GetCap2Task(int id)
        {
            var result = await Connection.QueryFirstOrDefaultAsync<SingleTask>(
                sql: "CAP.Read_Cap2Tasks_ByTaskId",
                new { ID = id },
                transaction: Transaction,
                commandType: CommandType.StoredProcedure);

            return result;
        }

        public async Task<IEnumerable<SingleTask>> GetIncompleteTasks(int projectId, string category)
        {
            var allTasks = await GetCap2Tasks(projectId);
            var incompleteTasks = allTasks.Where(t => 
                t.Category == category && 
                t.Status != "Completed" && 
                t.StatusId.HasValue);

            return incompleteTasks;
        }

        public async Task<bool> UpdateTask(SingleTask task, string currentUser)
        {
            try
            {
                var result = await Connection.ExecuteAsync(
                    sql: "CAP.Update_Tasks_ByTypeIdAndProjectId",
                    param: new
                    {
                        ID = task.ID,
                        ProjectId = task.ProjectId,
                        Category = task.Category,
                        Task = task.Task,
                        Value = task.Value,
                        Type = task.Type,
                        StatusId = task.StatusId,
                        Notes = task.Notes,
                        StatusDate = task.StatusDate,
                        LastModifiedBy = currentUser,
                        DueDate = task.DueDate,
                        AssignedTo = task.AssignedTo,
                        AssignedDate = task.AssignedDate,
                        SurveyDays = task.SurveyDays
                    },
                    transaction: Transaction,
                    commandType: CommandType.StoredProcedure);

                Commit();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> CloseTechnicalTasks(int projectId)
        {
            try
            {
                var result = await Connection.ExecuteAsync(
                    sql: "CAP.Close_TechnicalTasks",
                    param: new { ProjectId = projectId },
                    transaction: Transaction,
                    commandType: CommandType.StoredProcedure);

                Commit();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Ensure the database proc name matches your schema (e.g. CAP.Close_InvoiceTasks).
        /// </summary>
        public async Task<bool> CloseInvoiceTasks(int projectId)
        {
            try
            {
                var result = await Connection.ExecuteAsync(
                    sql: "CAP.Close_InvoiceTasks",
                    param: new { ProjectId = projectId },
                    transaction: Transaction,
                    commandType: CommandType.StoredProcedure);

                Commit();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IEnumerable<Status>> GetStatuses()
        {
            try
            {
                var result = await Connection.QueryAsync<Status>(
                    sql: "[CAP].[Read_Task_Statuses]",
                    transaction: Transaction,
                    commandType: CommandType.StoredProcedure);

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
