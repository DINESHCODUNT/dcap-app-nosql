import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";
import moment from "moment";
import { type SingleTask, resolveProjectTaskRowId } from "../Interface/Workflow";
import type { TaskStatus } from "../Interface/Task";

export function getStatuses(): Promise<AxiosResponse<TaskStatus[]>> {
    return axiosInstance.get(`/api/Task/GetStatuses`);
}

export interface SaveTaskRequest {
    id: number;
    status: number;
    statusDate?: string | null;
    assignedTo?: string | null;
    dueDate?: string | null;
    value?: number | null;
    surveyDays?: number | null;
}

function toOptionalBodyDate(d: string | Date | null | undefined): string | undefined {
    if (d == null || d === "") return undefined;
    if (d instanceof Date && Number.isNaN(d.getTime())) return undefined;
    const m = moment(d);
    return m.isValid() ? m.format("YYYY-MM-DD") : undefined;
}
export function toSaveTaskRequest(taskData: SingleTask): SaveTaskRequest {
    const id = taskData.id ?? resolveProjectTaskRowId(taskData);
    const status = taskData.statusId ?? taskData.StatusId;
    if (id == null || status == null) {
        throw new Error("Task id and status are required to save.");
    }

    const body: SaveTaskRequest = {
        id,
        status: Number(status),
    };

    const statusDate = toOptionalBodyDate(taskData.statusDate ?? taskData.StatusDate);
    if (statusDate !== undefined) {
        body.statusDate = statusDate;
    }

    const assignedRaw = taskData.AssignedTo ?? taskData.assignedTo;
    if (assignedRaw != null && String(assignedRaw).trim() !== "") {
        body.assignedTo = String(assignedRaw).trim();
    }

    const dueDate = toOptionalBodyDate(taskData.dueDate ?? taskData.DueDate);
    if (dueDate !== undefined) {
        body.dueDate = dueDate;
    }

    const val = taskData.Value ?? taskData.value;
    if (val !== undefined && val !== null && !Number.isNaN(Number(val))) {
        body.value = Number(val);
    }

    const survey = taskData.SurveyDays ?? taskData.surveyDays;
    if (survey !== undefined && survey !== null && survey !== "") {
        const n = Number(survey);
        if (!Number.isNaN(n)) {
            body.surveyDays = n;
        }
    }

    return body;
}

export function saveTask(taskData: SingleTask): Promise<AxiosResponse> {
    const body = toSaveTaskRequest(taskData);
    return axiosInstance.post(`/api/Task/SaveTask`, body);
}
