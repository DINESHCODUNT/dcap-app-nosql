import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";
import moment from "moment";

export interface DataSourceRequest {
    page?: number;
    pageSize?: number;
    skip?: number;
    take?: number;
    sort?: SortDescriptor[];
    filter?: FilterDescriptor;
    group?: GroupDescriptor[];
}

export interface SortDescriptor {
    field: string;
    dir: "asc" | "desc";
}

export interface FilterDescriptor {
    field: string;
    operator: string;
    value: any;
    logic?: "and" | "or";
    filters?: FilterDescriptor[];
}

export interface GroupDescriptor {
    field: string;
    dir: "asc" | "desc";
    aggregates?: any[];
}

export interface DataSourceResult<T> {
    data: T[];
    total: number;
    aggregates?: any;
}

export interface AllProjectsGrid {
    id: number;
    name: string;
    imo: string;
    pidNumber: string;
    capRegion: string;
    displayDrydockDate: string;
    companyName: string;
    wcn: string;
    projectStatusName: string;
    projectStatusColor?: string;
    statusColor?: string;
    isDownload?: string;
    projectPriority?: string;
    commentLastModifiedDate?: string;
    commentCreatedBy?: string;
    projectLastUpdatedDate?: string;
    percentComplete?: number;
    mostRecentComment?: string;
}

export interface ProjectSidebarStatsAggregate {
    total: number;
    online: number;
    offline: number;

    regions: Record<string, number>;
    statuses: Record<string, number>;
}

export async function getProjectStats(): Promise<ProjectSidebarStatsAggregate> {
    const res = await axiosInstance.get('/api/Project/GetProjectStats');
    const d = res.data;
    return {
        total: d.total ?? 0,
        online: d.online ?? 0,
        offline: d.offline ?? 0,

        regions: d.regions || {},
        statuses: d.statuses || {}
    };
}
export function normalizeAllProjectsGridPayload(responseData: unknown): AllProjectsGrid[] {
    if (!responseData) return [];
    if (Array.isArray(responseData)) return responseData as AllProjectsGrid[];
    if (typeof responseData === "object" && responseData !== null && "data" in responseData) {
        const nested = (responseData as { data: unknown }).data;
        if (Array.isArray(nested)) return nested as AllProjectsGrid[];
    }
    return [];
}

export function getSidebarConnectionStatus(project: AllProjectsGrid): "online" | "offline" {
    if (project.isDownload) {
        const downloadStatus = project.isDownload.toLowerCase();
        if (downloadStatus === "offline") return "offline";
    }
    return "online";
}

export function aggregateProjectStatsFromGridRows(
    projects: AllProjectsGrid[]
): ProjectSidebarStatsAggregate {
    let online = 0;
    let offline = 0;
    const regions: Record<string, number> = {};
    const statuses: Record<string, number> = {};

    for (const p of projects) {
        if (getSidebarConnectionStatus(p) === "online") {
            online += 1;
        } else {
            offline += 1;
        }

        const st = (p.projectStatusName || "Unknown").trim();
        statuses[st] = (statuses[st] || 0) + 1;

        const regionName = (p.capRegion || "Unknown").trim();
        regions[regionName] = (regions[regionName] || 0) + 1;
    }
    return {
        total: projects.length,
        online,
        offline,

        regions,
        statuses
    };
}

export const calculateDashboardStats = aggregateProjectStatsFromGridRows;

export interface ProjectCharts {
    category: string;
    value: number;
    color: string;
}

export interface ProjectChartsCount {
    count: number;
}

export interface RecentActivity {
    description: string;
    createDttm: string;
}

