import React from 'react';
import Modal from 'react-bootstrap/Modal';
import Spinner from 'react-bootstrap/Spinner';
import { Button, BUTTONS } from 'customer_portal-ui-shared';

export interface ConfirmationPopupProps {
    show?: boolean;
    onShow?: boolean;
    title?: string;
    message?: string | React.ReactNode;
    confirmText?: string;
    cancelText?: string;
    danger?: boolean;
    loading?: boolean;
    onConfirm: () => void;
    onClose: () => void;
}

const ConfirmationPopup: React.FC<ConfirmationPopupProps> = ({
    show,
    onShow,
    title = "Confirm",
    message = "Are you sure you want to continue?",
    confirmText = "Yes, Proceed",
    cancelText = "Cancel",
    loading = false,
    onConfirm,
    onClose
}) => {
    const isVisible = show ?? onShow ?? false;

    return (
        <Modal
            show={isVisible}
            onHide={onClose}
            centered
            backdrop="static"
            dialogClassName="app-popup"
            className="information-popup-modal"
            backdropClassName="information-popup-backdrop"
        >
            <Modal.Header closeButton>
                <Modal.Title>
                    <span>{title}</span>
                </Modal.Title>
            </Modal.Header>
            <Modal.Body>
                {typeof message === 'string' ? (
                    <p className="app-popup-message">{message}</p>
                ) : (
                    message
                )}
            </Modal.Body>
            <Modal.Footer>
                <Button
                    variant={BUTTONS.SECONDARY}
                    onClick={onClose}
                    disabled={loading}
                >
                    {cancelText}
                </Button>
                <Button
                    variant={BUTTONS.PRIMARY}
                    onClick={onConfirm}
                    disabled={loading}
                >
                    {loading ? (
                        <>
                            <Spinner
                                as="span"
                                animation="border"
                                size="sm"
                                role="status"
                                aria-hidden="true"
                                className="me-2"
                            />
                            Please wait...
                        </>
                    ) : (
                        confirmText
                    )}
                </Button>
            </Modal.Footer>
        </Modal>
    );
};

export default ConfirmationPopup;