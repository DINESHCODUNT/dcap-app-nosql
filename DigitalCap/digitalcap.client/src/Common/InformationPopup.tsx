import React from 'react';
import Modal from 'react-bootstrap/Modal';
import Spinner from 'react-bootstrap/Spinner';
import { Button, BUTTONS } from 'customer_portal-ui-shared';
import * as Icon from 'react-bootstrap-icons';
import { GetTitle, GetPopupMessage, Buttons } from './UtilityMessage';

export interface InformationPopupProps {
    show?: boolean;
    onShow?: boolean;
    onClose: () => void;
    title?: string;
    message?: string | React.ReactNode;
    variant?: 'success' | 'error' | 'info';
    okText?: string;

    // Backward compatibility props:
    pageName?: string;
    name?: string;
    status?: string;
    applicationName?: string;
    skippedExaminationNames?: string;
    loading?: boolean;
    onDownload?: () => void;
    Continue?: () => void;
    okClose?: () => void;
    nextPage?: boolean;
}

export default function InformationPopup({
    show,
    onShow,
    onClose,
    title,
    message,
    variant,
    okText = "Ok",
    pageName,
    status,
    loading,
    onDownload,
    Continue,
    okClose,
    nextPage
}: InformationPopupProps) {
    const isVisible = show ?? onShow ?? false;
    const resolvedTitle = title || GetTitle(pageName) || "Information";
    const resolvedMessage = message ?? (pageName ? GetPopupMessage(pageName) : "");

    const handleClose = () => {
        onClose();
    };

    const renderIcon = () => {
        if (variant === 'error') {
            return <Icon.ExclamationCircleFill className="text-danger flex-shrink-0" size={18} />;
        }
        if (variant === 'info') {
            return <Icon.InfoCircleFill className="text-primary flex-shrink-0" size={18} />;
        }
        return null;
    };

    return (
        <Modal
            show={isVisible}
            onHide={handleClose}
            centered
            backdrop="static"
            dialogClassName="app-popup"
            className="information-popup-modal"
            backdropClassName="information-popup-backdrop"
        >
            <Modal.Header closeButton>
                <Modal.Title>
                    {renderIcon()}
                    <span>{resolvedTitle}</span>
                </Modal.Title>
            </Modal.Header>
            <Modal.Body>
                {typeof resolvedMessage === 'string' ? (
                    <p className="app-popup-message">{resolvedMessage}</p>
                ) : (
                    resolvedMessage
                )}
            </Modal.Body>
            <Modal.Footer>
                {pageName === 'FileSizeExceeded' || pageName === 'FileType' ? (
                    <>
                        {loading ? (
                            <Button variant={BUTTONS.PRIMARY} disabled>
                                <Spinner size="sm" animation="border" />
                            </Button>
                        ) : (
                            onDownload && (
                                <Button onClick={onDownload} variant={BUTTONS.PRIMARY}>
                                    Download File
                                </Button>
                            )
                        )}
                        <Button onClick={handleClose} variant={BUTTONS.SECONDARY}>
                            Cancel
                        </Button>
                    </>
                ) : (
                    <>
                        {nextPage && Continue && (
                            <Button onClick={Continue}>{Buttons.Continue}</Button>
                        )}
                        {status ? (
                            <Button variant={BUTTONS.PRIMARY} onClick={okClose}>{Buttons.ok}</Button>
                        ) : (
                            <Button variant={BUTTONS.PRIMARY} onClick={handleClose}>{okText || Buttons.ok}</Button>
                        )}
                    </>
                )}
            </Modal.Footer>
        </Modal>
    );
}