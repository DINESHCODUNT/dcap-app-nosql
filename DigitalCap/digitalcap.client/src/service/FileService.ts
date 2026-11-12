import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";
import {
    FileDataType,
    getFileDataTypeDisplayName,
    inferFileDataTypeLoose,
    parseFileDataType,
} from "../constants/fileDataTypes";
import { formatDisplayDateTime } from "../utils/dateTime";

/** Merge root row with nested shapes some APIs use (`file`, `projectFile`, …). */
function mergeBlocksForFileTypeLookup(row: unknown): Record<string, unknown> {
    const r = row as Record<string, unknown>;
    const merged: Record<string, unknown> = { ...r };
    const nests = [
        r.file,
        r.File,
        r.projectFile,
        r.ProjectFile,
        r.document,
        r.Document,
        r.model,
        r.Model,
        r.entity,
        r.Entity,
    ];
    for (const n of nests) {
        if (n && typeof n === "object" && !Array.isArray(n)) {
            Object.assign(merged, n as Record<string, unknown>);
        }
    }
    return merged;
}

function keyLooksLikeFileDataTypeField(key: string): boolean {
    const k = key.toLowerCase();
    if (/filename|filepath|fullpath|contenttype|mimetype|filesize|sizeof|length$/i.test(k)) {
        return false;
    }
    return (
        k.includes("datatype") ||
        k.includes("filedatatype") ||
        (k.includes("filetype") && !k.includes("filename")) ||
        k.includes("documenttype") ||
        (k.includes("category") && !k.includes("imagecategory")) ||
        /^type$/i.test(k) ||
        /^kind$/i.test(k)
    );
}

function resolveDataTypeFromRow(r: Record<string, unknown>): number | null {
    const candidates: unknown[] = [
        r.dataType,
        r.DataType,
        r.data_type,
        r.fileDataType,
        r.FileDataType,
        r.dataTypeEnum,
        r.DataTypeEnum,
        r.type,
        r.Type,
        r.fileType,
        r.FileType,
        r.file_type,
        r.fileTypeName,
        r.FileTypeName,
        r.fileTypeLabel,
        r.FileTypeLabel,
        r.file_type_label,
        r.documentType,
        r.DocumentType,
        r.fileTypeId,
        r.FileTypeId,
        r.category,
        r.Category,
    ];
    for (const c of candidates) {
        const p = parseFileDataType(c);
        if (p != null) return p;
    }
    for (const c of candidates) {
        if (typeof c === "string" && c.length > 0 && c.length < 256) {
            const loose = inferFileDataTypeLoose(c);
            if (loose != null) return loose;
        }
    }
    for (const [key, val] of Object.entries(r)) {
        if (!keyLooksLikeFileDataTypeField(key)) continue;
        const p = parseFileDataType(val);
        if (p != null) return p;
    }
    for (const [key, val] of Object.entries(r)) {
        if (!keyLooksLikeFileDataTypeField(key) || typeof val !== "string") continue;
        if (val.length >= 256) continue;
        const loose = inferFileDataTypeLoose(val);
        if (loose != null) return loose;
    }
    return null;
}

/** Avoid `Boolean("false") === true`; treat missing as not available to client. */
function parseAvailableForClientFlag(v: unknown): boolean {
    if (v === true || v === 1) return true;
    if (v === false || v === 0) return false;
    if (typeof v === "string") {
        const t = v.trim().toLowerCase();
        if (t === "true" || t === "1" || t === "yes") return true;
        if (t === "false" || t === "0" || t === "no" || t === "") return false;
    }
    return false;
}

export interface ProjectFile {
    id: number;
    fileName?: string;
    FileName?: string;
    fileType?: string;
    FileType?: string;
    description?: string;
    Description?: string;
    dataType?: number;
    DataType?: number;
    uploadedOn?: string | Date;
    UploadedOn?: string | Date;
    uploadedBy?: string;
    UploadedBy?: string;
    lastModifiedBy?: string;
    LastModifiedBy?: string;
    lastModifiedOn?: string | Date;
    LastModifiedOn?: string | Date;
    availableForClient?: boolean;
    AvailableForClient?: boolean;
    [key: string]: unknown;
}

