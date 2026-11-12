import React, { useState, useEffect, useCallback } from "react";
import { Offcanvas } from "react-bootstrap";
import { useSearchParams } from "react-router-dom";
import {
    Button,
    BUTTONS,
    Divider,
    Input,
    AbsSelect,
    Label,
} from "customer_portal-ui-shared";
import * as absUserService from '../../../../service/AbsUserService';
import type { AccountViewModel, UserAccountModel } from '../../../../service/AbsUserService';
import * as userPermissionService from '../../../../service/UserPermissionService';
import { ROLES } from '../../../File/appConstants';
import { toFriendlyErrorMessage } from '../../../../utils/errorMessage';

export interface CapUser {
    id: number;
    aspNetUserId: string;
    firstName: string;
    lastName: string;
    email: string;
    userRole?: string;
    timeZone: string;
    phone: number | string;
    company: string;
}

type FormData = {
    aspNetUserId: string;
    first_name: string;
    last_name: string;
    email: string;
    user_role: string;
    time_zone: string;
    phone: number | string;
    company: string;
};

interface RoleOption {
    id: string | number;
    name: string;
}

type Props = {
    show: boolean;
    onHide: () => void;
    onSaved: (action: "CreateUser" | "UpdateUser") => void;
    onError: (message: string) => void;
    editUser?: CapUser | null;
    isClientUsers: boolean;
    company?: string;
    clientId?: string | number;
    userRole?: string;
    isMyProfile?: boolean;
    className?: string;
    backdropClassName?: string;
    existingUsers?: CapUser[];
};

