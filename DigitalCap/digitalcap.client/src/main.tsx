import "regenerator-runtime/runtime";
import React from "react";
import ReactDOM from "react-dom/client";
import { MsalProvider } from "@azure/msal-react";
import { AuthProvider } from "./context/AuthContext";
import { PermissionProvider } from "./context/PermissionContext";
import initializeMsal from "./msalInstance";
import App from "./App";
import { PopupProvider } from "./Common/PopupProvider";
import { BulkUploadProvider } from "./Common/BulkUploadProvider";
import { SideDialogManagerProvider, SideDialogContainer } from "customer_portal-ui-shared";
import "regenerator-runtime/runtime";
import { Provider } from "react-redux";
import { store } from "../src/store/store";

(async () => {
    try {
        const msalInstance = await initializeMsal();
        globalThis.global = globalThis;
        const container = document.getElementById("root");
        if (container) {
            ReactDOM.createRoot(container).render(
                <React.StrictMode>
                    <Provider store={store}>
                        <MsalProvider instance={msalInstance}>
                            <SideDialogManagerProvider>
                                <AuthProvider>
                                    <PermissionProvider>
                                        <PopupProvider>
                                            <BulkUploadProvider>
                                                <App />
                                                <div id="side-dialog-root">
                                                    <SideDialogContainer />
                                                </div>
                                            </BulkUploadProvider>
                                        </PopupProvider>
                                    </PermissionProvider>
                                </AuthProvider>
                            </SideDialogManagerProvider>
                        </MsalProvider>
                    </Provider>
                </React.StrictMode>
            );
        }
    } catch (error) {
        const container = document.getElementById("root");
        if (container) {
            ReactDOM.createRoot(container).render(
                <div style={{ padding: "20px", textAlign: "center" }}>
                    <h1>Failed to initialize authentication</h1>
                    <p>Please refresh the page or contact support.</p>
                </div>
            );
        }
    }
})();