export interface ProjectPermissions {
    addProject: boolean;
    exportProject: boolean;
    cancelProject?: boolean;
    deleteProject?: boolean;
}
export function getAllProjectsGridData(request?: DataSourceRequest): Promise<AxiosResponse<AllProjectsGrid[]>> {
    const routePath = `/api/Project/GetAllProjectsGridData`;

    const requestBody: DataSourceRequest = request || {};

    const cleanBody: any = {};
    if (requestBody.page !== undefined) cleanBody.page = requestBody.page;
    if (requestBody.pageSize !== undefined) cleanBody.pageSize = requestBody.pageSize;
    if (requestBody.skip !== undefined) cleanBody.skip = requestBody.skip;
    if (requestBody.take !== undefined) cleanBody.take = requestBody.take;
    if (requestBody.sort) cleanBody.sort = requestBody.sort;
    if (requestBody.filter) cleanBody.filter = requestBody.filter;
    if (requestBody.group) cleanBody.group = requestBody.group;

    return axiosInstance.post(routePath, cleanBody, {
        headers: {
            'Content-Type': 'application/json'
        }
    });
}

export function getProjectList(): Promise<AxiosResponse<ProjectPermissions>> {
    const routePath = `/api/Project/List`;
    return axiosInstance.get(routePath);
}
export function getProjectByStatuses(isElectron: boolean = false): Promise<AxiosResponse<ProjectCharts[]>> {
    const controller = isElectron ? "Offline" : "Cap";
    const routePath = `/api/${controller}/GetProjectByStatuses`;
    return axiosInstance.get(routePath);
}


export function getRecentActivities(isElectron: boolean = false, projectId?: number): Promise<AxiosResponse<RecentActivity[]>> {
    const controller = isElectron ? "Offline" : "Activity";
    const routePath = `/api/${controller}/GetRecentActivities`;
    const params: any = {};
    if (projectId) {
        params.projectId = projectId;
    }
    return axiosInstance.get(routePath, {
        params: Object.keys(params).length > 0 ? params : undefined
    })
};

export function GetRecentActivities2(isElectron: boolean = false, projectId?: number): Promise<AxiosResponse<RecentActivity[]>> {
    const controller = isElectron ? "Offline" : "Activity";
    const routePath = `/api/${controller}/GetRecentActivities2`;
    const params: any = {};
    if (projectId) {
        params.projectId = projectId;
    }
    return axiosInstance.get(routePath, {
        params: Object.keys(params).length > 0 ? params : undefined
    })
};

export function GetRecentActivities2ByProjectId(isElectron: boolean = false, projectId?: number): Promise<AxiosResponse<RecentActivity[]>> {
    const controller = isElectron ? "Offline" : "Activity";
    const routePath = `/api/${controller}/GetRecentActivities2ByProjectId`;
    const params: any = {};
    if (projectId) {
        params.projectId = projectId;
    }
    return axiosInstance.get(routePath, {
        params: Object.keys(params).length > 0 ? params : undefined
    })
};

export function GetAllActivities2ByProjectId(isElectron: boolean = false, projectId?: number): Promise<AxiosResponse<RecentActivity[]>> {
    const controller = isElectron ? "Offline" : "Activity";
    const routePath = `/api/${controller}/GetAllActivities2ByProjectId`;
    const params: any = {};
    if (projectId) {
        params.projectId = projectId;
    }
    return axiosInstance.get(routePath, {
        params: Object.keys(params).length > 0 ? params : undefined
    })
};

export function getProjectCount(isElectron: boolean = false): Promise<AxiosResponse<ProjectChartsCount>> {
    const controller = isElectron ? "Offline" : "Cap";
    const routePath = `/api/${controller}/GetProjectCount`;
    return axiosInstance.get(routePath);
}

export function parseProjectCountResponse(apiBody: unknown): number {
    if (apiBody == null || typeof apiBody !== "object") return 0;
    const body = apiBody as Record<string, unknown>;
    if (typeof body.count === "number") return body.count;
    const nested = body.data;
    if (Array.isArray(nested) && nested.length > 0) {
        const first = nested[0] as Record<string, unknown>;
        if (typeof first?.count === "number") return first.count;
    }
    return 0;
}

export function parseProjectChartsResponse(apiBody: unknown): ProjectCharts[] {
    if (!apiBody) return [];
    if (Array.isArray(apiBody)) return apiBody as ProjectCharts[];
    if (typeof apiBody === "object" && apiBody !== null && "data" in apiBody) {
        const d = (apiBody as { data: unknown }).data;
        if (Array.isArray(d)) return d as ProjectCharts[];
    }
    return [];
}