export function normalizeProjectFile(row: unknown): {
    id: number;
    fullName: string;
    fileTypeLabel: string;
    description: string;
    dataType: number | null;
    uploadedOn: string;
    uploadedBy: string;
    modifiedLine: string;
    availableForClient: boolean;
} {
    const r = row as Record<string, unknown>;
    const id = Number(r.id ?? r.Id ?? 0);

    /** Prefer `name` when present — API `fullName` sometimes duplicates extension (e.g. `.xlsx.xlsx`). */
    const baseName = r.name ?? r.Name;
    const extRaw = r.extension ?? r.Extension;
    const fromFull = r.fullName ?? r.FullName ?? r.fileName ?? r.FileName;

    let fullName = "";
    if (baseName != null && String(baseName).trim() !== "") {
        const n = String(baseName).trim();
        const e = extRaw != null ? String(extRaw).trim() : "";
        if (!e) {
            fullName = n;
        } else {
            const extNorm = e.startsWith(".") ? e.toLowerCase() : `.${e.toLowerCase()}`;
            fullName = n.toLowerCase().endsWith(extNorm) ? n : `${n}${e.startsWith(".") ? e : `.${e}`}`;
        }
    } else if (fromFull != null && String(fromFull).trim() !== "") {
        fullName = String(fromFull).trim();
    }

    const description = String(r.description ?? r.Description ?? "");
    const merged = mergeBlocksForFileTypeLookup(row);
    const dt = resolveDataTypeFromRow(merged);
    const apiTypeText = String(
        merged.fileTypeLabel ??
        merged.FileTypeLabel ??
        merged.fileTypeName ??
        merged.FileTypeName ??
        merged.fileType ??
        merged.FileType ??
        merged.file_type ??
        merged.typeName ??
        merged.TypeName ??
        merged.dataTypeName ??
        merged.DataTypeName ??
        merged.fileTypeText ??
        merged.FileTypeText ??
        ""
    ).trim();
    const fromEnum =
        dt != null && Number.isFinite(dt) ? getFileDataTypeDisplayName(Number(dt)) : null;
    const apiLooksLikeRawEnumNumber = /^\d+$/.test(apiTypeText);
    const fileTypeLabel =
        (apiTypeText && !apiLooksLikeRawEnumNumber ? apiTypeText : "") ||
        (fromEnum ?? "") ||
        apiTypeText ||
        "";
    const uploadedOn =
        r.uploadedOn ??
        r.UploadedOn ??
        r.uploadDate ??
        r.UploadDate ??
        r.createdDate ??
        r.CreatedDate ??
        r.lastModifiedDate ??
        r.LastModifiedDate ??
        r.lastModifiedOn ??
        r.LastModifiedOn;
    const uploadedBy = String(r.uploadedBy ?? r.UploadedBy ?? r.createdBy ?? r.CreatedBy ?? "");
    const modBy = String(r.lastModifiedBy ?? r.LastModifiedBy ?? "");
    const modOn = r.lastModifiedOn ?? r.LastModifiedOn ?? r.lastModifiedDate ?? r.LastModifiedDate;
    let modifiedLine = "-";
    if (modBy || modOn) {
        const on =
            modOn != null && modOn !== ""
                ? formatDisplayDateTime(modOn as string) || "N/A"
                : "N/A";
        modifiedLine = modBy ? `${modBy} / ${on}` : String(on);
    }
    const rawAvail =
        r.availableForClient ??
        r.AvailableForClient ??
        r.availableToClient ??
        r.AvailableToClient;
    const availableForClient = parseAvailableForClientFlag(rawAvail);

    return {
        id,
        fullName,
        fileTypeLabel,
        description,
        dataType: dt ?? null,
        uploadedOn: uploadedOn != null && uploadedOn !== "" ? String(uploadedOn) : "",
        uploadedBy,
        modifiedLine,
        availableForClient,
    };
}

