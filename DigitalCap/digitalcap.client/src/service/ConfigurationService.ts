import type { AxiosResponse } from "axios";
import axiosInstance from "./axiosInstance";
import type { AppConfiguration } from "../Interface/AppConfiguration";

const routePath = "/api/Configuration/GetConfiguration";

export function getConfiguration(): Promise<AxiosResponse<AppConfiguration>> {
    return axiosInstance.get(routePath);
}