export function countByChartCategory(charts: ProjectCharts[], ...aliases: string[]): number {
    const want = new Set(aliases.map((a) => a.toLowerCase().trim()));
    for (const row of charts) {
        const cat = String(row.category ?? "").trim().toLowerCase();
        if (want.has(cat)) {
            const v = Number(row.value);
            return Number.isFinite(v) ? v : 0;
        }
    }
    return 0;
}

export function getCertsByRegion(isElectron: boolean = false): Promise<AxiosResponse<ProjectCharts[]>> {
    const controller = isElectron ? "Offline" : "Cap";
    const routePath = `/api/${controller}/GetCertsByRegion`;
    return axiosInstance.get(routePath);
}

export function getCertsCount(isElectron: boolean = false): Promise<AxiosResponse<ProjectChartsCount>> {
    const controller = isElectron ? "Offline" : "Cap";
    const routePath = `/api/${controller}/GetCertsCount`;
    return axiosInstance.get(routePath);
}

export function exportAllProjectsGrid(): Promise<AxiosResponse<Blob>> {
    const routePath = `/api/Report/ExportAllProjectsGrid`;
    return axiosInstance.get(routePath, {
        responseType: 'blob'
    });
}

export function exportProjectsGridPartial(
    minRange: number,
    maxRange: number
): Promise<AxiosResponse<{ result: Array<Record<string, string>> }>> {
    const routePath = `/api/Report/ExportProjectsGridPartial`;
    return axiosInstance.get(routePath, {
        params: { minRange, maxRange }
    });
}

export function updatePriority(
    id: number,
    priority: number | null
): Promise<AxiosResponse> {
    const routePath = `/api/Project/UpdatePriority`;
    return axiosInstance.post(routePath, null, {
        params: { id, priority }
    });
}

export function getRecentActivityUrl(): string {
    return `/api/Project/RecentActivity`;
}

export function downloadExportedFile(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
}

export function buildAllProjectsExportRows(projects: AllProjectsGrid[]): Array<Record<string, string>> {
    return projects.map((p, index) => {
        const name = (p.name ?? '').trim().replace(/\t/g, '');
        return {
            'Item #': String(index + 1),
            Priority: p.projectPriority ?? '',
            'Project Name': name,
            IMO: p.imo ?? '',
            'Project ID': p.pidNumber ?? '',
            'CAP Coordinator': p.capRegion ?? '',
            'Potential Drydock Date': p.displayDrydockDate ?? '',
            Client: p.companyName ?? '',
            WCN: p.wcn ?? '',
            Status: p.projectStatusName ?? '',
            Download: p.isDownload ?? '',
            '% Complete': p.percentComplete != null ? String(p.percentComplete) : '',
            'Last Updated': p.projectLastUpdatedDate ?? '',
            'Comment by': p.commentCreatedBy ?? '',
            'Last comment': p.mostRecentComment ?? ''
        };
    });
}

export interface ProjectsExportJobStatusResponse {
    status: "Processing" | "Completed" | "Failed" | "NotFound";
    rows?: Array<Record<string, string>>;
    error?: string;
}

export function startExportAllProjects(): Promise<AxiosResponse<{ jobId: string; status: string }>> {
    return axiosInstance.post<{ jobId: string; status: string }>(`/api/Report/StartExportAllProjects`);
}

export function getExportAllProjectsStatus(jobId: string): Promise<AxiosResponse<ProjectsExportJobStatusResponse>> {
    return axiosInstance.get<ProjectsExportJobStatusResponse>(`/api/Report/GetExportAllProjectsStatus`, {
        params: { jobId }
    });
}

