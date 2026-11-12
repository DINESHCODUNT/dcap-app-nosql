using DigitalCap.Core.Models.Grading;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;


namespace DigitalCap.Core.ViewModels
{
    public class GradingViewModel
    {

        public bool IsActive { get; set; }

        public bool Editable { get; set; }
    }

    public class GradingListViewModel
    {
        public GradingListViewModel()
        {
            Status = Active;
        }
        public int Id { get; set; }
        [Display(Name = "Part Name")]
        public string TemplateName { get; set; }
        [Display(Name = "Vessel Template")]
        public string VesselType { get; set; }
        public string SectionName { get; set; }
        public string GradingName { get; set; }

        [Required]

        public bool Status { get; set; } = true;

        public int CategoryId { get; set; }
        public string TemplateId { get; set; }
        public int VesselTypeId { get; set; }
        public Guid SectionId { get; set; }
        public bool Active { get; set; } = true;
        public bool InActive { get; set; }
        public List<Models.Grading.Grading> gradingslist { get; set; }

        public List<GradingSection> sections { get; set; }
        public List<BreadCrumb> Breadcrumbs { get; set; } = new List<BreadCrumb>();
        public string Controller { get; set; }
        public string PostAction { get; set; }
        public string Title { get; set; }
        public string CancelUrl { get; set; }
        public string SaveText { get; set; }
        public bool RequiredInReport { get; set; } = true;
        public bool NewGrading { get; set; }

        public int TanktypeId { get; set; }
        public int GradingId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
    }
}
