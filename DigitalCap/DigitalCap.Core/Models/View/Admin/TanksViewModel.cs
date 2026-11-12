using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.Models.View.Admin
{
    public class TanksViewModel
    {
        public bool IsActive { get; set; }
        public List<Tank> Tanks { get; set; }
    }
    public class TankFilterModel
    {
        public string Text { get; set; }
        public string Value { get; set; }
    }
    public class Tank
    {
        public Guid TankId { get; set; }

        [Display(Name = "Tank Name")]
        public string TankName { get; set; }

        public int VesselId { get; set; }

        [Display(Name = "Vessel Type")]
        public string VesselType { get; set; }

        [Display(Name = "Vessel Name")]
        public string VesselName { get; set; }

        [Display(Name = "IMO Number")]
        public string IMONumber { get; set; }

        [Display(Name = "Tank Type")]
        public string TankType { get; set; }

        public int TankTypeId { get; set; }
        public bool Status { get; set; } = true;

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }
        public List<BreadCrumb> Breadcrumbs { get; set; } = new List<BreadCrumb>();
        public string Controller { get; set; }
        public string PostAction { get; set; }
        public string Title { get; set; }
        public string CancelUrl { get; set; }
        public string SaveText { get; set; }
        public bool NewDescription { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
        public bool IsDownload { get; set; }
        [StringLength(100)]
        public string Subheader { get; set; }
        public string ProjectName { get; set; }

        public int ProjectId { get; set; }
    }

    public class Vessels
    {
        public int VesselId { get; set; }
        public string VesselName { get; set; }
    }

    public class VesselTypes
    {
        public string VesselType { get; set; }
    }
    public class IMOTankFilterModel
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public int ProjectId { get; set; }
    }

    public class CreateTankRequest
    {
        public Guid? TankId { get; set; }
        public string TankName { get; set; }
        public string? Subheader { get; set; }

        public string? VesselType { get; set; }
        public string? VesselName { get; set; }
        public string TankType { get; set; }
        public int TankTypeId { get; set; }
        public string? IMONumber { get; set; }
        public string? ProjectName { get; set; }
        public int? ProjectId { get; set; }
        public bool Status { get; set; }
    }


}
