using DigitalCap.Core.Models.Grading;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class ManageGradingResponse
    {
        public bool IsActive { get; set; }
        public bool Editable { get; set; }

        public int GradingRestoreFilter { get; set; }
        public int SearchGradingRestoreFilter { get; set; }

        public List<Grading> Gradings { get; set; }
    }
}