export async function exportAllProjectsGridPaginated(
    projectCount: number,
    onProgress?: (progress: number) => void
): Promise<Array<Record<string, string>>> {
    void projectCount;
    const startRes = await startExportAllProjects();
    const jobId = startRes.data?.jobId;
    if (!jobId) {
        throw new Error("Unable to start export job. Please try again.");
    }

    let rows: Array<Record<string, string>> = [];
    while (true) {
        await new Promise(r => setTimeout(r, 5000));
        const statusRes = await getExportAllProjectsStatus(jobId);
        const s = statusRes.data;
        if (s.status === "Completed") {
            rows = s.rows ?? [];
            break;
        }
        if (s.status === "Failed") {
            throw new Error(s.error || "Unable to export projects. Please try again later.");
        }
        if (s.status === "NotFound") {
            throw new Error("The export was lost. Please try again.");
        }
    }

    const projectRecords: Array<Array<Record<string, string>>> = [rows];
    onProgress?.(100);

    const finalRecordList: Array<Record<string, string>> = [];
    let count = 1;

    for (const partition of projectRecords) {
        for (const record of partition) {
            record["Item #"] = count.toString();
            finalRecordList.push(record);
            count++;
        }
    }

    return finalRecordList;
}


export function getAllUsers(): Promise<AxiosResponse> {
    const routePath = `/api/Project/GetAllUsers`;
    return axiosInstance.get(routePath);
}

export function getAllSurveyors(): Promise<AxiosResponse> {
    const routePath = `/api/Project/GetAllSurveyors`;
    return axiosInstance.get(routePath);
}

export function getAllClients(wcnList?: boolean): Promise<AxiosResponse> {
    const routePath = `/api/Project/GetAllClients`;
    return axiosInstance.get(routePath, {
        params: wcnList ? { wcnList: true } : undefined
    });
}

export function getProjectById(projectId: number): Promise<AxiosResponse> {
    const routePath = `/api/Project/Landing`;
    return axiosInstance.get(routePath, {
        params: { ID: projectId }
    });
}

export type ProjectLifecycleAction = "cancel" | "delete" | "close";

export function postProjectLifecycleAction(
    action: ProjectLifecycleAction,
    projectId: number
): Promise<AxiosResponse<unknown>> {
    const segment =
        action === "cancel"
            ? "CancelProject"
            : action === "delete"
                ? "DeleteProject"
                : "CloseProject";
    return axiosInstance.post(`/api/Project/${segment}`, null, {
        params: { id: projectId },
    });
}

export function createProject(projectData: any): Promise<AxiosResponse> {
    const routePath = `/api/Project/Create`;
    return axiosInstance.post(routePath, projectData);
}

export function updateProject(projectData: any): Promise<AxiosResponse> {
    const routePath = `/api/Project/Update_Project`;
    return axiosInstance.post(routePath, projectData);
}

export function createReassessment(projectId: number): Promise<AxiosResponse> {
    return axiosInstance.post(`/api/Project/Update`, null, {
        params: { id: projectId },
    });
}
export interface ReportDetailsViewModel {
    gradesId?: number;
    projectId: number;
    capCertificateNumber?: number;
    classReportNumber?: number;
    structuralReportNumber?: number;
    capCertificateIssuanceDate?: string | Date;
    classReportDate?: string | Date;
    structuralReportDate?: string | Date;
    finalGrade?: number;
    structuralGrade?: number;
    fatigueGrade?: number;
    renewalGrade?: number;
    materialGrade?: number;
    gaugingGrade?: number;
    hullGirderStrength?: number;
    cap1CertificateIssuanceDate?: string | Date;
    /** C# model uses string */
    cap1FinalGrade?: string | number;
    projectName?: string;
}
export interface ReportPermissionResponse {
    reportEdit?: boolean;
    reportView?: boolean;
}
export function getReportDetails(projectId: number): Promise<AxiosResponse<ReportDetailsViewModel>> {
    const routePath = `/api/Project/Landing`;
    return axiosInstance.get(routePath, {
        params: { ID: projectId }
    });
}

const num = (v: unknown): number | undefined => {
    if (v === null || v === undefined || v === "") return undefined;
    const n = Number(v);
    return Number.isFinite(n) ? n : undefined;
};

