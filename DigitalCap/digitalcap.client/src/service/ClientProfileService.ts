import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";
import type { DataSourceRequest } from "../Interface/Grading";

export function getClientProfiles(request?: DataSourceRequest): Promise<AxiosResponse> {
    const routePath = `/api/ABSAdmin/GetClientProfiles`;

    const params: any = {};

    if (request) {
        if (request.sort) {
            params.sort = JSON.stringify(request.sort);
        }
        if (request.filter) {
            params.filter = JSON.stringify(request.filter);
        }
        if (request.group) {
            params.group = JSON.stringify(request.group);
        }
    }

    return axiosInstance.get(routePath, {
        params: Object.keys(params).length > 0 ? params : undefined,
        paramsSerializer: (params) => {
            const queryParts: string[] = [];
            for (const key in params) {
                if (params[key] !== null && params[key] !== undefined && params[key] !== '') {
                    queryParts.push(`${key}=${encodeURIComponent(params[key])}`);
                }
            }
            return queryParts.join('&');
        }
    });
}

export function getCreateClientProfile(): Promise<AxiosResponse> {
    const routePath = `/api/ABSAdmin/GetCreateClientProfile`;
    return axiosInstance.get(routePath);
}


export function createClientProfile(formData: {
    Id?: string;
    wcn: string;
    companyName: string;
    companyAddress: string;
    companyBillingEmail: string;
    companyBillingSystemURL: string;

}): Promise<AxiosResponse> {

    const routePath = `/api/ABSAdmin/CreateClientProfile`;
    return axiosInstance.post(routePath, formData);
}


export function getTimeZone(clientTz: string): Promise<AxiosResponse> {
    const routePath = `/api/ABSAdmin/GetTimeZone`;
    return axiosInstance.get(routePath, {
        params: { clientTz }
    });
}

export function getClientProfileById(clientId: string): Promise<AxiosResponse> {
    const routePath = `/api/ABSAdmin/GetClientProfile`;
    return axiosInstance.get(routePath, {
        params: { clientId }
    });
}

export function saveClientProfile(model: any, freshmeat: boolean = false): Promise<AxiosResponse> {
    const routePath = `/api/ABSAdmin/SaveClientProfile`;
    return axiosInstance.post(routePath, model, {
        params: { freshmeat }
    });
}

export function editClientProfile(formData: {
    id: string;
    wcn: string;
    companyName: string;
    companyAddress: string;
    companyBillingEmail?: string;
    companyBillingSystemURL?: string;
}): Promise<AxiosResponse> {
    const routePath = `/api/ABSAdmin/EditClientProfile`;
    return axiosInstance.put(routePath, formData);
}

export function deleteClientProfile(clientId: string): Promise<AxiosResponse> {
    const routePath = `/api/ABSAdmin/DeleteClientProfile`;
    return axiosInstance.post(routePath, {
        Id: clientId
    });
}

export function getClientProfileWithDetails(
    clientId: string,
    freshmeat: boolean = false
): Promise<AxiosResponse> {
    var routePath = `/api/AbsAdmin/GetClientProfileWithDetails`;
    return axiosInstance.get(routePath, {
        params: {
            clientId,
            freshmeat
        }
    });
}