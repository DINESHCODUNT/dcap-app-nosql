using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class SingleTask
    {
        public int? ID { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int? StatusId { get; set; }
        public int? TaskId { get; set; }
        public string Category { get; set; }
        public string Task { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }

        public decimal? Value { get; set; }

        public DateTime? StatusDate { get; set; }

        public DateTime? LastModifiedDate { get; set; }
        public string LastModifiedBy { get; set; }
        public int? DaysWorked { get; set; }
        public bool? HasAttachments { get; set; }
        public int NumberOfAttachments { get; set; }
        public int NumberOfComments { get; set; }
        public DateTime? DueDate { get; set; }

        public string AssignedTo { get; set; }
        public bool IsMilestone { get; set; }
        public byte? PercentageComplete { get; set; }
        public DateTime? AssignedDate { get; set; }
        public int? SurveyDays { get; set; }

        public bool View { get; set; }
        public bool Update { get; set; }
    }
}
