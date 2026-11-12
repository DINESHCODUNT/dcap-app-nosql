using System;
using System.Collections.Generic;

namespace DigitalCap.Core.Models.DataTransfer
{
    /// <summary>ReportStatutoryCertificates row as the reference transfers it (dbo.GetReportStatutoryCertificatesById).</summary>
    public class TransferReportStatutoryCertificates
    {
        public int ProjectId { get; set; }
        public DateTime? LlliIssueDate { get; set; }
        public DateTime? LlliExpiryDate { get; set; }
        public DateTime? SafetyConIssueDate { get; set; }
        public DateTime? SafetyConExpiryDate { get; set; }
        public DateTime? SafetyRadioIssueDate { get; set; }
        public DateTime? SafetyRadioExpiryDate { get; set; }
        public DateTime? SafetyEquipmentIssueDate { get; set; }
        public DateTime? SafetyEquipmentExpiryDate { get; set; }
        public DateTime? IoppIssueDate { get; set; }
        public DateTime? IoppExpiryDate { get; set; }
        public DateTime? IapIssueDate { get; set; }
        public DateTime? IapExpiryDate { get; set; }
        public DateTime? IafsIsuueDate { get; set; }
        public DateTime? IafsExpiryDate { get; set; }
        public DateTime? IsmIssueDate { get; set; }
        public DateTime? IsmExpiryDate { get; set; }
        public DateTime? IsExpiryDate { get; set; }
        public DateTime? IspsIssueDate { get; set; }
        public DateTime? IspsExpiryDate { get; set; }
        public DateTime? CargoGearReTestIssueDate { get; set; }
        public DateTime? CargoGearReTestExpiryDate { get; set; }
        public DateTime? CargoGearAnnualIssueCertificate { get; set; }
        public DateTime? CargoGearAnnualExpiryCertificate { get; set; }
        public DateTime? IbwmIssueDate { get; set; }
        public DateTime? IbwmExpiryDate { get; set; }
        public DateTime? IsppSewageIssueDate { get; set; }
        public DateTime? IsppSewageExpiryDate { get; set; }
        public DateTime? IeeIssueDate { get; set; }
        public DateTime? IeeExpiryDate { get; set; }
        public DateTime? MaritimeLabourIssueDate { get; set; }
        public DateTime? MaritimeLabourExpiryDate { get; set; }
        public DateTime? fitnessGasCertificateIssueDate { get; set; }
        public DateTime? fitnessGasCertificateExpiryDate { get; set; }
    }

    /// <summary>config.ReportPart row (reference DigitalCAP.Entities.Models.ReportPart).</summary>
    public class TransferReportPart
    {
        public int PartId { get; set; }
        public int? ProjectId { get; set; }
        public string? PartName { get; set; }
        public int? SequenceNo { get; set; }
        public bool? IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsDeleted { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool? IsSync { get; set; }
    }

    /// <summary>A section with offline changes, as stored locally (SectionId kept verbatim so lookups match the stored text).</summary>
    public class DirtySection
    {
        public int TemplateId { get; set; }
        public string SectionId { get; set; } = string.Empty;
    }

    /// <summary>One role-page permission row of dbo.GetPermissionOffline (reference OfflinePermission).</summary>
    public class OfflineRolePermission
    {
        public string RoleName { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public bool Read { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
    }

    /// <summary>
    /// What the desktop app needs to evaluate the signed-in user's permissions offline. Read from Azure SQL at
    /// download time (the reference fetched the same data from DataTransferDevAPI GetRolePermissions /
    /// GetCurrentUserRoles and kept it in the browser) and stored in the local Offline* tables.
    /// </summary>
    public class OfflineUserSecurity
    {
        /// <summary>The user's e-mail / UPN (CurrentUsername).</summary>
        public string UserName { get; set; } = string.Empty;
        /// <summary>AspNetUsers.Id online — the id stored in Comments.CreatedBy and similar columns.</summary>
        public string? AspNetUserId { get; set; }
        public List<string> Roles { get; set; } = new();
        /// <summary>Permission claim types of the user's roles and of the user (DigitalCap.Core.Security.Permission names).</summary>
        public List<string> Claims { get; set; } = new();
        /// <summary>Every role's page permissions (the full table, as the reference stored it).</summary>
        public List<OfflineRolePermission> RolePermissions { get; set; } = new();
    }

    public class PendingChanges
    {
        public bool HasChanges { get; set; }
        public int SectionCount { get; set; }
    }

    public class LockOwner
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    public class OnlineLockStatus
    {
        public bool IsLocal { get; set; }
        public bool IsOnline { get; set; }
        public bool LockedByMe { get; set; }
        public LockOwner? LockedBy { get; set; }
    }

    public enum DataTransferStatus
    {
        Ok,
        Conflict,
        Offline,
        NotFound,
        Error
    }

    public class DataTransferResult
    {
        public DataTransferStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Released { get; set; }
        /// <summary>Sync only: true once the server accepted the data (MoveToOnlineTable succeeded).</summary>
        public bool Uploaded { get; set; }
        public LockOwner? LockedBy { get; set; }

        public bool Success => Status == DataTransferStatus.Ok;

        public static DataTransferResult Ok(string message) => new() { Status = DataTransferStatus.Ok, Message = message };
        public static DataTransferResult Fail(DataTransferStatus status, string message) => new() { Status = status, Message = message };
    }
}
