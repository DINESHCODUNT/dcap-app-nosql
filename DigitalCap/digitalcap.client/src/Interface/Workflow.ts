
export const ProgressState = {
    None: 0,
    NotStarted: 1,
    InProgress: 2,
    Completed: 3,
} as const;

export type ProgressState = (typeof ProgressState)[keyof typeof ProgressState];

export interface ProgressNode {
    title: string;
    state: ProgressState;
    statusDate?: string | Date | null;
    completedDate?: string | Date | null;
    date?: string | Date | null;
}

export interface Workflow {
    id?: number;
    taskId?: number;
    Task?: string;
    name?: string;
    description?: string;
    title?: string;
    type?: string;
    statusId?: number;
    status?: string
    statusDate?: string | Date | null;
    isMilestone?: boolean;
    Category?: string;
    AssignedDate?: string | Date | null;
    AssignedTo?: string;
    DueDate?: string | Date | null;
    DaysWorked?: number;
    HasAttachments?: boolean;
    LastModifiedBy?: string;
    LastModifiedDate?: string | Date | null;
    Notes?: string;
    NumberOfAttachments?: number;
    NumberOfComments?: number;
    PercentageComplete?: number;
    ProjectId?: number;
    Projectid?: number;
    ProjectName?: string;
    SurveyDays?: number;
    Update?: boolean;
    View?: boolean;
    Value?: number;
    ID?: number;
    [key: string]: any;
}

/** Task row shape used by workflow / revenue editors and SaveTask. */
export type WorkflowTask = Workflow;
export type SingleTask = Workflow;

export function resolveProjectTaskRowId(td: Workflow | null | undefined): number | undefined {
    if (!td) return undefined;
    const r = td as Record<string, unknown>;
    const raw =
        r.id ??
        r.projectTaskId ??
        td.id ??
        td.taskId;
    if (raw == null || raw === "") return undefined;
    const n = Number(raw);
    return Number.isFinite(n) ? n : undefined;
}

export interface Section {
    sectionName: string;
    gridName?: string;
    tasks: Workflow[];
}

export interface TasksViewModel {
    projectId?: number | null;
    projectName?: string;
    sections?: Section[];
}

export interface ProgressModel {
    nodes?: ProgressNode[];
}

export interface WorkflowViewModel {
    progressModel?: ProgressModel;
    tasksViewModel?: TasksViewModel;
    percentComplete?: number;
    projectId?: number;
    projectName?: string;
    sections?: Section[];
}
