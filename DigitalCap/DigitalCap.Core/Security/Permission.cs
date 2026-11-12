using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DigitalCap.Core.Security
{
    public enum Permission
    {
        #region ABS Dashboard
        ViewAbsDashboard,
        #endregion

        #region Recent Activity All Projects
        ViewRecentActivityAllProjectsAllRegions,

        ViewRecentActivityAllProjectsOwnRegion,
        #endregion

        #region Contracted Backlog Gantt Chart
        ViewContractedBacklogAllRegions,

        ViewContractedBacklogOwnRegion,

        ViewContractedBacklogOwnProjects,
        #endregion

        #region Survey Trends
        ViewSurveyTrends,
        #endregion

        #region All Projects
        [Description("View All Projects")]
        ViewAllProjects,

        [Description("Add Project")]
        AddProject,

        [Description("Open Project")]
        OpenProject,

        [Description("Export Project")]
        ExportProject,
        #endregion

        #region ABS Project Landing Page
        [Description("View ABS Project Landing Page")]
        ViewAbsProjectLandingPage,

        [Description("Revenue Tracker")]
        AbsProjectLandingPageRevenueTracker,

        [Description("Manage My Comments")]
        AbsProjectLandingPageManageMyComments,

        [Description("Edit Report Details")]
        AbsProjectLandingPageEditReportDetails,

        [Description("Cancel Button")]
        AbsProjectLandingPageCancelButton,

        [Description("Delete Button")]
        AbsProjectLandingPageDeleteButton,

        [Description("Close Button")]
        AbsProjectLandingPageCloseButton,

        [Description("Reassessment Button")]
        AbsProjectLandingPageReassessmentButton,

        [Description("Project Details Link")]
        AbsProjectLandingPageProjectDetailsLink,

        [Description("Workflow Link")]
        AbsProjectLandingPageWorkflowLink,

        [Description("Recent Activity Link")]
        AbsProjectLandingPageRecentActivityLink,

        [Description("Project Files Link")]
        AbsProjectLandingPageProjectFilesLink,

        [Description("Project Files Link (Only Internal Documents Bucket)")]
        AbsProjectLandingPageProjectFilesLinkOnlyInternalDocumentsBucket,

        [Description("Report Link")]
        AbsProjectLandingPageReportLink,

        [Description("Client Deliverable Report Link")]
        AbsProjectLandingPageClientDeliverableReportLink,

        #endregion

        #region Invoicing And Tasks
        [Description("View Invoicing And Tasks")]
        ViewInvoicingAndTasks,

        [Description("View Task Page")]
        InvoicingAndTasksViewTaskPage,

        [Description("Edit Task Details")]
        InvoicingAndTasksEditTaskDetails,

        [Description("Manage My Comments")]
        InvoicingAndTasksManageMyComments,

        [Description("Manage Files")]
        InvoicingAndTasksManageFiles,
        #endregion

        #region Workflow And Tasks
        [Description("View Workflow And Tasks")]
        ViewWorkflowAndTasks,

        [Description("View Task Page")]
        WorkflowAndTasksViewTaskPage,

        [Description("Edit Task Details")]
        WorkflowAndTasksEditTaskDetails,

        [Description("Manage My Comments")]
        WorkflowAndTasksManageMyComments,

        [Description("Manage Files")]
        WorkflowAndTasksManageFiles,

        [Description("Manage Files (Only Internal Documents Bucket)")]
        WorkflowAndTasksManageFilesOnlyInternalDocumentsBucket,

        #endregion

        #region ABS Project Files
        [Description("View ABS Project Files")]
        ViewAbsProjectFiles,

        [Description("Add Files")]
        AbsProjectFilesAddFiles,

        [Description("Add Files (Only Internal Documents Bucket)")]
        AbsProjectFilesAddFilesOnlyInternalDocumentsBucket,

        [Description("Delete Files")]
        AbsProjectFilesDeleteFiles,

        [Description("Delete Files (Only Internal Documents Bucket)")]
        AbsProjectFilesDeleteFilesOnlyInternalDocumentsBucket,

        [Description("Edit/Publish Files")]
        AbsProjectFilesEditPublishFiles,

        [Description("Edit/Publish Files (Only Internal Documents Bucket)")]
        AbsProjectFilesEditPublishFilesOnlyInternalDocumentsBucket,

        #endregion

        #region Project Details
        [Description("View Project Details")]
        ViewProjectDetails,

        [Description("Edit/Save Project Details")]
        EditSaveProjectDetails,

        #endregion

        #region Project Recent Activity
        [Description("View Project Recent Activity")]
        ViewProjectRecentActivity,
        #endregion

        #region Manage Client Profiles
        ViewClientProfiles,

        ManageClientProfiles,

        ManageClientAdmins,

        EditClientProfileDetails,

        #endregion

        #region Manage ABS Users
        ViewAbsUsers,

        ManageAbsUsers,
        #endregion

        #region Client Dashboard
        ViewClientDashboard,

        ClientDashboardDownloadDeliverables,
        #endregion

        #region Client Project Landing Page
        ViewClientProjectLandingPage,

        ClientProjectLandingPageDownloadDeliverables,

        ClientProjectLandingPageShareDeliverables,

        ClientProjectLandingPageProjectFilesLink,
        #endregion

        #region Client Project Files
        ViewClientProjectFiles,

        ManageClientProjectFiles,
        #endregion

        #region Manage Client Users
        ViewClientUsers,

        ManageClientUsers,
        #endregion

        #region Special
        SystemAdministrator,

        #endregion
    }
}
