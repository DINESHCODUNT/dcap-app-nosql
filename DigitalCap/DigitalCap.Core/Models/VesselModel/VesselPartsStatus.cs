using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.VesselModel
{
    public class VesselPartsStatus : CachedData
    {
        public string Uid { get; set; }
        public string ParentUid { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string SurveyName { get; set; }
        public DateTime? SurveyDueDate { get; set; }
        public string InspectionType { get; set; }
        public string AbsProgram { get; set; }
        public string Status { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? DoneDate { get; set; }
        public DateTime? ExtendedDueDate { get; set; }
        public string AbsGroup { get; set; }
        public string AbsSubGroup { get; set; }
        public string AbsCategory { get; set; }
        public string AssetNumber { get; set; }
        public string ItemType { get; set; }

        public string ItemTypeHierarchy { get; set; }

        public string Hierarchy { get; set; }

    }

}
