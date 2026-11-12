using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class ShareTaskViewModel
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public string Message { get; set; }
        public string Recipients { get; set; }
    }
}