export function parseReportDetailsFromLanding(apiData: unknown, projectId: number): ReportDetailsViewModel {
    const d = apiData as Record<string, unknown>;
    if (!d || typeof d !== "object") {
        return { projectId };
    }
    const nested =
        (d.reportDetails as Record<string, unknown>) ||
        (d.ReportDetails as Record<string, unknown>) ||
        (d.reportDetailsViewModel as Record<string, unknown>) ||
        null;
    const src: Record<string, unknown> = nested ? { ...d, ...nested } : d;

    const pick = (camel: string, pascal: string): unknown =>
        src[camel] ?? src[pascal] ?? d[camel] ?? d[pascal];

    return {
        gradesId: num(pick("gradesId", "GradesId")),
        projectId: num(pick("projectId", "ProjectId")) ?? projectId,
        capCertificateNumber: num(pick("capCertificateNumber", "CAPCertificateNumber")),
        classReportNumber: num(pick("classReportNumber", "ClassReportNumber")),
        structuralReportNumber: num(pick("structuralReportNumber", "StructuralReportNumber")),
        capCertificateIssuanceDate:
            (pick("capCertificateIssuanceDate", "CAPCertificateIssuanceDate") as string) || undefined,
        classReportDate: (pick("classReportDate", "ClassReportDate") as string) || undefined,
        structuralReportDate: (pick("structuralReportDate", "StructuralReportDate") as string) || undefined,
        finalGrade: num(pick("finalGrade", "FinalGrade")),
        structuralGrade: num(pick("structuralGrade", "StructuralGrade")),
        fatigueGrade: num(pick("fatigueGrade", "FatigueGrade")),
        renewalGrade: num(pick("renewalGrade", "RenewalGrade")),
        materialGrade: num(pick("materialGrade", "MaterialGrade")),
        gaugingGrade: num(pick("gaugingGrade", "GaugingGrade")),
        hullGirderStrength: num(pick("hullGirderStrength", "HullGirderStrength")),
        cap1CertificateIssuanceDate:
            (pick("cap1CertificateIssuanceDate", "CAP1CertificateIssuanceDate") as string) || undefined,
        cap1FinalGrade: ((): string | number | undefined => {
            const v = pick("cap1FinalGrade", "CAP1FinalGrade");
            if (v == null || v === "") return undefined;
            return typeof v === "string" ? v : num(v);
        })(),
        projectName: ((): string | undefined => {
            const direct = (pick("projectName", "ProjectName") as string)?.trim();
            if (direct) return direct;
            const pd = d.projectDetails ?? d.ProjectDetails;
            if (pd && typeof pd === "object") {
                const p = pd as Record<string, unknown>;
                const vn = (p.vesselName ?? p.VesselName ?? p.name ?? p.Name) as string | undefined;
                if (vn && String(vn).trim()) return String(vn).trim();
                const pn = (p.projectName ?? p.ProjectName) as string | undefined;
                if (pn && String(pn).trim()) return String(pn).trim();
            }
            return undefined;
        })(),
    };
}

export async function fetchReportDetailsModel(projectId: number): Promise<ReportDetailsViewModel> {
    const res = await getReportDetails(projectId);
    return parseReportDetailsFromLanding(res.data, projectId);
}

export interface SynchedOnlineContact {
    name: string;
    email: string;
}

export function parseSynchedOnlineFromLanding(apiData: unknown): SynchedOnlineContact | null {
    if (!apiData || typeof apiData !== "object") return null;
    const d = apiData as Record<string, unknown>;
    const raw = d.synchedOnline;
    if (raw == null || typeof raw !== "object") return null;
    const o = raw as Record<string, unknown>;
    const name = String(o.name ?? "").trim();
    const emailId = String(o.emailId ?? "").trim();
    if (!name && !emailId) return null;
    return { name: name || "—", email: emailId || "—" };
}

export function parseReleaseFlagFromLanding(apiData: unknown): boolean {
    if (!apiData || typeof apiData !== "object") return false;
    const d = apiData as Record<string, unknown>;
    const v = d.release ?? d.Release;
    return v === true || v === "True" || v === "true";
}

