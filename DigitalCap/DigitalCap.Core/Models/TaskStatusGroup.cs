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
}
