import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";
import type { SurveyReportResponse } from "../Interface/SurveyReport";
const SURVEY_REPORT_API = "/api/SurveyReport";
const SURVEY_PROJECT_API = "/api/Survey";
const TANK_API = "/api/Tank";

const OFFLINE_REPORT_API = "/Offline";
const OFFLINE_PROJECT_API = "/OfflineSurvey";
export interface SurveyReportIndexResponse {
    reportPartGrid: any[];
    projectId: number;
}

export interface SurveyReportDataResponse {
    data: any;
}
function surveyReportBase(isOffline: boolean): string {
    return isOffline ? OFFLINE_REPORT_API : SURVEY_REPORT_API;
}

function surveyProjectBase(isOffline: boolean): string {
    return isOffline ? OFFLINE_PROJECT_API : SURVEY_PROJECT_API;
}

function tankBase(isOffline: boolean): string {
    return isOffline ? OFFLINE_REPORT_API : TANK_API;
}

function toFormBody(data: Record<string, string | number | boolean>): string {
    const p = new URLSearchParams();
    Object.entries(data).forEach(([k, v]) => {
        if (v !== undefined && v !== null) p.append(k, String(v));
    });
    return p.toString();
}

const FORM_URLENCODED = { "Content-Type": "application/x-www-form-urlencoded" } as const;

export interface ExportSections {
    sectionId: string;
    sectionName?: string;
    requiredInReport?: boolean;
    includeInReport?: boolean;
    tankTypeId?: number;
    isSubSection?: boolean;
    parentId?: string;
    tankType?: string;
}

export interface ExportPart {
    partId: number;
    partName?: string;
    requiredInReport?: boolean;
    includeInReport?: boolean;
    sections?: ExportSections[];
}

export interface UpdateReportPayload {
    projectId: number | string;
    templateId: number | string;
    reportPart: number;
    value: string | null;
    imonumber: string | number;
    hullNo?: string | number | null;
    surveyDate?: string | null;
}

export function updateReport(
    payload: UpdateReportPayload,
    isOffline = false
): Promise<AxiosResponse<unknown>> {

    const base = surveyReportBase(isOffline);

    return axiosInstance.post(
        `${base}/UpdateReport`,
        payload
    );
}

export interface DeleteSurveyImagePayload {
    projectId: number | string;
    cardId: number | string;
    templateId: number | string;
    sectionId: string;
    imageId: number | string;
    replaceImage: boolean;
}

export function deleteImage(
    payload: DeleteSurveyImagePayload
): Promise<AxiosResponse<unknown>> {

    return axiosInstance.post(
        `${SURVEY_REPORT_API}/DeleteProjectImage`,
        null,
        {
            params: {
                templateId: payload.templateId,
                projectId: payload.projectId,
                sectionId: payload.sectionId,
                cardId: payload.cardId,
                imageId: payload.imageId
            }
        }
    );
}
export function deleteGenericImage(
    templateId: number | string,
    projectId: number,
    sectionId: string,
    cardId: number,
    imageId: number
) {

    return axiosInstance.post(
        `${SURVEY_REPORT_API}/DeleteGenericImage`,
        null,
        {
            params: {
                templateId,
                projectId,
                sectionId,
                cardId,
                imageId
            }
        }
    );
}

export interface UploadSurveyImagePayload {
    file: File;
    projectId: number | string;
    cardId: number | string;
    templateId: number | string;
    sectionId: string;
    imageId: number | string;
    replaceImage: boolean;
}

export interface UploadSurveyImageResponse {
    FileId?: string;
    Image?: string;
    image?: string;
}

