import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";

export function searchClass(filter?: string): Promise<AxiosResponse> {
    const routePath = `/api/Class/Search`;
    return axiosInstance.get(routePath, {
        params: filter ? { filter } : undefined
    });
}
