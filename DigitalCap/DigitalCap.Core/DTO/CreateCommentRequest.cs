using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class CreateCommentRequest
    {
        public int ProjectId { get; set; }
        public int? ProjectTaskId { get; set; }
        public string Comment { get; set; }
    }
}
