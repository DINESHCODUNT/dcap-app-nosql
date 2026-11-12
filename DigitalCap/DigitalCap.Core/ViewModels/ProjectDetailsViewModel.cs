using DigitalCap.Core.Enumerations;
using DigitalCap.Core.Helpers.Constants;
using DigitalCap.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DigitalCap.Core.ViewModels
{
    public class ProjectDetailsViewModel
    {
        public int? Id { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public ProjectStatus ProjectStatus { get; set; }
        public string VesselName { get; set; }
        public int? IMO { get; set; }
        public string CompanyName { get; set; }
        public DateTime? BuildDate { get; set; }
        public CapType CAPType { get; set; }
        public string CAPRegion { get; set; }
        public string CAPScope { get; set; }
        public string CAPScopeOther { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTimeOffset? StatusChangedDate { get; set; }
        public string StatusChangedBy { get; set; }
        public string ProjectComments { get; set; }
        public string CAPCoordinatorName { get; set; }
        public string CAPCoordinatorTelephone { get; set; }
        public string CAPCoordinatorMobile { get; set; }
        public string CAPCoordinatorEmail { get; set; }
        public string CAPCoordinatorManagerName { get; set; }
        public string CAPCoordinatorManagerTelephone { get; set; }
        public string CAPCoordinatorManagerMobile { get; set; }
        public string CAPCoordinatorManagerEmail { get; set; }

        [Display(Name = "CAP Contract Value")]
        public decimal? CapContractValue { get; set; }

        public ProjectDetailsViewModel(Project project, List<CAPCoordinator> capCoordinators = null)
        {
            Id = project.Id;
            ProjectId = project.PIDNumber;
            VesselName = project.VesselName;
            IMO = project.IMO;
            CompanyName = project.CompanyName;
            CAPType = project.CapType;
            CAPRegion = project.CapRegion;
            CAPScope = project.CapScope;
            CAPScopeOther = project.CapScopeOther;
            ProjectStatus = (ProjectStatus)project.ProjectStatus;
            CreatedDate = project.CreatedDate;
            CreatedBy = project.CreatedBy;
            LastModifiedBy = project.LastModifiedBy;
            CapContractValue = project.CapContractValue;
            StatusChangedBy = project.StatusChangedBy;
            StatusChangedDate = project.StatusChangedDate;
            ProjectComments = project.ProjectComments;
            ProjectName = project.Name;

            if (capCoordinators != null)
            {
                var capCoordinator = capCoordinators.Find(x => x.Region != "Manager");
                CAPCoordinatorName = capCoordinator.Name;
                CAPCoordinatorTelephone = capCoordinator.Telephone;
                CAPCoordinatorMobile = capCoordinator.Mobile;
                CAPCoordinatorEmail = capCoordinator.Email;

                var capCoordinatorManager = capCoordinators.Find(x => x.Region == "Manager");
                CAPCoordinatorManagerName = capCoordinatorManager.Name;
                CAPCoordinatorManagerTelephone = capCoordinatorManager.Telephone;
                CAPCoordinatorManagerMobile = capCoordinatorManager.Mobile;
                CAPCoordinatorManagerEmail = capCoordinatorManager.Email;
            }


            if (project.MonthBuilt != null && project.YearBuilt != null && project.MonthBuilt != 0 && project.YearBuilt != 0)
            {
                BuildDate = new DateTime(project.YearBuilt.Value, project.MonthBuilt.Value, 1);
            }

        }
    }

}
