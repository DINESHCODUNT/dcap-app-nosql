import { PublicClientApplication, type Configuration, type IPublicClientApplication } from "@azure/msal-browser";
import axios, { type AxiosResponse } from "axios";
interface AppConfig {
  azureAdConfig: Configuration;
  azureAdMsalConfig?: Configuration;
}

interface ConfigModule {
  default: AppConfig;
}

const loadConfig = async (): Promise<AppConfig> => {
  try {
    const environment: string = import.meta.env.VITE_APP_ENV || import.meta.env.REACT_APP_ENV || "local";

    if (environment.toLowerCase() === "local") {
      const configModule = await import(`./config.local.json`) as ConfigModule;
      return configModule.default;
    } else {
      const proxyUrl: string = import.meta.env.VITE_APP_PROXY_URL;
      const response: AxiosResponse<AppConfig> = await axios.get(
        `${proxyUrl}/api/Auth/GetAuthConfig`
      );
      return response.data;
    }
  } catch (error) {
    throw error;
  }
};

let msalInstance: IPublicClientApplication | null = null;

const initializeMsal = async (): Promise<IPublicClientApplication> => {
  if (msalInstance) {
    return msalInstance;
  }

  try {
    let currentPath = window.location.pathname;
    if (
      !localStorage.getItem("authConfig") ||
      currentPath === "/" ||
      currentPath.toLowerCase().includes("/internal")
    ) {
      sessionStorage.clear();
      const cookies = document.cookie.split(";");
      cookies.forEach((cookie) => {
        const name = cookie.split("=")[0].trim();
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 UTC;path=/;`;
      });

      const authTypeValue = currentPath.toLowerCase().includes("/internal") ? "internal" : "external";
      localStorage.setItem("authConfig", authTypeValue);
    }

    const AppConfig: AppConfig = await loadConfig();
    const msalConfig: Configuration = AppConfig.azureAdConfig;

    msalInstance = new PublicClientApplication(msalConfig);
    await msalInstance.initialize();
  } catch (error) {
    throw error;
  }

  return msalInstance;
};

export default initializeMsal;