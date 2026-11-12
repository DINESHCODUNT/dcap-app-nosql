using DapperExtensions.Mapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class TaskStatusGroup
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public int StartedTaskId { get; set; }
        public int CompletedTaskId { get; set; }
    }

    public class TaskStatusGroupOffline
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public int StartedTaskId { get; set; }
        public int CompletedTaskId { get; set; }
    }
    public class TaskStatusGroupMapper : ClassMapper<TaskStatusGroup>
    {
        public TaskStatusGroupMapper()
        {
            Table("[CAP].[TaskStatusGroups]");
            AutoMap();
        }
    }
    public class TaskStatusGroupOfflineMapper : ClassMapper<TaskStatusGroupOffline>
    {
        public TaskStatusGroupOfflineMapper()
        {
            Table("[TaskStatusGroups]");
            AutoMap();

        }
    }

}
