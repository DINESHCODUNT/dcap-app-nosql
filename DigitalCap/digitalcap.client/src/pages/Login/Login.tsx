import React, { useState, useEffect, useRef } from "react";
import { useMsal } from "@azure/msal-react";
import { useNavigate } from "react-router-dom";
import ABSLogo from "../../assets/images/ABSLogo.svg";
import * as accountService from "../../service/LoginService";
import { Button, BUTTONS, Input, Label } from 'customer_portal-ui-shared';
import { getApiScopes, getLoginScopes } from "../../utils/authConfig";

const loginRequest = {
    scopes: getLoginScopes(),
};

const Login: React.FC = () => {
    const { instance } = useMsal();
    const navigate = useNavigate();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [rememberMe, setRememberMe] = useState(false);
    const [showPasswordScreen, setShowPasswordScreen] = useState(false);
    const [loading, setLoading] = useState(false);
    const [emailError, setEmailError] = useState("");
    const [passwordError, setPasswordError] = useState("");
    const [responseMsg, setResponseMsg] = useState("");
    const hasProcessedRedirect = useRef(false);

    useEffect(() => {
        if (hasProcessedRedirect.current) return;

        const hasRedirectParams = window.location.hash || window.location.search.includes("code=");
        if (!hasRedirectParams) return;

        hasProcessedRedirect.current = true;

        const handleRedirect = async () => {
            try {
                const response = await instance.handleRedirectPromise();
                if (response && response.account) {
                    try {
                        const apiScopes = await getApiScopes();
                        const tokenResponse = await instance.acquireTokenSilent({
                            scopes: apiScopes,
                            account: response.account,
                        });
                        if (tokenResponse.accessToken) {
                            localStorage.setItem("token", tokenResponse.accessToken);
                            localStorage.setItem("user", JSON.stringify(response.account));
                            navigate("/app/Project", { replace: true });
                        }
                    } catch (tokenError: any) {
                        setResponseMsg("Failed to get access token. Please try again.");
                    }
                }
            } catch (error: any) {
                if (error.errorCode && error.errorCode !== "interaction_in_progress") {
                }
            }
        };

        handleRedirect();
    }, [instance, navigate]);

    const isCorpUser = (emailVal: string): boolean => {
        return emailVal.toLowerCase().endsWith("@eagle.org");
    };

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

    const handleNext = async () => {
        if (!validateEmail(email)) return;

        if (isCorpUser(email)) {
            try {
                await instance.loginRedirect({
                    ...loginRequest,
                    loginHint: email,
                });
            } catch (error: any) {
                setResponseMsg(`Failed to redirect: ${error.message || error.errorCode || "Unknown error"}`);
            }
        } else {
            setShowPasswordScreen(true);
            setResponseMsg("");
        }
    };

    const handleLogin = async () => {
        if (!validateEmail(email)) return;

        if (isCorpUser(email)) {
            return handleNext();
        }

        if (!password) {
            setPasswordError("Please enter password");
            return;
        }
        setPasswordError("");
        setLoading(true);
        try {
            const response = await accountService.login({
                email,
                password,
                rememberMe,
            });

            if (!response.data.isSuccess) {
                setResponseMsg(response.data.errorMessage || "Invalid login credentials");
                setShowPasswordScreen(false);
                setPassword("");
                setPasswordError("");
                return;
            }

            if (response.data.token) {
                localStorage.setItem("token", response.data.token);
            }

            if (response.data.user) {
                localStorage.setItem("user", JSON.stringify(response.data.user));
            }

            navigate("/app/Project", { replace: true });
        } catch (err: any) {
            const errorMessage =
                err?.response?.data?.message ||
                err?.response?.data?.error ||
                "Invalid login credentials";
            setResponseMsg(errorMessage);
        } finally {
            setLoading(false);
        }
    };

    const handleRequestAccess = async () => {
        if (!validateEmail(email)) return;

        setLoading(true);
        try {
            const response = await accountService.requestAccess({ email: email.trim() });
            if (response.status === 200 || response.status === 201) {
                setResponseMsg("An e-mail requesting access was sent successfully.");
                setEmail("");
                setEmailError("");
            } else {
                setResponseMsg("Request sent, but received unexpected response.");
            }
        } catch (err: any) {
            const errorMessage =
                err?.response?.data?.message ||
                err?.response?.data?.error ||
                "Failed to send request";
            setResponseMsg(errorMessage);
        } finally {
            setLoading(false);
        }
    };

    const handleBack = () => {
        setShowPasswordScreen(false);
        setPassword("");
        setPasswordError("");
        setResponseMsg("");
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
                                    <h3 className="login-title">Condition Assessment Program (CAP)</h3>
                                </div>

                                <div className="login-form">

                                    {/* ── Email Screen ── */}
                                    {!showPasswordScreen && (
                                        <form onSubmit={(e) => { e.preventDefault(); handleNext(); }}>
                                            <div className="form-group">
                                                <Label name="Email Address" bold />
                                                <div>
                                                    <Input
                                                        placeholder="Email"
                                                        value={email}
                                                        onChange={(value: any) => {

                                                            const val = typeof value === "string"
                                                                ? value
                                                                : (value?.target?.value ?? "");
                                                            setEmail(val);
                                                            if (emailError) validateEmail(val);
                                                        }}
                                                        disabled={loading}
                                                        autoComplete="email"
                                                        maxLength={100}
                                                    />
                                                </div>
                                                {emailError && (
                                                    <label className="text-danger small">{emailError}</label>
                                                )}
                                            </div>

                                            {responseMsg && (
                                                <div className="alert alert-info mt-2">{responseMsg}</div>
                                            )}

                                            <div className="login-actions d-flex justify-content-between mt-30">
                                                <Button
                                                    variant={BUTTONS.SECONDARY}
                                                    type="button"
                                                    onClick={handleRequestAccess}
                                                    disabled={loading}
                                                >
                                                    Request Access
                                                </Button>
                                                <Button
                                                    variant={BUTTONS.PRIMARY}
                                                    type="submit"
                                                    onClick={(e: React.MouseEvent) => { e.preventDefault(); handleNext(); }}
                                                    disabled={loading}
                                                >
                                                    Next
                                                </Button>
                                            </div>
                                        </form>
                                    )}

                                    {/* ── Password Screen ── */}
                                    {showPasswordScreen && (
                                        <form onSubmit={(e) => { e.preventDefault(); handleLogin(); }}>
                                            <div className="form-group mb-2">
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
                                                            if (emailError) validateEmail(val);
                                                        }}
                                                        disabled={loading}
                                                        autoComplete="email"
                                                        maxLength={100}
                                                    />
                                                </div>
                                                {emailError && (
                                                    <label className="text-danger small">{emailError}</label>
                                                )}
                                            </div>

                                            <div className="form-group mt-3">
                                                <Label name="Password" bold />
                                                <div>
                                                    <Input
                                                        placeholder="Password"
                                                        type="password"
                                                        value={password}
                                                        onChange={(value: any) => {
                                                            const val = typeof value === "string"
                                                                ? value
                                                                : (value?.target?.value ?? "");
                                                            setPassword(val);
                                                            if (passwordError) setPasswordError("");
                                                        }}
                                                        disabled={loading}
                                                        autoComplete="current-password"
                                                        maxLength={100}
                                                    />
                                                </div>
                                                {passwordError && (
                                                    <label className="text-danger small">{passwordError}</label>
                                                )}
                                            </div>

                                            {/* Remember Me & Forgot Password */}
                                            <div className="d-flex justify-content-between align-items-center mt-2 mb-3">
                                                <div className="form-check">
                                                    <input
                                                        className="form-check-input"
                                                        type="checkbox"
                                                        id="rememberMe"
                                                        checked={rememberMe}
                                                        onChange={(e) => setRememberMe(e.target.checked)}
                                                        disabled={loading}
                                                    />
                                                    <label className="form-check-label" htmlFor="rememberMe">
                                                        Remember me
                                                    </label>
                                                </div>
                                                <a
                                                    href="#"
                                                    onClick={(e) => {
                                                        e.preventDefault();
                                                        navigate("/forgot-password");
                                                    }}
                                                    className="text-primary text-decoration-none small"
                                                >
                                                    Forgot your password?
                                                </a>
                                            </div>

                                            {responseMsg && (
                                                <div className="alert alert-danger mt-2">{responseMsg}</div>
                                            )}

                                            <div className="login-actions d-flex justify-content-between mt-30">
                                                <Button
                                                    variant={BUTTONS.SECONDARY}
                                                    type="button"
                                                    onClick={handleBack}
                                                    disabled={loading}
                                                >
                                                    Back
                                                </Button>
                                                <Button
                                                    variant={BUTTONS.PRIMARY}
                                                    type="submit"
                                                    onClick={(e: React.MouseEvent) => { e.preventDefault(); handleLogin(); }}
                                                    disabled={loading}
                                                >
                                                    {loading ? "Logging in..." : "Log In"}
                                                </Button>
                                            </div>
                                        </form>
                                    )}

                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Login;
