import React, { useCallback, useEffect, useImperativeHandle, useMemo, useState, forwardRef, useRef } from "react";
import Dropzone from "react-dropzone";
import { AbsReactTable, TABLE, Button, AbsSelect, BUTTONS } from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import { Spinner } from "react-bootstrap";
import styled from "styled-components";
import _uniqueId from "lodash/uniqueId";
import { DROP_CLICK_LABEL, SUPPORT_TYPE_LABEL, dropZoneStyle, mimiTypesAccepted } from "./constants";
import * as fileService from "../../service/FileService";
import { FileDataType, isClientDeliverablesType } from "../../constants/fileDataTypes";
import { usePopup } from "../../Common/PopupProvider";
import { resultMessage } from "../../Common/UtilityMessage";

const MAX_FILE_BYTES = 300 * 1024 * 1024;

const StyledTable = styled.div`
    thead tr th {
        font-weight: 600 !important;
        font-size: 0.9rem !important;
        color: #002a4e;
    }
`;

export interface CommonAttachmentProps {
    projectId?: number;
    taskId?: number | null;
    addInternalDocumentsOnly?: boolean;
    /** Client-side filter from DocumentList search (matches name, type label, date, user, client flag). */
    searchQuery?: string;
    /** Bumped by DocumentList Clear Filter to reset any table-side filter UI state. */
    filterResetKey?: number;
    activeTab?: 'abs' | 'vessel';
}

export type FlushAttachmentsOptions = {
    suppressSuccessPopup?: boolean;
};

export type FlushAttachmentsResult = { ok: boolean; message?: string };

export type CommonAttachmentHandle = {
    /**
     * Saves in-progress server-row metadata edit (if any), then uploads all pending rows that have a file type.
     * Pending rows without a type are skipped. Per-row upload/delete from the grid still works as before.
     */
    flushAttachments: (opts?: FlushAttachmentsOptions) => Promise<FlushAttachmentsResult>;
};

/** Local queue row (before API upload) — same idea as reference `!original.Id`. */
interface PendingRow {
    tempKey: string;
    file: File;
    fileName: string;
    dataType: number | null;
}

interface ServerRow {
    id: number;
    fileName: string;
    dataType: number | null;
    fileTypeLabel: string;
    uploadedOn: string;
    uploadedBy: string;
    availableForClient: boolean;
}

const UPLOAD_MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

/** Edit mode — matches MVC “Available to Client” Yes/No dropdown. */
const AVAILABLE_TO_CLIENT_OPTIONS = [
    { text: "Yes", value: "true" },
    { text: "No", value: "false" },
];

function formatUploadDateDisplay(raw: string): string {
    if (!raw) return "-";
    const d = new Date(raw);
    if (Number.isNaN(d.getTime())) return raw;
    const day = String(d.getDate()).padStart(2, "0");
    const mon = UPLOAD_MONTHS[d.getMonth()] ?? "";
    return `${day}-${mon}-${d.getFullYear()}`;
}

function getFileNameWithoutExtension(name: string): string {
    return name.replace(/\.[^/.]+$/, "");
}

/** Extension fallback when `file.type` is empty (common on Windows) — matches SUPPORT_TYPE_LABEL list. */
const OK_EXT = new Set([
    "doc", "docx", "jpg", "pdf", "txt", "xls", "xlsx", "png", "jpeg", "rtf", "zip", "dwg", "gif", "csv",
    "ppt", "bmp", "pptx", "msg", "mp3", "mp4", "avi",
]);

/** Isolated so `selected` array identity is stable when `dataType` is unchanged (avoids AbsSelect sync loops). */
const EditModeFileTypeSelect = React.memo(function EditModeFileTypeSelect({
    dataType,
    options,
    onCommit,
}: {
    dataType: number | null;
    options: { text: string; value: string }[];
    onCommit: (next: number | null) => void;
}) {
    const selected = useMemo(
        () => (dataType != null ? [String(dataType)] : []),
        [dataType]
    );
    return (
        <AbsSelect
            mult={false}
            placeholder="Select Type"
            options={options}
            selected={selected}
            onChange={(sel: unknown) => {
                const next = String((Array.isArray(sel) && sel[0]) || sel || "");
                const parsed = next ? parseInt(next, 10) : null;
                const normalized =
                    parsed != null && !Number.isNaN(parsed) ? parsed : null;
                onCommit(normalized);
            }}
        />
    );
});