export function uploadSurveyImage(
    payload: UploadSurveyImagePayload,
    isOffline = false
): Promise<AxiosResponse<UploadSurveyImageResponse>> {
    const base = surveyReportBase(isOffline);
    const formData = new FormData();
    formData.append("file", payload.file);
    formData.append("projectId", String(payload.projectId));
    formData.append("cardId", String(payload.cardId));
    formData.append("templateId", String(payload.templateId));
    formData.append("sectionId", payload.sectionId);
    formData.append("imageId", String(payload.imageId));
    formData.append("replaceImage", String(payload.replaceImage));
    return axiosInstance.post<UploadSurveyImageResponse>(`${base}/UploadImage`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
}

export interface UploadGenericSurveyImagePayload extends UploadSurveyImagePayload {
    cardName: string;
}

export function uploadGenericSurveyImage(
    payload: UploadGenericSurveyImagePayload,
    isOffline = false
): Promise<AxiosResponse<UploadSurveyImageResponse>> {
    const base = surveyReportBase(isOffline);
    const formData = new FormData();
    formData.append("file", payload.file);
    formData.append("projectId", String(payload.projectId));
    formData.append("cardId", String(payload.cardId));
    formData.append("templateId", String(payload.templateId));
    formData.append("sectionId", payload.sectionId);
    formData.append("imageId", String(payload.imageId));
    formData.append("replaceImage", String(payload.replaceImage));
    formData.append("cardName", payload.cardName.trim());
    return axiosInstance.post<UploadSurveyImageResponse>(`${base}/UploadGenericImage`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
}

export function removeSurveyUploadStaging(
    payload: Record<string, string | number | boolean | undefined>
): Promise<AxiosResponse<unknown>> {
    return axiosInstance.post(`/File/Remove`, null, { params: payload });
}

export function refreshSurveyStatus(
    projectId: number | string,
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyReportBase(isOffline);
    const body = toFormBody({ projectId });
    return axiosInstance.post<string>(`${base}/RefreshSurveyStatus`, body, {
        headers: FORM_URLENCODED,
        responseType: "text",
    });
}

export function refreshCertificates(
    projectId: number | string,
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyReportBase(isOffline);
    const body = toFormBody({ projectId });
    return axiosInstance.post<string>(`${base}/RefreshCertificates`, body, {
        headers: FORM_URLENCODED,
        responseType: "text",
    });
}

export function refreshSurveyStatusAndCertificates(
    projectId: number | string,
    isOffline = false
): Promise<[AxiosResponse<string>, AxiosResponse<string>]> {
    return Promise.all([
        refreshSurveyStatus(projectId, isOffline),
        refreshCertificates(projectId, isOffline),
    ]);
}

export function getReportBySectionId(
    projectId: number | string,
    templateSectionId: number | string,
    sectionId: string,
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyReportBase(isOffline);
    const body = toFormBody({ projectId, templateSectionId, SectionId: sectionId });
    return axiosInstance.post<string>(`${base}/GetReportBySectionId`, body, {
        headers: FORM_URLENCODED,
        responseType: "text",
    });
}

export function validateReport(
    projectId: number | string,
    templateSectionId: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyReportBase(isOffline);
    const body = toFormBody({ projectId, templateSectionId });
    return axiosInstance.post(`${base}/ValidateReport`, body, { headers: FORM_URLENCODED });
}

export function validateGrading(
    projectId: number | string,
    templateSectionId: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {

    const base = surveyReportBase(isOffline);

    return axiosInstance.post(
        `${base}/ValidateGrading`,
        {
            projectId,
            templateSectionId
        }
    );
}

export function validatePreviousTabReport(
    projectId: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyReportBase(isOffline);
    const body = toFormBody({ projectId });
    return axiosInstance.post(`${base}/ValidatePreviousTabReport`, body, { headers: FORM_URLENCODED });
}

export function exportAllValidation(
    projectId: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {

    const base = surveyReportBase(isOffline);

    return axiosInstance.post(
        `${base}/ExportAllValidation`,
        null,
        {
            params: { projectId }
        }
    );
}

export function exportValidationPartial(
    projectId: number | string,
    sectionid: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyReportBase(isOffline);
    return axiosInstance.post(`${base}/ExportValidationPartial`, null, {
        params: { projectId, sectionid }
    });
}

export function mergeReportPdfSurveyReport(
    params: {
        id: string;
        allTemplate: boolean;
        projectId: number | string;
        templateId: number | string;
    },
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyReportBase(isOffline);
    const body = toFormBody({
        id: params.id,
        allTemplate: params.allTemplate,
        projectId: params.projectId,
        templateId: params.templateId,
    });
    return axiosInstance.post<string>(`${base}/MergeReportPDF`, body, {
        headers: FORM_URLENCODED,
        responseType: "text",
    });
}

export function saveReportFileSurveyReport(
    payload: {
        contentType: string;
        base64: string;
        allTemplate: boolean;
        id: string;
        sectionId: string;
        totalTemplates: number;
        totalSections: number;
        templateId: number | string;
        projectId: number | string;
    },
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyReportBase(isOffline);
    const body = toFormBody({
        contentType: payload.contentType,
        base64: payload.base64,
        allTemplate: payload.allTemplate,
        id: payload.id,
        sectionId: payload.sectionId,
        totalTemplates: payload.totalTemplates,
        totalSections: payload.totalSections,
        templateId: payload.templateId,
        projectId: payload.projectId,
    });
    return axiosInstance.post<string>(`${base}/SaveReportFile`, body, {
        headers: FORM_URLENCODED,
        responseType: "text",
    });
}

export function createReport(
    projectId: number | string,
    templateSectionId: number | string
): Promise<AxiosResponse<boolean>> {
    return axiosInstance.post<boolean>(
        `${SURVEY_REPORT_API}/CreateReport`,
        toFormBody({ projectId, templateSectionId }),
        { headers: FORM_URLENCODED }
    );
}

export function deleteReport(
    projectId: number | string,
    templateSectionId: number | string
): Promise<AxiosResponse<boolean>> {

    return axiosInstance.post<boolean>(
        `${SURVEY_REPORT_API}/DeleteReport`,
        null,
        {
            params: {
                projectId,
                templateSectionId
            }
        }
    );
}

export function getExportViewSurveyReport(
    projectId: number | string,
    allTemplate: boolean,
    templateId: number | string,
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyReportBase(isOffline);
    return axiosInstance.get<string>(`${base}/Export`, {
        params: { projectId, allTemplate, templateId },
        responseType: "text",
    });
}


export function updateGradingNotes(payload: {
    templateId: number | string;
    projectId: number | string;
    sectionId: string;
    gradingId: number | string;
    value: string;
}) {

    return axiosInstance.post(
        `${SURVEY_REPORT_API}/UpdateGradingNotes`,
        payload
    );
}
export function updateGradingCondition(
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        gradingId: number | string;
        choices: { choiceName: string; value: boolean }[];
    },
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const base = surveyReportBase(isOffline);
    return axiosInstance.post<boolean>(
        `${base}/UpdateGradingCondition`,
        payload
    );
}
export function updateSectionNote(
    payload: { templateId: number | string; projectId: number | string; sectionId: string; value: string },
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const base = surveyReportBase(isOffline);
    return axiosInstance.post<boolean>(`${base}/UpdateSectionNote`, toFormBody(payload), {
        headers: FORM_URLENCODED,
    });
}

export function updateImageDescriptionDropdownCard(
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardId: number | string;
        value: number | string;
    },
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const base = surveyReportBase(isOffline);
    return axiosInstance.post<boolean>(
        `${base}/UpdateImageDescriptionDropdownCard`,
        payload
    );
}

export function updateAdditionalDescription(
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardId: number | string;
        value: string;
    },
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const base = surveyReportBase(isOffline);
    return axiosInstance.post<boolean>(
        `${base}/UpdateAdditionalDescription`,
        payload
    );
}

export function updateCurrentCondition(
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardId: number | string;
        value: number | string;
    },
    isOffline = false
): Promise<AxiosResponse<boolean>> {

    const base = surveyReportBase(isOffline);

    return axiosInstance.post<boolean>(
        `${base}/UpdateCurrentCondition`,
        payload
    );
}