const getDefaultTimeZones = (): Array<{ text: string; value: string }> => {
    return [
        { text: "(UTC-12:00) International Date Line West", value: "Dateline Standard Time" },
        { text: "(UTC-11:00) Coordinated Universal Time-11", value: "UTC-11" },
        { text: "(UTC-10:00) Aleutian Islands", value: "Aleutian Standard Time" },
        { text: "(UTC-10:00) Hawaii", value: "Hawaiian Standard Time" },
        { text: "(UTC-09:30) Marquesas Islands", value: "Marquesas Standard Time" },
        { text: "(UTC-09:00) Alaska", value: "Alaskan Standard Time" },
        { text: "(UTC-09:00) Coordinated Universal Time-09", value: "UTC-09" },
        { text: "(UTC-08:00) Baja California", value: "Pacific Standard Time (Mexico)" },
        { text: "(UTC-08:00) Coordinated Universal Time-08", value: "UTC-08" },
        { text: "(UTC-08:00) Pacific Time (US & Canada)", value: "Pacific Standard Time" },
        { text: "(UTC-07:00) Arizona", value: "US Mountain Standard Time" },
        { text: "(UTC-07:00) Chihuahua, La Paz, Mazatlan", value: "Mountain Standard Time (Mexico)" },
        { text: "(UTC-07:00) Mountain Time (US & Canada)", value: "Mountain Standard Time" },
        { text: "(UTC-06:00) Central America", value: "Central America Standard Time" },
        { text: "(UTC-06:00) Central Time (US & Canada)", value: "Central Standard Time" },
        { text: "(UTC-06:00) Easter Island", value: "Easter Island Standard Time" },
        { text: "(UTC-06:00) Guadalajara, Mexico City, Monterrey", value: "Central Standard Time (Mexico)" },
        { text: "(UTC-06:00) Saskatchewan", value: "Canada Central Standard Time" },
        { text: "(UTC-05:00) Bogota, Lima, Quito, Rio Branco", value: "SA Pacific Standard Time" },
        { text: "(UTC-05:00) Chetumal", value: "Eastern Standard Time (Mexico)" },
        { text: "(UTC-05:00) Eastern Time (US & Canada)", value: "Eastern Standard Time" },
        { text: "(UTC-05:00) Haiti", value: "Haiti Standard Time" },
        { text: "(UTC-05:00) Havana", value: "Cuba Standard Time" },
        { text: "(UTC-05:00) Indiana (East)", value: "US Eastern Standard Time" },
        { text: "(UTC-04:00) Asuncion", value: "Paraguay Standard Time" },
        { text: "(UTC-04:00) Atlantic Time (Canada)", value: "Atlantic Standard Time" },
        { text: "(UTC-04:00) Caracas", value: "Venezuela Standard Time" },
        { text: "(UTC-04:00) Cuiaba", value: "Central Brazilian Standard Time" },
        { text: "(UTC-04:00) Georgetown, La Paz, Manaus, San Juan", value: "SA Western Standard Time" },
        { text: "(UTC-04:00) Santiago", value: "Pacific SA Standard Time" },
        { text: "(UTC-03:30) Newfoundland", value: "Newfoundland Standard Time" },
        { text: "(UTC-03:00) Araguaina", value: "Tocantins Standard Time" },
        { text: "(UTC-03:00) Brasilia", value: "E. South America Standard Time" },
        { text: "(UTC-03:00) Cayenne, Fortaleza", value: "SA Eastern Standard Time" },
        { text: "(UTC-03:00) City of Buenos Aires", value: "Argentina Standard Time" },
        { text: "(UTC-03:00) Greenland", value: "Greenland Standard Time" },
        { text: "(UTC-03:00) Montevideo", value: "Montevideo Standard Time" },
        { text: "(UTC-03:00) Punta Arenas", value: "Magallanes Standard Time" },
        { text: "(UTC-03:00) Saint Pierre and Miquelon", value: "Saint Pierre Standard Time" },
        { text: "(UTC-03:00) Salvador", value: "Bahia Standard Time" },
        { text: "(UTC-02:00) Coordinated Universal Time-02", value: "UTC-02" },
        { text: "(UTC-02:00) Mid-Atlantic - Old", value: "Mid-Atlantic Standard Time" },
        { text: "(UTC-01:00) Azores", value: "Azores Standard Time" },
        { text: "(UTC-01:00) Cabo Verde Is.", value: "Cape Verde Standard Time" },
        { text: "(UTC+00:00) Casablanca", value: "Morocco Standard Time" },
        { text: "(UTC+00:00) Coordinated Universal Time", value: "UTC" },
        { text: "(UTC+00:00) Dublin, Edinburgh, Lisbon, London", value: "GMT Standard Time" },
        { text: "(UTC+00:00) Monrovia, Reykjavik", value: "Greenwich Standard Time" },
        { text: "(UTC+01:00) Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna", value: "W. Europe Standard Time" },
        { text: "(UTC+01:00) Belgrade, Bratislava, Budapest, Ljubljana, Prague", value: "Central Europe Standard Time" },
        { text: "(UTC+01:00) Brussels, Copenhagen, Madrid, Paris", value: "Romance Standard Time" },
        { text: "(UTC+01:00) Sarajevo, Skopje, Warsaw, Zagreb", value: "Central European Standard Time" },
        { text: "(UTC+01:00) West Central Africa", value: "W. Central Africa Standard Time" },
        { text: "(UTC+02:00) Amman", value: "Jordan Standard Time" },
        { text: "(UTC+02:00) Athens, Bucharest", value: "GTB Standard Time" },
        { text: "(UTC+02:00) Beirut", value: "Middle East Standard Time" },
        { text: "(UTC+02:00) Cairo", value: "Egypt Standard Time" },
        { text: "(UTC+02:00) Chisinau", value: "E. Europe Standard Time" },
        { text: "(UTC+02:00) Damascus", value: "Syria Standard Time" },
        { text: "(UTC+02:00) Gaza, Hebron", value: "West Bank Standard Time" },
        { text: "(UTC+02:00) Harare, Pretoria", value: "South Africa Standard Time" },
        { text: "(UTC+02:00) Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius", value: "FLE Standard Time" },
        { text: "(UTC+02:00) Jerusalem", value: "Israel Standard Time" },
        { text: "(UTC+02:00) Kaliningrad", value: "Kaliningrad Standard Time" },
        { text: "(UTC+02:00) Tripoli", value: "Libya Standard Time" },
        { text: "(UTC+02:00) Windhoek", value: "Namibia Standard Time" },
        { text: "(UTC+03:00) Baghdad", value: "Arabic Standard Time" },
        { text: "(UTC+03:00) Istanbul", value: "Turkey Standard Time" },
        { text: "(UTC+03:00) Kuwait, Riyadh", value: "Arab Standard Time" },
        { text: "(UTC+03:00) Minsk", value: "Belarus Standard Time" },
        { text: "(UTC+03:00) Moscow, St. Petersburg", value: "Russian Standard Time" },
        { text: "(UTC+03:00) Nairobi", value: "E. Africa Standard Time" },
        { text: "(UTC+03:30) Tehran", value: "Iran Standard Time" },
        { text: "(UTC+04:00) Abu Dhabi, Muscat", value: "Arabian Standard Time" },
        { text: "(UTC+04:00) Astrakhan, Ulyanovsk", value: "Astrakhan Standard Time" },
        { text: "(UTC+04:00) Baku", value: "Azerbaijan Standard Time" },
        { text: "(UTC+04:00) Izhevsk, Samara", value: "Russia Time Zone 3" },
        { text: "(UTC+04:00) Port Louis", value: "Mauritius Standard Time" },
        { text: "(UTC+04:00) Saratov", value: "Saratov Standard Time" },
        { text: "(UTC+04:00) Tbilisi", value: "Georgian Standard Time" },
        { text: "(UTC+04:00) Volgograd", value: "Volgograd Standard Time" },
        { text: "(UTC+04:30) Kabul", value: "Afghanistan Standard Time" },
        { text: "(UTC+05:00) Ashgabat, Tashkent", value: "West Asia Standard Time" },
        { text: "(UTC+05:00) Ekaterinburg", value: "Ekaterinburg Standard Time" },
        { text: "(UTC+05:00) Islamabad, Karachi", value: "Pakistan Standard Time" },
        { text: "(UTC+05:30) Chennai, Kolkata, Mumbai, New Delhi", value: "India Standard Time" },
        { text: "(UTC+05:30) Sri Jayawardenepura", value: "Sri Lanka Standard Time" },
        { text: "(UTC+05:45) Kathmandu", value: "Nepal Standard Time" },
        { text: "(UTC+06:00) Astana", value: "Central Asia Standard Time" },
        { text: "(UTC+06:00) Dhaka", value: "Bangladesh Standard Time" },
        { text: "(UTC+06:00) Omsk", value: "Omsk Standard Time" },
        { text: "(UTC+06:30) Yangon (Rangoon)", value: "Myanmar Standard Time" },
        { text: "(UTC+07:00) Bangkok, Hanoi, Jakarta", value: "SE Asia Standard Time" },
        { text: "(UTC+07:00) Barnaul, Gorno-Altaysk", value: "Altai Standard Time" },
        { text: "(UTC+07:00) Hovd", value: "W. Mongolia Standard Time" },
        { text: "(UTC+07:00) Krasnoyarsk", value: "North Asia Standard Time" },
        { text: "(UTC+07:00) Novosibirsk", value: "N. Central Asia Standard Time" },
        { text: "(UTC+07:00) Tomsk", value: "Tomsk Standard Time" },
        { text: "(UTC+08:00) Beijing, Chongqing, Hong Kong, Urumqi", value: "China Standard Time" },
        { text: "(UTC+08:00) Irkutsk", value: "North Asia East Standard Time" },
        { text: "(UTC+08:00) Kuala Lumpur, Singapore", value: "Singapore Standard Time" },
        { text: "(UTC+08:00) Perth", value: "W. Australia Standard Time" },
        { text: "(UTC+08:00) Taipei", value: "Taipei Standard Time" },
        { text: "(UTC+08:00) Ulaanbaatar", value: "Ulaanbaatar Standard Time" },
        { text: "(UTC+08:45) Eucla", value: "Aus Central W. Standard Time" },
        { text: "(UTC+09:00) Chita", value: "Transbaikal Standard Time" },
        { text: "(UTC+09:00) Osaka, Sapporo, Tokyo", value: "Tokyo Standard Time" },
        { text: "(UTC+09:00) Pyongyang", value: "North Korea Standard Time" },
        { text: "(UTC+09:00) Seoul", value: "Korea Standard Time" },
        { text: "(UTC+09:00) Yakutsk", value: "Yakutsk Standard Time" },
        { text: "(UTC+09:30) Adelaide", value: "Cen. Australia Standard Time" },
        { text: "(UTC+09:30) Darwin", value: "AUS Central Standard Time" },
        { text: "(UTC+10:00) Brisbane", value: "E. Australia Standard Time" },
        { text: "(UTC+10:00) Canberra, Melbourne, Sydney", value: "AUS Eastern Standard Time" },
        { text: "(UTC+10:00) Guam, Port Moresby", value: "West Pacific Standard Time" },
        { text: "(UTC+10:00) Hobart", value: "Tasmania Standard Time" },
        { text: "(UTC+10:00) Vladivostok", value: "Vladivostok Standard Time" },
        { text: "(UTC+10:30) Lord Howe Island", value: "Lord Howe Standard Time" },
        { text: "(UTC+11:00) Bougainville Island", value: "Bougainville Standard Time" },
        { text: "(UTC+11:00) Chokurdakh", value: "Russia Time Zone 10" },
        { text: "(UTC+11:00) Magadan", value: "Magadan Standard Time" },
        { text: "(UTC+11:00) Norfolk Island", value: "Norfolk Standard Time" },
        { text: "(UTC+11:00) Sakhalin", value: "Sakhalin Standard Time" },
        { text: "(UTC+11:00) Solomon Is., New Caledonia", value: "Central Pacific Standard Time" },
        { text: "(UTC+12:00) Anadyr, Petropavlovsk-Kamchatsky", value: "Russia Time Zone 11" },
        { text: "(UTC+12:00) Auckland, Wellington", value: "New Zealand Standard Time" },
        { text: "(UTC+12:00) Coordinated Universal Time+12", value: "UTC+12" },
        { text: "(UTC+12:00) Fiji", value: "Fiji Standard Time" },
        { text: "(UTC+12:00) Petropavlovsk-Kamchatsky - Old", value: "Kamchatka Standard Time" },
        { text: "(UTC+12:45) Chatham Islands", value: "Chatham Islands Standard Time" },
        { text: "(UTC+13:00) Coordinated Universal Time+13", value: "UTC+13" },
        { text: "(UTC+13:00) Nuku'alofa", value: "Tonga Standard Time" },
        { text: "(UTC+13:00) Samoa", value: "Samoa Standard Time" },
        { text: "(UTC+14:00) Kiritimati Island", value: "Line Islands Standard Time" }
    ];
};