export function getDownloadedDataExists(projectId: number): Promise<AxiosResponse<boolean>> {
    return axiosInstance.get<boolean>(`/api/DataTransferAPI/GetDownloadedData`, {
        params: { projectId },
    });
}
export function updateOfflineReleasedStatus(projectId: number | string): Promise<AxiosResponse<unknown>> {
    return axiosInstance.post(`/Offline/UpdateReleasedStatus`, null, {
        params: { projectId },
    });
}

function toApiDateJson(
    val: string | Date | undefined | null
): string | null {
    if (val == null || val === "") return null;
    const m = moment(val);
    return m.isValid() ? m.format("YYYY-MM-DD") : null;
}
export function saveReportDetails(reportDetails: ReportDetailsViewModel): Promise<AxiosResponse> {
    const routePath = `/api/Project/SaveReportDetails`;
    /** API validates [Required] on non-nullable strings — send real values, never null. */
    const projectName =
        (reportDetails.projectName && String(reportDetails.projectName).trim()) ||
        `Project ${reportDetails.projectId}`;
    const cap1Raw =
        reportDetails.cap1FinalGrade !== undefined && reportDetails.cap1FinalGrade !== null
            ? String(reportDetails.cap1FinalGrade).trim()
            : "";
    const cap1FinalGrade =
        cap1Raw ||
        (reportDetails.finalGrade != null ? String(reportDetails.finalGrade) : "") ||
        "N/A";

    const body = {
        gradesId: reportDetails.gradesId ?? 0,
        projectId: reportDetails.projectId,
        projectName,
        capCertificateNumber: reportDetails.capCertificateNumber ?? null,
        classReportNumber: reportDetails.classReportNumber ?? null,
        structuralReportNumber: reportDetails.structuralReportNumber ?? null,
        capCertificateIssuanceDate: toApiDateJson(reportDetails.capCertificateIssuanceDate),
        classReportDate: toApiDateJson(reportDetails.classReportDate),
        structuralReportDate: toApiDateJson(reportDetails.structuralReportDate),
        finalGrade: reportDetails.finalGrade ?? null,
        structuralGrade: reportDetails.structuralGrade ?? null,
        fatigueGrade: reportDetails.fatigueGrade ?? null,
        renewalGrade: reportDetails.renewalGrade ?? null,
        materialGrade: reportDetails.materialGrade ?? null,
        gaugingGrade: reportDetails.gaugingGrade ?? null,
        hullGirderStrength: reportDetails.hullGirderStrength ?? null,
        cap1CertificateIssuanceDate: toApiDateJson(reportDetails.cap1CertificateIssuanceDate),
        cap1FinalGrade,
        reportDetailsComplete: false,
        reportEdit: false,
        isNewProject: false,
        vesselTypeId: 0,
    };

    return axiosInstance.post(routePath, body, {
        headers: {
            "Content-Type": "application/json",
        },
    });
}

export function getReportPermission(payload: {
    projectpermissions?: any;
    rolepermissions?: any;
    projectId: number;
    roles?: string;
}): Promise<AxiosResponse<ReportPermissionResponse>> {
    const routePath = `/Offline/GetReportPermission`;
    return axiosInstance.post(routePath, payload, {
        headers: {
            'Content-Type': 'application/json'
        }
    });
}

export function GetList(): Promise<AxiosResponse> {
    const routePath = `/api/Project/List`;
    return axiosInstance.get(routePath);
}
export function lookupByImo(imo?: string): Promise<AxiosResponse> {
    const routePath = `/api/Vessel/LookUpByImo`;
    return axiosInstance.get(routePath, {
        params: { imoNumber: imo }
    });
}

export function lookupByClassNumber(classNumber?: string): Promise<AxiosResponse> {
    const routePath = `/api/Vessel/LookUp`;
    return axiosInstance.get(routePath, {
        params: { classNumber }
    });
}
export function releaseOfflineDownload(projectId: number): Promise<AxiosResponse> {
    const routePath = "/api/Project/ReleaseOfflineDownload";
    return axiosInstance.post(routePath, null, {
        params: { projectId }
    });
}
