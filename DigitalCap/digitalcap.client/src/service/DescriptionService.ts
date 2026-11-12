import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";
import type { ImageDescriptionViewModel } from "../Interface/Description";

export function getDescriptions(
    pageNumber: number = 1,
    pageSize: number = 50
): Promise<AxiosResponse> {

    const routePath = `/api/Description/GetAllDescription`;

    return axiosInstance.get(routePath, {
        params: {
            pageNumber,
            pageSize
        }
    });
}

export function getTemplateNameFilter(): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GradingTemplateNameFilter_TemplateName`;
    return axiosInstance.get(routePath);
}

export function getVesselTypeFilter(): Promise<AxiosResponse> {
    const routePath = `/api/Grading/GradingVesselTypeFilter_VesselType`;
    return axiosInstance.get(routePath);
}

export function getSectionsByTemplateAndVessel(templateName: string, vesselType: string): Promise<AxiosResponse> {
    const routePath = `/api/Description/GetSectionNameByTemplateNameAndVesselType`;
    return axiosInstance.get(routePath, {
        params: {
            templateName,
            vesselType,
        },
    });
}

export function getDescriptionsManage(isActive: boolean = false,
    descriptionRestoreFilter: number = 0,
    searchDescriptionRestoreFilter: number = 0): Promise<AxiosResponse> {

    const routePath = `/api/Description/ManageDescription`;

    return axiosInstance.get(routePath, {
        params: {
            isActive,
            descriptionRestoreFilter,
            searchDescriptionRestoreFilter
        }
    });
}

export function setManageDescriptionFilters(
    request: {
        descriptionRestoreFilter: number;
        searchDescriptionRestoreFilter: number;
    }
): Promise<AxiosResponse> {

    const routePath = `/api/Description/manage-description-redirect`;

    return axiosInstance.post(routePath, request);
}

export function addNewDescription(model: ImageDescriptionViewModel): Promise<AxiosResponse> {
    const routePath = `/api/Description/AddNewDescription`;

    return axiosInstance.post(routePath, model);
}

export function editDescription(model: ImageDescriptionViewModel): Promise<AxiosResponse> {
    const routePath = `/api/Description/EditDescription`;
    return axiosInstance.put(routePath, model);
}

export function getEditDescriptionById(formData: {
    descriptionId: number;
    sectionId: string;
    tanktypeId?: number | undefined;

}): Promise<AxiosResponse> {
    const routePath = `/api/Description / EditDescriptionById`;
    return axiosInstance.get(routePath, { params: formData });

}
export interface UpdateDescriptionRequest {
    vesselType: string;
    templateName: string;
    sectionId: string;
    descriptionName: string;
    status: boolean;
}

export function updateDescription(payload: UpdateDescriptionRequest): Promise<AxiosResponse> {
    const routePath = `/api/Description/UpdateImageDescription`;
    return axiosInstance.post(routePath, payload);
}

export function getDescriptionVesselTypeFilter(): Promise<AxiosResponse> {
    const routePath = `/api/Description/DescriptionVesselTypeFilter`;
    return axiosInstance.get(routePath);
}