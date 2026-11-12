using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models
{
    public class ProjectTankSectionMapping
    {
        public Guid TankSectionMappingId { get; set; }
        public int? ProjectId { get; set; }
        public int? PartId { get; set; }
        public int? TankTypeId { get; set; }
        public int? TotalCards { get; set; }
        public int? PlaceholderCount { get; set; }
        public int? FileNameCount { get; set; }
        public bool? IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsDeleted { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool? IsSync { get; set; }
    }
    public class ProjectSubSection
    {
        public Guid Id { get; set; }
        public Guid ProjectSectionId { get; set; }
        public string SectionName { get; set; }
        public int? Totalcards { get; set; }
        public int? FileNameCount { get; set; }
        public int? PlaceholderCount { get; set; }
        public bool? IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public bool? IsSync { get; set; }
        public int OrderNumber { get; set; }
    }

    public class ProjectSectionMapping
    {
        public Guid SectionMappingId { get; set; }
        public int? ProjectId { get; set; }
        public int? PartId { get; set; }
        public string SectionName { get; set; }
        public string SubHeader { get; set; }
        public int? TotalCards { get; set; }
        public int? PlaceholderCount { get; set; }
        public int? FileNameCount { get; set; }
        public bool? IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsDeleted { get; set; }
        public bool? HasSubSection { get; set; }
        public int? Order { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool? IsSync { get; set; }
        public bool? RequiredInReport { get; set; }
    }

    public class ProjectSectionGradingMapping
    {
        public Guid SectionId { get; set; }
        public int CategoryId { get; set; }
        public bool? IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public bool? IsSync { get; set; }
    }

    public class ProjectSubSectionDescriptionMapping
    {
        public Guid SubSectionId { get; set; }
        public int CategoryId { get; set; }
        public bool? IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public bool? IsSync { get; set; }
    }
    public class ProjectSectionDescriptionMapping
    {
        public Guid SectionId { get; set; }
        public int CategoryId { get; set; }
        public bool? IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public bool? IsSync { get; set; }
    }

    public class H_I_Grading
    {
        public int Id { get; set; }
        public string LabelName { get; set; }
        public int? GradingConditionGroupId { get; set; }
        public bool? SpecialGasGrading { get; set; }
    }
}