function appendGenericCardFormFields(
    formData: FormData,
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardId: number | string;
        cardName: string;
    }
) {
    formData.append("templateId", String(payload.templateId));
    formData.append("projectId", String(payload.projectId));
    formData.append("sectionId", payload.sectionId);
    formData.append("cardId", String(payload.cardId));
    formData.append("cardName", payload.cardName.trim());
}

export function updateGenericImageDescriptionDropdownCard(
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardId: number | string;
        value: number | string;
        cardName: string;
    },
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const base = surveyReportBase(isOffline);
    const formData = new FormData();
    appendGenericCardFormFields(formData, payload);
    formData.append("value", String(payload.value));
    return axiosInstance.post<boolean>(`${base}/UpdateGenericImageDescriptionDropdownCard`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
}

export function updateGenericAdditionalDescription(
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardId: number | string;
        value: string;
        cardName: string;
    },
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const base = surveyReportBase(isOffline);
    const formData = new FormData();
    appendGenericCardFormFields(formData, payload);
    formData.append("value", payload.value);
    return axiosInstance.post<boolean>(`${base}/UpdateGenericAdditionalDescription`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
}

export function updateGenericCurrentCondition(
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardId: number | string;
        value: number | string;
        cardName: string;
    },
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const base = surveyReportBase(isOffline);
    const formData = new FormData();
    appendGenericCardFormFields(formData, payload);
    formData.append("value", String(payload.value));
    return axiosInstance.post<boolean>(`${base}/UpdateGenericCurrentCondition`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
}

export interface UpdateHIReportPayload {
    id: string;
    projectId: number;

    companyName?: string;
    certificateNumber?: string;

    expiryDate?: string | Date | null;
    ExpiryDate?: string | Date | null;

    thicknessMeasurementA?: string;
    thicknessMeasurementB?: string;
    thicknessMeasurementC?: string;

    additionalSurveyorComments?: string;
    maintanceExecuted?: string;
    accommodation?: string;
    photoGraphs?: string;

    operationalTestLocation?: string;
    operationalTestStart?: string | Date | null;
    OperationalTestStart?: string | Date | null;
    operationalTestFinish?: string | Date | null;
    OperationalTestFinish?: string | Date | null;

    optionalTestSatisFactory?: string;

    associatedReportNumber?: string;
    associatedReportDate?: string;

    remarks?: string;

    leadSurveyorName?: string;
    leadSurveyorNumber?: string;

    surveyorName?: string;
    surveyorNumber?: string;

