import React, { useEffect, useState } from "react";
import { InteractionStatus } from "@azure/msal-browser";
import { useMsal } from "@azure/msal-react";
import { useNavigate } from "react-router-dom";
import { getLoginScopes } from "../utils/authConfig";
import AppLoader from "../Common/AppLoader";

const DOMAIN_HINT = import.meta.env.VITE_APP_DOMAIN_HINT;
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export const AuthRedirectWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const { instance, inProgress } = useMsal();
    const navigate = useNavigate();
    const [msalInitialized, setMsalInitialized] = useState(false);

    useEffect(() => {
        // Clean up any stale postLoginRedirect stored in localStorage
        localStorage.removeItem("postLoginRedirect");
    }, []);

    useEffect(() => {
        const handleRedirect = async () => {
            try {
                const redirectResult = await instance.handleRedirectPromise();
                if (!redirectResult) {
                    const accounts = instance.getAllAccounts();
                    const isInternalRoute = window.location.pathname.toLowerCase().includes("/internal");

                    if (isInternalRoute) {
                        if (accounts.length === 0) {
                            if (inProgress === InteractionStatus.None) {
                                const searchParams = new URLSearchParams(window.location.search);
                                const rawHint = (searchParams.get("loginHint") || "").trim();
                                const loginHint = EMAIL_REGEX.test(rawHint) ? rawHint : undefined;

                                await instance.loginRedirect({
                                    scopes: getLoginScopes(),
                                    domainHint: DOMAIN_HINT,
                                    ...(loginHint ? { loginHint } : {})
                                });
                            }
                            return;
                        } else {
                            navigate("/app/Project", { replace: true });
                            setMsalInitialized(true);
                            return;
                        }
                    }
                }
                setMsalInitialized(true);
            } catch (error: any) {
            }
        };

        handleRedirect();
    }, [instance, inProgress, navigate]);

    if (!msalInitialized) {
        return <AppLoader />;
    }

    return <>{children}</>;
};
