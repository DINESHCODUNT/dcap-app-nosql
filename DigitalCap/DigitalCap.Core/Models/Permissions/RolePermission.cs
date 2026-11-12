using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DigitalCap.Core.Models.Permissions
{
    public class UserRolePermission
    {
        public Guid Id { get; set; }
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }
        public bool IsReadable { get; set; }
        public bool IsEditable { get; set; }
        public bool IsDeleted { get; set; }
        public bool ReadExist { get; set; }
        public bool DeleteExist { get; set; }
        public bool EditExist { get; set; }
    }
    public class UserRolePermissionViewModel
    {
        public Guid Id { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public Guid PermissionId { get; set; }
        public string PermissionName { get; set; }
        public bool IsReadable { get; set; }
        public bool IsEditable { get; set; }
        public bool IsDeleted { get; set; }
        public bool ReadExist { get; set; }
        public bool DeleteExist { get; set; }
        public bool EditExist { get; set; }
    }

    public class PermissionViewModel
    {
        public string PermissionId { get; set; }
        public string PermissionName { get; set; }
        public bool Read { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
    }

    public class Role
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public enum ManagePages
    {
       
        [Description("Project-Add")]
        AddProject,
        //Cancel Project
        [Description("Project-Cancel")]
        CancelProject,
        //Delete Project
        [Description("Project-Delete")]
        DeleteProject,
        //View Project
        [Description("Project-View")]
        ViewProject,
        //Export Project
        [Description("Project-Export")]
        ExportProject,
        //Document-Internal doc
        [Description("Document-Internal doc")]
        InternalDoc,
        //Document-Client Deliverables
        [Description("Document-Client Deliverables")]
        ClientDeliverables,
        //Document-Internal Invoicing
        [Description("Document-Internal Invoicing")]
        InternalInvoicing,
        //Project Files
        [Description("Project Files")]
        ProjectFiles,
        //Report Export
        [Description("Report-Export")]
        ReportExport,
        //Report Export All
        [Description("Report-Export All")]
        ReportExportAll,
        //Report-Edit
        [Description("Report-Edit")]
        ReportEdit,
        //Reset Template
        [Description("Report-Reset Template")]
        ResetTemplate,
        //Revenue Tracker
        [Description("Revenue Tracker")]
        RevenueTracker,
        //Manage Tank
        [Description("Manage Tank")]
        ManageTank,
        //Manage Grades
        [Description("Manage Grades")]
        ManageGrades,
        //Manage Description
        [Description("Manage Description")]
        ManageDescription,
        //Manage Template
        [Description("Manage Template")]
        ManageTemplate,
        //Manage Export
        [Description("Manage Export")]
        ManageExport,

    }
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute == null ? value.ToString() : attribute.Description;
        }
    }
}