    lastVisitDate?: string | Date | null;
    LastVisitDate?: string | Date | null;

    individualA?: string;
    individualB?: string;
    individualC?: string;
    individualD?: string;
    individualE?: string;
    individualF?: string;

    scouringOrPitting?: string;
    substantialCorrosionRemaining?: string;
    statutoryDeficiencies?: string;
    foundSatisfactory?: string;
    underRemarks?: string;

    bottomPlattingScouringOrPitting?: string;
}

export function updateHIReport(
    payload: UpdateHIReportPayload,
    isOffline = false
): Promise<AxiosResponse<boolean>> {

    const base = surveyReportBase(isOffline);

    return axiosInstance.post<boolean>(
        `${base}/UpdateH_IReport`,
        payload,
        {
            headers: {
                "Content-Type": "application/json",
            },
        }
    );
}

export function placeImageSurveyReport(
    payload: {
        projectId: number | string;
        templateId: number | string;
        sectionId: string;
        imageId: number | string;
        cardId: number | string;
        cardName: string;
        descriptionId: number | string;
        currentConditionId: number | string;
    },
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyReportBase(isOffline);
    return axiosInstance.post(`${base}/PlaceImage`, toFormBody(payload), { headers: FORM_URLENCODED });
}

export function deleteBulkUploadImage(fileId: number | string): Promise<AxiosResponse<unknown>> {
    return axiosInstance.post(`${SURVEY_REPORT_API}/Delete`, toFormBody({ fileId }), {
        headers: FORM_URLENCODED,
    });
}

export function downloadImageForPlacement(imageId: number | string): Promise<AxiosResponse<Blob>> {
    return axiosInstance.get(`${SURVEY_REPORT_API}/DownloadImageForPlacement`, {
        params: { imageId },
        responseType: "blob",
    });
}

export function getPhotoPlacementDropdownPartialSurveyReport(
    projectId: number | string,
    templateId: number | string,
    sectionId: string
): Promise<AxiosResponse<string>> {
    return axiosInstance.get<string>(`${SURVEY_REPORT_API}/GetPhotoPlacementDropdownPartial`, {
        params: { projectId, templateId, sectionId },
        responseType: "text",
    });
}

export function getSurveyPhotoPlacementPartial(
    projectId: number | string,
    templateSectionId: number | string,
    assignmentId: string,
    appIds: string
): Promise<AxiosResponse<string>> {
    return axiosInstance.get<string>(`${SURVEY_REPORT_API}/GetSurveyPhotoPlacementPartial`, {
        params: { projectId, templateSectionId, assignmentId, appIds },
        responseType: "text",
    });
}

export function getBulkUploadImagesListSurveyReport(): Promise<AxiosResponse<unknown>> {
    return axiosInstance.get(`${SURVEY_REPORT_API}/GetBulkUploadImagesList`);
}

export const IndexPartial = async (
    projectId: number | string,
    templateSectionId: number | string,
    imonumber: number | string = 0
) => {
    return axiosInstance.get(`/api/SurveyReport/IndexPartial`, {
        params: { projectId, templateSectionId, imonumber }
    });
};

export const SurveyIndexPartial = async (
    projectId: number | string,
    templateSectionId: number | string,
    imonumber: number | string = 0
) => {
    return axiosInstance.get(`/api/Survey/IndexPartial`, {
        params: { projectId, templateSectionId, imo: imonumber }
    });
};

export function checkShowExportSettings(
    projectId: number | string,
    templateId?: number | string,
    imonumber?: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.get(`${base}/CheckShowExportSettings`, {
        params: { projectId, templateId, imo: imonumber },
    });
}

export function checkShowExportAllSettings(
    projectId: number | string,
    imonumber?: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.get(`${base}/CheckShowExportAllSettings`, { params: { projectId, imo: imonumber } });
}

export function getProjectExportConfig(
    projectId: number | string,
    templateId?: number | string,
    imonumber?: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.get(`${base}/GetProjectExportConfig`, {
        params: { projectId, templateId, imo: imonumber },
    });
}

export function getProjectExportAllConfig(
    projectId: number | string,
    imonumber?: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.get(`${base}/GetProjectExportAllConfig`, { params: { projectId, imo: imonumber } });
}

export function getAllSections(
    projectId: number | string,
    templateId?: number | string,
    imonumber?: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.get(`${base}/GetAllSections`, { params: { projectId, templateId, imo: imonumber } });
}

export function getAllParts(
    projectId: number | string,
    imonumber?: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.get(`${base}/GetAllParts`, { params: { projectId, imo: imonumber } });
}

export function validateProjectReport(
    projectId: number | string,
    templateSectionId: number | string,
    imo: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.post(
        `${base}/ValidateProjectReport`,
        toFormBody({ projectId, templateSectionId, imo }),
        { headers: FORM_URLENCODED }
    );
}

