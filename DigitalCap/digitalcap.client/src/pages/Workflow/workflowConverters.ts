import type { TasksViewModel, SingleTask } from "../../Interface/Workflow";
import { resolveProjectTaskRowId } from "../../Interface/Workflow";

export type BadgeColor =
    | "primary"
    | "secondary"
    | "success"
    | "danger"
    | "warning"
    | "info"
    | "light"
    | "dark";

/**
 * Grid row model — detail icons follow MVC `generateTaskDetailsColumnTemplate`:
 * `NumberOfComments`, `NumberOfAttachments`, `StatusDate`, `Value`, `DueDate`, `AssignedTo`.
 * Values are mirrored here for convenience; `TaskRowMeta` also reads `singleTaskData` (PascalCase).
 */
export interface WorkflowTask {
    id: string;
    name: string;
    badge: {
        color: BadgeColor;
        label: string;
    };
    starred: boolean;
    numberOfComments?: number;
    numberOfAttachments?: number;
    statusDate?: string | Date | null;
    dueDate?: string | Date | null;
    assignedTo?: string;
    assignedDate?: string | Date | null;
    value?: number | null;
    category?: string;
    percentageComplete?: number;
    type?: string;
    singleTaskData?: SingleTask;
}

export interface WorkflowSectionModel {
    id: string;
    title: string;
    totalTasks: number;
    completedTasks: number;
    tasks: WorkflowTask[];
}

export function getTaskStatus(
    status?: string,
    statusId?: number
): { color: BadgeColor; label: string } {
    if (status) {
        const statusLower = status.toLowerCase().trim();
        switch (statusLower) {
            case "completed":
                return { color: "success", label: "Completed" };
            case "in progress":
            case "inprogress":
                return { color: "warning", label: "In Progress" };
            case "not started":
            case "notstarted":
                return { color: "primary", label: "Not Started" };
            case "overdue":
                return { color: "danger", label: "Overdue" };
            default:
                return { color: "primary", label: status };
        }
    }

    if (statusId !== undefined && statusId !== null) {
        switch (statusId) {
            case 1:
                return { color: "primary", label: "Not Started" };
            case 2:
                return { color: "warning", label: "In Progress" };
            case 3:
                return { color: "success", label: "Completed" };
            case 4:
                return { color: "danger", label: "Overdue" };
            default:
                return { color: "primary", label: "Not Started" };
        }
    }

    return { color: "primary", label: "Not Started" };
}

export function convertSingleTaskToWorkflowTask(singleTask: SingleTask, _index: number): WorkflowTask {
    const rowId = resolveProjectTaskRowId(singleTask);
    const taskId = rowId != null ? String(rowId) : "";

    const taskObj = singleTask as Record<string, unknown>;
    let taskName = (taskObj.task as string) || "Unnamed Task";
    if (taskName && typeof taskName === "string") {
        taskName = taskName.trim();
    }
    const status = getTaskStatus(singleTask.status, singleTask.statusId);
    const isStarred = singleTask.isMilestone;

    const st = singleTask as Record<string, unknown>;

    return {
        id: taskId,
        name: taskName,
        badge: status,
        starred: Boolean(isStarred),
        numberOfComments:
            (typeof st.NumberOfComments === "number" ? st.NumberOfComments : undefined) ??
            (typeof st.numberOfComments === "number" ? st.numberOfComments : undefined),
        numberOfAttachments:
            (typeof st.NumberOfAttachments === "number" ? st.NumberOfAttachments : undefined) ??
            (typeof st.numberOfAttachments === "number" ? st.numberOfAttachments : undefined),
        statusDate: (st.StatusDate ?? st.statusDate) as string | Date | null | undefined,
        dueDate: (st.DueDate ?? st.dueDate ?? singleTask.DueDate) as string | Date | null | undefined,
        assignedTo: (st.AssignedTo ?? st.assignedTo ?? singleTask.AssignedTo) as string | undefined,
        assignedDate: singleTask.AssignedDate ?? (st.assignedDate as string | Date | null | undefined),
        value: (() => {
            const raw = st.Value ?? st.value;
            if (raw == null || raw === "") return null;
            const n = Number(raw);
            return Number.isFinite(n) ? n : null;
        })(),
        category: (st.Category ?? st.category) as string | undefined,
        percentageComplete: singleTask.PercentageComplete,
        type: singleTask.Type || singleTask.type,
        singleTaskData: singleTask,
    };
}

export function convertTasksViewModelToSections(tasksViewModel?: TasksViewModel): WorkflowSectionModel[] {
    if (!tasksViewModel?.sections || tasksViewModel.sections.length === 0) {
        return [];
    }

    return tasksViewModel.sections.map((section, sectionIndex) => {
        const tasks = (section.tasks || []).map((task, taskIndex) =>
            convertSingleTaskToWorkflowTask(task, taskIndex)
        );
        const completedTasks = tasks.filter((t) => t.badge.label === "Completed").length;
        return {
            id: section.gridName || `section-${sectionIndex}`,
            title: section.sectionName || `Section ${sectionIndex + 1}`,
            totalTasks: tasks.length,
            completedTasks,
            tasks,
        };
    });
}
