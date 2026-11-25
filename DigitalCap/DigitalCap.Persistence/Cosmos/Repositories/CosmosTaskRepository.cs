using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Interfaces.Repository;
using DigitalCap.Core.Models;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.ViewModels;
using DigitalCap.Persistence.Cosmos.Documents;
using DigitalCap.Persistence.Cosmos.Infrastructure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DigitalCap.Persistence.Cosmos.Repositories
{
    public class CosmosTaskRepository : CosmosRepositoryBase<ProjectTaskStatusDocument>, ITaskRepository
    {
        private readonly Container _tasksContainer;
        private readonly Container _statusesContainer;
        private readonly Container _commentsContainer;
        private readonly Container _filesContainer;
        private readonly Container _projectsContainer;
        private readonly Container _taskStatusGroupsContainer;
        private readonly CosmosActivityWriter _activityWriter;
        private readonly ProjectStatusRecalculator _statusRecalculator;

        public CosmosTaskRepository(
            ICosmosContainerFactory containerFactory,
            CosmosActivityWriter activityWriter,
            ProjectStatusRecalculator statusRecalculator,
            ILogger<CosmosTaskRepository> logger)
            : base(containerFactory, CosmosContainers.CAP_ProjectTaskStatus, logger)
        {
            _tasksContainer = containerFactory.GetContainer(CosmosContainers.CAP_Tasks);
            _statusesContainer = containerFactory.GetContainer(CosmosContainers.CAP_Statuses);
            _commentsContainer = containerFactory.GetContainer(CosmosContainers.CAP_Comments);
            _filesContainer = containerFactory.GetContainer(CosmosContainers.CAP_Files);
            _projectsContainer = containerFactory.GetContainer(CosmosContainers.CAP_Projects);
            _taskStatusGroupsContainer = containerFactory.GetContainer(CosmosContainers.CAP_TaskStatusGroups);
            _activityWriter = activityWriter;
            _statusRecalculator = statusRecalculator;
        }

        public void Commit() { }

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
            var utcNow = DateTime.UtcNow;
            var pk = new PartitionKey((double)projectId);
            var ptsList = await QueryAsync(new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId").WithParameter("@projectId", projectId), pk);
            if (ptsList.Count == 0)
            {
                return Enumerable.Empty<SingleTask>();
            }

            var project = await ReadProjectAsync(projectId);
            var tasksDict = await LoadTasksDictionaryAsync();
            var statusesDict = await LoadStatusesDictionaryAsync();

            // Comments count per task (cross-partition query for ProjectTaskId)
            var ptsIds = ptsList.Select(x => x.ID).ToList();
            var commentsCountByTask = new Dictionary<int, int>();
            if (ptsIds.Count > 0)
            {
                var commentsQuery = new QueryDefinition("SELECT c.ProjectTaskId, COUNT(1) AS cnt FROM c WHERE ARRAY_CONTAINS(@ptsIds, c.ProjectTaskId) AND c.IsDeleted = false GROUP BY c.ProjectTaskId")
                    .WithParameter("@ptsIds", ptsIds);
                using var feed = _commentsContainer.GetItemQueryIterator<Newtonsoft.Json.Linq.JObject>(commentsQuery);
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var row in response)
                    {
                        var ptId = row["ProjectTaskId"]?.Value<int>();
                        var cnt = row["cnt"]?.Value<int>() ?? 0;
                        if (ptId.HasValue)
                        {
                            commentsCountByTask[ptId.Value] = cnt;
                        }
                    }
                }
            }

            // Files count per task (cross-partition query for TaskId)
            var filesCountByTask = new Dictionary<int, int>();
            if (ptsIds.Count > 0)
            {
                var filesQuery = new QueryDefinition("SELECT c.TaskId FROM c WHERE ARRAY_CONTAINS(@taskIds, c.TaskId)")
                    .WithParameter("@taskIds", ptsIds);
                using var feed = _filesContainer.GetItemQueryIterator<FileDocument>(filesQuery);
                while (feed.HasMoreResults)
                {
                    var response = await feed.ReadNextAsync();
                    foreach (var f in response)
                    {
                        if (f.TaskId.HasValue)
                        {
                            filesCountByTask[f.TaskId.Value] = filesCountByTask.GetValueOrDefault(f.TaskId.Value) + 1;
                        }
                    }
                }
            }

            var result = new List<SingleTask>();
            foreach (var pts in ptsList)
            {
                int? dynamicStatusId = DynamicTaskStatus.Compute(pts, ptsList, project, utcNow);

                // INNER JOIN on Statuses and Tasks: missing or null -> row is dropped
                if (!dynamicStatusId.HasValue ||
                    !pts.TaskId.HasValue ||
                    !statusesDict.TryGetValue(dynamicStatusId.Value, out var statusDoc) ||
                    !tasksDict.TryGetValue(pts.TaskId.Value, out var taskDoc))
                {
                    continue;
                }

                int? daysWorked = DynamicTaskStatus.ComputeDaysWorked(dynamicStatusId, pts.StatusDate, pts.LastModifiedDate, utcNow);
                int numComments = commentsCountByTask.GetValueOrDefault(pts.ID, 0);
                int numAttachments = filesCountByTask.GetValueOrDefault(pts.ID, 0);

                result.Add(new SingleTask
                {
                    ID = pts.ID,
                    ProjectId = pts.ProjectId,
                    StatusId = dynamicStatusId,
                    TaskId = taskDoc.Id,
                    Category = taskDoc.Category!,
                    Type = taskDoc.Cap2Type!,
                    Task = taskDoc.Cap2Name!,
                    IsMilestone = taskDoc.IsMilestone ?? false,
                    PercentageComplete = taskDoc.PercentageComplete,
                    Status = statusDoc.Status!,
                    Value = pts.Value,
                    Notes = pts.Notes!,
                    StatusDate = pts.StatusDate,
                    LastModifiedDate = pts.LastModifiedDate,
                    LastModifiedBy = pts.LastModifiedBy!,
                    DueDate = pts.DueDate,
                    AssignedTo = pts.AssignedTo!,
                    AssignedDate = pts.AssignedDate,
                    DaysWorked = daysWorked,
                    NumberOfComments = numComments,
                    NumberOfAttachments = numAttachments
                });
            }

            return result;
        }

        public async Task<IEnumerable<SingleTask>> GetTasks(int projectId)
        {
            var utcNow = DateTime.UtcNow;
            var pk = new PartitionKey((double)projectId);
            var ptsList = await QueryAsync(new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId").WithParameter("@projectId", projectId), pk);
            if (ptsList.Count == 0)
            {
                return Enumerable.Empty<SingleTask>();
            }

            var project = await ReadProjectAsync(projectId);
            var tasksDict = await LoadTasksDictionaryAsync();
            var statusesDict = await LoadStatusesDictionaryAsync();

            var result = new List<SingleTask>();
            foreach (var pts in ptsList)
            {
                int? dynamicStatusId = DynamicTaskStatus.Compute(pts, ptsList, project, utcNow);

                // INNER JOIN on Statuses and Tasks: missing or null -> row is dropped
                if (!dynamicStatusId.HasValue ||
                    !pts.TaskId.HasValue ||
                    !statusesDict.TryGetValue(dynamicStatusId.Value, out var statusDoc) ||
                    !tasksDict.TryGetValue(pts.TaskId.Value, out var taskDoc))
                {
                    continue;
                }

                int? daysWorked = DynamicTaskStatus.ComputeDaysWorked(dynamicStatusId, pts.StatusDate, pts.LastModifiedDate, utcNow);

                result.Add(new SingleTask
                {
                    ID = pts.ID,
                    ProjectId = pts.ProjectId,
                    StatusId = dynamicStatusId,
                    TaskId = taskDoc.Id,
                    Category = taskDoc.Category!,
                    Type = taskDoc.Type!,
                    Task = taskDoc.Name!,
                    Status = statusDoc.Status!,
                    Value = pts.Value,
                    Notes = pts.Notes!,
                    StatusDate = pts.StatusDate,
                    SurveyDays = pts.SurveyDays,
                    LastModifiedDate = pts.LastModifiedDate,
                    LastModifiedBy = pts.LastModifiedBy!,
                    DaysWorked = daysWorked
                });
            }

            return result;
        }

        public async Task<SingleTask> GetCap2Task(int id)
        {
            var idStr = id.ToString();
            var ptsQuery = new QueryDefinition("SELECT * FROM c WHERE c.id = @id").WithParameter("@id", idStr);
            var items = await QueryAsync(ptsQuery);
            if (items.Count == 0)
            {
                return null!;
            }

            var pts = items[0];
            var utcNow = DateTime.UtcNow;

            List<ProjectTaskStatusDocument> siblings;
            ProjectDocument? project = null;
            if (pts.ProjectId.HasValue)
            {
                var pk = new PartitionKey((double)pts.ProjectId.Value);
                siblings = await QueryAsync(new QueryDefinition("SELECT * FROM c WHERE c.ProjectId = @projectId").WithParameter("@projectId", pts.ProjectId.Value), pk);
                project = await ReadProjectAsync(pts.ProjectId.Value);
            }
            else
            {
                siblings = new List<ProjectTaskStatusDocument>();
            }

            var tasksDict = await LoadTasksDictionaryAsync();
            var statusesDict = await LoadStatusesDictionaryAsync();

            int? dynamicStatusId = DynamicTaskStatus.Compute(pts, siblings, project, utcNow);

            if (!dynamicStatusId.HasValue ||
                !pts.TaskId.HasValue ||
                !statusesDict.TryGetValue(dynamicStatusId.Value, out var statusDoc) ||
                !tasksDict.TryGetValue(pts.TaskId.Value, out var taskDoc))
            {
                return null!;
            }

            return new SingleTask
            {
                ID = pts.ID,
                ProjectId = pts.ProjectId,
                Category = taskDoc.Category!,
                Type = taskDoc.Cap2Type!,
                Task = taskDoc.Cap2Name!,
                IsMilestone = taskDoc.IsMilestone ?? false,
                PercentageComplete = taskDoc.PercentageComplete,
                Status = statusDoc.Status!,
                StatusId = dynamicStatusId,
                Value = pts.Value,
                Notes = pts.Notes!,
                StatusDate = pts.StatusDate,
                LastModifiedDate = pts.LastModifiedDate,
                LastModifiedBy = pts.LastModifiedBy!,
                DueDate = pts.DueDate,
                AssignedTo = pts.AssignedTo!,
                AssignedDate = pts.AssignedDate,
                SurveyDays = pts.SurveyDays
            };
        }

        public async Task<List<TaskStatusGroup>> GetStatusGroups(string category)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE STRINGEQUALS(RTRIM(c.Category), RTRIM(@cat), true) ORDER BY c.Id")
                .WithParameter("@cat", category);

            var result = new List<TaskStatusGroup>();
            using var feed = _taskStatusGroupsContainer.GetItemQueryIterator<TaskStatusGroupDocument>(query);
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                foreach (var doc in response)
                {
                    result.Add(new TaskStatusGroup
                    {
                        Id = doc.Id,
                        Category = doc.Category!,
                        Name = doc.Name!,
                        StartedTaskId = doc.StartedTaskId,
                        CompletedTaskId = doc.CompletedTaskId
                    });
                }
            }

            return result;
        }

        public async Task<IEnumerable<Status>> GetStatuses()
        {
            var query = new QueryDefinition("SELECT c.ID, c.Status FROM c WHERE STRINGEQUALS(RTRIM(c.Type), 'Task', true)");

            var result = new List<Status>();
            using var feed = _statusesContainer.GetItemQueryIterator<StatusDocument>(query);
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                foreach (var doc in response)
                {
                    result.Add(new Status
                    {
                        ID = doc.ID,
                        Name = doc.Status!
                    });
                }
            }

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
            string? oldAssigned = null;
            bool updated = false;

            var cutAssignedTo = task.AssignedTo != null && task.AssignedTo.Length > 100 ? task.AssignedTo[..100] : task.AssignedTo;

            // 1. Read and update the PTS item with id = task.ID in partition task.ProjectId (if both specified)
            if (task.ID.HasValue && task.ProjectId.HasValue)
            {
                var idStr = task.ID.Value.ToString();
                var pk = new PartitionKey((double)task.ProjectId.Value);

                try
                {
                    var response = await Container.ReadItemAsync<JObject>(idStr, pk);
                    var doc = response.Resource;
                    if (doc != null)
                    {
                        oldAssigned = doc["AssignedTo"]?.Value<string>();

                        var cutNotes = task.Notes != null && task.Notes.Length > 4000 ? task.Notes[..4000] : task.Notes;
                        var lastModBy = currentUser == null ? "SYSTEM" : (currentUser.Length > 100 ? currentUser[..100] : currentUser);
                        var roundValue = task.Value.HasValue ? Math.Round(task.Value.Value, 4, MidpointRounding.AwayFromZero) : (decimal?)null;

                        doc["StatusId"] = task.StatusId.HasValue ? (JToken)task.StatusId.Value : JValue.CreateNull();
                        doc["Notes"] = cutNotes != null ? (JToken)cutNotes : JValue.CreateNull();
                        doc["StatusDate"] = task.StatusDate.HasValue ? (JToken)task.StatusDate.Value.Date : JValue.CreateNull();
                        doc["Value"] = roundValue.HasValue ? (JToken)roundValue.Value : JValue.CreateNull();
                        doc["LastModifiedDate"] = DateTime.UtcNow;
                        doc["LastModifiedBy"] = lastModBy;
                        doc["DueDate"] = task.DueDate.HasValue ? (JToken)task.DueDate.Value.Date : JValue.CreateNull();
                        doc["AssignedTo"] = cutAssignedTo != null ? (JToken)cutAssignedTo : JValue.CreateNull();
                        doc["AssignedDate"] = task.AssignedDate.HasValue ? (JToken)task.AssignedDate.Value.Date : JValue.CreateNull();
                        doc["SurveyDays"] = task.SurveyDays.HasValue ? (JToken)task.SurveyDays.Value : JValue.CreateNull();

                        await Container.ReplaceItemAsync(doc, idStr, pk);
                        updated = true;
                    }
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    updated = false;
                }
            }

            // 2. Recalculate every project's status
            await _statusRecalculator.RecalculateAllAsync();

            // 3. Look up task name from Tasks table
            string? taskName = null;
            if (task.ID.HasValue)
            {
                var idStr = task.ID.Value.ToString();
                var findPtsQuery = new QueryDefinition("SELECT TOP 1 c.TaskId FROM c WHERE c.id = @id")
                    .WithParameter("@id", idStr);
                var matchingPts = await QueryAsync(findPtsQuery);
                if (matchingPts.Count > 0 && matchingPts[0].TaskId.HasValue)
                {
                    var taskIdStr = matchingPts[0].TaskId.Value.ToString();
                    try
                    {
                        var taskResponse = await _tasksContainer.ReadItemAsync<TaskDocument>(taskIdStr, new PartitionKey(taskIdStr));
                        taskName = taskResponse.Resource?.Name;
                    }
                    catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        taskName = null;
                    }
                }
            }

            // 4. Activity: 'Updated task ' + name (null name -> null text; cut 500)
            string? actText = taskName != null ? "Updated task " + taskName : null;
            if (actText != null && actText.Length > 500) actText = actText[..500];
            await _activityWriter.CreateActivityAsync(task.ProjectId, actText, currentUser);

            // 5. If assignedTo changed (ignore case and trailing spaces only) -> assignment activity
            var oldTrim = (oldAssigned ?? string.Empty).TrimEnd(' ');
            var newTrim = (cutAssignedTo ?? string.Empty).TrimEnd(' ');
            if (!string.Equals(oldTrim, newTrim, StringComparison.OrdinalIgnoreCase))
            {
                var assignText = string.Concat("Task ", taskName ?? string.Empty, " assigned to ", cutAssignedTo ?? string.Empty);
                if (assignText.Length > 500) assignText = assignText[..500];
                await _activityWriter.CreateActivityAsync(task.ProjectId, assignText, currentUser);
            }

            return updated;
        }

        public Task<SingleTask> GetTask(int id)
        {
            throw new NotSupportedException("CAP.Read_Task_ById does not exist in the database and is not used.");
        }

        public Task<bool> CloseTechnicalTasks(int projectId)
        {
            throw new NotSupportedException("CAP.Close_TechnicalTasks does not exist in the database and is not used.");
        }

        public Task<bool> CloseInvoiceTasks(int projectId)
        {
            throw new NotSupportedException("CAP.Close_InvoiceTasks does not exist in the database and is not used.");
        }

        private async Task<ProjectDocument?> ReadProjectAsync(int projectId)
        {
            var projIdStr = projectId.ToString();
            try
            {
                var response = await _projectsContainer.ReadItemAsync<ProjectDocument>(projIdStr, new PartitionKey(projIdStr));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private async Task<Dictionary<int, TaskDocument>> LoadTasksDictionaryAsync()
        {
            var dict = new Dictionary<int, TaskDocument>();
            var query = new QueryDefinition("SELECT * FROM c");
            using var feed = _tasksContainer.GetItemQueryIterator<TaskDocument>(query);
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                foreach (var t in response)
                {
                    dict[t.Id] = t;
                }
            }
            return dict;
        }

        private async Task<Dictionary<int, StatusDocument>> LoadStatusesDictionaryAsync()
        {
            var dict = new Dictionary<int, StatusDocument>();
            var query = new QueryDefinition("SELECT * FROM c");
            using var feed = _statusesContainer.GetItemQueryIterator<StatusDocument>(query);
            while (feed.HasMoreResults)
            {
                var response = await feed.ReadNextAsync();
                foreach (var s in response)
                {
                    dict[s.ID] = s;
                }
            }
            return dict;
        }
    }
}
