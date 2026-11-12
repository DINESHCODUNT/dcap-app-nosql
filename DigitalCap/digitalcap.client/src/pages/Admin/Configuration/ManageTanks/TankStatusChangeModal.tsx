import React, { useState, useEffect } from 'react';
import { Modal } from 'react-bootstrap';
import { Button, BUTTONS, RadioInput } from 'customer_portal-ui-shared';
import { usePopup } from '../../../../Common/PopupProvider';
import { updateTankStatus } from '../../../../service/TankService';
import { toFriendlyErrorMessage } from '../../../../utils/errorMessage';

export interface SelectedTankItem {
    id: string;
    name?: string;
    status: unknown;
}

export interface TankStatusChangeModalProps {
    show: boolean;
    onHide: () => void;
    selectedTanks: SelectedTankItem[];
    imo?: string;
    projectId?: string | number;
    onDone: () => Promise<void> | void;
}

export function toSelectedTanks(rows: unknown[]): SelectedTankItem[] {
    if (!Array.isArray(rows)) return [];
    return rows
        .filter((r: any) => !r?.checkboxDisabled)
        .map((r: any) => ({
            id: String(r?.tankId || r?.id || ""),
            name: String(r?.tankName || r?.tank_name || ""),
            status: r?.status
        }))
        .filter(t => t.id !== "");
}

export const isTankActive = (status: unknown): boolean => {
    if (typeof status === "boolean") return status;
    if (typeof status === "number") return status === 1;
    if (typeof status === "string") {
        const s = status.trim().toLowerCase();
        return s === "active" || s === "true" || s === "1";
    }
    return false;
};

const TankStatusChangeModal: React.FC<TankStatusChangeModalProps> = ({
    show,
    onHide,
    selectedTanks,
    imo,
    projectId,
    onDone
}) => {
    const popup = usePopup();
    const [targetStatus, setTargetStatus] = useState<boolean>(true);
    const [isSaving, setIsSaving] = useState<boolean>(false);

    useEffect(() => {
        if (show) {
            setIsSaving(false);
            if (selectedTanks.length > 0) {
                const hasActive = selectedTanks.some(t => isTankActive(t.status));
                const hasInactive = selectedTanks.some(t => !isTankActive(t.status));
                if (hasInactive && !hasActive) {
                    setTargetStatus(false);
                } else {
                    setTargetStatus(true);
                }
            } else {
                setTargetStatus(true);
            }
        }
    }, [show, selectedTanks]);

    const handleProceed = async () => {
        if (selectedTanks.length === 0) {
            onHide();
            popup.info({
                title: "Warning",
                message: "Please select at least one tank"
            });
            return;
        }

        const idsToChange = selectedTanks
            .filter(t => isTankActive(t.status) !== targetStatus)
            .map(t => t.id);

        if (idsToChange.length === 0) {
            onHide();
            const warningMessage = selectedTanks.length === 1
                ? (targetStatus ? "Selected tank already Active" : "Selected tank already InActive")
                : (targetStatus ? "Selected tank(s) already Active" : "Selected tank(s) already InActive");

            popup.info({
                title: "Warning",
                message: warningMessage
            });
            return;
        }

        setIsSaving(true);
        let saveSucceeded = false;
        try {
            const payload: any = {
                data: idsToChange,
                status: targetStatus,
                IMO: imo || ""
            };
            if (projectId != null && projectId !== "") {
                payload.ProjectId = projectId;
            }

            await updateTankStatus(payload);
            saveSucceeded = true;

            onHide();

            const targetLabel = targetStatus ? "Active" : "InActive";
            const skippedCount = selectedTanks.length - idsToChange.length;
            let successMessage: string;

            if (skippedCount > 0) {
                successMessage = `${idsToChange.length} tank(s) changed to ${targetLabel}. ${skippedCount} tank(s) already ${targetLabel} were left unchanged.`;
            } else if (idsToChange.length === 1) {
                successMessage = "Tank status changed";
            } else {
                successMessage = "Tank statuses changed";
            }

            popup.success({
                title: "Success",
                message: successMessage
            });
        } catch (e: any) {
            if (!saveSucceeded) {
                const rawError = e?.response?.data?.message ?? e?.response?.data ?? e?.message;
                const friendly = toFriendlyErrorMessage(rawError, "Something went wrong");
                popup.error({
                    title: "Error",
                    message: friendly
                });
            }
        } finally {
            setIsSaving(false);
        }

        if (saveSucceeded) {
            try {
                await onDone();
            } catch (refreshErr) {
                console.error("Failed to refresh tank list after status update:", refreshErr);
            }
        }
    };

    return (
        <Modal
            show={show}
            dialogClassName="app-popup"
            centered
            onHide={() => {
                if (!isSaving) {
                    onHide();
                }
            }}
            animation={false}
            backdrop="static"
        >
            <Modal.Header closeButton={!isSaving}>
                <Modal.Title>Confirm</Modal.Title>
            </Modal.Header>
            <Modal.Body>
                <div className="row">
                    <div className="col-md-12">
                        <div className="d-flex justify-content-center align-items-center flex-column">
                            <h5 className="_600 tx-16 text-center mb-2">Are you sure?</h5>
                            <p className="_500 tx-14 text-center mb-3">
                                You want to change {selectedTanks.length} tanks Status?
                            </p>
                            <div className="d-flex justify-content-center flex-row gap-3">
                                <RadioInput
                                    label="Active"
                                    checked={targetStatus === true}
                                    onChange={() => setTargetStatus(true)}
                                />
                                <RadioInput
                                    label="Inactive"
                                    checked={targetStatus === false}
                                    onChange={() => setTargetStatus(false)}
                                />
                            </div>
                        </div>
                    </div>
                </div>
            </Modal.Body>
            <Modal.Footer className="gap-2">
                <Button variant={BUTTONS.SECONDARY} onClick={onHide} disabled={isSaving}>
                    No, Cancel
                </Button>
                <Button variant={BUTTONS.PRIMARY} onClick={handleProceed} disabled={isSaving}>
                    {isSaving ? "Updating..." : "Yes, Proceed"}
                </Button>
            </Modal.Footer>
        </Modal>
    );
};

export default TankStatusChangeModal;
