import React, { useState, useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import ABSLogo from "../../assets/images/ABSLogo.svg";
import * as accountService from "../../service/LoginService";
import { Button, BUTTONS, Input, Label } from 'customer_portal-ui-shared';

const ResetPassword: React.FC = () => {
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();

    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [passwordError, setPasswordError] = useState("");
    const [confirmPasswordError, setConfirmPasswordError] = useState("");

    const [responseMsg, setResponseMsg] = useState("");
    const [loading, setLoading] = useState(false);
    const [success, setSuccess] = useState(false);

    const email = searchParams.get("email") || "";
    const token = searchParams.get("token") || "";

    useEffect(() => {
        if (!email || !token) {
            setResponseMsg("Invalid password reset link. Missing email or token.");
        }
    }, [email, token]);

    const validateForm = (): boolean => {
        let isValid = true;

        if (!password) {
            setPasswordError("Please enter a new password");
            isValid = false;
        } else if (password.length < 8) {
            setPasswordError("Password must be at least 8 characters long");
            isValid = false;
        } else {
            setPasswordError("");
        }

        if (!confirmPassword) {
            setConfirmPasswordError("Please confirm your new password");
            isValid = false;
        } else if (password !== confirmPassword) {
            setConfirmPasswordError("Passwords do not match");
            isValid = false;
        } else {
            setConfirmPasswordError("");
        }

        return isValid;
    };

    const handleSubmit = async () => {
        if (!email || !token) return;
        if (!validateForm()) return;

        setLoading(true);
        try {
            await accountService.resetPassword({
                email,
                token,
                newPassword: password
            });
            setSuccess(true);
            setResponseMsg("Your password has been successfully reset. You can now log in.");
        } catch (err: any) {
            const errorMessage =
                err?.response?.data?.message ||
                err?.response?.data?.error ||
                "An error occurred while resetting your password.";
            setResponseMsg(errorMessage);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="login-wrapper">
            <div className="container">
                <div className="login-content">
                    <div className="row justify-content-center">
                        <div className="col-md-6">
                            <div className="form-block">
                                <div className="img-logo mb-25">
                                    <img
                                        src={ABSLogo}
                                        alt="DCAP Logo"
                                        className="filter-dark-blue login-logo"
                                    />
                                </div>

                                <div className="red-line"></div>

                                <div className="mt-25 mb-25">
                                    <h3 className="login-title">Reset Password</h3>
                                    <p className="mt-2 text-muted small">
                                        {success ? "Success" : email ? `Resetting password for ${email}` : "Invalid Link"}
                                    </p>
                                </div>

                                <div className="login-form">
                                    {!success && email && token && (
                                        <>
                                            <div className="form-group mb-3">
                                                <Label name="New Password" bold />
                                                <div>
                                                    <Input
                                                        placeholder="New Password"
                                                        type="password"
                                                        value={password}
                                                        onChange={(value: any) => {
                                                            const val = typeof value === "string" ? value : (value?.target?.value ?? "");
                                                            setPassword(val);
                                                            if (passwordError) setPasswordError("");
                                                        }}
                                                        disabled={loading}
                                                        maxLength={100}
                                                    />
                                                </div>
                                                {passwordError && (
                                                    <label className="text-danger small">{passwordError}</label>
                                                )}
                                            </div>

                                            <div className="form-group mb-3">
                                                <Label name="Confirm Password" bold />
                                                <div>
                                                    <Input
                                                        placeholder="Confirm Password"
                                                        type="password"
                                                        value={confirmPassword}
                                                        onChange={(value: any) => {
                                                            const val = typeof value === "string" ? value : (value?.target?.value ?? "");
                                                            setConfirmPassword(val);
                                                            if (confirmPasswordError) setConfirmPasswordError("");
                                                        }}
                                                        disabled={loading}
                                                        maxLength={100}
                                                    />
                                                </div>
                                                {confirmPasswordError && (
                                                    <label className="text-danger small">{confirmPasswordError}</label>
                                                )}
                                            </div>
                                        </>
                                    )}

                                    {responseMsg && (
                                        <div className="mt-3 mb-3">
                                            <label className={success ? "text-success fw-bold" : "text-danger fw-bold"}>
                                                {responseMsg}
                                            </label>
                                        </div>
                                    )}

                                    {!success && email && token && (
                                        <div className="form-group d-flex justify-content-between align-items-center mt-4">
                                            <Button
                                                onClick={handleSubmit}
                                                disabled={loading}
                                                btnType={BUTTONS.PRIMARY}
                                                type="button"
                                            >
                                                Reset Password
                                            </Button>
                                        </div>
                                    )}

                                    <div className="mt-4 text-center">
                                        <a
                                            href="#"
                                            onClick={(e) => {
                                                e.preventDefault();
                                                navigate("/Login");
                                            }}
                                            className="text-primary text-decoration-none"
                                        >
                                            Back to Login
                                        </a>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ResetPassword;
