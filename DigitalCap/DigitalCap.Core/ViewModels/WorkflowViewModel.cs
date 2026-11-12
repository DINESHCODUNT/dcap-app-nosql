using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class WorkflowViewModel
    {
        public ProgressModel ProgressModel { get; set; }
        public TasksViewModel TasksViewModel { get; set; }
        public byte PercentComplete { get; set; }
    }
}
