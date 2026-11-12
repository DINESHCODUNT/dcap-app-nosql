import React, { useState, useEffect, useContext, useRef } from "react";
import {
    SideDialog,
    Input,
    DatePickerInput,
    Label,
    Button,
    AbsSelect,
    SideDialogManagerContext,
    Divider,
    BUTTONS,
} from "customer_portal-ui-shared";
import * as Icon from "react-bootstrap-icons";
import { getStatuses, saveTask } from "../../service/TaskService";
import type { SingleTask } from "../../Interface/Workflow";
import { resolveProjectTaskRowId } from "../../Interface/Workflow";
import DocumentList, { type CommonAttachmentHandle } from "../File/DocumentList";
import Comments, { type CommentsHandle } from "../Activity/Comments";
import { usePopup } from "../../Common/PopupProvider";
import { resultMessage } from "../../Common/UtilityMessage";

interface WorkflowEditProps {
    id: string;
    taskName?: string;
    taskId?: string;
    projectId?: number;
    taskData?: SingleTask;
    onSaveSuccess?: () => void;
}

type FormData = {
    assigned_to: string;
    status: string;
    datestatus: Date | null;
    duedate: Date | null;
    value: number;
    surveyDays: number;
};

const WorkflowEdit: React.FC<WorkflowEditProps> = ({
    id,
    taskName,
    taskId,
    projectId,
    taskData,
    onSaveSuccess,
}) => {
    const sideDialog = useContext(SideDialogManagerContext) as {
        removeDialog?: (dialogId: string) => void;
    };

    const [taskDetails, setTaskDetails] = useState<SingleTask | null>(taskData ?? null);
    const [statusOptions, setStatusOptions] = useState<{ text: string; value: string }[]>([]);
    const popup = usePopup();
    const [saving, setSaving] = useState(false);
    const [formErrors, setFormErrors] = useState<Record<string, string>>({});
    const commentsRef = useRef<CommentsHandle>(null);
    const attachmentsRef = useRef<CommonAttachmentHandle>(null);
    const [orchestrationError, setOrchestrationError] = useState("");

    const [formData, setFormData] = useState<FormData>({
        assigned_to: "",
        status: "",
        datestatus: null,
        duedate: null,
        value: 0,
        surveyDays: 0,
    });

    const handleClose = () => {
        sideDialog?.removeDialog?.(id);
    };

    useEffect(() => {
        getStatuses().then((res) => {
            const list = Array.isArray(res.data) ? res.data : [];
            setStatusOptions(
                list
                    .map((s: { name?: string; id?: number; }) => ({
                        text: String(s.name ?? ""),
                        value: String(s.id ?? ""),
                    }))
                    .filter((o: { value: string }) => o.value)
            );
        });
    }, []);

    useEffect(() => {
        if (taskData) {
            setTaskDetails(taskData);

            const sid = taskData.StatusId ?? taskData.statusId;
            const statusName = taskData.Status ?? taskData.status;

            let status = sid != null ? String(sid) : "";
            if (
                statusOptions.length > 0 &&
                status &&
                !statusOptions.some((o) => o.value === status)
            ) {
                const nm = String(statusName ?? "").toLowerCase();
                const hit = statusOptions.find((o) => o.text.toLowerCase() === nm);
                if (hit) status = hit.value;
            }
            if (!status && statusOptions.length > 0) {
                const nm = String(statusName ?? "").toLowerCase();
                const hit = statusOptions.find((o) => o.text.toLowerCase() === nm);
                if (hit) status = hit.value;
            }

            setFormData({
                assigned_to: taskData.AssignedTo ?? taskData.assignedTo ?? "",
                status,
                datestatus:
                    taskData.StatusDate ?? taskData.statusDate
                        ? new Date((taskData.StatusDate ?? taskData.statusDate) as string | Date)
                        : null,
                duedate:
                    taskData.DueDate ?? taskData.dueDate
                        ? new Date((taskData.DueDate ?? taskData.dueDate) as string | Date)
                        : null,
                value: Number(taskData.Value ?? taskData.value ?? 0),
                surveyDays: Number(taskData.SurveyDays ?? taskData.surveyDays ?? 0),
            });

        } else if (taskName) {
            setTaskDetails({
                Task: taskName,
                ID: taskId ? parseInt(taskId, 10) : undefined,
            } as SingleTask);
        }
    }, [taskData, taskName, taskId, projectId, statusOptions]);

    const buildPayloadForSave = (td: SingleTask, fd: FormData): SingleTask => {
        const rowId = resolveProjectTaskRowId(td);
        return {
            ...td,
            id: rowId,
            StatusId: parseInt(String(fd.status), 10) || td.StatusId,
            statusId: parseInt(String(fd.status), 10) || td.statusId,
            StatusDate: fd.datestatus,
            statusDate: fd.datestatus,
            AssignedTo: fd.assigned_to,
            assignedTo: fd.assigned_to,
            AssignedDate: td.AssignedDate ?? td.assignedDate ?? new Date(),
            DueDate: fd.duedate,
            dueDate: fd.duedate,
            Value: fd.value,
            value: fd.value,
            SurveyDays: fd.surveyDays,
            surveyDays: fd.surveyDays,
        };
    };

    const validateForm = (): boolean => {
        const errors: Record<string, string> = {};
        if (!String(formData.status ?? "").trim()) {
            errors.status = "Status is required.";
        }
        setFormErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const handleSaveWorkflow = async () => {
        if (!taskDetails) return;
        setFormErrors({});
        setOrchestrationError("");
        if (!validateForm()) return;

        const label = statusOptions.find((o) => o.value === String(formData.status))?.text;
        const merged = buildPayloadForSave(taskDetails, formData);
        setSaving(true);

        try {
            const res = await saveTask({
                ...taskDetails,
                ...merged,
                Status: label ?? taskDetails.Status,
            });
            const updated = res?.data as SingleTask | undefined;

            if (updated && typeof updated === "object") {
                setTaskDetails({ ...taskDetails, ...merged, ...updated, Status: label ?? taskDetails.Status });
                setFormData((f) => ({
                    ...f,
                    status: String(updated.statusId ?? f.status),
                    datestatus: updated.statusDate
                        ? new Date((updated.statusDate) as string)
                        : f.datestatus,
                    assigned_to: String(updated.assignedTo ?? f.assigned_to),
                    duedate: updated.dueDate
                        ? new Date((updated.dueDate) as string)
                        : f.duedate,
                    value: Number(updated.value ?? f.value),
                    surveyDays: Number(updated.surveyDays ?? f.surveyDays),
                }));
            } else {
                setTaskDetails({ ...taskDetails, ...merged, Status: label ?? taskDetails.Status });
            }
            setFormErrors({});

            const commentsOk =
                commentsRef.current == null
                    ? true
                    : await commentsRef.current.saveIfNeeded({ suppressSuccessPopup: true });
            if (!commentsOk) {
                const msg = "Task details were saved, but the comment could not be saved. Fix the issue and try Save again.";
                setOrchestrationError(msg);
                popup.error({ message: msg });
                return;
            }

            const attachRes =
                attachmentsRef.current == null
                    ? { ok: true as const }
                    : await attachmentsRef.current.flushAttachments({ suppressSuccessPopup: true });
            if (!attachRes.ok) {
                const msg = attachRes.message ?? "Task details were saved, but one or more file changes could not be completed.";
                setOrchestrationError(msg);
                popup.error({ message: msg });
                return;
            }

            onSaveSuccess?.();
            window.dispatchEvent(new Event("refreshProjectList"));
            handleClose();
            popup.success({
                title: "Task Details",
                message: resultMessage("Task details", "Saved")
            });

        } catch (e: any) {
            popup.error({ message: e?.message || "Failed to save task details." });
        } finally {
            setSaving(false);
        }
    };

    const handleCancelWorkflow = () => {
        if (taskData) {
            let status = String(taskData.StatusId ?? taskData.statusId ?? "");
            if (
                statusOptions.length > 0 &&
                status &&
                !statusOptions.some((o) => o.value === String(status))
            ) {
                const nm = String(taskData.Status ?? taskData.status ?? "").toLowerCase();
                const hit = statusOptions.find((o) => o.text.toLowerCase() === nm);
                if (hit) status = hit.value;
            }
            setTaskDetails(taskData);
            setFormData({
                assigned_to: taskData.assignedTo ?? "",
                status,
                datestatus: taskData.statusDate
                    ? new Date((taskData.statusDate) as string)
                    : null,
                duedate: taskData.dueDate
                    ? new Date((taskData.dueDate) as string)
                    : null,
                value: Number(taskData.value ?? 0),
                surveyDays: Number(taskData.surveyDays ?? 0),
            });
        }
        setFormErrors({});
        handleClose();
    };

    const category = taskDetails?.Category || "";
    const isInvoicing = category === "Invoicing";
    const isSurvey = taskDetails?.Task === "Survey";
    const displayTitle = taskDetails?.Task || taskName || "Record Review Requested";
    const parsedCommentTaskId = taskId != null ? parseInt(taskId, 10) : NaN;

    return (
        <>
            <SideDialog
                id={id}
                confirmOnClose={false}
                className="right-side-dialog"
                showWarningMinimize={() => false}
                title={
                    <div className="side-panel-title">
                        <h5>Manage Workflow - {displayTitle}</h5>
                        <div className="side-header-actions">
                            <Button className="close-button" onClick={handleClose}>
                                <Icon.X />
                            </Button>
                        </div>
                    </div>
                }
                minimizedTitle="Edit Workflow Details"
            >
                {() => (
                    <>
                        <h3 className="form-group-title">Workflow Details</h3>
                        <div className="row">
                            <div className="col-md-6 form-group">
                                <Label name="Assign To (Email)" bold />
                                <Input
                                    label=""
                                    placeholder="Enter Assign To (Email)"
                                    value={formData.assigned_to}
                                    onChange={(value: string) => {
                                        setFormData((f) => ({
                                            ...f,
                                            assigned_to: value,
                                        }));
                                        if (formErrors.assigned_to) {
                                            setFormErrors((p) => {
                                                const n = { ...p };
                                                delete n.assigned_to;
                                                return n;
                                            });
                                        }
                                    }}
                                />
                                {formErrors.assigned_to && (
                                    <span className="text-danger small d-block mt-1">{formErrors.assigned_to}</span>
                                )}
                            </div>
                            <div className="col-md-6 form-group">
                                <Label name="Status" bold />
                                <AbsSelect
                                    key={formData.status + "-" + statusOptions.length}
                                    mult={false}
                                    onChange={(selected: unknown) => {
                                        const v = String((Array.isArray(selected) && selected[0]) || selected || "");
                                        setFormData((f) => ({ ...f, status: v }));
                                    }}
                                    options={statusOptions}
                                    selected={formData.status ? [formData.status] : []}
                                    placeholder="Select a Status"
                                />
                                {formErrors.status && (
                                    <span className="text-danger small d-block mt-1">{formErrors.status}</span>
                                )}
                            </div>

                            <div className="col-md-6 form-group">
                                <Label name="Date of Status" bold />
                                <DatePickerInput
                                    onChange={(d: Date | null) => {
                                        setFormData((f) => ({ ...f, datestatus: d }));
                                        if (formErrors.datestatus) {
                                            setFormErrors((p) => {
                                                const n = { ...p };
                                                delete n.datestatus;
                                                return n;
                                            });
                                        }
                                    }}
                                    value={formData.datestatus}
                                />
                                {formErrors.datestatus && (
                                    <span className="text-danger small d-block mt-1">{formErrors.datestatus}</span>
                                )}
                            </div>
                            <div className="col-md-6 form-group">
                                {isInvoicing ? (
                                    <>
                                        <Label name="Value" bold />
                                        <Input
                                            type="number"
                                            label=""
                                            placeholder="Enter Value"
                                            value={formData.value}
                                            onChange={(v: string) => {
                                                const value = parseFloat(v) || 0;
                                                setFormData((f) => ({ ...f, value }));
                                                if (formErrors.value) {
                                                    setFormErrors((p) => {
                                                        const n = { ...p };
                                                        delete n.value;
                                                        return n;
                                                    });
                                                }
                                            }}
                                        />
                                        {formErrors.value && (
                                            <span className="text-danger small d-block mt-1">{formErrors.value}</span>
                                        )}
                                    </>
                                ) : (
                                    <>
                                        <Label name="Due Date" bold />
                                        <DatePickerInput
                                            onChange={(d: Date | null) => {
                                                setFormData((f) => ({ ...f, duedate: d }));
                                                if (formErrors.duedate) {
                                                    setFormErrors((p) => {
                                                        const n = { ...p };
                                                        delete n.duedate;
                                                        return n;
                                                    });
                                                }
                                            }}
                                            value={formData.duedate}
                                        />
                                        {formErrors.duedate && (
                                            <span className="text-danger small d-block mt-1">{formErrors.duedate}</span>
                                        )}
                                    </>
                                )}
                            </div>

                            {isInvoicing && isSurvey && (
                                <div className="col-md-6 form-group">
                                    <Label name="Survey Days" bold />
                                    <Input
                                        type="number"
                                        label=""
                                        placeholder="Enter Survey Days"
                                        value={formData.surveyDays}
                                        onChange={(v: string) => {
                                            const n = parseInt(v, 10) || 0;
                                            setFormData((f) => ({ ...f, surveyDays: n }));
                                            if (formErrors.surveyDays) {
                                                setFormErrors((p) => {
                                                    const next = { ...p };
                                                    delete next.surveyDays;
                                                    return next;
                                                });
                                            }
                                        }}
                                    />
                                    {formErrors.surveyDays && (
                                        <span className="text-danger small d-block mt-1">{formErrors.surveyDays}</span>
                                    )}
                                </div>
                            )}
                        </div>

                        {projectId != null && !Number.isNaN(parsedCommentTaskId) ? (
                            <Comments
                                ref={commentsRef}
                                sectionTitleClassName="mt-3"
                                projectId={projectId}
                                projectTaskId={parsedCommentTaskId}
                                notes={taskDetails?.Notes}
                                globalpermission={null}
                            />
                        ) : null}

                        <DocumentList
                            ref={attachmentsRef}
                            projectId={projectId}
                            taskId={
                                taskId
                                    ? (() => {
                                        const n = parseInt(taskId, 10);
                                        return Number.isNaN(n) ? null : n;
                                    })()
                                    : null
                            }
                        />

                        <Divider className="divider-1 mt-4" />
                        {orchestrationError ? (
                            <div className="col-md-12 mt-2">
                                <span className="text-danger small">{orchestrationError}</span>
                            </div>
                        ) : null}
                        <div className="col-md-12 form-group mt-3">
                            <div className="d-flex gap-2">
                                <Button
                                    type="button"
                                    variant={BUTTONS.PRIMARY}
                                    onClick={handleSaveWorkflow}
                                    disabled={saving || !taskDetails}
                                >
                                    {saving ? "Updating…" : "Update"}
                                </Button>
                                <Button
                                    type="button"
                                    variant={BUTTONS.SECONDARY}
                                    onClick={handleCancelWorkflow}
                                    disabled={saving}
                                >
                                    Cancel
                                </Button>
                            </div>
                        </div>
                    </>
                )}
            </SideDialog>
        </>
    );
};

export default WorkflowEdit;
