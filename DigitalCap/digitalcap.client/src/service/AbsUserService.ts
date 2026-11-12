import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";

export function getAbsUsers(params?: {
    userUpdated?: boolean;
    userCreated?: boolean;
}): Promise<AxiosResponse> {
    const routePath = `/api/AbsAdmin/ManageAbsUsers`;

    const queryParams: any = {};
    if (params?.userUpdated !== undefined) {
        queryParams.userUpdated = params.userUpdated;
    }
    if (params?.userCreated !== undefined) {
        queryParams.userCreated = params.userCreated;
    }

    return axiosInstance.get(routePath, {
        params: Object.keys(queryParams).length > 0 ? queryParams : undefined
    });
}

export function getClientUsers(params?: {
    userUpdated?: boolean;
    userCreated?: boolean;
}): Promise<AxiosResponse> {
    const routePath = `/api/AbsAdmin/ManageClientUsers`;

    const queryParams: any = {};
    if (params?.userUpdated !== undefined) {
        queryParams.userUpdated = params.userUpdated;
    }
    if (params?.userCreated !== undefined) {
        queryParams.userCreated = params.userCreated;
    }

    return axiosInstance.get(routePath, {
        params: Object.keys(queryParams).length > 0 ? queryParams : undefined
    });
}
export function getCapUsers(params?: {
    isClientUsers?: boolean;
    clientId?: string | number;
    company?: string;
    userUpdated?: boolean;
    userCreated?: boolean;
}): Promise<AxiosResponse> {
    if (params?.isClientUsers) {
        return getClientUsers({
            userUpdated: params.userUpdated,
            userCreated: params.userCreated
        });
    } else {
        return getAbsUsers({
            userUpdated: params?.userUpdated,
            userCreated: params?.userCreated
        });
    }
}
export interface CapUserForDelete {
    Id: number;
    AspNetUserId: string;
    FirstName?: string;
    LastName?: string;
    Email?: string;
    UserRole?: string;
    Time_zone?: string;
    Phone?: number | string;
    Company?: string;
}

export function deleteAbsUser(aspNetUserId: string): Promise<AxiosResponse> {
    return axiosInstance.post(
        `/api/AbsAdmin/DeleteAbsUser?aspNetUserId=${aspNetUserId}`
    );
}
export function deleteClientUser(aspNetUserId: string): Promise<AxiosResponse> {
    return axiosInstance.post("/api/AbsAdmin/DeleteClientUser", {
        aspNetUserId: aspNetUserId
    });
}
export function deleteCapUser(
    aspNetUserId: string,
    params?: {
        isClientUsers?: boolean;
        company?: string;
    }
): Promise<AxiosResponse> {
    if (params?.isClientUsers) {
        return deleteClientUser(aspNetUserId);
    } else {
        return deleteAbsUser(aspNetUserId);
    }
}
export interface AddClientAdminInitDto {
    Company?: string;
    DefaultRole?: string;
    ClientId?: string | number;
}

export function getAddAbsUserInit(params?: {
    company?: string;
}): Promise<AxiosResponse<AddClientAdminInitDto>> {
    const routePath = `/api/AbsAdmin/AddAbsUser`;
    const queryParams: any = {};
    if (params?.company) {
        queryParams.company = params.company;
    }
    return axiosInstance.get(routePath, {
        params: Object.keys(queryParams).length > 0 ? queryParams : undefined
    });
}
export function getAddClientUserInit(params?: {
    clientId?: string | number;
    company?: string;
}): Promise<AxiosResponse<AddClientAdminInitDto>> {
    const routePath = `/api/AbsAdmin/AddClientUser`;
    const queryParams: any = {};
    if (params?.clientId) {
        queryParams.clientId = params.clientId;
    }
    if (params?.company) {
        queryParams.company = params.company;
    }
    return axiosInstance.get(routePath, {
        params: Object.keys(queryParams).length > 0 ? queryParams : undefined
    });
}
export function getCapUserForEdit(
    userId: string | number,
    params?: {
        isClientUsers?: boolean;
        company?: string;
    }
): Promise<AxiosResponse> {
    const action = params?.isClientUsers ? "EditClientUser" : "EditAbsUser";
    const routePath = `/api/AbsAdmin/${action}`;

    const queryParams: any = {
        userId: userId
    };
    if (params?.company) {
        queryParams.company = params.company;
    }

    return axiosInstance.get(routePath, {
        params: queryParams
    });
}
export interface AddAbsUserRequestDto {
    FirstName?: string;
    LastName?: string;
    Email?: string;
    Phone?: string;
    TimeZone?: string;
    Company?: string;
    ClientId?: string | number | null;
    Role?: string;
    UpdatedRoles?: string[];
}
export interface UserAccountModel {
    FirstName?: string;
    LastName?: string;
    Phone?: string;
    TimeZone?: string;
    ClientId?: string | number;
    AspNetUserId?: string;
    Company?: string;
    Email?: string;
    Title?: string;
    ProfileImageUrl?: string;
}

