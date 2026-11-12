import React from "react";
import { LoadingIcon } from "customer_portal-ui-shared";

export const DEFAULT_LOADING_MESSAGE = "Loading Data . . .";

interface AppLoaderProps {
    message?: string;
    inline?: boolean;
}

const AppLoader: React.FC<AppLoaderProps> = ({
    message = DEFAULT_LOADING_MESSAGE,
    inline = false,
}) => (
    <div className={inline ? "app-loader-inline" : "app-loader"} role="status" aria-live="polite">
        <LoadingIcon loaderMessage={message} />
    </div>
);

export default AppLoader;
