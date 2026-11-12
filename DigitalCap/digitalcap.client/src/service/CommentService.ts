import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";

export interface Comment {
    id: number;
    comment: string;
    createdBy: string;
    lastModifiedDate: string | Date;
    editable?: boolean;
    projectId?: number;
    projectTaskId?: number;
}

export function normalizeCommentFromApi(row: unknown): Comment {
    const r = row as Record<string, unknown>;

    return {
        id: Number(r.id ?? r.Id ?? 0),
        comment: String(r.comment ?? r.Comment ?? ""),
        createdBy: String(r.createdBy ?? r.CreatedBy ?? ""),
        lastModifiedDate: (r.lastModifiedDate ?? r.LastModifiedDate ?? "") as string | Date,

        editable: ((): boolean | undefined => {
            const v =
                r.editable ??
                r.Editable ??
                r.isEditable ??
                r.IsEditable ??
                r.commentEditable ??
                r.CommentEditable;
            if (v === undefined || v === null) return undefined;
            if (typeof v === "string") {
                const s = v.toLowerCase().trim();
                if (s === "true" || s === "1") return true;
                if (s === "false" || s === "0") return false;
                return s.length > 0;
            }
            return Boolean(v);
        })(),
        projectId: r.projectId != null || r.ProjectId != null ? Number(r.projectId ?? r.ProjectId) : undefined,
        projectTaskId:
            r.projectTaskId != null || r.ProjectTaskId != null
                ? Number(r.projectTaskId ?? r.ProjectTaskId)
                : undefined,
    };
}

export interface CommentsViewModel {
    projectId: number;
    projectTaskId: number;
    notes?: string;
    allowManageMyComments?: boolean;
}

export function getComments(
    projectId: number,
    projectTaskId: number,
    notes?: string,
    isElectron: boolean = false
): Promise<AxiosResponse<Comment[]>> {
    const controller = isElectron ? "Offline" : "Comment";
    const routePath = `/api/${controller}/GetComments`;

    return axiosInstance.get(routePath, {
        params: {
            projectId,
            projectTaskId,
            notes: notes || ''
        }
    });
}

export const createComment = (
    projectId: number,
    projectTaskId: number,
    comment: string
) => {

    return axiosInstance.post(
        "/api/Comment/CreateComment",
        {
            projectId,
            projectTaskId,
            comment
        }
    );

};
export const updateComment = (
    id: number,
    projectId: number,
    comment: string
) => {

    return axiosInstance.post(
        "/api/Comment/UpdateComment",
        {
            id,
            projectId,
            comment
        }
    );

};

export async function deleteComment(id: number, projectId: number, comment: string) {
    return axiosInstance.post("/api/Comment/DeleteComment", {
        id,
        projectId,
        comment,
    });
}