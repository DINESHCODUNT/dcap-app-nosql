export const Permission = {
    WorkflowAndTasksEditTaskDetails: 'WorkflowAndTasksEditTaskDetails',
    WorkflowAndTasksManageMyComments: 'WorkflowAndTasksManageMyComments',
    WorkflowAndTasksManageFiles: 'WorkflowAndTasksManageFiles',
    WorkflowAndTasksManageFilesOnlyInternalDocumentsBucket: 'WorkflowAndTasksManageFilesOnlyInternalDocumentsBucket',

    InvoicingAndTasksEditTaskDetails: 'InvoicingAndTasksEditTaskDetails',
    InvoicingAndTasksManageMyComments: 'InvoicingAndTasksManageMyComments',
    InvoicingAndTasksManageFiles: 'InvoicingAndTasksManageFiles',

    ViewAbsProjectFiles: 'ViewAbsProjectFiles'
} as const;

export type PermissionType = typeof Permission[keyof typeof Permission];
