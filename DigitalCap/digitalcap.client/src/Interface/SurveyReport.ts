
export interface Section {
    sectionId: string;
    tanktypeId: number;
    sectionName: string;
    templateId: number;
}

export interface ReportPart {
    templateId: number;
    name: string;
    reportPartExists: boolean;
    sectionIds: string[];
    sectionsList: Section[];
}

export interface SurveyReport {
    projectId: number;
    projectName: string | null;
    reportTemplateSectionId: string | null;
    reportNo: string | null;
    reportPort: string | null;
    reportDate: string | null;
    vessel: any | null;
    surveyStatus: string | null;
    statutoryCertificatesExpirationDates: any | null;
    statutoryCertificatesIssuedDates: any | null;
    template: any | null;
    tankReportView: any | null;
    sections: any | null;
    assignmentId: number | null;
    projectStatus: string | null;
    isSynched: boolean;
}

export interface UnplacedImage {
    id: number;
    imageId: number;
    base64String: string;
}

export interface SurveyReportResponse {
    projectId: number;
    projectName: string;
    report: SurveyReport;
    reportPartGrid: ReportPart[];
    unplacedImages: UnplacedImage[];
}