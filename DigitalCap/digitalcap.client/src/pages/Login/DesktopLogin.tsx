import React, { useEffect, useState } from "react";
import { useMsal } from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { getLoginScopes } from "../../utils/authConfig";
import { DESKTOP_URL, getFreshToken } from "../../utils/desktop";
import AppLoader from "../../Common/AppLoader";

const DOMAIN_HINT = import.meta.env.VITE_APP_DOMAIN_HINT;

const DesktopLogin: React.FC = () => {
    const { instance, inProgress } = useMsal();
    const [redirecting, setRedirecting] = useState(false);

    useEffect(() => {
        const processDesktopLogin = async () => {
            if (inProgress !== InteractionStatus.None || redirecting) {
                return;
            }

            const accounts = instance.getAllAccounts();
            if (accounts.length === 0) {
                sessionStorage.setItem("desktopLoginReturn", window.location.pathname + window.location.search);
                await instance.loginRedirect({
                    scopes: getLoginScopes(),
                    domainHint: DOMAIN_HINT,
                });
                return;
            }

            setRedirecting(true);
            const token = await getFreshToken();

            const queryParams = new URLSearchParams(window.location.search);
            const redirectUrlParam = queryParams.get("redirectUrl");
            const redirectURL =
                redirectUrlParam && redirectUrlParam.startsWith("/") && !redirectUrlParam.startsWith("//")
                    ? redirectUrlParam
                    : "/app/Project";

            window.location.replace(
                `${DESKTOP_URL}/SetToken#token=${encodeURIComponent(token)}&redirectURL=${encodeURIComponent(redirectURL)}`
            );
        };

        processDesktopLogin();
    }, [instance, inProgress, redirecting]);

    return <AppLoader />;
};

export default DesktopLogin;