export interface AccountViewModel {
    Id?: string;
    Email?: string;
    Company?: string;
    UserAccount?: UserAccountModel;
    Clients?: any[];
    Password?: string;
    ConfirmPassword?: string;
    Title?: string;
    SaveText?: string;
    CancelUrl?: string;
    AspNetUser?: string;
    Controller?: string;
    PostAction?: string;
    DisplayRole?: string;
    UpdatedRole?: string;
    AbsUser?: boolean;
    ClientUser?: boolean;
    NewUser?: boolean;
    IsEnabled?: boolean;
}

export function addAbsUser(userData: AccountViewModel): Promise<AxiosResponse> {
    const routePath = `/api/AbsAdmin/AddAbsUser`;
    return axiosInstance.post(routePath, userData);
}

export interface CreateAbsUserRequest {
    firstName: string;
    lastName: string;
    email: string;
    phone?: string | number;
    timeZone?: string;
    company?: string;
    role?: string;
    clientId?: string | number;
}

export function addClientUser(userData: AccountViewModel): Promise<AxiosResponse> {
    const routePath = `/api/AbsAdmin/AddClientUser`;
    return axiosInstance.post(routePath, userData);
}
export function editAbsUser(userData: AccountViewModel): Promise<AxiosResponse> {
    const routePath = `/api/AbsAdmin/EditAbsUser`;
    return axiosInstance.post(routePath, userData);
}

export function editClientUser(userData: AccountViewModel): Promise<AxiosResponse> {
    const routePath = `/api/AbsAdmin/EditClientUser`;
    return axiosInstance.post(routePath, userData);
}

export function saveCapUser(
    userData: AccountViewModel,
    params?: {
        isClientUsers?: boolean;
        company?: string;
        clientId?: string | number;
        isEdit?: boolean;
    }
): Promise<AxiosResponse> {
    if (params?.isEdit) {
        if (params?.isClientUsers) {
            return editClientUser(userData);
        } else {
            return editAbsUser(userData);
        }
    } else {
        if (params?.isClientUsers) {
            return addClientUser(userData);
        } else {
            return addAbsUser(userData);
        }
    }
}
export interface CreateClientAdminDto {
    firstName: string;
    lastName: string;
    email: string;
    phone?: string | number;
    timeZone?: string;
    clientId?: string | number;
    company?: string;
    role?: string;
    updatedRoles?: string[];
}

export function addClientAdmin(dto: CreateClientAdminDto): Promise<AxiosResponse> {
    const routePath = `/api/AbsAdmin/AddClientAdminAsync`;
    return axiosInstance.post(routePath, dto);
}
export function addClientAdminAlternative(dto: CreateClientAdminDto): Promise<AxiosResponse> {
    const routePath = `/api/ClientProfile/AddClientAdminAsync`;
    return axiosInstance.post(routePath, dto);
}

export interface TimeZoneOption {
    id: string | number;
    name: string;
    value: string;
}

export function getTimeZones(clientTz?: string): Promise<AxiosResponse> {
    const routePath = `/api/AbsAdmin/GetTimeZone`;

    const clientTimeZone = clientTz || Intl.DateTimeFormat().resolvedOptions().timeZone;

    return axiosInstance.get(routePath, {
        params: {
            clientTz: clientTimeZone
        }
    });
}
export function manageAbsUsers(): Promise<AxiosResponse> {
    const routePath = `/api/AbsAdmin/ManageAbsUsers`;
    return axiosInstance.get(routePath);
}