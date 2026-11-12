using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Permissions
{
    public class UserPermission
    {
        public Guid Id { get; set; }
        public string PermissionName { get; set; }
        public string RoleName { get; set; }

    }
    public class Permissions
    {
        public Guid Id { get; set; }
        public string PermissionName { get; set; }
        public bool Read { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
    }
    public class Mode
    {
        public bool Read { get; set; }
        public bool Edit { get; set; }
        public bool Delete { get; set; }
    }
    public class CurrentUserPermission
    {
        public Mode AddProject { get; set; }
        public Mode CancelProject { get; set; }
        public Mode DeleteProject { get; set; }
        public Mode InternalDoc { get; set; }
        public Mode ClientDeliverables { get; set; }
        public Mode InternalInvoicing { get; set; }
        public Mode ReportExport { get; set; }
        public Mode ReportExportAll { get; set; }
        public Mode ProjectFiles { get; set; }
        public Mode Report_Edit { get; set; }
        public Mode ResetTemplate { get; set; }
        public Mode RevenueTracker { get; set; }
        public Mode ViewProject { get; set; }
        public Mode ManageTank { get; set; }
        public Mode ManageGrades { get; set; }
        public Mode ManageDescription { get; set; }
        public Mode ExportProject { get; set; }
        public Mode ManageExport { get; set; }

        public void MapFromPermissions(List<Permissions> permissions)
        {
            AddProject = GetModeForPermission(permissions, ManagePages.AddProject);
            CancelProject = GetModeForPermission(permissions, ManagePages.CancelProject);
            DeleteProject = GetModeForPermission(permissions, ManagePages.DeleteProject);
            InternalDoc = GetModeForPermission(permissions, ManagePages.InternalDoc);
            ClientDeliverables = GetModeForPermission(permissions, ManagePages.ClientDeliverables);
            InternalInvoicing = GetModeForPermission(permissions, ManagePages.InternalInvoicing);
            ReportExport = GetModeForPermission(permissions, ManagePages.ReportExport);
            ReportExportAll = GetModeForPermission(permissions, ManagePages.ReportExportAll);
            ProjectFiles = GetModeForPermission(permissions, ManagePages.ProjectFiles);
            Report_Edit = GetModeForPermission(permissions, ManagePages.ReportEdit);
            ResetTemplate = GetModeForPermission(permissions, ManagePages.ResetTemplate);
            RevenueTracker = GetModeForPermission(permissions, ManagePages.RevenueTracker);
            ViewProject = GetModeForPermission(permissions, ManagePages.ViewProject);
            ManageTank = GetModeForPermission(permissions, ManagePages.ManageTank);
            ManageGrades = GetModeForPermission(permissions, ManagePages.ManageGrades);
            ManageDescription = GetModeForPermission(permissions, ManagePages.ManageDescription);
            ExportProject = GetModeForPermission(permissions, ManagePages.ExportProject);
            ManageExport = GetModeForPermission(permissions, ManagePages.ManageExport);


        }
        private Mode GetModeForPermission(List<Permissions> permissions, ManagePages page)
        {
            var permission = permissions.FirstOrDefault(p => p.PermissionName.Replace(" ", "") == page.ToString());
            return permission != null ? new Mode { Read = permission.Read, Edit = permission.Edit, Delete = permission.Delete } : new Mode();
        }
    }

}
