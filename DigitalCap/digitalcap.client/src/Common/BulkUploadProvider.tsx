import React, { createContext, useContext, useState, useRef, useEffect, useCallback } from 'react';
import { usePopup } from './PopupProvider';
import { TransferProgressBar } from './TransferProgressBar';
import { toFriendlyErrorMessage } from '../utils/errorMessage';

export interface FailedUploadItem {
    label: string;
    error: string;
}

export interface BulkUploadJob<T> {
    rows: T[];
    uploadRow: (row: T) => Promise<void>;
    getRowLabel: (row: T) => string;
    onComplete?: () => Promise<void> | void;
}

export interface BulkUploadContextType {
    isRunning: boolean;
    total: number;
    completed: number;
    failed: FailedUploadItem[];
    startBulkUpload: <T>(job: BulkUploadJob<T>) => boolean;
}

const BulkUploadContext = createContext<BulkUploadContextType | undefined>(undefined);

export const BulkUploadProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const popup = usePopup();
    const [isRunning, setIsRunning] = useState(false);
    const [total, setTotal] = useState(0);
    const [completed, setCompleted] = useState(0);
    const [failed, setFailed] = useState<FailedUploadItem[]>([]);

    const isRunningRef = useRef(false);
    const isMountedRef = useRef(true);

    useEffect(() => {
        isMountedRef.current = true;
        return () => {
            isMountedRef.current = false;
        };
    }, []);

    useEffect(() => {
        if (!isRunning) return;

        const handleBeforeUnload = (e: BeforeUnloadEvent) => {
            e.preventDefault();
            e.returnValue = "";
            return "";
        };

        window.addEventListener("beforeunload", handleBeforeUnload);
        return () => {
            window.removeEventListener("beforeunload", handleBeforeUnload);
        };
    }, [isRunning]);

    const startBulkUpload = useCallback(<T,>(job: BulkUploadJob<T>): boolean => {
        if (isRunningRef.current) {
            return false;
        }

        isRunningRef.current = true;
        const totalCount = job.rows.length;
        setIsRunning(true);
        setTotal(totalCount);
        setCompleted(0);
        setFailed([]);

        (async () => {
            const failedItems: FailedUploadItem[] = [];
            let completedCount = 0;

            try {
                for (const row of job.rows) {
                    try {
                        await job.uploadRow(row);
                    } catch (err: any) {
                        const label = job.getRowLabel(row);
                        const error = toFriendlyErrorMessage(err?.message || err, "Upload failed");
                        failedItems.push({ label, error });
                    } finally {
                        completedCount++;
                        if (isMountedRef.current) {
                            setCompleted(completedCount);
                            setFailed([...failedItems]);
                        }
                    }
                }
            } finally {
                isRunningRef.current = false;
                if (isMountedRef.current) {
                    setIsRunning(false);
                }

                if (job.onComplete) {
                    try {
                        await job.onComplete();
                    } catch (refreshErr) {
                        console.error("Failed to refresh report after bulk upload:", refreshErr);
                    }
                }

                if (failedItems.length === 0) {
                    popup.success("File has been uploaded successfully.", "Upload File");
                } else {
                    const labels = failedItems.map((f) => f.label);
                    popup.error(
                        {
                            message: `${failedItems.length} of ${totalCount} image(s) failed to upload: ${labels.join(", ")}`
                        },
                        "Upload File"
                    );
                }
            }
        })();

        return true;
    }, [popup]);

    return (
        <BulkUploadContext.Provider
            value={{
                isRunning,
                total,
                completed,
                failed,
                startBulkUpload
            }}
        >
            {children}
            {isRunning && (
                <div className="notification-model bulk-upload-notification">
                    <div className="notification-overlay">
                        <div className="notification-body">
                            <p className="notification-title" style={{ fontWeight: 600 }}>
                                Bulk Image Upload
                            </p>
                            <div className="mb-2" role="status" aria-live="polite">
                                Uploading {Math.min(completed + 1, total)} of {total} image(s)…
                            </div>
                            <TransferProgressBar
                                percent={total ? (completed / total) * 100 : 0}
                                height="6px"
                                variant="bg-primary"
                            />
                        </div>
                    </div>
                </div>
            )}
        </BulkUploadContext.Provider>
    );
};

export const useBulkUpload = (): BulkUploadContextType => {
    const context = useContext(BulkUploadContext);
    if (!context) {
        throw new Error("useBulkUpload must be used within a BulkUploadProvider");
    }
    return context;
};
