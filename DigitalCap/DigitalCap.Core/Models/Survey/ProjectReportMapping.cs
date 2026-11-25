using DigitalCap.Core.Models;
using DigitalCap.Core.Models.ImageDescription;
using DigitalCap.Core.Models.ReportConfig;
using DigitalCap.Core.Models.Survey;
using DigitalCap.Core.Models.Tank;
using DigitalCap.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Survey
{
    public class ProjectReportMapping
    {
        public Guid Id { get; set; }
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }
        public string Notes { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsSync { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public DateTime UpdatedDttm { get; set; }
        public bool RequiredInReport { get; set; }
    }
    public class Images
    {
        public int Id { get; set; }
        public string AssignmentId { get; set; }
        public string FileId { get; set; }
        public DateTime InsertDate { get; set; }
        public int ProjectId { get; set; }
    }
    public class LoginData
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public string ProviderKey { get; set; }
    }

    public class ProjectCardMapping
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }
        public int CardId { get; set; }
        public int DescriptionId { get; set; }
        public string AdditionalDescription { get; set; }
        public int? CurrentCondition { get; set; }
        public int ImageId { get; set; }
        public string Src { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public bool IsSync { get; set; }



    }
    public class Comment
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int ProjectTaskId { get; set; }
        public string CommentText { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedDate { get; set; }
    }

    public class IsSynchedOnline
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ProjectId { get; set; }
        public DateTime DownloadOfflineDate { get; set; }
        public string UserName { get; set; }
        public string EmailId { get; set; }
        public DateTime SynchedDate { get; set; }
        public bool IsSynched { get; set; }
        public string Name { get; set; }

    }

    public class HandIProjectGrading
    {
        public int Id { get; set; }
        public int? ProjectID { get; set; }
        public int? GradingId { get; set; }
        public string Value { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public string CreatedBY { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBY { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool IsSync { get; set; }
    }
    public class HandIProjectGradingConditions
    {
        public int Id { get; set; }
        public int? GradingID { get; set; }
        public int? ProjectId { get; set; }
        public string ConditionLabel { get; set; }
        public bool? Value { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public string CreatedBY { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBY { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool IsSync { get; set; }
    }

    public class ProjectGradingMapping
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }
        public int GradingId { get; set; }
        public string Value { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public DateTime UpdatedDttm { get; set; }
        public bool IsSync { get; set; }
    }
    public class ProjectGradingConditionMapping
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TemplateId { get; set; }
        public Guid SectionId { get; set; }
        public int GradingId { get; set; }
        public string GradingConditionLabel { get; set; }
        public bool GradingConditionValue { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDttm { get; set; }
        public DateTime UpdateDttm { get; set; }
        public DateTime UpdatedDttm { get; set; }
        public bool IsSync { get; set; }

    }
}
