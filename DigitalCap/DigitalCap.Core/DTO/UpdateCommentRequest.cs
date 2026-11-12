using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class UpdateCommentRequest
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public string Comment { get; set; } = string.Empty;
    }
}
