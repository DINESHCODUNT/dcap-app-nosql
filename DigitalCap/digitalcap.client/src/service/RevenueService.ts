import axiosInstance from "./axiosInstance";
import type { SingleTask, TasksViewModel } from "../Interface/Workflow";
import { resolveProjectTaskRowId } from "../Interface/Workflow";
import * as workflowService from "./WorkflowService";

export interface RevenueSummary {
    capContractValue: number | null;
    invoiceSum: number | null;
    /** When true, show “View Invoicing” link (MVC: Model.RevenueTracker) */
    revenueTracker: boolean;
    projectId: number;
}

function num(v: unknown): number | null {
    if (v === null || v === undefined || v === "") return null;
    const n = Number(v);
    return Number.isFinite(n) ? n : null;
}

function bool(v: unknown, defaultTrue = false): boolean {
    if (v === undefined || v === null) return defaultTrue;
    if (typeof v === "boolean") return v;
    if (typeof v === "string") return v.toLowerCase() === "true";
    return Boolean(v);
}

/**
 * Parse CAP contract value, invoice sum, and revenue flag from Project/Landing (same page as MVC project landing).
 */
export function parseRevenueSummaryFromLanding(apiData: unknown, projectId: number): RevenueSummary {
    const d = apiData as Record<string, unknown>;
    if (!d || typeof d !== "object") {
        return {
            capContractValue: null,
            invoiceSum: null,
            revenueTracker: true,
            projectId,
        };
    }
    const pd = (d.projectDetails ?? d.ProjectDetails) as Record<string, unknown> | undefined;
    const pick = (camel: string, pascal: string) =>
        d[camel] ?? d[pascal] ?? pd?.[camel] ?? pd?.[pascal];

    return {
        capContractValue: num(pick("capContractValue", "CapContractValue")),
        invoiceSum: num(pick("invoiceSum", "InvoiceSum")),
        revenueTracker: bool(pick("revenueTracker", "RevenueTracker"), true),
        projectId: num(pick("projectId", "ProjectId")) ?? projectId,
    };
}

export async function fetchRevenueSummary(projectId: number): Promise<RevenueSummary> {
    const res = await axiosInstance.get(`/api/Project/Landing`, {
        params: { ID: projectId },
    });
    return parseRevenueSummaryFromLanding(res.data, projectId);
}

const REVENUE_SECTION_KEYWORDS = [
    "invoice",
    "intercompany",
    "prf",
    "cost memo",
    "cost increase",
    "cost-memo",
];

/**
 * Prefer invoicing-related sections for Revenue Tracker tab (MVC groups invoice tasks).
 */
export function filterRevenueTasksViewModel(tv: TasksViewModel | undefined): TasksViewModel | undefined {
    if (!tv?.sections?.length) return tv;

    const byCategory = tv.sections.filter((section) =>
        (section.tasks || []).some((t) => {
            const c = (t.Category ?? (t as { category?: string }).category ?? "").toString();
            return c.toLowerCase() === "invoicing";
        })
    );
    if (byCategory.length > 0) {
        return { ...tv, sections: byCategory };
    }

    const byName = tv.sections.filter((s) => {
        const n = (s.sectionName || "").toLowerCase();
        return REVENUE_SECTION_KEYWORDS.some((k) => n.includes(k));
    });
    if (byName.length > 0) {
        return { ...tv, sections: byName };
    }

    return tv;
}

/**
 * Merge MVC grid fields from full workflow task into Invoice/Index task (same project task row id).
 * Invoice API often omits NumberOfComments, NumberOfAttachments, etc.
 */
function mergeTaskGridFieldsFromWorkflow(invoice: SingleTask, wf: SingleTask | undefined): SingleTask {
    if (!wf) return invoice;
    const inv = invoice as Record<string, unknown>;
    const w = wf as Record<string, unknown>;
    const pick = (pascal: string, camel: string): unknown => {
        const a = inv[pascal] ?? inv[camel];
        if (a !== undefined && a !== null) return a;
        return w[pascal] ?? w[camel];
    };
    return {
        ...invoice,
        NumberOfComments: pick("NumberOfComments", "numberOfComments") as number | undefined,
        NumberOfAttachments: pick("NumberOfAttachments", "numberOfAttachments") as number | undefined,
        StatusDate: pick("StatusDate", "statusDate") as string | Date | null | undefined,
        Value: pick("Value", "value") as number | undefined,
        DueDate: pick("DueDate", "dueDate") as string | Date | null | undefined,
        AssignedTo: pick("AssignedTo", "assignedTo") as string | undefined,
    };
}