function isMimeAccepted(file: File): boolean {
    if (file.type && mimiTypesAccepted.includes(file.type)) return true;
    const ext = file.name.split(".").pop()?.toLowerCase();
    if (!file.type && ext && OK_EXT.has(ext)) return true;
    if (!file.type && ext === "dwg") {
        return ["image/vnd.dwg", "image/x-dwg"].some((t) => mimiTypesAccepted.includes(t));
    }
    return false;
}

const CommonAttachment = forwardRef<CommonAttachmentHandle, CommonAttachmentProps>(function CommonAttachment(
    { projectId, taskId, addInternalDocumentsOnly = false, searchQuery = "", filterResetKey = 0, activeTab = 'abs' },
    ref
) {
    const [dataTypeOptions, setDataTypeOptions] = useState<{ text: string; value: string }[]>([]);
    const [pendingRows, setPendingRows] = useState<PendingRow[]>([]);
    const [serverRows, setServerRows] = useState<ServerRow[]>([]);
    const [loading, setLoading] = useState(false);
    const [uploading, setUploading] = useState(false);
    const [downloadingIds, setDownloadingIds] = useState<number[]>([]);
    const [dropErrors, setDropErrors] = useState<string[]>([]);
    const [editingServerId, setEditingServerId] = useState<number | null>(null);
    const [editDraft, setEditDraft] = useState<{
        fileName: string;
        dataType: number | null;
        availableForClient: boolean;
    } | null>(null);
    const popup = usePopup();
    const [savingMeta, setSavingMeta] = useState(false);
    const [tableKey, setTableKey] = useState(0);

    const initialFilterState = useMemo(
        () => ({
            searchType: "list",
            resetPageIndex: false,
            expandAll: false,
            resetFilters: false,
            deselectRows: false,
            filters: {},
            sort: null,
        }),
        []
    );

    const [appliedFilters, setAppliedFilters] = useState({ ...initialFilterState });

    useEffect(() => {
        setTableKey((k) => k + 1);
        setAppliedFilters({
            ...initialFilterState,
            resetFilters: true,
        });
    }, [filterResetKey, initialFilterState]);

    const loadOptions = useCallback(async () => {
        if (!projectId) return;
        try {
            const opts = await fileService.fetchUploadDataTypeOptions(projectId);
            const mapped = opts.map((o) => ({ text: o.text, value: String(o.value) }));
            setDataTypeOptions(
                mapped.length > 0
                    ? mapped
                    : [
                        { text: "Client Deliverables", value: String(FileDataType.ClientDeliverables) },
                        { text: "Internal Documents", value: String(FileDataType.InternalDocuments) },
                        { text: "Internal Invoicing", value: String(FileDataType.InternalInvoicing) },
                    ]
            );
        } catch {
            setDataTypeOptions([
                { text: "Client Deliverables", value: String(FileDataType.ClientDeliverables) },
                { text: "Internal Documents", value: String(FileDataType.InternalDocuments) },
                { text: "Internal Invoicing", value: String(FileDataType.InternalInvoicing) },
            ]);
        }
    }, [projectId]);

    const loadServerRows = useCallback(async () => {
        if (!projectId) {
            setServerRows([]);
            return;
        }
        setLoading(true);
        try {
            const res = await fileService.getProjectFiles(projectId, taskId ?? null);
            const list = Array.isArray(res.data) ? res.data : [];
            setServerRows(
                list.map((raw) => {
                    const n = fileService.normalizeProjectFile(raw);
                    return {
                        id: n.id,
                        fileName: n.fullName,
                        dataType: n.dataType,
                        fileTypeLabel: n.fileTypeLabel,
                        uploadedOn: n.uploadedOn,
                        uploadedBy: n.uploadedBy,
                        availableForClient: n.availableForClient,
                    };
                })
            );
        } catch {
            setServerRows([]);
        } finally {
            setLoading(false);
        }
    }, [projectId, taskId]);

    useEffect(() => {
        loadOptions();
    }, [loadOptions]);

    useEffect(() => {
        loadServerRows();
    }, [loadServerRows]);

    const docTypeSelectOptions = useMemo(() => {
        let opts = dataTypeOptions.length > 0
            ? dataTypeOptions
            : [
                { text: "Client Deliverables", value: String(FileDataType.ClientDeliverables) },
                { text: "Internal Documents", value: String(FileDataType.InternalDocuments) },
                { text: "Internal Invoicing", value: String(FileDataType.InternalInvoicing) },
            ];

        if (activeTab === 'abs') {
            opts = opts.filter(o => o.value !== String(FileDataType.VesselDocuments));
        } else if (activeTab === 'vessel') {
            opts = opts.filter(o => o.value === String(FileDataType.VesselDocuments));
        }
        return opts;
    }, [dataTypeOptions, activeTab]);

    const updatePending = (tempKey: string, patch: Partial<PendingRow>) => {
        setPendingRows((prev) => prev.map((r) => (r.tempKey === tempKey ? { ...r, ...patch } : r)));
    };

    const startEditServerRow = (sr: ServerRow) => {
        setEditingServerId(sr.id);
        setEditDraft({
            fileName: sr.fileName,
            dataType: addInternalDocumentsOnly ? FileDataType.InternalDocuments : sr.dataType,
            availableForClient: Boolean(sr.availableForClient),
        });
        setDropErrors([]);
    };

    const cancelEditServerRow = () => {
        setEditingServerId(null);
        setEditDraft(null);
    };

    const commitFileMetadata = async (): Promise<boolean> => {
        if (editingServerId == null || !editDraft) return true;
        if (editDraft.dataType == null) {
            setDropErrors(["Select File Type before saving."]);
            return false;
        }
        const name = editDraft.fileName.trim();
        if (!name) {
            setDropErrors(["File name is required."]);
            return false;
        }
        setSavingMeta(true);
        setDropErrors([]);
        try {
            const availableToClient = isClientDeliverablesType(editDraft.dataType)
                ? editDraft.availableForClient
                : null;

            await fileService.updateFileMetadata({
                id: editingServerId,
                name,
                dataType: editDraft.dataType,
                availableToClient: availableToClient ?? false,
            });
            popup.success(resultMessage(name, "Updated"), "Project Files");
            cancelEditServerRow();
            await loadServerRows();
            return true;
        } catch (e: any) {
            popup.error(e?.response?.data || e?.message || "Could not save file details. Try again.", "Project Files");
            setDropErrors(["Could not save file details. Try again."]);
            return false;
        } finally {
            setSavingMeta(false);
        }
    };

    const handleDrop = (acceptedFiles: File[], rejections: any[]) => {
        if (!projectId) return;
        const errors: string[] = [];
        if (rejections.length) {
            errors.push("Some files were rejected by the browser.");
        }

        const validFiles = acceptedFiles.filter((f) => {
            if (f.size > MAX_FILE_BYTES) return false;
            return isMimeAccepted(f);
        });
        const invalidType = acceptedFiles.filter((f) => f.size <= MAX_FILE_BYTES && !isMimeAccepted(f));
        const oversized = acceptedFiles.filter((f) => f.size > MAX_FILE_BYTES);

        if (invalidType.length > 0) {
            errors.push(`Unsupported file type(s): ${invalidType.map((f) => f.name).join(", ")}`);
        }
        if (oversized.length > 0) {
            errors.push(`File(s) exceed 300 MB: ${oversized.map((f) => f.name).join(", ")}`);
        }

        const existingNames = new Set(
            [...pendingRows.map((p) => p.fileName.toLowerCase()), ...serverRows.map((s) => s.fileName.toLowerCase())]
        );

        const newPending: PendingRow[] = [];
        validFiles.forEach((item) => {
            const base = getFileNameWithoutExtension(item.name);
            const fileExtension = item.name.split(".").pop()!;
            let uniqueFileName = `${base}.${fileExtension}`;
            let counter = 1;
            while (existingNames.has(uniqueFileName.toLowerCase())) {
                uniqueFileName = `${base} (${counter}).${fileExtension}`;
                counter += 1;
            }
            existingNames.add(uniqueFileName.toLowerCase());

            const defaultDt = activeTab === 'vessel'
                ? FileDataType.VesselDocuments
                : (addInternalDocumentsOnly ? FileDataType.InternalDocuments : null);
            newPending.push({
                tempKey: _uniqueId("pending_"),
                file: item,
                fileName: uniqueFileName,
                dataType: defaultDt,
            });
        });

        if (newPending.length > 0) {
            setPendingRows((prev) => [...prev, ...newPending]);
        }
        setDropErrors(errors.length ? errors : []);
    };

    const removePending = (tempKey: string) => {
        setPendingRows((prev) => prev.filter((r) => r.tempKey !== tempKey));
    };

    const uploadPendingRow = async (
        row: PendingRow,
        suppressSuccessPopup?: boolean
    ): Promise<boolean> => {
        if (!projectId || row.dataType == null) {
            setDropErrors(["Select File Type for each file before upload."]);
            return false;
        }
        setUploading(true);
        setDropErrors([]);

        try {
            await fileService.uploadTaskFileWithFallback({
                projectId,
                dataType: row.dataType,
                file: row.file,
                taskId: taskId ?? null,
            });

            removePending(row.tempKey);
            await loadServerRows();
            if (!suppressSuccessPopup) {
                popup.success({
                    title: "Upload File",
                    message: resultMessage(row.fileName || "File", "Uploaded")
                });
            }
            return true;
        } catch (e) {
            setDropErrors(["Upload failed. Please try again."]);
            return false;
        } finally {
            setUploading(false);
        }
    };

    useImperativeHandle(
        ref,
        () => ({
            flushAttachments: async (opts) => {
                if (!projectId) {
                    return { ok: true };
                }
                if (editingServerId != null && editDraft) {
                    const metaOk = await commitFileMetadata();
                    if (!metaOk) {
                        return {
                            ok: false,
                            message: "Could not save file details. Select file type and name, or cancel the edit.",
                        };
                    }
                }
                const queue = pendingRows.filter((r) => r.dataType != null);
                for (const row of queue) {
                    const up = await uploadPendingRow(row, opts?.suppressSuccessPopup);
                    if (!up) {
                        return { ok: false, message: "One or more file uploads failed." };
                    }
                }
                return { ok: true };
            },
        }),
        [projectId, editingServerId, editDraft, pendingRows, taskId]
    );

    const downloadServerRow = async (row: ServerRow) => {
        setDropErrors([]);
        setDownloadingIds((prev) => [...prev, row.id]);
        try {
            await fileService.downloadProjectFile(row.id, row.fileName);
        } catch (e) {
            setDropErrors(["Download failed. If this persists, confirm the download API route matches the app."]);
        } finally {
            setDownloadingIds((prev) => prev.filter((id) => id !== row.id));
        }
    };

    const handleDeleteClick = async (row: ServerRow) => {
        const confirmed = await popup.confirm({
            title: "Delete File",
            message: `Are you sure you want to delete ${row.fileName}?`,
            confirmText: "Delete",
            danger: true
        });
        if (!confirmed) return;

        try {
            await fileService.deleteProjectFile(row.id);
            await loadServerRows();
            popup.success({
                title: "Delete File",
                message: resultMessage(row.fileName || "File", "Deleted")
            });
        } catch (e: any) {
            setDropErrors(["Could not delete file. Try again."]);
            popup.error({ message: e?.message || "Could not delete file. Try again." });
        }
    };

    type TableRow = {
        __kind: "pending" | "server";
        __key: string;
        file_name: string;
        document_type: string;
        upload_date: string;
        upload_user: string;
        available_client: string;
        serverId?: number;
        isEditing?: boolean;
        editDraft?: { fileName: string; dataType: number | null; availableForClient: boolean };
        savingMeta?: boolean;
    };

    const tableData: TableRow[] = useMemo(() => {
        const resolveTypeLabel = (dataType: number | null, fallback = ""): string => {
            if (dataType == null) return fallback;
            return (
                docTypeSelectOptions.find((x) => x.value === String(dataType))?.text
                || fallback
                || String(dataType)
            );
        };

        const p = pendingRows.map((pr) => ({
            __kind: "pending" as const,
            __key: pr.tempKey,
            file_name: pr.fileName,
            document_type: resolveTypeLabel(pr.dataType),
            upload_date: "-",
            upload_user: "-",
            available_client: "-",
            isEditing: false,
        }));

        const s = serverRows.map((sr) => ({
            __kind: "server" as const,
            __key: `s-${sr.id}`,
            file_name: sr.fileName,
            document_type: resolveTypeLabel(sr.dataType, sr.fileTypeLabel || ""),
            upload_date: formatUploadDateDisplay(sr.uploadedOn),
            upload_user: sr.uploadedBy || "-",
            available_client: isClientDeliverablesType(sr.dataType) ? (sr.availableForClient ? "Yes" : "No") : "-",
            serverId: sr.id,
            isEditing: editingServerId === sr.id,
            editDraft: editingServerId === sr.id && editDraft ? editDraft : undefined,
            savingMeta: editingServerId === sr.id ? savingMeta : false,
        }));
        return [...p, ...s];
    }, [pendingRows, serverRows, docTypeSelectOptions, editingServerId, editDraft, savingMeta]);

    const applyFilter = useCallback((filtersObj: any) => {
        const filteredData = Object.fromEntries(
            Object.entries(filtersObj.filters || {}).filter(
                ([, value]) => (value as { searchTerm: string })?.searchTerm !== ""
                    || ((value as { selectedFilters?: string[] })?.selectedFilters?.length ?? 0) > 0
                    || (value as { isFiltersApplied?: boolean })?.isFiltersApplied
            )
        );
        setAppliedFilters({
            ...filtersObj,
            filters: filteredData,
            searchType: "list",
            resetPageIndex: false,
            resetFilters: false,
        });
    }, []);

    const filteredTableData = useMemo(() => {
        let baseData = tableData;

        if (activeTab === 'abs') {
            baseData = baseData.filter(row => {
                const originalType = row.__kind === 'server' ? serverRows.find(sr => sr.id === row.serverId)?.dataType : pendingRows.find(pr => pr.tempKey === row.__key)?.dataType;
                return originalType !== FileDataType.VesselDocuments;
            });
        } else if (activeTab === 'vessel') {
            baseData = baseData.filter(row => {
                const originalType = row.__kind === 'server' ? serverRows.find(sr => sr.id === row.serverId)?.dataType : pendingRows.find(pr => pr.tempKey === row.__key)?.dataType;
                return originalType === FileDataType.VesselDocuments;
            });
        }

        const q = searchQuery.trim().toLowerCase();
        if (!q) return baseData;

        return baseData.filter((row) => {
            const parts: string[] = [
                row.file_name,
                row.document_type,
                row.upload_date,
                row.upload_user,
            ];
            if (row.available_client !== "-") {
                parts.push(row.available_client);
            }
            return parts.join(" ").toLowerCase().includes(q);
        });
    }, [tableData, searchQuery, activeTab, serverRows, pendingRows]);

    const callbacksRef = useRef({
        commitFileMetadata,
        cancelEditServerRow,
        downloadServerRow,
        startEditServerRow,
        handleDeleteClick,
    });
    useEffect(() => {
        callbacksRef.current = {
            commitFileMetadata,
            cancelEditServerRow,
            downloadServerRow,
            startEditServerRow,
            handleDeleteClick,
        };
    });

    const allColumns = useMemo(
        () => [
            {
                label: "File Name",
                accessor: "file_name",
                accessorKey: "file_name",
                allowShowHide: false,
                Cell: ({ row }: { row: { original: TableRow } }) => {
                    const o = row.original;
                    if (o.__kind === "server" && o.serverId != null && o.isEditing && o.editDraft) {
                        return (
                            <input
                                type="text"
                                className="new-text-input"
                                value={o.editDraft.fileName}
                                onChange={(e) =>
                                    setEditDraft((prev) =>
                                        prev ? { ...prev, fileName: e.target.value } : prev
                                    )
                                }
                            />
                        );
                    }
                    return <span>{o.file_name}</span>;
                },
            },
            {
                label: "File Type",
                accessor: "document_type",
                accessorKey: "document_type",
                allowShowHide: false,
                Cell: ({ row }: { row: { original: TableRow } }) => {
                    const o = row.original;
                    if (o.__kind === "pending") {
                        const pr = pendingRows.find((x) => x.tempKey === o.__key);
                        const v = pr?.dataType != null ? String(pr.dataType) : "";
                        if (addInternalDocumentsOnly) {
                            return (
                                <span className="small text-muted">
                                    {docTypeSelectOptions.find((x) => x.value === String(FileDataType.InternalDocuments))
                                        ?.text ?? "Internal Documents"}
                                </span>
                            );
                        }
                        return (
                            <AbsSelect
                                mult={false}
                                placeholder="Select Type"
                                options={docTypeSelectOptions}
                                selected={v ? [v] : []}
                                onChange={(sel) => {
                                    const next = String((Array.isArray(sel) && sel[0]) || sel || "");
                                    const parsed = next ? parseInt(next, 10) : null;

                                    if (pr?.dataType === parsed) return;

                                    updatePending(o.__key, {
                                        dataType: parsed,
                                    });
                                }}
                            />
                        );
                    }

                    const sr = serverRows.find((x) => x.id === o.serverId);
                    if (!sr) return null;

                    if (o.isEditing && o.editDraft) {
                        if (addInternalDocumentsOnly) {
                            return (
                                <span className="small text-muted">
                                    {docTypeSelectOptions.find((x) => x.value === String(FileDataType.InternalDocuments))
                                        ?.text ?? "Internal Documents"}
                                </span>
                            );
                        }
                        return (
                            <EditModeFileTypeSelect
                                dataType={o.editDraft.dataType}
                                options={docTypeSelectOptions}
                                onCommit={(normalized) => {
                                    setEditDraft((prev) => {
                                        if (!prev) return prev;
                                        if (prev.dataType === normalized) return prev;
                                        const nextAvail =
                                            normalized != null && isClientDeliverablesType(normalized)
                                                ? prev.availableForClient
                                                : false;
                                        return { ...prev, dataType: normalized, availableForClient: nextAvail };
                                    });
                                }}
                            />
                        );
                    }

                    const typeLabel =
                        docTypeSelectOptions.find((x) => x.value === String(sr.dataType))?.text ??
                        sr.fileTypeLabel ??
                        "—";
                    return <span className="small text-muted">{typeLabel}</span>;
                },
            },
            {
                label: "Upload Date",
                accessor: "upload_date",
                accessorKey: "upload_date",
                allowShowHide: false,
                Cell: ({ row }: { row: { original: TableRow } }) => (
                    <span className="small text-muted">{row.original.upload_date}</span>
                ),
            },
            {
                label: "Upload User",
                accessor: "upload_user",
                accessorKey: "upload_user",
                allowShowHide: false,
                Cell: ({ row }: { row: { original: TableRow } }) => (
                    <span className="small text-muted">{row.original.upload_user}</span>
                ),
            },
            {
                label: "Available to Client?",
                accessor: "available_client",
                accessorKey: "available_client",
                allowShowHide: false,
                Cell: ({ row }: { row: { original: TableRow } }) => {
                    const o = row.original;
                    if (o.__kind === "pending") {
                        return <span className="small text-muted">-</span>;
                    }
                    if (
                        o.__kind === "server" &&
                        o.serverId != null &&
                        o.isEditing &&
                        o.editDraft
                    ) {
                        if (!isClientDeliverablesType(o.editDraft.dataType)) {
                            return <span className="small text-muted">-</span>;
                        }
                        return (
                            <AbsSelect
                                key={`server-edit-client-${o.serverId}`}
                                mult={false}
                                placeholder="Select"
                                options={AVAILABLE_TO_CLIENT_OPTIONS}
                                selected={[o.editDraft.availableForClient ? "true" : "false"]}
                                onChange={(sel: unknown) => {
                                    const v = String(
                                        (Array.isArray(sel) && sel[0]) || sel || "false"
                                    );
                                    const next = v === "true";
                                    setEditDraft((prev) => {
                                        if (!prev) return prev;
                                        if (prev.availableForClient === next) return prev;
                                        return { ...prev, availableForClient: next };
                                    });
                                }}
                            />
                        );
                    }
                    if (o.__kind === "server" && o.serverId != null) {
                        const srView = serverRows.find((x) => x.id === o.serverId);
                        if (srView && !isClientDeliverablesType(srView.dataType)) {
                            return <span className="small text-muted">-</span>;
                        }
                    }
                    if (o.available_client === "-") {
                        return <span className="small text-muted">-</span>;
                    }
                    if (o.available_client === "Yes") {
                        return (
                            <span className="small d-inline-flex align-items-center gap-1">
                                <span
                                    className="d-inline-flex align-items-center justify-content-center rounded-circle bg-success text-white"
                                    style={{ width: 18, height: 18, fontSize: 11, lineHeight: 1 }}
                                >
                                    ✓
                                </span>
                                Yes
                            </span>
                        );
                    }
                    return (
                        <span className="small d-inline-flex align-items-center gap-1">
                            <span
                                className="d-inline-flex align-items-center justify-content-center rounded-circle text-white"
                                style={{
                                    width: 18,
                                    height: 18,
                                    fontSize: 11,
                                    lineHeight: 1,
                                    backgroundColor: "#ffc107",
                                }}
                            >
                                ✕
                            </span>
                            No
                        </span>
                    );
                },
            },
            {
                label: "Actions",
                accessor: "actions",
                accessorKey: "actions",
                allowShowHide: false,
                filter: false,
                filtering: false,
                sorting: false,
                Cell: ({ row }: { row: { original: TableRow } }) => {
                    const o = row.original;
                    if (o.__kind === "pending") {
                        const pr = pendingRows.find((x) => x.tempKey === o.__key);
                        return (
                            <div className="d-flex gap-1 align-items-center">
                                <Button
                                    type="button"
                                    variant={BUTTONS.TABLEDOWNLOAD} startIcon={<Icon.Check2 />}
                                    className="btn-sm p-1"
                                    title="Upload"
                                    disabled={uploading || !pr?.dataType}
                                    onClick={() => pr && uploadPendingRow(pr)}
                                >
                                </Button>
                                <Button
                                    type="button"
                                    variant={BUTTONS.TABLEDOWNLOAD} startIcon={<Icon.Trash />}
                                    className="btn-sm p-1 text-danger"
                                    title="Remove"
                                    disabled={uploading}
                                    onClick={() => removePending(o.__key)}
                                >

                                </Button>
                            </div>
                        );
                    }
                    const sr = serverRows.find((x) => x.id === o.serverId);
                    if (!sr) return null;

                    if (o.isEditing) {
                        return (
                            <div className="d-flex gap-1 align-items-center">
                                <Button
                                    type="button"
                                    variant={BUTTONS.TABLEDOWNLOAD} startIcon={<Icon.Check2 />}
                                    className="btn-sm p-1"
                                    title="Save"
                                    disabled={o.savingMeta}
                                    onClick={() => void callbacksRef.current.commitFileMetadata()}
                                >
                                </Button>
                                <Button
                                    type="button"
                                    variant={BUTTONS.TABLEDOWNLOAD} startIcon={<Icon.XLg />}
                                    className="btn-sm p-1 text-danger"
                                    title="Cancel"
                                    disabled={o.savingMeta}
                                    onClick={() => callbacksRef.current.cancelEditServerRow()}
                                >
                                </Button>
                            </div>
                        );
                    }

                    return (
                        <div className="d-flex gap-1 align-items-center">
                            <Button
                                type="button"
                                variant={BUTTONS.TABLEDOWNLOAD}
                                startIcon={downloadingIds.includes(sr.id) ? <Spinner animation="border" size="sm" /> : <Icon.Download />}
                                className="btn-sm p-1"
                                title="Download"
                                disabled={downloadingIds.includes(sr.id)}
                                onClick={() => callbacksRef.current.downloadServerRow(sr)}
                            >
                            </Button>
                            {activeTab !== 'vessel' && (
                                <Button
                                    type="button"
                                    variant={BUTTONS.TABLEDOWNLOAD} startIcon={<Icon.Pencil />}
                                    className="btn-sm p-1"
                                    title="Edit"
                                    onClick={() => callbacksRef.current.startEditServerRow(sr)}
                                >
                                </Button>
                            )}
                            <Button
                                type="button"
                                variant={BUTTONS.TABLEDOWNLOAD} startIcon={<Icon.Trash />}
                                className="btn-sm p-1 text-danger"
                                title="Delete"
                                onClick={() => callbacksRef.current.handleDeleteClick(sr)}
                            >
                            </Button>
                        </div>
                    );
                },
            },
        ],
        [
            pendingRows,
            serverRows,
            docTypeSelectOptions,
            uploading,
            addInternalDocumentsOnly,
            downloadingIds,
        ]
    );

    const columns = useMemo(() => {
        if (activeTab === 'vessel') {
            return allColumns.filter(c => c.accessorKey !== 'document_type' && c.accessorKey !== 'available_client');
        }
        return allColumns;
    }, [allColumns, activeTab]);

    if (!projectId) {
        return (
            <StyledTable className="po-attachments">
                <p className="text-muted small">Select a project to upload and manage files.</p>
            </StyledTable>
        );
    }

    return (
        <>
            <StyledTable className="po-attachments">
                {activeTab !== 'vessel' && (
                    <div className="upload-section w-100 mb-3">
                        <Dropzone
                            onDrop={handleDrop}
                            multiple
                            maxSize={MAX_FILE_BYTES}
                            disabled={uploading}
                        >
                            {({ getRootProps, getInputProps }) => (
                                <div {...getRootProps({ style: dropZoneStyle })}>
                                    <input {...getInputProps()} />
                                    <div className="d-flex p-15 align-items-center flex-column justify-content-center">
                                        <h3 className="tx-16 _600">{DROP_CLICK_LABEL}</h3>
                                        <p className="p-2 text-center mb-1">{SUPPORT_TYPE_LABEL}</p>
                                        <p className="small text-muted mb-0">
                                            File upload limit: Maximum size per file is 300 MB.
                                        </p>
                                    </div>
                                </div>
                            )}
                        </Dropzone>
                    </div>
                )}
                <div className="custom-react-table">
                    <AbsReactTable
                        key={tableKey}
                        loading={loading}
                        columns={columns}
                        pagination={false}
                        expand={false}
                        allowColumnsShowHide={false}
                        data={filteredTableData}
                        variant={TABLE.VARIANT.SIMPLE}
                        applyFilterCallback={applyFilter}
                        appliedFilters={appliedFilters}
                        allDataProvided
                    />
                </div>

                {dropErrors.length > 0 && (
                    <div className="mt-2" style={{ color: "coral" }}>
                        {dropErrors.map((msg, i) => (
                            <div key={i}>{msg}</div>
                        ))}
                    </div>
                )}
            </StyledTable>
        </>
    );
});

export default CommonAttachment;
