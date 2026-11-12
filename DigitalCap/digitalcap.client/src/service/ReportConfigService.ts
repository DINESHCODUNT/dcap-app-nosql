import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";

export interface ReportPart {
    vesselTypePartMappingId?: number;
    partId?: number;
    vesselTypeId: number;
    partName: string;
    sequenceNo: number;
    isActive: boolean;
    createdBy?: string;
    isDeleted?: boolean;
}

export interface NormalSection {
    vesselTypeNormalSectionMappingId?: string | null;
    normalSectionMappingId?: string | null;
    vesselTypePartMappingId: number;
    vesselTypeId: number;
    sectionName: string;
    subHeader: string;
    totalCards: number;
    placeholderCount: number;
    fileNameCount: number;
    isActive: boolean;
    createdBy?: string;
    isDeleted?: boolean;
    partName?: string;
    dirty?: boolean;
}

export interface TankSection {
    vessselTypeTankSectionMappingId?: string | null;
    vesselTypePartMappingId: number;
    vesselTypeId: number;
    tankTypeId: number;
    tankType: string;
    totalCards: number;
    placeholderCount: number;
    fileNameCount: number;
    isActive: boolean;
    isMapped: boolean;
    createdBy?: string;
    isDeleted?: boolean;
    partName?: string;
    dirty?: boolean;
}

export interface SubSection {
    subSectionId: string;
    sectionId: string;
    sectionName: string;
    totalCards: number;
    placeholderCount: number;
    fileNameCount: number;
    isActive: boolean;
    isDeleted?: boolean;
    createdBy?: string;
    modifiedBy?: string;
}

export interface ReportConfigPayload {
    reportParts?: ReportPart[];
    normalSectionMappings?: NormalSection[];
    tankSectionMappings?: TankSection[];
    normalSubSectionMappings?: SubSection[];
}

export function getReportPartsByVesselType(vesselTypeId: number | string): Promise<AxiosResponse> {
    const routePath = `/api/ReportConfigAPI/GetReportPartsByVesselType`;
    return axiosInstance.get(routePath, {
        params: { vesselTypeId }
    });
}

export function getSectionNamesByPartId(vesselTypeId: number | string, partId: number | string): Promise<AxiosResponse> {
    const routePath = `/api/ReportConfigAPI/GetSectionNamesByPartId`;
    return axiosInstance.get(routePath, {
        params: { vesselTypeId, partId }
    });
}
export function createReportPartConfig(vesselTypeId: number | string, payload: ReportConfigPayload): Promise<AxiosResponse> {
    const routePath = `/api/ReportConfigAPI/CreateReportPartConfig?vesselTypeId=${vesselTypeId}`;
    return axiosInstance.post(routePath, payload);
}

export function saveReportTemplateConfig(
    vesselTypeId: number,
    payload: ReportConfigPayload
): Promise<AxiosResponse> {
    const routePath = `/api/ReportConfigAPI/CreateReportPartConfig`;
    return axiosInstance.post(routePath, payload, {
        params: { vesselTypeId }
    });
}