import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import ABSLogo from "../../assets/images/ABSLogo.svg";
import * as accountService from "../../service/LoginService";
import { Button, BUTTONS, Input, Label } from 'customer_portal-ui-shared';
import { toFriendlyErrorMessage } from "../../utils/errorMessage";

const ForgotPassword: React.FC = () => {
    const navigate = useNavigate();
    const [email, setEmail] = useState("");
    const [emailError, setEmailError] = useState("");
    const [responseMsg, setResponseMsg] = useState("");
    const [loading, setLoading] = useState(false);
    const [success, setSuccess] = useState(false);

    const validateEmail = (value: string): boolean => {
        if (!value.trim()) {
            setEmailError("Please enter email");
            return false;
        }
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value.trim())) {
            setEmailError("Please enter a valid email address");
            return false;
        }
        setEmailError("");
        return true;
    };

    const handleSubmit = async () => {
        if (!validateEmail(email)) return;

        setLoading(true);
        try {
            const response = await accountService.forgotPassword({ email: email.trim() });
            setSuccess(true);
            setResponseMsg(toFriendlyErrorMessage(response?.data?.data || response?.data?.message, "If the email is registered, a password reset link has been sent."));
        } catch (err: any) {
            const errorMessage =
                err?.response?.data?.message ||
                err?.response?.data?.error ||
                "An error occurred while attempting to send a reset link.";
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
                                    <h3 className="login-title">Forgot Password</h3>
                                    <p className="tx-14 _500">
                                        Enter your registered email address, and we’ll send you an email with instructions to reset your password.
                                    </p>
                                </div>

                                <div className="login-form">
                                    <div className="form-group">
                                        <Label name="Email Address" bold />
                                        <div>
                                            <Input
                                                placeholder="Email"
                                                type="email"
                                                value={email}
                                                onChange={(value: any) => {
                                                    const val = typeof value === "string"
                                                        ? value
                                                        : (value?.target?.value ?? "");
                                                    setEmail(val);
                                                    if (emailError) setEmailError("");
                                                }}
                                                disabled={loading || success}
                                                maxLength={100}
                                            />
                                        </div>
                                        {emailError && (
                                            <label className="text-danger small">{emailError}</label>
                                        )}
                                    </div>

                                    {responseMsg && (
                                        <div className="mt-3">
                                            <label className={success ? "text-success fw-bold" : "text-danger fw-bold"}>
                                                {responseMsg}
                                            </label>
                                        </div>
                                    )}

                                    {!success && (
                                        <div className="form-group d-flex justify-content-between align-items-center mt-4">
                                            <Button
                                                onClick={handleSubmit}
                                                disabled={loading}
                                                btnType={BUTTONS.PRIMARY}
                                                type="button"
                                            >
                                                Submit
                                            </Button>
                                        </div>
                                    )}

                                    <div className="mt-3 text-center">
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

export default ForgotPassword;
