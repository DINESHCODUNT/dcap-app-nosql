import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";

export function getReportExportPartByVesselType(vesselTypeId: number | string): Promise<AxiosResponse> {
    const routePath = `/api/Export/GetReportExportPartByVesselType`;
    return axiosInstance.get(routePath, {
        params: { vesselTypeId }
    });
}
export function getReportExportPartByImo(projectId: number | string): Promise<AxiosResponse> {
    const routePath = `/api/Export/GetReportExportPartByImo`;
    return axiosInstance.get(routePath, {
        params: { projectId }
    });
}

export function getVesselType(): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GetVesselType`;
    return axiosInstance.get(routePath);
}

export function saveRequiredInExport(payload: {
    SelectedImoNumber: number;
    SelectedVesselTypeId: number;
    exportParts: Array<{
        PartId: number;
        PartName: string;
        RequiredInReport: boolean;
        IncludeInReport: boolean;
        sections: Array<{
            SectionId: string;
            SectionName: string;
            RequiredInReport: boolean;
            IncludeInReport: boolean;
            TankTypeId: number;
        }>;
    }>;
}): Promise<AxiosResponse> {
    const routePath = `/api/Export/SaveRequiredInExport`;
    return axiosInstance.post(routePath, payload);
}