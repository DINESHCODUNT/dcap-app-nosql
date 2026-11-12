export interface DataSourceRequest {
    page?: number;
    pageSize?: number;
    skip?: number;
    take?: number;
    sort?: SortDescriptor[];
    filter?: FilterDescriptor;
    group?: GroupDescriptor[];
}

export interface SortDescriptor {
    field: string;
    dir: 'asc' | 'desc';
}

export interface FilterDescriptor {
    field?: string;
    operator?: string;
    value?: any;
    logic?: 'and' | 'or';
    filters?: FilterDescriptor[];
}

export interface GroupDescriptor {
    field: string;
    dir?: 'asc' | 'desc';
    aggregates?: any[];
}

export interface DataSourceResult<T> {
    data: T[];
    total: number;
    aggregates?: any;
}

export interface ManageGradingRow {
    gradingId: number;
    sectionId: number;
    tanktypeId: number;
    vesselType: string;
    templateName: string;
    sectionName: string;
    gradingName: string;
    isActive: boolean;
    requiredInReport: boolean;
}
export interface GradingFormData {
    id?: number;
    gradingId?: number;
    tanktypeId?: number;
    templateId?: number;
    sectionId?: number;
    vesselType: string;
    templateName: string;
    sectionName: string;
    gradingName: string;
    status: boolean;
    requiredInReport: boolean;
}