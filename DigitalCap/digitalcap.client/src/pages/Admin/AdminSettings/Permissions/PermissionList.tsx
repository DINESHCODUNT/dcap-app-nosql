import React, { useEffect, useMemo } from 'react';
import { AbsSelect, AbsSearch, CheckboxInput, Label } from "customer_portal-ui-shared";
import AppLoader from '../../../../Common/AppLoader';
import * as userPermissionService from '../../../../service/UserPermissionService';
import { usePopup } from '../../../../Common/PopupProvider';

interface RoleOption {
    id: string | number;
    name: string;
}

interface Permission {
    id: string | number | null;
    permissionId: string | number;
    permissionName: string;
    isReadable: boolean;
    isEditable: boolean;
    isDeleted: boolean;
    readExist: boolean;
    editExist: boolean;
    deleteExist: boolean;
}

type FormData = {
    user_role: string;
};

const PermissionList: React.FC = () => {
    const popup = usePopup();
    const [val, updateVal] = React.useState<string>("");
    const [searchApplied, updateSearchApplied] = React.useState<string | null>(null);
    const [loading, setLoading] = React.useState<boolean>(false);
    const [roleId, setRoleId] = React.useState<string | number | null>(null);
    const [roles, setRoles] = React.useState<RoleOption[]>([]);
    const [permissions, setPermissions] = React.useState<Permission[]>([]);
    const [formData, setFormData] = React.useState<FormData>({
        user_role: "",
    });

    useEffect(() => {
        loadUserRoles();
    }, []);

    useEffect(() => {
        if (roleId) {
            loadRolePermissions(roleId);
        } else {
            setPermissions([]);
        }
    }, [roleId]);

    const loadUserRoles = async () => {
        try {
            setLoading(true);
            const response = await userPermissionService.getUserRoles();
            const rolesData = response.data || [];
            const formattedRoles = rolesData.map((role: any) => ({
                id: role.id || role.Id,
                name: role.name || role.Name
            }));
            setRoles(formattedRoles);
        } catch (error) {
        } finally {
            setLoading(false);
        }
    };

    const loadRolePermissions = async (selectedRoleId: string | number) => {
        try {
            setLoading(true);
            const response = await userPermissionService.getRolePermission(selectedRoleId);
            const permissionsData = response.data || [];
            setPermissions(permissionsData);
        } catch (error) {
        } finally {
            setLoading(false);
        }
    };

    const handleRoleChange = (selected: string[] | any) => {
        const selectedRoleId = selected && selected[0] ? selected[0] : "";
        setFormData({ ...formData, user_role: selectedRoleId });
        setRoleId(selectedRoleId || null);
    };

    const handleCheckboxChange = async (
        permissionId: string | number,
        id: string | number | null,
        currentValue: boolean,
        key: "read" | "edit" | "delete"
    ) => {
        if (!roleId) return;

        const newValue = !currentValue;

        try {
            const response = await userPermissionService.updatePermission({
                roleId: roleId,
                permissionId: permissionId,
                id: id,
                value: newValue,
                key: key
            });
            if (response.data) {
                setPermissions(response.data.data);
            } else {
                setPermissions(prev => prev.map(perm => {
                    if (perm.permissionId === permissionId) {
                        if (key === "read") {
                            return { ...perm, isReadable: newValue };
                        } else if (key === "edit") {
                            return { ...perm, isEditable: newValue };
                        } else if (key === "delete") {
                            return { ...perm, isDeleted: newValue };
                        }
                    }
                    return perm;
                }));
            }
        } catch (error: any) {
            popup.error(error?.response?.data || error?.message || "Unable to update the permission. Please try again.", "Permissions");
        }
    };

    const filteredPermissions = useMemo(() => {
        if (!searchApplied || searchApplied.trim() === "") {
            return permissions;
        }
        const searchLower = searchApplied.toLowerCase().trim();
        return permissions.filter(perm =>
            perm.permissionName?.toLowerCase().includes(searchLower)
        );
    }, [permissions, searchApplied]);

    const roleOptions = roles.map(role => ({
        text: role.name,
        value: String(role.id)
    }));


    return (
        <>
            <div className="row">
                <div className="col-md-8">
                    <h5 className='_600'> Manage Permission</h5>

                </div>
                <div className="col-md-6 mb-3 mt-2">
                    <Label name="Select User Role" bold />
                    <AbsSelect
                        mult={false}
                        onChange={handleRoleChange}
                        options={roleOptions}
                        selected={formData.user_role ? [formData.user_role] : []}
                        placeholder="Select User Role"
                    />
                </div>
                <div className="col-md-6  d-flex justify-content-end mt-4">
                    <div className="d-flex justify-content-end align-items-center gap-2">
                        <AbsSearch
                            handleClear={() => {
                                updateVal("");
                                updateSearchApplied(null);
                            }}
                            value={val}
                            handleChange={(e: any) => {
                                const value = e.target.value;
                                updateVal(value);

                                if (value.trim() === "") {
                                    updateSearchApplied(null);
                                } else {
                                    updateSearchApplied(value);
                                }
                            }}
                            handleSearch={() => {
                                updateSearchApplied(val === "" ? null : val);
                            }}
                            placeholder="Search by page name"
                            searchApplied={searchApplied !== null}
                        />
                        <a
                            href="#"
                            className='text-link text-nowrap _500'
                            onClick={(e) => {
                                e.preventDefault();
                                updateVal("");
                                updateSearchApplied(null);
                            }}
                        >
                            Clear Filter
                        </a>
                    </div>
                </div>
            </div>
            {loading && (
                <AppLoader inline />
            )}
            {roleId && (
                <div className="custom-react-table">
                    <table className='table table-bordered custom-table'>
                        <thead>
                            <tr>
                                <th>Pages</th>
                                <th>View</th>
                                <th>Create/Edit</th>
                                <th>Delete</th>
                            </tr>
                        </thead>
                        <tbody>
                            {filteredPermissions.length > 0 ? (
                                filteredPermissions.map((permission) => (
                                    <tr key={permission.permissionId}>
                                        <td>{permission.permissionName}</td>
                                        <td>
                                            <CheckboxInput
                                                label=" "
                                                checked={permission.isReadable}
                                                onChange={() =>
                                                    handleCheckboxChange(
                                                        permission.permissionId,
                                                        permission.id,
                                                        permission.isReadable,
                                                        "read"
                                                    )
                                                }
                                                disabled={!permission.readExist}
                                            />
                                        </td>
                                        <td>
                                            <CheckboxInput
                                                label=" "
                                                checked={permission.isEditable}
                                                onChange={() =>
                                                    handleCheckboxChange(
                                                        permission.permissionId,
                                                        permission.id,
                                                        permission.isEditable,
                                                        "edit"
                                                    )
                                                }
                                                disabled={!permission.editExist}
                                            />
                                        </td>
                                        <td>
                                            <CheckboxInput
                                                label=" "
                                                checked={permission.isDeleted}
                                                onChange={() =>
                                                    handleCheckboxChange(
                                                        permission.permissionId,
                                                        permission.id,
                                                        permission.isDeleted,
                                                        "delete"
                                                    )
                                                }
                                                disabled={!permission.deleteExist}
                                            />
                                        </td>
                                    </tr>
                                ))
                            ) : (
                                <tr>
                                    <td colSpan={4} className="text-center">
                                        {searchApplied ? "No permissions found matching your search." : "No permissions available."}
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            )}        </>
    );
};

export default PermissionList;