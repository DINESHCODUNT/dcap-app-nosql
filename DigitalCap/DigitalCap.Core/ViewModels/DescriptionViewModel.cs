using DigitalCap.Core.Models.Grading;
using DigitalCap.Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.ViewModels
{

    public class DescriptionViewModel
    {
        public bool IsActive { get; set; }
        public bool Editable { get; set; }
    }


    public class ImageDescriptionViewModel
    {
        public int Id { get; set; }

        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string VesselType { get; set; }
        public string SectionName { get; set; }



        public string DescriptionName { get; set; }
        public bool isActive { get; set; }

        public Guid? SectionId { get; set; }
        public int? TankTypeId { get; set; }
    }

}
