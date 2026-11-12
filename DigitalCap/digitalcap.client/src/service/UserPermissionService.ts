import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";
export function getUserRoles(): Promise<AxiosResponse> {
    const routePath = `/api/UserPermission/GetUserRoles`;
    return axiosInstance.get(routePath);
}
export function getRolePermission(roleId: string | number): Promise<AxiosResponse> {
    const routePath = `/api/UserPermission/GetRolePermission`;
    return axiosInstance.get(routePath, {
        params: { RoleId: roleId }
    });
}
export function updatePermission(payload: {
    roleId: string | number;
    permissionId: string | number;
    id: string | number | null;
    value: boolean;
    key: "read" | "edit" | "delete";
}): Promise<AxiosResponse> {
    const routePath = `/api/UserPermission/UpdatePermission`;
    const params = {
        Id: payload.id ? String(payload.id) : "00000000-0000-0000-0000-000000000000",
        RoleId: String(payload.roleId),
        PermissionId: String(payload.permissionId),
        Key: payload.key,
        Value: payload.value
    };

    return axiosInstance.post(routePath, null, {
        params: params
    });
}
export function GetUserMenuPermissions(): Promise<AxiosResponse> {
    const routePath = `/api/UserPermission/GetUserMenuPermissions`;
    return axiosInstance.get(routePath);
}