/** MVC / Web API sometimes returns `{ data: [...] }` or nested wrappers instead of a bare array. */
export function unwrapProjectFilesListPayload(data: unknown, depth = 0): unknown[] {
    if (depth > 10) return [];
    if (Array.isArray(data)) return data;
    if (data && typeof data === "object") {
        const o = data as Record<string, unknown>;
        const preferredKeys = [
            "data",
            "Data",
            "files",
            "Files",
            "results",
            "Results",
            "items",
            "Items",
            "rows",
            "Rows",
        ];
        for (const k of preferredKeys) {
            const inner = o[k];
            if (Array.isArray(inner)) return inner;
        }
        for (const v of Object.values(o)) {
            if (Array.isArray(v)) return v;
        }
        for (const v of Object.values(o)) {
            if (v && typeof v === "object" && !Array.isArray(v)) {
                const nested = unwrapProjectFilesListPayload(v, depth + 1);
                if (nested.length > 0) return nested;
            }
        }
    }
    return [];
}

export function getProjectFiles(
    projectId: number,
    taskId?: number | null
): Promise<AxiosResponse<ProjectFile[]>> {

    if (taskId != null && !Number.isNaN(Number(taskId))) {
        return axiosInstance.get(`/api/File/GetTaskFiles`, {
            params: { taskId }
        });
    }

    return axiosInstance.get(`/api/File/GetProjectFiles`, {
        params: { projectId }
    });
}

/**
 * Matches `FileController.UploadTaskFile` vs `UploadProjectFile`:
 * - Task context (workflow / revenue task details): POST `UploadTaskFile` with `taskId`.
 * - Project Files (no task): POST `UploadProjectFile` — no `taskId`.
 * Kendo used SaveField("uploadedFile") + projectId, dataType, optional taskId in form.
 */
