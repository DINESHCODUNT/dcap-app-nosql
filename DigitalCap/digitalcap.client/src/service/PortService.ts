import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";

export function searchPort(text?: string, filter?: { logic?: string }): Promise<AxiosResponse> {
    const routePath = `/api/Port/Search`;

    const params: any = {};

    if (text !== undefined) {
        params.text = text;
    }

    if (filter && filter.logic) {
        params['filter[logic]'] = filter.logic;
    }

    return axiosInstance.get(routePath, {
        params: Object.keys(params).length > 0 ? params : undefined
    });
}
