import React, { useEffect, useState } from "react";
import { useMsal } from "@azure/msal-react";
import { useNavigate } from "react-router-dom";
import { getApiScopes } from "../src/utils/authConfig";
import AppLoader from "./Common/AppLoader";

const Success: React.FC = () => {
    const { instance } = useMsal();
    const navigate = useNavigate();
    const [status, setStatus] = useState<"loading" | "error">("loading");
    const [errorMsg, setErrorMsg] = useState("");

    useEffect(() => {
        let cancelled = false;

        const run = async () => {
            try {
                const scopes = await getApiScopes();
                const accounts = instance.getAllAccounts();

                if (!accounts.length) {
                    if (!cancelled) {
                        setErrorMsg("Sign-in failed: no account found. Please try again.");
                        setStatus("error");
                        setTimeout(() => navigate("/", { replace: true }), 2500);
                    }
                    return;
                }

                const tokenRequest = {
                    account: accounts[0],
                    scopes,
                    redirectUri: `${window.location.origin}/success`,
                };

                let accessToken: string | null = null;

                try {
                    const response = await instance.acquireTokenSilent(tokenRequest);
                    accessToken = response.accessToken || null;
                } catch {
                    try {
                        const response = await instance.acquireTokenPopup(tokenRequest);
                        accessToken = response.accessToken || null;
                    } catch (popupErr) {
                    }
                }

                if (cancelled) return;

                if (accessToken) {
                    localStorage.setItem("token", accessToken);
                    localStorage.removeItem("postLoginRedirect");
                    navigate("/app/Project", { replace: true });
                } else {
                    setErrorMsg("Could not obtain an access token. Please sign in again.");
                    setStatus("error");
                    setTimeout(() => navigate("/", { replace: true }), 2500);
                }
            } catch (err) {
                if (!cancelled) {
                    setErrorMsg("An error occurred. Redirecting to login...");
                    setStatus("error");
                    setTimeout(() => navigate("/", { replace: true }), 2500);
                }
            }
        };

        run();
        return () => { cancelled = true; };
    }, [instance, navigate]);

    if (status === "error") {
        return (
            <div style={{ display: "flex", flexDirection: "column", justifyContent: "center", alignItems: "center", height: "100vh", gap: 12 }}>
                <p style={{ color: "#c0392b", fontSize: 15 }}>{errorMsg}</p>
                <p style={{ color: "#777", fontSize: 13 }}>Redirecting to login...</p>
            </div>
        );
    }

    return <AppLoader />;
};

export default Success;