const AddUser: React.FC<Props> = ({
    show,
    onHide,
    onSaved,
    onError,
    editUser,
    isClientUsers,
    company: companyProp,
    clientId: clientIdProp,
    userRole: userRoleProp,
    isMyProfile = false,
    className,
    backdropClassName,
    existingUsers,
}) => {
    const [searchParams] = useSearchParams();
    const company = companyProp || searchParams.get('company') || '';
    const clientId = clientIdProp != null && clientIdProp !== ''
        ? String(clientIdProp)
        : (searchParams.get('clientId') || '');
    const userRole = userRoleProp || searchParams.get('role') || searchParams.get('userRole') || '';
    const shouldPrefillCompanyAndRole = !!(company && userRole);

    const [roles, setRoles] = useState<RoleOption[]>([]);
    const [formData, setFormData] = useState<FormData>({
        aspNetUserId: "",
        first_name: "",
        last_name: "",
        email: "",
        user_role: "",
        time_zone: "India Standard Time",
        phone: "",
        company: "",
    });
    const [formErrors, setFormErrors] = useState<{ [key: string]: string }>({});
    const [isSaving, setIsSaving] = useState<boolean>(false);
    const [timeZoneOptions, setTimeZoneOptions] = useState<Array<{ text: string; value: string }>>([]);
    const [loadingTimeZones, setLoadingTimeZones] = useState<boolean>(false);
    const [editingUserId, setEditingUserId] = useState<string | null>(null);
    const [editingUserData, setEditingUserData] = useState<CapUser | null>(null);
    const [isCompanyFromUrl, setIsCompanyFromUrl] = useState<boolean>(false);
    const [isRoleFromUrl, setIsRoleFromUrl] = useState<boolean>(false);
    const [profileRoleLabel, setProfileRoleLabel] = useState<string>("");

    const roleOptions = roles.map(role => ({
        text: role.name,
        value: String(role.id)
    }));

    const getRoleNameFromId = (roleId: string): string => {
        if (!roleId) return "";
        const role = roles.find(r => String(r.id) === String(roleId));
        return role?.name || roleId;
    };

    const getRoleDisplayName = (): string => {
        if (formData.user_role) {
            const matched = roles.find(
                r => String(r.id) === String(formData.user_role) || r.name === formData.user_role
            );
            if (matched?.name) return matched.name;
        }
        return profileRoleLabel || userRole || formData.user_role || "";
    };

    const loadUserRoles = async () => {
        try {
            const response = await userPermissionService.getUserRoles();
            const raw = response.data?.data ?? response.data;
            const rolesData = Array.isArray(raw) ? raw : [];
            const formattedRoles = rolesData.map((role: any) => ({
                id: role.id || role.Id,
                name: role.name || role.Name
            }));
            setRoles(formattedRoles);
        } catch (error) {
        }
    };

    const loadTimeZones = async () => {
        setLoadingTimeZones(true);
        try {
            const clientTz = Intl.DateTimeFormat().resolvedOptions().timeZone;
            const response = await absUserService.getTimeZones(clientTz);

            let timeZonesData: any[] = [];
            if (Array.isArray(response.data)) {
                timeZonesData = response.data;
            }

            const options = timeZonesData.map((tz: any) => ({
                text: tz.name || tz.displayName || tz.timeZone || `${tz.id} - ${tz.name}`,
                value: String(tz.id || tz.value || tz.timeZoneId || tz.name)
            }));

            if (options.length > 0) {
                setTimeZoneOptions(options);
            } else {
                setTimeZoneOptions(getDefaultTimeZones());
            }
        } catch (error) {
            setTimeZoneOptions(getDefaultTimeZones());
        } finally {
            setLoadingTimeZones(false);
        }
    };

    const resetForm = useCallback(() => {
        const defaultCompany = company || "ABS";
        const defaultTimeZone = "India Standard Time";

        setFormData({
            aspNetUserId: "",
            first_name: "",
            last_name: "",
            email: "",
            user_role: "",
            time_zone: defaultTimeZone,
            phone: "",
            company: defaultCompany,
        });
        setFormErrors({});
        setEditingUserId(null);
        setEditingUserData(null);
        setIsCompanyFromUrl(false);
        setIsRoleFromUrl(false);
        setProfileRoleLabel("");
    }, [company]);

    useEffect(() => {
        loadUserRoles();
    }, []);

    useEffect(() => {
        if (show && timeZoneOptions.length === 0) {
            loadTimeZones();
        }
    }, [show]);

    useEffect(() => {
        if (!show) return;

        if (editUser) {
            setFormErrors({});
            setEditingUserId(editUser.aspNetUserId);
            setEditingUserData(editUser);

            let roleIdValue = "";
            if (editUser.userRole && roles.length > 0) {
                const matchedRole = roles.find(role =>
                    role.name === editUser.userRole ||
                    String(role.id) === String(editUser.userRole)
                );
                if (matchedRole) {
                    roleIdValue = String(matchedRole.id);
                }
            }

            setFormData({
                aspNetUserId: editUser.aspNetUserId || "",
                first_name: editUser.firstName || "",
                last_name: editUser.lastName || "",
                email: editUser.email || "",
                user_role: roleIdValue || editUser.userRole || "",
                time_zone: editUser.timeZone || "",
                phone: editUser.phone || "",
                company: editUser.company || company || "",
            });
            setIsCompanyFromUrl(false);
            if (isMyProfile) {
                setIsRoleFromUrl(true);
                setProfileRoleLabel(editUser.userRole || "");
            } else {
                setIsRoleFromUrl(false);
                setProfileRoleLabel("");
            }
            return;
        }

        if (isMyProfile) return;

        if (shouldPrefillCompanyAndRole) {
            let roleIdValue = userRole;
            if (roles.length > 0) {
                const matchedRole = roles.find(role =>
                    role.name === userRole ||
                    String(role.id) === String(userRole)
                );
                if (matchedRole) {
                    roleIdValue = String(matchedRole.id);
                }
            }

            setFormData(prev => ({
                ...prev,
                company: company,
                user_role: roleIdValue
            }));
            setIsCompanyFromUrl(!!company);
            setIsRoleFromUrl(true);
            setProfileRoleLabel(userRole);
            setEditingUserId(null);
            setEditingUserData(null);
        } else {
            resetForm();
        }
    }, [show, isMyProfile, editUser, roles.length, shouldPrefillCompanyAndRole, company, userRole, resetForm]);

    useEffect(() => {
        if (editingUserData && timeZoneOptions.length > 0 && editingUserId) {
            const originalTimeZone = editingUserData.timeZone;
            if (originalTimeZone) {
                let matchedTz: (typeof timeZoneOptions)[number] | null | undefined = null;

                matchedTz = timeZoneOptions.find(tz =>
                    String(tz.value || "").toLowerCase().trim() === String(originalTimeZone || "").toLowerCase().trim()
                );

                if (!matchedTz) {
                    matchedTz = timeZoneOptions.find(tz =>
                        String(tz.text || "").toLowerCase().trim() === String(originalTimeZone || "").toLowerCase().trim()
                    );
                }

                if (!matchedTz) {
                    matchedTz = timeZoneOptions.find(tz =>
                        String(tz.value || "").trim() === String(originalTimeZone || "").trim()
                    );
                }

                if (!matchedTz) {
                    matchedTz = timeZoneOptions.find(tz =>
                        String(tz.text || "").trim() === String(originalTimeZone || "").trim()
                    );
                }

                if (!matchedTz) {
                    const originalTzLower = String(originalTimeZone || "").toLowerCase().trim();
                    matchedTz = timeZoneOptions.find(tz => {
                        const tzValue = String(tz.value || "").toLowerCase().trim();
                        const tzText = String(tz.text || "").toLowerCase().trim();

                        if (tzValue === originalTzLower || tzText === originalTzLower) return true;

                        if (tzValue.startsWith(originalTzLower) || originalTzLower.startsWith(tzValue)) {
                            const tzValueBase = tzValue.replace(/standard time$/i, "").trim();
                            const originalBase = originalTzLower.replace(/standard time$/i, "").trim();
                            if (tzValueBase === originalBase) return true;
                        }

                        return false;
                    });
                }

                if (matchedTz) {
                    setFormData(prev => ({
                        ...prev,
                        time_zone: matchedTz!.value
                    }));
                } else {
                    setFormData(prev => ({
                        ...prev,
                        time_zone: originalTimeZone
                    }));
                }
            }
        }
    }, [timeZoneOptions, editingUserId, editingUserData]);

    useEffect(() => {
        if (show && !editingUserId && timeZoneOptions.length > 0) {
            const defaultTz = timeZoneOptions.find(tz =>
                tz.value === "India Standard Time" ||
                tz.text.includes("Chennai") ||
                tz.text.includes("India") ||
                tz.value.includes("India")
            );
            if (defaultTz) {
                setFormData(prev => {
                    if (prev.time_zone === defaultTz.value) return prev;
                    return { ...prev, time_zone: defaultTz.value };
                });
            }
        }
    }, [timeZoneOptions, show, editingUserId]);

    useEffect(() => {
        if (editingUserData && roles.length > 0 && editingUserId) {
            const originalRole = editingUserData.userRole;
            if (originalRole) {
                const matchedRole = roles.find(role =>
                    role.name === originalRole ||
                    String(role.id) === String(originalRole)
                );
                if (matchedRole) {
                    const roleIdStr = String(matchedRole.id);
                    setFormData(prev => {
                        if (prev.user_role === roleIdStr) return prev;
                        return { ...prev, user_role: roleIdStr };
                    });
                }
            }
        }
    }, [roles, editingUserId, editingUserData]);

    const validateForm = () => {
        const errors: any = {};

        if (!formData.first_name.trim())
            errors.first_name = "First name required";

        if (!formData.last_name.trim())
            errors.last_name = "Last name required";

        if (!formData.email.trim())
            errors.email = "Email required";
        else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email))
            errors.email = "Invalid email";
        else if (existingUsers && existingUsers.length > 0) {
            const trimmedEmail = formData.email.trim().toLowerCase();
            const isDuplicate = existingUsers.some(
                u => (u.email || "").trim().toLowerCase() === trimmedEmail &&
                     (!editingUserId || u.aspNetUserId !== editingUserId)
            );
            if (isDuplicate) {
                errors.email = "This email address already exists. Please use a different email address.";
            }
        }

        if (!formData.time_zone)
            errors.time_zone = "Time zone required";

        if (!formData.user_role && !isMyProfile)
            errors.user_role = "Role required";

        setFormErrors(errors);

        return Object.keys(errors).length === 0;
    };

    const getResolvedRoleName = () => getRoleNameFromId(formData.user_role);

    const isClientAdminContext = () => {
        const roleName = getResolvedRoleName() || userRole;
        return roleName === ROLES.CLIENT_ADMIN;
    };

    const buildAccountViewModel = (isEdit: boolean): AccountViewModel => {
        const roleName = getResolvedRoleName();
        const isClientAdmin = isClientAdminContext();
        const defaultCompany = isClientUsers && !isClientAdmin
            ? (formData.company || company || "")
            : (formData.company || company || "ABS");

        const userAccountData: UserAccountModel = {
            FirstName: formData.first_name.trim(),
            LastName: formData.last_name.trim(),
            Phone: formData.phone ? String(formData.phone) : "",
            TimeZone: formData.time_zone || "",
            Company: defaultCompany,
            Email: formData.email.trim(),
            AspNetUserId: isEdit ? (editingUserId || "") : "",
            Title: "",
            ProfileImageUrl: ""
        };

        if (clientId && clientId.trim() !== '') {
            userAccountData.ClientId = clientId;
        }

        return {
            Id: isEdit ? (editingUserData?.id?.toString() || "") : "",
            Email: formData.email.trim(),
            UpdatedRole: roleName,
            AbsUser: !isClientUsers && !isClientAdmin,
            ClientUser: isClientUsers && !isClientAdmin,
            NewUser: !isEdit,
            Company: defaultCompany,
            Title: "",
            Clients: [],
            Password: "",
            ConfirmPassword: "",
            SaveText: "Save",
            CancelUrl: "",
            AspNetUser: isEdit ? (editingUserId || "") : "",
            Controller: "",
            PostAction: "",
            DisplayRole: roleName,
            UserAccount: userAccountData
        };
    };

    const handleSave = async () => {
        if (!validateForm()) return;

        setIsSaving(true);
        setFormErrors({});

        try {
            const isEdit = !!(editingUserId && editingUserData);
            const useClientUserApi = isClientUsers && !isClientAdminContext();
            const accountViewModel = buildAccountViewModel(isEdit);

            await absUserService.saveCapUser(accountViewModel, {
                isClientUsers: useClientUserApi,
                isEdit,
            });

            resetForm();
            onHide();
            onSaved(isEdit ? "UpdateUser" : "CreateUser");
        } catch (error: any) {
            let rawMsg = "";
            if (typeof error?.response?.data === "string") {
                rawMsg = error.response.data;
            } else if (error?.response?.data?.message) {
                rawMsg = error.response.data.message;
            } else if (error?.response?.data?.errors) {
                const errorsObj = error.response.data.errors;
                const firstKey = Object.keys(errorsObj)[0];
                rawMsg = Array.isArray(errorsObj[firstKey]) ? errorsObj[firstKey][0] : String(errorsObj[firstKey]);
            } else {
                rawMsg = error?.response?.data?.title || error?.message || "Something went wrong";
            }
            rawMsg = toFriendlyErrorMessage(rawMsg);

            const isDuplicateEmail =
                rawMsg.toLowerCase().includes("email") &&
                (rawMsg.toLowerCase().includes("exist") || rawMsg.toLowerCase().includes("already"));

            if (isDuplicateEmail) {
                const validationMsg = "This email address already exists. Please use a different email address.";
                setFormErrors(prev => ({
                    ...prev,
                    email: validationMsg,
                    submit: ""
                }));
            } else {
                setFormErrors({ submit: rawMsg });
                onError(rawMsg);
            }
        } finally {
            setIsSaving(false);
        }
    };

    const handleCancel = () => {
        resetForm();
        onHide();
    };

    const handleRoleChange = (selected: string[] | any) => {
        const selectedRoleId = selected && selected[0] ? selected[0] : "";

        setFormData(prev => ({
            ...prev,
            user_role: selectedRoleId
        }));
        if (formErrors.user_role) {
            setFormErrors(prev => ({ ...prev, user_role: "" }));
        }
    };

    return (
        <Offcanvas
            show={show}
            placement="end"
            onHide={handleCancel}
            backdrop="static"
            className={className}
            backdropClassName={backdropClassName}
        >
            <Offcanvas.Header closeButton>
                <div>
                    <Offcanvas.Title>
                        {isMyProfile ? "My Profile" : editingUserId ? "Edit User" : "Add New User"}
                    </Offcanvas.Title>

                    {(editingUserId || isMyProfile) && (
                        <div className="text-muted tx-12 mt-1">
                            {editingUserData?.email || formData.email}
                        </div>
                    )}
                </div>
            </Offcanvas.Header>

            <Offcanvas.Body>
                <>
                    {formErrors.submit && (
                        <div className="alert alert-danger">
                            {formErrors.submit}
                        </div>
                    )}

                    <div className="row">

                        <div className="col-md-12 form-group">
                            <Label name="First Name" bold required />

                            <Input
                                value={formData.first_name}
                                placeholder="Enter First Name"
                                onChange={(value: string) => {
                                    setFormData(prev => ({
                                        ...prev,
                                        first_name: value
                                    }));

                                    setFormErrors(prev => ({
                                        ...prev,
                                        first_name: ""
                                    }));
                                }}
                            />

                            {formErrors.first_name &&
                                <span className="text-danger">
                                    {formErrors.first_name}
                                </span>}
                        </div>

                        <div className="col-md-12 form-group">

                            <Label name="Last Name" bold required />

                            <Input
                                value={formData.last_name}
                                placeholder="Enter Last Name"
                                onChange={(value: string) => {
                                    setFormData(prev => ({
                                        ...prev,
                                        last_name: value
                                    }));

                                    setFormErrors(prev => ({
                                        ...prev,
                                        last_name: ""
                                    }));
                                }}
                            />

                            {formErrors.last_name &&
                                <span className="text-danger">
                                    {formErrors.last_name}
                                </span>}
                        </div>

                        <div className="col-md-12 form-group">

                            <Label name="Select Time Zone" bold required />

                            {loadingTimeZones ? (
                                <div>Loading...</div>
                            ) : (
                                <AbsSelect
                                    mult={false}
                                    options={timeZoneOptions}
                                    selected={
                                        formData.time_zone
                                            ? [formData.time_zone]
                                            : []
                                    }
                                    onChange={(selected: any) => {

                                        setFormData(prev => ({
                                            ...prev,
                                            time_zone: selected[0]
                                        }));

                                        setFormErrors(prev => ({
                                            ...prev,
                                            time_zone: ""
                                        }));
                                    }}
                                />
                            )}

                        </div>

                        <div className="col-md-12 form-group">

                            <Label name="Phone" bold />

                            <Input
                                placeholder="Enter Phone"
                                value={String(formData.phone ?? "")}
                                onChange={(value: string) =>
                                    setFormData(prev => ({
                                        ...prev,
                                        phone: value
                                    }))
                                }
                            />

                        </div>

                        <div className="col-md-12 form-group">

                            <Label name="E-mail" bold required />

                            <Input
                                placeholder="Enter Email"
                                value={formData.email}
                                disabled={!!editingUserId}
                                onChange={(value: string) => {
                                    setFormData(prev => ({
                                        ...prev,
                                        email: value
                                    }));

                                    setFormErrors(prev => ({
                                        ...prev,
                                        email: ""
                                    }));
                                }}
                            />
                            {formErrors.email && (
                                <span className="text-danger">
                                    {formErrors.email}
                                </span>
                            )}

                        </div>

                        <div className="col-md-12 form-group">

                            <Label name="Company" bold />

                            {isCompanyFromUrl ? (

                                <div className="form-control-plaintext fw-bold">
                                    {formData.company}
                                </div>

                            ) : (

                                <Input
                                    placeholder="Enter Company"
                                    value={formData.company}
                                    onChange={(value: string) => {
                                        setFormData(prev => ({ ...prev, company: value }));
                                    }}
                                    disabled
                                />

                            )}

                        </div>

                        <div className="col-md-12 form-group">

                            <Label name="User Role" bold required />

                            {isRoleFromUrl || isMyProfile ? (

                                <div className="form-control-plaintext fw-bold">
                                    {getRoleDisplayName()}
                                </div>

                            ) : (

                                <>
                                    <AbsSelect
                                        mult={false}
                                        options={roleOptions}
                                        selected={
                                            formData.user_role
                                                ? [formData.user_role]
                                                : []
                                        }
                                        onChange={handleRoleChange}
                                    />
                                    {formErrors.user_role &&
                                        <span className="text-danger">
                                            {formErrors.user_role}
                                        </span>}
                                </>

                            )}

                        </div>

                        <div className="col-md-12">
                            <Divider className="divider-1" />
                        </div>

                        <div className="col-md-12 mt-3">

                            <div className="d-flex gap-2">

                                <Button
                                    variant={BUTTONS.PRIMARY}
                                    onClick={handleSave}
                                    disabled={isSaving}
                                >
                                    {isMyProfile || editingUserId ? "Update" : "Save"}
                                </Button>

                                <Button
                                    variant={BUTTONS.SECONDARY}
                                    onClick={handleCancel}
                                >
                                    Cancel
                                </Button>

                            </div>

                        </div>

                    </div>
                </>
            </Offcanvas.Body>

        </Offcanvas>
    );
};

export default AddUser;
