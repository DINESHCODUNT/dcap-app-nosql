using DigitalCap.Core.Models.ImageDescription;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.DTO
{
    public class ManageDescriptionResponse
    {
        public bool IsActive { get; set; }
        public bool Editable { get; set; }

        public int DescriptionRestoreFilter { get; set; }
        public int SearchDescriptionRestoreFilter { get; set; }

        public List<ImageDescriptions> ImageDescriptions { get; set; }
    }
}