export const validateProjectGrading = (
    projectId: any,
    templateSectionId: any,
    imo: any,
    settings: any[] = []
) => {
    return axiosInstance.post(
        `/api/Survey/ValidateProjectGrading`,
        settings,
        {
            params: {
                projectId,
                templateSectionId,
                imo
            }
        }
    );
};

export function validateProjectPreviousTabReport(
    projectId: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.post(
        `${base}/ValidateProjectPreviousTabReport`,
        toFormBody({ projectId }),
        { headers: FORM_URLENCODED }
    );
}

export function exportAllProjectValidation(
    projectId: number | string,
    imo: number | string,
    settings: ExportPart[],
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.post(`${base}/ExportAllProjectValidation`, settings, {
        params: {
            projectId,
            imo
        }
    });
}

export function exportAllProjectPartialValidation(
    projectId: number | string,
    imo: number | string,
    settings: ExportPart[],
    partid: number | string,
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.post(`${base}/ExportAllProjectPartialValidation`, settings, {
        params: {
            projectId,
            imo,
            partid
        }
    });
}

/** Stores export parts in session (SurveyController.ExportAllConfig) */
export function exportAllConfig(
    projectId: number | string,
    allTemplate: boolean,
    templateId: number | string,
    imo: number | string,
    data: ExportPart[]
): Promise<AxiosResponse<unknown>> {
    return axiosInstance.post(
        `${SURVEY_PROJECT_API}/ExportAllConfig`,
        { data },
        {
            params: { projectId, allTemplate, templateId, imo },
            headers: { "Content-Type": "application/json" },
        }
    );
}

export function getProjectExportView(
    projectId: number | string,
    allTemplate: boolean,
    templateId: number | string,
    imo: number | string,
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.get<string>(`${base}/Export`, {
        params: { projectId, allTemplate, templateId, imo },
        responseType: "text",
    });
}

export function mergeReportPdfProject(
    params: {
        id: string;
        allTemplate: boolean;
        projectId: number | string;
        templateId: number | string;
        imo: number | string;
    },
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.post<string>(
        `${base}/MergeReportPDF`,
        toFormBody({
            id: params.id,
            allTemplate: params.allTemplate,
            projectId: params.projectId,
            templateId: params.templateId,
            imo: params.imo,
        }),
        { headers: FORM_URLENCODED, responseType: "text" }
    );
}

export function saveReportFileProject(
    payload: {
        contentType: string;
        base64: string;
        allTemplate: boolean;
        id: string;
        sectionId: string;
        totalTemplates: number;
        totalSections: number;
        templateId: number | string;
        projectId: number | string;
    },
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.post<string>(`${base}/SaveReportFile`, toFormBody(payload), {
        headers: FORM_URLENCODED,
        responseType: "text",
    });
}

/** Prefer single consolidated Web API; view historically posted `imo` as extra field */
export function getProjectReportHtml(
    projectId: number | string,
    templateSectionId: number | string,
    imo?: number | string,
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.post<string>(
        `${base}/GetReport`,
        toFormBody({
            projectId,
            templateSectionId,
            ...(imo !== undefined ? { imo } : {}),
        }),
        { headers: FORM_URLENCODED, responseType: "text" }
    );
}

export function updateHandIGradingNotes(
    projectId: number | string,
    gradingId: number | string,
    value: string,
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.post<boolean>(
        `${base}/UpdateHandIGradingNotes`,
        toFormBody({ projectId, gradingId, value }),
        { headers: FORM_URLENCODED }
    );
}

export function updateHandIGradingCondition(
    projectId: number | string,
    gradingId: number | string,
    choiceName: string,
    value: boolean,
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.post<boolean>(
        `${base}/UpdateHandIGradingCondition`,
        toFormBody({ projectId, gradingId, choiceName, value }),
        { headers: FORM_URLENCODED }
    );
}

export function placeImageProject(
    payload: {
        projectId: number | string;
        templateId: number | string;
        sectionId: string;
        imageId: number | string;
        cardId: number | string;
        cardName: string;
        descriptionId: number | string;
        currentConditionId: number | string;
    },
    isOffline = false
): Promise<AxiosResponse<unknown>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.post(`${base}/PlaceImage`, toFormBody(payload), { headers: FORM_URLENCODED });
}

export function getPhotoPlacementDropdownPartialProject(
    projectId: number | string,
    templateId: number | string,
    sectionId: string,
    imo: number | string,
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.get<string>(`${base}/GetPhotoPlacementDropdownPartial`, {
        params: { projectId, templateId, sectionId, imo },
        responseType: "text",
    });
}

