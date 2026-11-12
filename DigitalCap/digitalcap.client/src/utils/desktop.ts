import initializeMsal from "../msalInstance";

export const DESKTOP_URL = "http://localhost:55562";

export const API_SCOPES = ["d8a3a596-6fcc-4047-ac76-d990b30f1bb0/.default"];

export const getFreshToken = async (): Promise<string> => {
    try {
        const msalInstance = await initializeMsal();
        const accounts = msalInstance.getAllAccounts();
        if (accounts.length > 0) {
            const response = await msalInstance.acquireTokenSilent({
                account: accounts[0],
                scopes: API_SCOPES,
            });
            if (response.accessToken) {
                localStorage.setItem("token", response.accessToken);
                return response.accessToken;
            }
        }
    } catch {
        // Fallback to localStorage token if acquireTokenSilent fails
    }
    return localStorage.getItem("token") || "";
};
