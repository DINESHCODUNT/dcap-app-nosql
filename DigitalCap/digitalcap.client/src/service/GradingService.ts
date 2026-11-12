import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";
import type { DataSourceRequest } from "../Interface/Grading";

export function getGradings(request?: DataSourceRequest): Promise<AxiosResponse> {
    const routePath = `/api/Grading/FilterMenuCustomization_Read`;

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
export function createGrading(formData: {
    vesselType: string;
    templateName: string;
    sectionName: string;
    gradingName: string;
    isActive: boolean;
    requiredInReport: boolean;
    templateId?: number;
    sectionId?: string;
    tanktypeId?: number;
}): Promise<AxiosResponse> {

    const routePath = `/api/ProjectConfig/AddNewGrading`;
    return axiosInstance.post(routePath, formData);
}

export function getVesselTypeFilter(): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GradingVesselTypeFilter_VesselType`;
    return axiosInstance.get(routePath);
}

export function getTemplateNameFilter(): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GradingTemplateNameFilter_TemplateName`;
    return axiosInstance.get(routePath);
}

export function getSectionNameFilter(): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GradingSectionNameFilter_SectionName`;
    return axiosInstance.get(routePath);
}

export function getGradingNameFilter(): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GradingNameFilter_GradingName`;
    return axiosInstance.get(routePath);
}

export function getVesselType(): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GetVesselType`;
    return axiosInstance.get(routePath);
}

export function getTemplateName(vesselType?: string): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GetTemplateName`;
    return axiosInstance.get(routePath, {
        params: { vesselType }
    });
}
export function getSectionNameByTemplateNameAndVesselType(templateName: string, vesselType: string): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GetSectionNameByTemplateNameAndVesselType`;
    return axiosInstance.get(routePath, {
        params: { templateName, vesselType }
    });
}
export function getSectionName(templateId: number, vesselType: string): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GetSectionName`;
    return axiosInstance.get(routePath, {
        params: { templateId, vesselType }
    });
}

export function getGradingById(gradingId: number, sectionId: number, tankTypeId: number): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GetGradingById`;
    return axiosInstance.get(routePath, {
        params: { gradingId, sectionId, tankTypeId }
    });
}
export function checkGradingNameExists(
    vesselType: string,
    sectionName: string,
    partName: string,
    labelName: string
): Promise<AxiosResponse> {
    const routePath = `/api/Grading/CheckGradingNameExists`;
    return axiosInstance.get(routePath, {
        params: { vesselType, sectionName, partName, labelName }
    });
}

export const navigateToEditGrading = (gradingId: number, sectionId: number, tankTypeId: number): void => {
    window.location.href = `/app/Configuration/EditGradingById?gradingId=${gradingId}&SectionId=${sectionId}&TankTypeId=${tankTypeId}`;
};

export function updateGrading(formData: {
    gradingId: number;
    vesselType: string;
    templateName: string;
    sectionName: string;
    gradingName: string;
    isActive: boolean;
    requiredInReport: boolean;
    templateId?: number;
    sectionId?: string;
    tanktypeId?: number;
}): Promise<AxiosResponse> {

    const routePath = `/api/ProjectConfig/EditGrading`;
    return axiosInstance.post(routePath, formData);
}
export function deleteGrading(deleteGradingId: number, deleteTanktypeId: number): Promise<AxiosResponse> {
    return axiosInstance.post("/api/ProjectConfig/DeleteGrading", {
        gradingId: deleteGradingId,
        tankId: deleteTanktypeId
    });
}

export function GetSectionNameByTemplateNameAndVesselType(TemplateName: string, VesselType: string): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GetSectionNameByTemplateNameAndVesselType`;
    return axiosInstance.get(routePath, {
        params: { TemplateName, VesselType }
    });
}
