import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";
import type { DataSourceRequest } from "../Interface/Grading";
import type { ManageTankResponse, TankDto } from "../Interface/ProjectTank";

export function getTanks(request?: DataSourceRequest): Promise<AxiosResponse> {
    const routePath = `/api/Tank/FilterMenuCustomization_Read`;

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
export function GetMappedTankTypes(projectId?: number, vesselType?: string): Promise<AxiosResponse> {
    const routePath = `/api/Tank/GetMappedTankTypes`;
    return axiosInstance.get(routePath, {
        params: { projectId, vesselType }
    });
}
export function GetVesselDetails(): Promise<AxiosResponse> {
    const routePath = `/api/Tank/GetVesselDetails`;
    return axiosInstance.get(routePath, {
        params: {}
    });
}

export type TankPayload = {
    tankId?: string | null;
    tankName: string;
    subheader: string;
    vesselType: string | null;
    vesselName?: string | null;
    imoNumber?: string | null;
    projectId?: number | null;
    projectName?: string | null;
    tankType: string;
    status: boolean;
};

export function createTank(payload: {
    tankName: string;
    subheader: string;
    vesselType?: string | null;
    imoNumber?: string | null;
    projectId?: number | null;
    tankTypeId: number;
    status: boolean;
}) {
    return axiosInstance.post("/api/ProjectConfig/AddNewTank", payload);
}


export function updateTank(
    formData: TankPayload
): Promise<AxiosResponse> {

    const routePath = `/api/ProjectConfig/EditTank`;
    return axiosInstance.post(routePath, formData);
}

export function deleteTank(
    tankId: string,
    IMO: string = "",
    ProjectId: number = 0
): Promise<any> {
    return axiosInstance.get(`/api/Tank/DeleteTanks`, {
        params: {
            tankId,
            IMO,
            ProjectId
        }
    });
}
export function updateTankStatus(payload: {
    data: string[];
    status: boolean;
    IMO?: string;
}) {
    return axiosInstance.post(
        "/api/Tank/ManageTankActiveCheckBox",
        payload
    );
}
export function ManageTankFilter_Project(IMO?: string): Promise<AxiosResponse> {
    const routePath = `/api/Tank/ManageTankFilter_Project`;
    return axiosInstance.get(routePath, {
        params: { IMO }
    });
}

export function ManageTankFilter_TankType(IMO?: string): Promise<AxiosResponse> {
    const routePath = `/api/Tank/ManageTankFilter_TankType`;
    return axiosInstance.get(routePath, {
        params: { IMO }
    });
}
export function ManageTankFilter_TankName(IMO?: string): Promise<AxiosResponse> {
    const routePath = `/api/Tank/ManageTankFilter_TankName`;
    return axiosInstance.get(routePath, {
        params: { IMO }
    });
}


export const getManageTanks = (params: {
    imo?: string;
    projectId?: string;
    vesselType?: string;
    tankType?: string;
    tankName?: string;
    status?: boolean;
}) => {
    return axiosInstance.get<ManageTankResponse>(
        "/api/ProjectConfig/ManageTank",
        {
            params: {
                isActive: false,
                imo: params.imo ?? null,
                projectId: params.projectId ?? null,
                tankRestoreFilter: 0,
                searchRestoreFilter: 0,
                vesselType: params.vesselType ?? null,
                tankType: params.tankType ?? null,
                tankName: params.tankName ?? null,
                status: params.status ?? null
            }
        }
    );
};


export const getFilteredTanks = (params: {
    vesselType?: string;
    tankType?: string;
    imo?: string;
    projectId?: string;
    pageIndex?: number;
    pageSize?: number;
}): Promise<AxiosResponse> => {
    const routePath = `/api/Tank/FilterMenuCustomization_Read`;

    const filterConditions: any[] = [];

    if (params.vesselType) {
        filterConditions.push({
            field: "vesselType",
            operator: "eq",
            value: params.vesselType
        });
    }

    if (params.tankType) {
        filterConditions.push({
            field: "tankType",
            operator: "eq",
            value: params.tankType
        });
    }

    if (params.imo) {
        filterConditions.push({
            field: "imoNumber",
            operator: "eq",
            value: params.imo
        });
    }

    const request: any = {
        filter: filterConditions.length > 0 ? {
            logic: "and",
            filters: filterConditions
        } : undefined,
        page: params.pageIndex ?? 0,
        pageSize: params.pageSize ?? 20
    };

    return axiosInstance.get(routePath, {
        params: {
            filter: request.filter ? JSON.stringify(request.filter) : undefined,
            page: request.page,
            pageSize: request.pageSize
        },
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
};

export const getAllTanks = (params?: {
    imo?: string;
}) => {
    return axiosInstance.get<TankDto[]>(
        "/api/ProjectConfig/GetFilteredTanks",
        {
            params: {
                imo: params?.imo ?? null,
                pageNumber: 1,
                pageSize: 999999
            }
        }
    );
};

export function GetProjectListByIMO(imoNumber: string): Promise<AxiosResponse> {
    const routePath = `/api/ProjectConfig/GetProjectListByIMO`;
    return axiosInstance.get(routePath, {
        params: { imoNumber }
    });
}
export function getVesselTankbyImo(imoNumber: string): Promise<AxiosResponse> {
    const routePath = `/api/ProjectConfig/GetVesselTankbyImo`;
    return axiosInstance.get(routePath, {
        params: { imoNumber }
    });
}

export function getShipType(): Promise<AxiosResponse> {
    const routePath = `/api/Tank/GetShipType`;
    return axiosInstance.get(routePath);
}

export function getVesselTypeList(): Promise<AxiosResponse> {
    const routePath = `/api/Tank/GetVesselTypeList`;
    return axiosInstance.get(routePath);
}