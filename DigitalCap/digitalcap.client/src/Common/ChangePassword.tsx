import React, { useEffect, useState } from "react";
import { Offcanvas, Spinner } from "react-bootstrap";
import { Button, BUTTONS, Input, Label } from "customer_portal-ui-shared";
import * as loginService from "../service/LoginService";
import { usePopup } from "./PopupProvider";

interface ChangePasswordProps {
    show: boolean;
    onHide: () => void;
    userEmail?: string;
}

type FormData = {
    oldPassword: string;
    newPassword: string;
    confirmPassword: string;
};

type FormErrors = Partial<Record<keyof FormData, string>>;

const initialFormData: FormData = {
    oldPassword: "",
    newPassword: "",
    confirmPassword: "",
};

const ChangePassword: React.FC<ChangePasswordProps> = ({ show, onHide, userEmail = "" }) => {
    const popup = usePopup();
    const [formData, setFormData] = useState<FormData>(initialFormData);
    const [formErrors, setFormErrors] = useState<FormErrors>({});
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState("");

    useEffect(() => {
        if (!show) {
            setFormData(initialFormData);
            setFormErrors({});
            setSubmitError("");
            setIsSubmitting(false);
        }
    }, [show]);

    const validateForm = (): boolean => {
        const errors: FormErrors = {};

        if (!formData.oldPassword.trim()) {
            errors.oldPassword = "Current password is required.";
        }

        if (!formData.newPassword.trim()) {
            errors.newPassword = "New password is required.";
        } else if (formData.newPassword.length < 6) {
            errors.newPassword = "New password must be at least 6 characters.";
        }

        if (!formData.confirmPassword.trim()) {
            errors.confirmPassword = "Confirm new password is required.";
        } else if (formData.newPassword !== formData.confirmPassword) {
            errors.confirmPassword = "The new password and confirmation password do not match.";
        }

        setFormErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const handleFieldChange = (field: keyof FormData, value: string) => {
        setFormData((prev) => ({ ...prev, [field]: value }));
        setSubmitError("");

        if (formErrors[field]) {
            setFormErrors((prev) => {
                const next = { ...prev };
                delete next[field];
                return next;
            });
        }
    };

    const handleSubmit = async () => {
        if (!validateForm()) {
            return;
        }

        setIsSubmitting(true);
        setSubmitError("");

        try {
            const response = await loginService.changePassword({
                oldPassword: formData.oldPassword,
                newPassword: formData.newPassword,
                confirmPassword: formData.confirmPassword,
            });

            const msg = response.data?.message || "Password changed successfully.";
            setFormData(initialFormData);
            onHide();
            popup.success(msg, "Change Password");
        } catch (error: any) {
            const responseData = error?.response?.data;
            let errMsg = "Unable to change password. Please try again.";

            if (responseData?.errors?.length) {
                errMsg = responseData.errors.join(" ");
            } else if (typeof responseData === "string") {
                errMsg = responseData;
            } else if (responseData?.message) {
                errMsg = responseData.message;
            }
            setSubmitError(errMsg);
            popup.error(errMsg, "Change Password");
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <Offcanvas show={show} placement="end" onHide={onHide} backdrop="static">
            <Offcanvas.Header closeButton className="border-0 pb-0">
                <div className="w-100 text-center">
                    <Offcanvas.Title className="w-100">Change Password</Offcanvas.Title>
                    {userEmail && <p className="text-muted mb-0 mt-2">{userEmail}</p>}
                </div>
            </Offcanvas.Header>

            <Offcanvas.Body>
                <div className="row">
                    <div className="col-md-12 form-group">
                        <Label name="Current Password" bold />
                        <Input
                            label=""
                            placeholder="Enter current password"
                            type="password"
                            value={formData.oldPassword}
                            onChange={(value: string) => handleFieldChange("oldPassword", value)}
                            disabled={isSubmitting}
                            autoComplete="current-password"
                            maxLength={100}
                        />
                        {formErrors.oldPassword && (
                            <div className="text-danger small mt-1">{formErrors.oldPassword}</div>
                        )}
                    </div>

                    <div className="col-md-6 form-group">
                        <Label name="New Password" bold />
                        <Input
                            label=""
                            placeholder="Enter new password"
                            type="password"
                            value={formData.newPassword}
                            onChange={(value: string) => handleFieldChange("newPassword", value)}
                            disabled={isSubmitting}
                            autoComplete="new-password"
                            maxLength={100}
                        />
                        {formErrors.newPassword && (
                            <div className="text-danger small mt-1">{formErrors.newPassword}</div>
                        )}
                    </div>

                    <div className="col-md-6 form-group">
                        <Label name="Confirm New Password" bold />
                        <Input
                            label=""
                            placeholder="Confirm new password"
                            type="password"
                            value={formData.confirmPassword}
                            onChange={(value: string) => handleFieldChange("confirmPassword", value)}
                            disabled={isSubmitting}
                            autoComplete="new-password"
                            maxLength={100}
                        />
                        {formErrors.confirmPassword && (
                            <div className="text-danger small mt-1">{formErrors.confirmPassword}</div>
                        )}
                    </div>

                    {submitError && (
                        <div className="col-md-12">
                            <div className="text-danger small">{submitError}</div>
                        </div>
                    )}

                    <div className="col-md-12 form-group mt-3">
                        <div className="d-flex justify-content-end gap-2">
                            <Button variant={BUTTONS.SECONDARY} onClick={onHide} disabled={isSubmitting}>
                                Cancel
                            </Button>
                            <Button variant={BUTTONS.PRIMARY} onClick={handleSubmit} disabled={isSubmitting}>
                                {isSubmitting ? (
                                    <>
                                        <Spinner animation="border" size="sm" className="me-2" />
                                        Updating...
                                    </>
                                ) : (
                                    "Update password"
                                )}
                            </Button>
                        </div>
                    </div>
                </div>
            </Offcanvas.Body>
        </Offcanvas>
    );
};

export default ChangePassword;
