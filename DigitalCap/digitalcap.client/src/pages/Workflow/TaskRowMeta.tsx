import React from "react";
import type { WorkflowTask } from "./workflowConverters";

function formatDateMvc(date: string | Date): string {
    const dateObj = typeof date === "string" ? new Date(date) : date;
    if (Number.isNaN(dateObj.getTime())) return "";
    const day = dateObj.getDate().toString().padStart(2, "0");
    const monthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    const month = monthNames[dateObj.getMonth()];
    const year = dateObj.getFullYear();
    return `${day}-${month}-${year}`;
}

function formatCurrencyMvc(value: number): string {
    return new Intl.NumberFormat("en-US", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    }).format(value);
}

function asRecord(task: WorkflowTask): Record<string, unknown> {
    return (task.singleTaskData ?? {}) as Record<string, unknown>;
}

function readNullableNumber(...candidates: unknown[]): number | null {
    for (const v of candidates) {
        if (v === undefined || v === null || v === "") continue;
        const n = Number(v);
        if (Number.isFinite(n)) return n;
    }
    return null;
}

function readStatusDate(task: WorkflowTask): string | Date | null | undefined {
    const s = asRecord(task);
    const raw = s.StatusDate ?? s.statusDate ?? task.statusDate;
    if (raw === undefined || raw === null) return null;
    return raw as string | Date;
}

function readDueDate(task: WorkflowTask): string | Date | null | undefined {
    const s = asRecord(task);
    const raw = s.DueDate ?? s.dueDate ?? task.dueDate;
    if (raw === undefined || raw === null) return null;
    return raw as string | Date;
}

function readValue(task: WorkflowTask): number | null {
    const s = asRecord(task);
    const raw = s.Value ?? s.value ?? task.value;
    if (raw === undefined || raw === null || raw === "") return null;
    const n = Number(raw);
    return Number.isFinite(n) ? n : null;
}

function readAssignedTo(task: WorkflowTask): string | null {
    const s = asRecord(task);
    const raw = s.AssignedTo ?? s.assignedTo ?? task.assignedTo;
    if (raw === undefined || raw === null) return null;
    return String(raw);
}

/**
 * MVC `generateTaskDetailsColumn` fields — values only (no icons); spacing via `.task-meta--values-only` flex gap.
 */
const TaskRowMeta: React.FC<{ task: WorkflowTask }> = ({ task }) => {
    const s = asRecord(task);

    const numberOfComments = readNullableNumber(
        s.NumberOfComments,
        s.numberOfComments,
        task.numberOfComments
    );
    const numberOfAttachments = readNullableNumber(
        s.NumberOfAttachments,
        s.numberOfAttachments,
        task.numberOfAttachments
    );

    const statusDate = readStatusDate(task);
    const dueDate = readDueDate(task);
    const value = readValue(task);
    const assignedTo = readAssignedTo(task);

    const showComments = numberOfComments !== null && numberOfComments !== 0;
    const showAttachments = numberOfAttachments !== null && numberOfAttachments !== 0;
    const showStatusDate = statusDate !== null;
    const showValue = value !== null;
    const showDueDate = dueDate !== null;
    const showAssignedTo = assignedTo !== null;

    const statusStr = statusDate != null ? formatDateMvc(statusDate) : "";
    const dueStr = dueDate != null ? formatDateMvc(dueDate) : "";

    const segments: { key: string; text: string; title: string }[] = [];
    if (showComments) {
        segments.push({ key: "comments", text: String(numberOfComments), title: "Comments" });
    }
    if (showAttachments) {
        segments.push({ key: "attachments", text: String(numberOfAttachments), title: "Attachments" });
    }
    if (showStatusDate && statusStr) {
        segments.push({ key: "statusDate", text: statusStr, title: "Status date" });
    }
    if (showValue && value !== null) {
        segments.push({ key: "value", text: formatCurrencyMvc(value), title: "Value" });
    }
    if (showDueDate && dueStr) {
        segments.push({ key: "due", text: `Due ${dueStr}`, title: "Due date" });
    }
    if (showAssignedTo) {
        segments.push({ key: "assigned", text: assignedTo, title: "Assigned to" });
    }

    if (segments.length === 0) return null;

    return (
        <div className="task-meta task-meta--values-only" aria-label="Task details">
            {segments.map((seg) => (
                <span key={seg.key} className="task-meta-segment" title={seg.title}>
                    {seg.text}
                </span>
            ))}
        </div>
    );
};

export default TaskRowMeta;
