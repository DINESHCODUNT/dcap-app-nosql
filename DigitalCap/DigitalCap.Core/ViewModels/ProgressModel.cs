using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class ProgressModel
    {

        public List<ProgressNode> Nodes { get; private set; }

        public ProgressModel(IEnumerable<SingleTask> tasks, List<TaskStatusGroup> groups) : this(tasks, groups, null)
        {
        }

        public ProgressModel(IEnumerable<SingleTask> tasks, List<TaskStatusGroup> groups, DateTimeOffset? createdDate)
        {
            Nodes = new List<ProgressNode>();
            BuildProgressModel(tasks, groups, createdDate);
        }

        private void BuildProgressModel(IEnumerable<SingleTask> tasks, List<TaskStatusGroup> groups, DateTimeOffset? createdDate)
        {

            if (tasks == null || !tasks.Any())
            {
                return;
            }
            var taskDict = tasks.ToDictionary(x => x.TaskId);

            Dictionary<string, List<TaskStatusGroup>> groupDict = CreateTaskGroupDictionary(groups);


            var groupNames = groups.Select(x => x.Name).Distinct();
            foreach (var name in groupNames)
            {
                ProgressState state = GetProgressState(taskDict, groupDict, name);
                DateTime? statusDate = null;
                if (state == ProgressState.InProgress || state == ProgressState.Completed)
                {
                    statusDate = GetStatusDate(taskDict, groupDict, name, state, createdDate);
                }

                Nodes.Add(new ProgressNode(name, state, statusDate));
            }
        }

        private static Dictionary<string, List<TaskStatusGroup>> CreateTaskGroupDictionary(List<TaskStatusGroup> groups)
        {
            var groupDict = new Dictionary<string, List<TaskStatusGroup>>();
            foreach (var group in groups)
            {
                if (!groupDict.ContainsKey(group.Name))
                {
                    groupDict.Add(group.Name, new List<TaskStatusGroup>());
                }
                groupDict[group.Name].Add(group);
            }

            return groupDict;
        }

        private static ProgressState GetProgressState(Dictionary<int?, SingleTask> taskDict, Dictionary<string, List<TaskStatusGroup>> groupDict, string name)
        {
            ProgressState state = ProgressState.NotStarted;
            if (groupDict[name].Any(x => x.CompletedTaskId == 0))
            {
                state = ProgressState.Completed;
            }
            else if (groupDict[name].All(x => taskDict[x.CompletedTaskId].StatusId == (int)CapTaskStatus.Completed))
            {
                state = ProgressState.Completed;
            }
            else if (groupDict[name].All(x => taskDict[x.StartedTaskId].StatusId == (int)CapTaskStatus.Completed))
            {
                state = ProgressState.InProgress;
            }

            return state;
        }

        private static DateTime? GetStatusDate(Dictionary<int?, SingleTask> taskDict, Dictionary<string, List<TaskStatusGroup>> groupDict, string name, ProgressState state, DateTimeOffset? createdDate)
        {
            if (state == ProgressState.Completed)
            {
                if (groupDict[name][0].CompletedTaskId == 0)
                {
                    return createdDate != null ? createdDate.Value.DateTime : null;
                }
                return taskDict[groupDict[name][0].CompletedTaskId].StatusDate;
            }
            else
            {
                return taskDict[groupDict[name][0].StartedTaskId].StatusDate;
            }
        }
    }

    public class ProgressNode
    {
        public string Title { get; set; }
        public ProgressState State { get; set; }
        public DateTime? StatusDate { get; set; }

        public ProgressNode(string title, ProgressState state, DateTime? statusDate = null)
        {
            Title = title;
            State = state;
            StatusDate = statusDate;
        }


    }

    public enum ProgressState
    {
        None = 0,
        [Description("Not Started")]
        NotStarted = 1,
        [Description("Started")]
        InProgress = 2,
        Completed = 3
    }
}
