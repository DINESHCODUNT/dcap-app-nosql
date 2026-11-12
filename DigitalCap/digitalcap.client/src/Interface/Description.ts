export interface ImageDescriptions {
    Id: number;
    gradingId: number;
    vesselType: string;
    templateName: string;
    sectionName: string;
    description: string;
    isActive: boolean;
    requiredInReport: boolean;
}

export interface ImageDescriptionViewModel {
    Id: number;
    gradingId: number;
    vesselType: string;
    templateName: string;
    sectionName: string;
    description: string;
    isActive: boolean;
}

export interface UpdateDescriptionRequest {
    vesselType: string;
    templateName: string;
    sectionId: string;
    descriptionName: string;
    status: boolean;
}