export function uploadTaskFile(params: {
    projectId: number;
    dataType: number;
    file: File;
    taskId?: number | null;
}): Promise<AxiosResponse<unknown>> {
    const { projectId, dataType, file, taskId } = params;
    const formData = new FormData();
    formData.append("uploadedFile", file);
    formData.append("projectId", String(projectId));
    formData.append("dataType", String(dataType));
    const taskIdNum = taskId != null ? Number(taskId) : NaN;
    const hasTask = Number.isFinite(taskIdNum);
    if (hasTask) {
        formData.append("taskId", String(taskIdNum));
    }
    const url = hasTask ? `/api/File/UploadTaskFile` : `/api/File/UploadProjectFile`;
    return axiosInstance.post(url, formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
}

/** @deprecated Same as `uploadTaskFile` when `taskId` is omitted; kept for call sites if any. */
export function uploadTaskFileViaUploadProjectFile(params: {
    projectId: number;
    dataType: number;
    file: File;
    taskId?: number | null;
}): Promise<AxiosResponse<unknown>> {
    return uploadTaskFile({ ...params, taskId: null });
}

export async function uploadTaskFileWithFallback(params: {
    projectId: number;
    dataType: number;
    file: File;
    taskId?: number | null;
}): Promise<AxiosResponse<unknown>> {
    return uploadTaskFile(params);
}

export function updateProjectFile(
    fileId: number,
    updates: {
        description?: string;
        dataType?: number;
        availableForClient?: boolean;
        fileName?: string;
        [key: string]: unknown;
    }
): Promise<AxiosResponse> {
    const routePath = `/api/File/UpdateProjectFile`;
    return axiosInstance.post(routePath, {
        id: fileId,
        ...updates,
    });
}

/** Matches FileController UpdateFileMetadata — all parameters [FromQuery]. */
export function updateFileMetadata(params: {
    id: number;
    name: string;
    dataType: number;
    availableToClient: boolean;
}): Promise<AxiosResponse<unknown>> {
    return axiosInstance.post(`/api/File/UpdateFileMetadata`, null, {
        params: {
            id: String(params.id),
            name: params.name,
            dataType: String(params.dataType),
            availableToClient: String(params.availableToClient),
        },
    });
}

/** Matches `FileController.Delete` — MVC action currently accepts `POST` with query `fileId`. */
export function deleteProjectFile(fileId: number): Promise<AxiosResponse> {
    return axiosInstance.post(`/api/File/Delete`, null, { params: { fileId } });
}

/** GET JSON `{ url }` from GetDownloadUrl and open it (matches [FromQuery] int fileId). */
export async function downloadProjectFile(fileId: number, fallbackFileName?: string): Promise<void> {
    const res = await axiosInstance.get(`/api/File/GetDownloadUrl`, {
        params: { fileId },
    });
    const data = res.data as unknown;
    if (data && typeof data === "object") {
        const o = data as Record<string, unknown>;
        const url =
            typeof o.url === "string"
                ? o.url
                : typeof o.Url === "string"
                    ? o.Url
                    : "";
        if (url) {
            const link = document.createElement("a");
            link.href = url;
            if (fallbackFileName?.trim()) {
                link.download = fallbackFileName.trim();
            }
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            return;
        }
    }
    throw new Error("Download failed: no URL in response");
}

/**
 * Prefer enum / value fields over `id` — row `id` is often the grid row PK (1,2,3…), while
 * `value` / `dataType` hold the real file-type id (e.g. 9, 10) from `AdditionalFilesDataTypes`.
 */
function pickDataTypeOptionValue(row: Record<string, unknown>): number {
    const raw =
        row.value ??
        row.Value ??
        row.key ??
        row.Key ??
        row.dataType ??
        row.DataType ??
        row.fileDataType ??
        row.FileDataType ??
        row.enumValue ??
        row.EnumValue ??
        row.typeId ??
        row.TypeId ??
        row.fileTypeId ??
        row.FileTypeId ??
        row.id ??
        row.Id;
    const n = Number(raw);
    return Number.isFinite(n) ? n : NaN;
}

function mapRowsToOptions(rows: Record<string, unknown>[]): { value: number; text: string }[] {
    return rows
        .map((row) => ({
            value: pickDataTypeOptionValue(row),
            text: String(row.text ?? row.Text ?? row.name ?? row.Name ?? row.label ?? row.Label ?? ""),
        }))
        .filter((o) => o.text.trim().length > 0 && !Number.isNaN(o.value) && o.value !== FileDataType.Empty);
}

/** Unwrap common API shapes: raw array, or { data | items | options | results }. */
function normalizeDataTypeApiPayload(data: unknown): { value: number; text: string }[] | null {
    if (data == null) return null;
    if (Array.isArray(data)) {
        const mapped = mapRowsToOptions(data as Record<string, unknown>[]);
        return mapped.length > 0 ? mapped : null;
    }
    if (typeof data === "object") {
        const o = data as Record<string, unknown>;
        const keys = ["data", "Data", "items", "Items", "options", "Options", "results", "Results", "rows", "Rows"];
        for (const k of keys) {
            const inner = o[k];
            if (Array.isArray(inner)) {
                const mapped = mapRowsToOptions(inner as Record<string, unknown>[]);
                if (mapped.length > 0) return mapped;
            }
        }
    }
    return null;
}

/**
 * File upload type dropdown — matches `FileController.GetDataTypes`:
 * GET /api/File/GetDataTypes → `{ value, text }[]` from `AdditionalFilesDataTypes`.
 * (`GetFileDataTypesForProject` etc. are not on this API; they were speculative fallbacks.)
 */
export async function fetchUploadDataTypeOptions(_projectId: number): Promise<{ value: number; text: string }[]> {
    try {
        const res = await axiosInstance.get(`/api/File/GetDataTypes`);
        const parsed = normalizeDataTypeApiPayload(res.data);
        if (parsed && parsed.length > 0) {
            return parsed;
        }
    } catch {
        /* caller falls back to static FileDataType labels */
    }
    return [];
}
