using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class CommentModel
    {
        public int Id { get; set; }
        public int? ProjectId { get; set; }
        public int? ProjectTaskId { get; set; } = null;
        public string UserId { get; set; }
        public bool Editable { get; set; }
        public string CreatedBy { get; set; }
        public DateTimeOffset LastModifiedDate { get; set; }
        public string Comment { get; set; }
    }

    public class CreateCommentRequest
    {
        public int ProjectId { get; set; }

        public int? ProjectTaskId { get; set; }

        public string Comment { get; set; } = "";
    }
}
