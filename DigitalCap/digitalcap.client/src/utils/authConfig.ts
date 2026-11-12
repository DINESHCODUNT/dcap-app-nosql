import axios, { type AxiosResponse } from "axios";

interface AppConfig {
  azureAdConfig: {
    scopes?: string[];
    auth?: {
      clientId?: string;
    };
  };
}

/**
 * Loads Azure AD configuration and returns the API scopes
 * This ensures all token acquisitions use the correct scope for the API
 */
export const getApiScopes = async (): Promise<string[]> => {
  try {
    const environment: string = import.meta.env.VITE_APP_ENV || import.meta.env.REACT_APP_ENV || "local";

    if (environment.toLowerCase() === "local") {
      const configModule = await import(`../config.local.json`);
      const config: AppConfig = configModule.default;
      return config.azureAdConfig.scopes || ["d8a3a596-6fcc-4047-ac76-d990b30f1bb0/.default"];
    } else {
      const proxyUrl: string = import.meta.env.VITE_APP_PROXY_URL;
      const response: AxiosResponse<AppConfig> = await axios.get(
        `${proxyUrl}/api/Auth/GetAuthConfig`
      );
      return response.data.azureAdConfig.scopes || ["d8a3a596-6fcc-4047-ac76-d990b30f1bb0/.default"];
    }
  } catch (error) {
    return ["d8a3a596-6fcc-4047-ac76-d990b30f1bb0/.default"];
  }
};

export const getLoginScopes = (): string[] => {
  return ["openid", "profile", "email"];
};
