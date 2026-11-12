import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import * as userPermissionService from "../service/UserPermissionService";
import { AuthContext } from "./AuthContext";

export interface PagePermission {
    read: boolean;
    edit: boolean;
    delete: boolean;
}

interface PermissionContextType {
    permissions: string[];
    roles: string[];
    pagePermissions: Record<string, PagePermission>;
    loadingPermissions: boolean;
    hasGlobalPermission: (perm: string) => boolean;
    hasPagePermission: (perm: string) => boolean;
    hasRole: (role: string) => boolean;
    reloadPermissions: () => Promise<void>;
}

const PermissionContext = createContext<PermissionContextType | undefined>(undefined);

export const PermissionProvider = ({ children }: { children: ReactNode }) => {
    const { user, loading: authLoading } = useContext(AuthContext);
    const [permissions, setPermissions] = useState<string[]>([]);
    const [roles, setRoles] = useState<string[]>([]);
    const [pagePermissions, setPagePermissions] = useState<Record<string, PagePermission>>({});
    const [loadingPermissions, setLoadingPermissions] = useState<boolean>(true);

    useEffect(() => {
        let isMounted = true;

        if (authLoading) {
            return;
        }

        const loadPermissions = async () => {
            if (!user) {
                if (isMounted) {
                    setPermissions([]);
                    setRoles([]);
                    setPagePermissions({});
                    setLoadingPermissions(false);
                }
                return;
            }

            if (isMounted) {
                setLoadingPermissions(true);
            }

            try {
                const response = await userPermissionService.GetUserMenuPermissions();
                const data = response.data?.data || response.data;

                if (isMounted) {
                    setPermissions(Array.isArray(data.permissions) ? data.permissions : []);
                    setRoles(Array.isArray(data.roles) ? data.roles : []);
                    setPagePermissions(data.pagePermissions || {});
                }
            } catch (error) {
                if (isMounted) {
                    setPermissions([]);
                    setRoles([]);
                    setPagePermissions({});
                }
            } finally {
                if (isMounted) {
                    setLoadingPermissions(false);
                }
            }
        };

        loadPermissions();

        return () => {
            isMounted = false;
        };
    }, [user, authLoading]);

    const reloadPermissions = async () => {
        if (!user) return;
        setLoadingPermissions(true);
        try {
            const response = await userPermissionService.GetUserMenuPermissions();
            const data = response.data?.data || response.data;
            setPermissions(Array.isArray(data.permissions) ? data.permissions : []);
            setRoles(Array.isArray(data.roles) ? data.roles : []);
            setPagePermissions(data.pagePermissions || {});
        } catch (error) {
            setPermissions([]);
            setRoles([]);
            setPagePermissions({});
        } finally {
            setLoadingPermissions(false);
        }
    };

    const hasGlobalPermission = (perm: string) => permissions.includes(perm);

    const hasPagePermission = (perm: string) => {
        const p = pagePermissions[perm];
        return p?.read || p?.edit || p?.delete;
    };

    const hasRole = (role: string) => roles.includes(role);

    return (
        <PermissionContext.Provider
            value={{
                permissions,
                roles,
                pagePermissions,
                loadingPermissions,
                hasGlobalPermission,
                hasPagePermission,
                hasRole,
                reloadPermissions,
            }}
        >
            {children}
        </PermissionContext.Provider>
    );
};

export const usePermissions = () => {
    const context = useContext(PermissionContext);
    if (context === undefined) {
        throw new Error("usePermissions must be used within a PermissionProvider");
    }
    return context;
};
