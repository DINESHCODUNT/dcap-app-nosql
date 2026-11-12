import axiosInstance from "./axiosInstance";
import type { AxiosResponse } from "axios";
import type { LoginRequest } from "../Interface/Login";

export function login(obj: LoginRequest): Promise<AxiosResponse> {
    return axiosInstance.post('/api/account/login', obj);
}

export function requestAccess(obj: { email: string }): Promise<AxiosResponse> {
    return axiosInstance.post('/api/account/RequestAccess', obj);
}

export interface ChangePasswordRequest {
    oldPassword: string;
    newPassword: string;
    confirmPassword: string;
}

export function changePassword(obj: ChangePasswordRequest): Promise<AxiosResponse> {
    return axiosInstance.post('/api/account/ChangePassword', {
        OldPassword: obj.oldPassword,
        NewPassword: obj.newPassword,
        ConfirmPassword: obj.confirmPassword,
    });
}

export function forgotPassword(obj: { email: string }): Promise<AxiosResponse> {
    return axiosInstance.post('/api/account/forgot-password', obj);
}

export function resetPassword(obj: { email: string, token: string, newPassword: string }): Promise<AxiosResponse> {
    return axiosInstance.post('/api/account/reset-password', obj);
}
