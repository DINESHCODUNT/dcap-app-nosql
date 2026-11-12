import React, { createContext, useContext, useState, useCallback } from 'react';
import InformationPopup from './InformationPopup';
import ConfirmationPopup from './ConfirmationPopup';
import { GENERIC_ERROR_MESSAGE, isTechnicalErrorText, toFriendlyErrorMessage } from '../utils/errorMessage';

export interface InfoPopupOptions {
    title?: string;
    message: string | React.ReactNode;
    okText?: string;
}

export type InfoPopupInput = string | React.ReactNode | InfoPopupOptions;

export interface ConfirmPopupOptions {
    title?: string;
    message: string | React.ReactNode;
    confirmText?: string;
    confirmButtonText?: string;
    cancelText?: string;
    cancelButtonText?: string;
    danger?: boolean;
}

export interface ConfirmAsyncOptions extends ConfirmPopupOptions {
    onConfirm: () => Promise<void>;
}

export interface PopupContextType {
    success: (input: InfoPopupInput, title?: string) => Promise<void>;
    error: (input: InfoPopupInput, title?: string) => Promise<void>;
    info: (input: InfoPopupInput, title?: string) => Promise<void>;
    confirm: (options: ConfirmPopupOptions) => Promise<boolean>;
    confirmAsync: (options: ConfirmAsyncOptions) => Promise<boolean>;
}

const PopupContext = createContext<PopupContextType | undefined>(undefined);

export const PopupProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [infoState, setInfoState] = useState<{
        show: boolean;
        title?: string;
        message?: string | React.ReactNode;
        variant?: 'success' | 'error' | 'info';
        okText?: string;
        onCloseAction?: () => void;
    }>({ show: false });

    const [confirmState, setConfirmState] = useState<{
        show: boolean;
        title?: string;
        message?: string | React.ReactNode;
        confirmText?: string;
        cancelText?: string;
        danger?: boolean;
        loading?: boolean;
        onConfirmAction?: () => void;
        onCancelAction?: () => void;
    }>({ show: false });

    const normalizeInfoOptions = (input: InfoPopupInput, title?: string): InfoPopupOptions => {
        if (typeof input === 'object' && input !== null && !React.isValidElement(input) && 'message' in input) {
            return input as InfoPopupOptions;
        }
        return { message: input as (string | React.ReactNode), title };
    };

    const showInfo = useCallback(
        (variant: 'success' | 'error' | 'info', input: InfoPopupInput, title?: string): Promise<void> => {
            const options = normalizeInfoOptions(input, title);
            let msg: any = options.message;
            if (msg == null || msg === "" || (typeof msg === "object" && !React.isValidElement(msg))) {
                msg = (msg && (msg.message || msg.title || msg.detail)) || "Something went wrong. Please try again.";
            }
            if (variant !== 'success') {
                if (typeof msg === "string") msg = toFriendlyErrorMessage(msg, msg && !isTechnicalErrorText(msg) ? msg : GENERIC_ERROR_MESSAGE);
            }
            return new Promise((resolve) => {
                setInfoState({
                    show: true,
                    title: options.title || (variant === 'success' ? 'Success' : variant === 'error' ? 'Error' : 'Information'),
                    message: msg,
                    variant,
                    okText: options.okText || 'Ok',
                    onCloseAction: () => {
                        setInfoState((prev) => ({ ...prev, show: false }));
                        resolve();
                    }
                });
            });
        },
        []
    );

    const success = useCallback((input: InfoPopupInput, title?: string) => showInfo('success', input, title), [showInfo]);
    const error = useCallback((input: InfoPopupInput, title?: string) => showInfo('error', input, title), [showInfo]);
    const info = useCallback((input: InfoPopupInput, title?: string) => showInfo('info', input, title), [showInfo]);

    const confirm = useCallback((options: ConfirmPopupOptions): Promise<boolean> => {
        return new Promise((resolve) => {
            setConfirmState({
                show: true,
                title: options.title || 'Confirm',
                message: options.message,
                confirmText: options.confirmText || options.confirmButtonText || 'Yes, Proceed',
                cancelText: options.cancelText || options.cancelButtonText || 'Cancel',
                danger: options.danger,
                loading: false,
                onConfirmAction: () => {
                    setConfirmState((prev) => ({ ...prev, show: false }));
                    resolve(true);
                },
                onCancelAction: () => {
                    setConfirmState((prev) => ({ ...prev, show: false }));
                    resolve(false);
                }
            });
        });
    }, []);

    const confirmAsync = useCallback((options: ConfirmAsyncOptions): Promise<boolean> => {
        return new Promise((resolve, reject) => {
            setConfirmState({
                show: true,
                title: options.title || 'Confirm',
                message: options.message,
                confirmText: options.confirmText || options.confirmButtonText || 'Yes, Proceed',
                cancelText: options.cancelText || options.cancelButtonText || 'Cancel',
                danger: options.danger,
                loading: false,
                onConfirmAction: async () => {
                    setConfirmState((prev) => ({ ...prev, loading: true }));
                    try {
                        await options.onConfirm();
                        setConfirmState((prev) => ({ ...prev, show: false, loading: false }));
                        resolve(true);
                    } catch (err) {
                        setConfirmState((prev) => ({ ...prev, show: false, loading: false }));
                        reject(err);
                    }
                },
                onCancelAction: () => {
                    setConfirmState((prev) => ({ ...prev, show: false, loading: false }));
                    resolve(false);
                }
            });
        });
    }, []);

    const handleInfoClose = () => {
        if (infoState.onCloseAction) {
            infoState.onCloseAction();
        } else {
            setInfoState((prev) => ({ ...prev, show: false }));
        }
    };

    const handleConfirm = () => {
        if (confirmState.onConfirmAction) {
            confirmState.onConfirmAction();
        }
    };

    const handleConfirmClose = () => {
        if (confirmState.onCancelAction) {
            confirmState.onCancelAction();
        } else {
            setConfirmState((prev) => ({ ...prev, show: false }));
        }
    };

    return (
        <PopupContext.Provider value={{ success, error, info, confirm, confirmAsync }}>
            {children}
            <InformationPopup
                show={infoState.show}
                title={infoState.title}
                message={infoState.message}
                variant={infoState.variant}
                okText={infoState.okText}
                onClose={handleInfoClose}
            />
            <ConfirmationPopup
                show={confirmState.show}
                title={confirmState.title}
                message={confirmState.message}
                confirmText={confirmState.confirmText}
                cancelText={confirmState.cancelText}
                danger={confirmState.danger}
                loading={confirmState.loading}
                onConfirm={handleConfirm}
                onClose={handleConfirmClose}
            />
        </PopupContext.Provider>
    );
};

export const usePopup = (): PopupContextType => {
    const context = useContext(PopupContext);
    if (!context) {
        throw new Error('usePopup must be used within a PopupProvider');
    }
    return context;
};