export function getProjectSurveyPhotoPlacementPartial(
    projectId: number | string,
    templateSectionId: number | string,
    assignmentId: string,
    appIds: string,
    isOffline = false
): Promise<AxiosResponse<string>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.get<string>(`${base}/GetProjectSurveyPhotoPlacementPartial`, {
        params: { projectId, templateSectionId, assignmentId, appIds },
        responseType: "text",
    });
}

export function getBulkUploadImagesListProject(): Promise<AxiosResponse<unknown>> {
    return axiosInstance.get(`${SURVEY_PROJECT_API}/GetBulkUploadImagesList`);
}


function appendTankCardFormFields(
    formData: FormData,
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardNumber: number | string;
        cardName: string;
    }
) {
    formData.append("templateId", String(payload.templateId));
    formData.append("projectId", String(payload.projectId));
    formData.append("sectionId", payload.sectionId);
    formData.append("cardNumber", String(payload.cardNumber));
    formData.append("cardName", payload.cardName.trim());
}

export function uploadTankCardImage(
    payload: {
        file: File;
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardNumber: number | string;
        imageId?: number | string;
        replaceImage?: boolean;
        cardName: string;
    },
    isOffline = false
): Promise<AxiosResponse<UploadSurveyImageResponse>> {
    const b = tankBase(isOffline);
    const formData = new FormData();
    formData.append("tankfile", payload.file);
    formData.append("templateId", String(payload.templateId));
    formData.append("projectId", String(payload.projectId));
    formData.append("sectionId", payload.sectionId);
    formData.append("cardNumber", String(payload.cardNumber));
    formData.append("imageId", String(payload.imageId ?? 0));
    formData.append("replaceImage", String(payload.replaceImage ?? false));
    formData.append("cardName", payload.cardName.trim());
    return axiosInstance.post<UploadSurveyImageResponse>(`${b}/UploadTankCardImage`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
}

export function updateTankCardAdditionalDescription(
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardNumber: number | string;
        value: string;
        cardName: string;
    },
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const b = tankBase(isOffline);
    const formData = new FormData();
    appendTankCardFormFields(formData, payload);
    formData.append("value", payload.value);
    return axiosInstance.post<boolean>(`${b}/UpdateTankCardAdditionalDescription`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
}

export function updateTankImageDescriptionDropdownCard(
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardNumber: number | string;
        value: number | string;
        cardName: string;
    },
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const b = tankBase(isOffline);
    const formData = new FormData();
    appendTankCardFormFields(formData, payload);
    formData.append("value", String(payload.value));
    return axiosInstance.post<boolean>(`${b}/UpdateTankImageDescriptionDropdownCard`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
}

export function updateTankCardCurrentCondition(
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardNumber: number | string;
        value: number | string;
        cardName: string;
    },
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const b = tankBase(isOffline);
    const formData = new FormData();
    appendTankCardFormFields(formData, payload);
    formData.append("value", String(payload.value));
    return axiosInstance.post<boolean>(`${b}/UpdateTankCardCurrentCondition`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
}

export function deleteTankCardImage(
    payload: {
        templateId: number | string;
        projectId: number | string;
        sectionId: string;
        cardNumber: number | string;
        imageId: number | string;
    },
    isOffline = false
): Promise<AxiosResponse<boolean>> {
    const b = tankBase(isOffline);
    const formData = new FormData();
    formData.append("templateId", String(payload.templateId));
    formData.append("projectId", String(payload.projectId));
    formData.append("sectionId", payload.sectionId);
    formData.append("cardNumber", String(payload.cardNumber));
    formData.append("imageId", String(payload.imageId));
    return axiosInstance.post<boolean>(`${b}/DeleteTankCardImage`, formData, {
        headers: { "Content-Type": "multipart/form-data" },
    });
}

export function releaseDataTransfer(payload: Record<string, unknown>): Promise<AxiosResponse<unknown>> {
    return axiosInstance.post(`/DataTransfer/Release`, payload);
}

export function backupOfflineDatabase(): Promise<AxiosResponse<unknown>> {
    return axiosInstance.post(`/Offline/BackupDatabase`);
}

export function GetReport(
    projectId: number,
    templateSectionId: number,
): Promise<AxiosResponse<ReportIndexViewModel>> {
    return axiosInstance.get(`${SURVEY_REPORT_API}/GetReport`, {
        params: { projectId, templateSectionId },
    });
}

export function validateSurveyReportData(
    projectId: number | string,
    templateSectionId: number | string,
    isOffline = false
) {
    const base = surveyReportBase(isOffline);

    return axiosInstance.post(
        `${base}/ValidateReport`,
        toFormBody({ projectId, templateSectionId }),
        { headers: FORM_URLENCODED }
    );
}

export function exportSurveyReportData(
    projectId: number | string,
    templateId: number | string,
    isOffline = false
) {
    const base = surveyReportBase(isOffline);

    return axiosInstance.get(`${base}/Export`, {
        params: {
            projectId,
            allTemplate: true,
            templateId
        }
    });
}

export function GetIsElectron(): Promise<AxiosResponse> {
    const routePath = `/api/SurveyReport/GetIsElectron`;
    return axiosInstance.get(routePath);
}

export interface ReportSection {
    sectionId: string;
    sectionName: string;
}

export interface ReportPart {
    templateId: number;
    name: string;
    reportPartExists: boolean;
    sectionIds: string[];
    sectionsList: ReportSection[];
}

export interface ReportIndexViewModel {
    projectId: number;
    projectName: string;
    reportPartGrid: ReportPart[];
    isSynched: boolean;
    reportExport?: boolean;
    reportExportAll?: boolean;
    resetTemplate?: boolean;
    imonumber?: number | string | null;
}
//6287
export function getSurveyReportIndex(
    projectId: number,
    isOffline = false
): Promise<AxiosResponse<ReportIndexViewModel>> {
    const base = surveyReportBase(isOffline);
    return axiosInstance.get(`${base}/SurveyReportIndex`, {
        params: { projectId },
    });
}

/** New-project report index (SurveyController.Index) */
export function getSurveyProjectIndex(
    projectId: number,
    isOffline = false
): Promise<AxiosResponse<ReportIndexViewModel>> {
    const base = surveyProjectBase(isOffline);
    return axiosInstance.get(`${base}/Index`, {
        params: { projectId },
    });
}

export function createSurveyReport(
    projectId: number,
    templateSectionId: number,
    isOffline = false
): Promise<AxiosResponse> {
    const base = surveyReportBase(isOffline);
    return axiosInstance.post(`${base}/CreateReport`, null, {
        params: { projectId, templateSectionId },
    });
}

export function deleteSurveyReport(
    projectId: number,
    templateSectionId: number,
    isOffline = false
): Promise<AxiosResponse> {
    const base = surveyReportBase(isOffline);
    return axiosInstance.post(`${base}/DeleteReport`, null, {
        params: { projectId, templateSectionId },
    });
}


export interface ReportSection {
    sectionId: string;
    sectionName: string;
}

export interface ReportPart {
    templateId: number;
    name: string;
    reportPartExists: boolean;
    sectionIds: string[];
    sectionsList: ReportSection[];
}

export interface ReportIndexViewModel {
    projectId: number;
    projectName: string;
    reportPartGrid: ReportPart[];
    isSynched: boolean;
    reportExport?: boolean;
    reportExportAll?: boolean;
    resetTemplate?: boolean;
    imonumber?: number | string | null;
}
export function GetBulkUploadSectionDropdown(
    projectId: number
): Promise<AxiosResponse<ReportIndexViewModel>> {
    return axiosInstance.get(`${SURVEY_REPORT_API}/GetBulkUploadSectionDropdown`, {
        params: { projectId },
    });
}
export function GetPhotoPlacementDropdownData(
    projectId: number,
    templateId: number,
    sectionId: string
): Promise<AxiosResponse<ReportIndexViewModel>> {
    return axiosInstance.get(`${SURVEY_REPORT_API}/GetPhotoPlacementDropdownData`, {
        params: { projectId, templateId, sectionId },
    });
}
export function GetUploadedImageCount(
    projectId: number,
    templateId: number,
    sectionId: string
): Promise<AxiosResponse<ReportIndexViewModel>> {
    return axiosInstance.get(`${SURVEY_REPORT_API}/GetUploadedImageCount`, {
        params: { projectId, templateId, sectionId },
    });
}

export const getSurveyReport = async (
    projectId: number,
    templateSectionId: string | null
): Promise<SurveyReportResponse> => {

    const routePath = `api/SurveyReport/SurveyReportIndex?projectId=${projectId}&templateSectionId=${templateSectionId}`;

    const response = await axiosInstance.get<SurveyReportResponse>(routePath);

    return response.data;
};

export const uploadImage = (formData: FormData) => {
    return axiosInstance.post(
        "/api/SurveyReport/UploadImage",
        formData,
        {
            headers: {
                "Content-Type": "multipart/form-data"
            }
        }
    );
};

export function exportReport(payload: {
    projectId: number;
    templateId?: number;
    sectionId?: string;
    allTemplate?: boolean;
}) {
    return axiosInstance.post(
        `/api/SurveyReport/SurveyReportPDF`,
        payload,
        { responseType: "blob" }
    );
}

export function exportCAPReportPdf(
    projectId: number
) {

    return axiosInstance.get(
        `/api/SurveyReport/ExportCAPReportPdf?projectId=${projectId}`,
        {
            responseType: "blob"
        }
    );
}

export const exportPartPdf = async (
    projectId: number,
    templateId: number,
    selectedSettings?: any
): Promise<any> => {
    try {
        const response = await axiosInstance.post(
            `/api/SurveyReport/ExportPartPdf?projectId=${projectId}&templateId=${templateId}`,
            selectedSettings || [],
            {
                responseType: "blob",
            }
        );
        return response;
    } catch (error) {
        throw error;
    }
};

export const exportHIPdf = async (
    projectId: number,
    templateId: number
): Promise<void> => {
    return axiosInstance.get(
        `/api/SurveyReport/ExportHIPdf?projectId=${projectId}&templateId=${templateId}`,
        {
            responseType: "blob"
        }
    );
}


export const exportFullReportPdf = async (projectId: number, selectedSettings: any[] = []) => {
    return axiosInstance.post(
        `/api/SurveyReport/ExportFullPdf`,
        selectedSettings,
        {
            params: { projectId },
            responseType: 'blob'
        }
    );
};

export const mergeReportPdf = async (payload: {
    id: string;
    allTemplate: boolean;
    projectId: number;
    templateId: number;
    imo: number;
    settings: any[];
}) => {
    return await axiosInstance.post(
        `/api/Survey/MergeReportPdf`,
        payload.settings,
        {
            params: {
                id: payload.id,
                allTemplate: payload.allTemplate,
                projectId: payload.projectId,
                templateId: payload.templateId,
                imo: payload.imo
            }
        }
    );
};
export const GetConfig = async (projectId: number) => {

    const res = await axiosInstance.get('/api/SurveyReport/GetConfig');

    const defaultId = res.data.defaultTemplateProjectId;

    const isNewProject = projectId >= defaultId;

    return {
        defaultId,
        isNewProject
    };
};

export const resetTemplate = async (
    projectId: number,
    templateId: number,
    isOffline = false
) => {

    const base = surveyReportBase(isOffline);

    return axiosInstance.post(
        `${base}/ResetTemplate`,
        null,
        {
            params: {
                projectId,
                templateId
            }
        }
    );
};

export interface UpdateSectionNotePayload {
    templateId: number;
    projectId: number;
    sectionId: string;
    value: string;
}

export function updateSectionNotes(
    payload: UpdateSectionNotePayload,
    isOffline = false
): Promise<AxiosResponse<unknown>> {

    const base = surveyReportBase(isOffline);

    return axiosInstance.post(
        `${base}/UpdateSectionNote`,
        null,
        {
            params: {
                templateId: payload.templateId,
                projectId: payload.projectId,
                sectionId: payload.sectionId,
                value: payload.value
            }
        }
    );
}


export interface JobStatusResponse {
    status: "Processing" | "Completed" | "Failed" | "NotFound";
    result?: string;
    error?: string;
    base64?: string;
    fileName?: string;
}

export function startExportAllValidation(projectId: number | string) {
    return axiosInstance.post<{ jobId: string }>(`${surveyReportBase(false)}/StartExportAllValidation`, null, { params: { projectId } });
}
export function startExportAllProjectValidation(projectId: number | string, imo: number | string, settings: ExportPart[]) {
    return axiosInstance.post<{ jobId: string }>(`${surveyReportBase(false)}/StartExportAllProjectValidation`, settings, { params: { projectId, imo } });
}
export function getValidationStatus(jobId: string) {
    return axiosInstance.get<JobStatusResponse>(`${surveyReportBase(false)}/GetValidationStatus`, { params: { jobId } });
}
export function startExportFullPdf(projectId: number, selectedSettings: any[] = []) {
    return axiosInstance.post<{ jobId: string }>(`/api/SurveyReport/StartExportFullPdf`, selectedSettings, { params: { projectId } });
}
export function getExportFullPdfStatus(jobId: string) {
    return axiosInstance.get<JobStatusResponse>(`/api/SurveyReport/GetExportFullPdfStatus`, { params: { jobId } });
}

/** Polls a job every 5 s (same as digital-cap) until Completed / Failed / NotFound. */
export async function pollJob(
    getStatus: (jobId: string) => Promise<AxiosResponse<JobStatusResponse>>,
    jobId: string,
    isCancelled: () => boolean = () => false
): Promise<JobStatusResponse> {
    while (true) {
        if (isCancelled()) throw new Error("cancelled");
        const res = await getStatus(jobId);
        const s = res.data;
        if (s.status === "Completed") return s;
        if (s.status === "Failed") throw new Error(s.error || "The job failed. Please try again.");
        if (s.status === "NotFound") throw new Error("The job was lost. Please try again.");
        await new Promise(r => setTimeout(r, 5000));
    }
}
export const getImageCards = (
    projectId: any,
    templateId: any,
    sectionId: any
) => {
    return axiosInstance.get(`/api/Survey/GetImageCards`, {
        params: { projectId, templateId, sectionId }
    });
};