function enrichTasksViewModelWithWorkflow(
    primary: TasksViewModel,
    workflowTvm: TasksViewModel | undefined
): TasksViewModel {
    if (!workflowTvm?.sections?.length) return primary;
    const map = new Map<number, SingleTask>();
    for (const section of workflowTvm.sections) {
        for (const t of section.tasks || []) {
            const id = resolveProjectTaskRowId(t);
            if (id != null) map.set(id, t);
        }
    }
    if (map.size === 0) return primary;

    const sections = (primary.sections ?? []).map((section) => ({
        ...section,
        tasks: (section.tasks || []).map((task) => {
            const id = resolveProjectTaskRowId(task);
            if (id == null) return task;
            const wf = map.get(id);
            return mergeTaskGridFieldsFromWorkflow(task, wf);
        }),
    }));
    return { ...primary, sections };
}

function normalizeInvoicePayload(data: unknown): TasksViewModel | undefined {
    const d = data as Record<string, unknown>;
    if (!d) return undefined;
    const tvm = (d.tasksViewModel ?? d.TasksViewModel) as TasksViewModel | undefined;
    if (tvm?.sections?.length) return tvm;

    const sections = (d.sections ?? d.Sections) as TasksViewModel["sections"] | undefined;
    const projectId = (d.projectId ?? d.ProjectId) as number | undefined;
    const projectName = (d.projectName ?? d.ProjectName) as string | undefined;

    if (sections && Array.isArray(sections)) {
        return {
            sections,
            projectId,
            projectName,
        } as TasksViewModel;
    }
    return undefined;
}

/**
 * Invoice list for Revenue Tracker tab.
 * GET api/Invoice/Index?id={projectId} → TasksViewModel; merged with GET api/Project/Workflow
 * so task rows include NumberOfComments, NumberOfAttachments, StatusDate, etc. (same as Workflow list).
 */
export async function fetchRevenueTaskSections(projectId: number): Promise<TasksViewModel | undefined> {
    let workflowData: Record<string, unknown> | undefined;
    let workflowTvm: TasksViewModel | undefined;

    try {
        const wf = await workflowService.getWorkflowByProjectId(projectId);
        workflowData = wf.data as Record<string, unknown> | undefined;
        workflowTvm =
            normalizeInvoicePayload(workflowData?.revenueTasksViewModel) ||
            normalizeInvoicePayload(workflowData?.RevenueTasksViewModel) ||
            normalizeInvoicePayload(workflowData?.invoiceTasksViewModel) ||
            normalizeInvoicePayload(workflowData?.InvoiceTasksViewModel) ||
            (workflowData?.tasksViewModel as TasksViewModel) ||
            (workflowData?.TasksViewModel as TasksViewModel) ||
            (workflowData?.sections && Array.isArray(workflowData.sections)
                ? ({
                    sections: workflowData.sections as TasksViewModel["sections"],
                    projectId: workflowData.projectId as number,
                    projectName: workflowData.projectName as string,
                } as TasksViewModel)
                : undefined);
    } catch (e) {
    }

    try {
        const r = await axiosInstance.get(`/api/Invoice/Index`, {
            params: { id: projectId },
        });
        const normalized = normalizeInvoicePayload(r.data);
        if (normalized?.sections?.length) {
            return enrichTasksViewModelWithWorkflow(normalized, workflowTvm);
        }
    } catch (e) {
    }

    if (!workflowData) return undefined;

    const direct =
        normalizeInvoicePayload(workflowData.revenueTasksViewModel) ||
        normalizeInvoicePayload(workflowData.RevenueTasksViewModel) ||
        normalizeInvoicePayload(workflowData.invoiceTasksViewModel) ||
        normalizeInvoicePayload(workflowData.InvoiceTasksViewModel);

    if (direct?.sections?.length) return direct;

    const base =
        (workflowData.tasksViewModel as TasksViewModel) ||
        (workflowData.TasksViewModel as TasksViewModel) ||
        (workflowData.sections
            ? ({
                sections: workflowData.sections as TasksViewModel["sections"],
                projectId: workflowData.projectId as number,
                projectName: workflowData.projectName as string,
            } as TasksViewModel)
            : undefined);

    return filterRevenueTasksViewModel(base);
}
