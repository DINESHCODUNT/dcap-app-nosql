using System;
using System.ComponentModel.DataAnnotations;

namespace DigitalCap.WebApi.Models
{
    public class SaveTaskRequest
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int Status { get; set; }

        public DateTime? StatusDate { get; set; }

        public string? AssignedTo { get; set; }

        public DateTime? DueDate { get; set; }

        public decimal? Value { get; set; }

        public int? SurveyDays { get; set; }
    }
}
