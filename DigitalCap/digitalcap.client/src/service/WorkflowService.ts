import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";
import type { WorkflowViewModel } from "../Interface/Workflow";

export {
    ProgressState,
    type ProgressNode,
    type SingleTask,
    type Section,
    type TasksViewModel,
    type ProgressModel,
    type WorkflowViewModel,
} from "../Interface/Workflow";

export function getWorkflowByProjectId(projectId: number): Promise<AxiosResponse<WorkflowViewModel>> {
    const routePath = `/api/Project/Workflow`;
    return axiosInstance.get(routePath, {
        params: { id: projectId },
    });
}
