import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Button, AbsReactTable, BUTTONS, Divider, Input, AbsSelect, AbsSearch, RadioInput, Label } from "customer_portal-ui-shared";
import * as Icon from 'react-bootstrap-icons';
import ManageTankTemplateList from './ManageTankTemplateList';
import TankStatusChangeModal, { type SelectedTankItem, isTankActive, toSelectedTanks } from './TankStatusChangeModal';
import { Offcanvas } from 'react-bootstrap';
import type { TankDto, SelectOption } from "../../../../Interface/ProjectTank";
import * as tankService from '../../../../service/TankService';
import * as gradingService from '../../../../service/GradingService';
import { usePopup } from '../../../../Common/PopupProvider';
import { resultMessage } from '../../../../Common/UtilityMessage';
interface ManageTankRow {
    tankId: string;
    tankName: string;
    subheader: string;
    vesselType: string;
    vesselName: string;
    imoNumber: string;
    tankType: string;
    projectName: string;
    projectId: string | number;
    status: boolean;
    statusText: string;
    action?: unknown;
    isDownload?: boolean;
    checkboxDisabled?: boolean;
}

type FilterFormData = {
    imo_number: string;
    project_name: string;
    project_id: string;
    tank_type: string;
    tank_name: string;
    activation_status: boolean | null;
};

type ModalFormData = {
    tank_name: string;
    subheader_name: string;
    vessel_type: string;
    vessel_name: string;
    imo_number: string;
    tank_type: string;
    activation_status: boolean | null;
    project_name: string;
    project_id?: number;
};

const toPageIndex = (value: string | number | undefined | null): number => {
    if (typeof value === "string") {
        return parseInt(value, 10) || 0;
    }
    return Number(value) || 0;
};

const toPageSize = (value: string | number | undefined | null): number => {
    if (typeof value === "string") {
        return parseInt(value, 10) || 50;
    }
    return Number(value) || 50;
};

const mapTankRows = (items: any[]): ManageTankRow[] =>
    items.map(item => {
        const status = isTankActive(item.status ?? item.Status);
        const isDownload = Boolean(item.isDownload ?? item.IsDownload);
        return {
            tankId: String(item.tankId ?? item.TankId ?? ""),
            vesselType: item.vesselType ?? item.VesselType ?? "",
            vesselName: item.vesselName ?? item.VesselName ?? "",
            projectName: item.projectName ?? item.ProjectName ?? "",
            projectId: item.projectId ?? item.ProjectId ?? "",
            tankType: item.tankType ?? item.TankType ?? "",
            status,
            statusText: status ? "Active" : "Inactive",
            tankName: item.tankName ?? item.TankName ?? "",
            subheader: item.subheader ?? item.Subheader ?? "",
            imoNumber: item.imoNumber ?? item.IMONumber ?? "",
            isDownload,
            checkboxDisabled: isDownload,
        };
    });

const mapProjectOptions = (data: any[]): SelectOption[] =>
    (Array.isArray(data) ? data : []).map((p: any) => ({
        text: p.text ?? p.Text ?? p.projectName ?? p.ProjectName ?? p.projectname ?? p.Projectname ?? "",
        value: String(p.projectId ?? p.ProjectId ?? p.value ?? p.Value ?? p.projectid ?? p.Projectid ?? "")
    }));

const filterTanksByProject = (
    tanks: ManageTankRow[],
    projectValue: string,
    projects: SelectOption[]
): ManageTankRow[] => {
    if (!projectValue) return tanks;

    const selectedProject = projects.find(p => String(p.value) === String(projectValue));
    const projectNameFilter = selectedProject?.text?.trim().toLowerCase();

    return tanks.filter((t) => {
        const tankProjectName = (t.projectName || "").trim().toLowerCase();

        if (projectNameFilter && tankProjectName) {
            return tankProjectName === projectNameFilter;
        }

        return String(t.projectId) === String(projectValue);
    });
};

const buildFilterOptionsFromTanks = (
    tanks: ManageTankRow[],
    setTankTypeOptions: (options: SelectOption[]) => void,
    setTankNameOptions: (options: SelectOption[]) => void,
    setStatusOptions: (options: SelectOption[]) => void
) => {
    const uniqueTankTypes = Array.from(new Set(tanks.map(t => t.tankType).filter(Boolean)));
    const uniqueTankNames = Array.from(new Set(tanks.map(t => t.tankName).filter(Boolean)));
    const uniqueStatuses = Array.from(new Set(tanks.map(t => t.status)));

    setTankTypeOptions(uniqueTankTypes.map(t => ({ text: t, value: t })));
    setTankNameOptions(uniqueTankNames.map(t => ({ text: t, value: t })));

    const statusOpts: SelectOption[] = [];
    if (uniqueStatuses.includes(true)) {
        statusOpts.push({ text: "Active", value: "Active" });
    }
    if (uniqueStatuses.includes(false)) {
        statusOpts.push({ text: "InActive", value: "InActive" });
    }
    setStatusOptions(statusOpts);
};

const ProjectTankList: React.FC = () => {
    const popup = usePopup();
    const [showMobileSidebar, setShowMobileSidebar] = useState(false);
    const [searchParams] = useSearchParams();
    const urlImo = searchParams.get('imoNumber') || '';
    const urlProject = searchParams.get('ProjectId') || '';
    const [val, updateVal] = React.useState<string>("");
    const [searchApplied, updateSearchApplied] = React.useState<string | null>(null);
    const [canEdit, setCanEdit] = useState<boolean>(false);
    const [selectedTanks, setSelectedTanks] = useState<SelectedTankItem[]>([]);
    const [tableKey, setTableKey] = useState<number>(0);
    const [open, setOpen] = React.useState<boolean>();
    const [statusModel, setStatusModel] = React.useState<boolean>(false);
    const [loading, setLoading] = React.useState(false);

    const [tanks, setTanks] = React.useState<TankDto[]>([]);

    const [imoOptions, setImoOptions] = React.useState<SelectOption[]>([]);
    const [tankTypeOptions, setTankTypeOptions] = React.useState<SelectOption[]>([]);
    const setTankNameOptions: any = () => { };
    const setStatusOptions: any = () => { };

    const [vesselTypeOptions, setVesselTypeOptions] = React.useState<SelectOption[]>([]);
    const [formTankTypeOptions, setFormTankTypeOptions] = React.useState<SelectOption[]>([]);
    const [vesselNameOptions, setVesselNameOptions] = React.useState<SelectOption[]>([]);
    const [projectNameOptions, setProjectNameOptions] = React.useState<SelectOption[]>([]);
    const [isSaving, setIsSaving] = React.useState<boolean>(false);
    const [formErrors, setFormErrors] = React.useState<{ [key: string]: string }>({});
    const [modalImoNumber, setModalImoNumber] = React.useState<string>("");
    const [editingTankId, setEditingTankId] = React.useState<string | null>(null);
    const [projectId, setProjectId] = React.useState<number | null>(null);
    const [activationStatus, setActivationStatus] = React.useState<boolean | null>(true);
    const [isFilterEnabled, setIsFilterEnabled] = React.useState(false);
    const [tanklist, setTankList] = React.useState<ManageTankRow[]>([]);
    const [filteredTanklist, setFilteredTanklist] = React.useState<ManageTankRow[]>([]);
    const [TankFilter, setTankFilter] = useState<ManageTankRow[]>([]);
    const [dataCount, setDataCount] = useState(0);
    const [paginatedTanklist, setPaginatedTanklist] = React.useState<ManageTankRow[]>([]);
    const [hierarchicalFilters, setHierarchicalFilters] = useState<{
        vesselType?: string;
        tankName?: string;
        tankType?: string;
    }>({});
    const [filterFormData, setFilterFormData] = React.useState<FilterFormData>({
        imo_number: urlImo || "",
        project_name: urlProject || "",
        project_id: urlProject || "",
        tank_type: "",
        tank_name: "",
        activation_status: null
    });

    const [modalFormData, setModalFormData] = React.useState<ModalFormData>({
        tank_name: "",
        subheader_name: "",
        vessel_type: "",
        vessel_name: "",
        imo_number: "",
        tank_type: "",
        activation_status: null,
        project_name: "",
        project_id: undefined
    });

    const initialFilterState = {
        searchType: "list",
        resetPageIndex: false,
        expandAll: false,
        resetFilters: false,
        deselectRows: false,
        filters: {},
        sort: null,
        pageIndex: 0,
        pageSize: "20"
    };
    const [appliedFilters, setAppliedFilters] = React.useState({
        ...initialFilterState,
    });
    const [projectOptions, setProjectOptions] = useState<SelectOption[]>([]);
    const [showProjectDropdown, setShowProjectDropdown] = useState(false);
    const [isProjectDisabled, setIsProjectDisabled] = useState(true);
    const tanklistRef = useRef<ManageTankRow[]>([]);
    const projectOptionsRef = useRef<SelectOption[]>([]);
    const imoRequestRef = useRef(0);

    const clearTankGridState = useCallback(() => {
        setTankList([]);
        setFilteredTanklist([]);
        setTankFilter([]);
        tanklistRef.current = [];
        setDataCount(0);
        setPaginatedTanklist([]);
        setProjectOptions([]);
        projectOptionsRef.current = [];
        setShowProjectDropdown(false);
        setTankTypeOptions([]);
        setTankNameOptions([]);
        setStatusOptions([]);
        setAppliedFilters(prev => ({
            ...prev,
            pageIndex: 0,
            resetPageIndex: true,
            filters: {}
        }));
    }, []);

    const updateFilterDropdownOptions = useCallback((
        sourceTanks: ManageTankRow[],
        isMultipleProjects: boolean,
        projectValue?: string,
        projects?: SelectOption[]
    ) => {
        if (isMultipleProjects && !projectValue) {
            setTankTypeOptions([]);
            setTankNameOptions([]);
            setStatusOptions([]);
            return;
        }

        const tanksForOptions = isMultipleProjects && projectValue
            ? filterTanksByProject(
                sourceTanks,
                projectValue,
                projects ?? projectOptionsRef.current
            )
            : sourceTanks;

        buildFilterOptionsFromTanks(
            tanksForOptions,
            setTankTypeOptions,
            setTankNameOptions,
            setStatusOptions
        );
    }, []);

    useEffect(() => {
        loadManageTanks(urlImo || undefined);
        loadVesselTypes();
        loadVesselNames();

        if (urlImo) {
            setIsFilterEnabled(true);
            (async () => {
                try {
                    const mappedData = await GetVesselTankbyImo(urlImo, false);
                    const res = await tankService.ManageTankFilter_Project(urlImo);
                    const projects = mapProjectOptions(res.data);

                    projectOptionsRef.current = projects;
                    setProjectOptions(projects);

                    if (projects.length > 1 || urlProject) {
                        setShowProjectDropdown(true);
                    } else if (projects.length === 1) {
                        setShowProjectDropdown(false);
                    }

                    updateFilterDropdownOptions(mappedData, projects.length > 1 || !!urlProject, urlProject || undefined, projects);

                    if (urlProject) {
                        applyGridFilters("", "", null, urlProject, mappedData, projects);
                    } else if (projects.length === 1) {
                        applyGridFilters("", "", null, projects[0].value, mappedData, projects);
                    }
                } catch (e) {
                }
            })();
        }
    }, []);

    const loadManageTanks = async (imo?: string) => {
        setLoading(true);
        try {
            const res = await tankService.getManageTanks({ imo });
            const data = res.data;
            setCanEdit(Boolean(data.editable ?? data.Editable));
            setTanks(data.tanks || []);
            setImoOptions(data.imoNumberOptions || []);
            setTankTypeOptions(data.tankTypeOptions || []);
            setTankNameOptions(data.tankNameOptions || []);
            setStatusOptions(data.tankStatusOptions || []);
        } catch (e) {
        } finally {
            setLoading(false);
        }
    };
    const GetVesselTankbyImo = async (imo?: string, showAllTanks = true): Promise<ManageTankRow[]> => {
        if (!imo) return [];

        setLoading(true);
        try {
            const res = await tankService.getVesselTankbyImo(imo);
            const data = res.data;
            setCanEdit(Boolean(data.editable ?? data.Editable));

            const mappedData = mapTankRows(data.tanks || data.Tanks || []);
            tanklistRef.current = mappedData;
            setTankList(mappedData);

            if (showAllTanks) {
                setTankFilter(mappedData);
                setFilteredTanklist(mappedData);
            } else {
                setTankFilter([]);
                setFilteredTanklist([]);
                setDataCount(0);
                setPaginatedTanklist([]);
            }

            setAppliedFilters(prev => ({
                ...prev,
                pageIndex: 0,
                resetPageIndex: true
            }));

            return mappedData;
        } catch (e) {
            return [];
        } finally {
            setLoading(false);
        }
    };


    const loadVesselTypes = async () => {
        try {
            const res = await gradingService.getVesselType();
            const options = res.data.data.map((v: any) => ({
                text: v.vesselType,
                value: v.id.toString()
            }));
            setVesselTypeOptions(options);
        } catch (e) {
        }
    };

    const loadTankTypes = async (projectId: number, vesselType: string) => {
        try {
            const res = await tankService.GetMappedTankTypes(
                projectId ?? 0,
                vesselType ?? ""
            );

            const options = (res.data || []).map((p: any) => ({
                text: p.tankType,
                value: String(p.tankTypeId ?? p.id ?? "")
            }));

            setFormTankTypeOptions(options);

            return options;
        } catch (e) {
            setFormTankTypeOptions([]);
            return [];
        }
    };


    const loadVesselNames = async (): Promise<SelectOption[]> => {
        try {
            const res = await tankService.GetVesselDetails();
            const raw = res.data;
            const data: any[] = Array.isArray(raw) ? raw
                : Array.isArray(raw?.result?.data) ? raw.result.data
                    : Array.isArray(raw?.data) ? raw.data
                        : [];

            const options = data.map((p: any) => ({
                text: p.vesselName,
                value: String(p.imo)
            }));

            setVesselNameOptions(options);
            return options;
        } catch (e) {
            return [];
        }
    };
    const handleVesselTypeChange = (selected: any) => {
        const value = selected?.[0] ?? "";

        const vesselTypeText = vesselTypeOptions.find(
            v => String(v.value) === String(value)
        )?.text || "";

        setModalFormData(prev => ({
            ...prev,
            vessel_type: value,
            vessel_name: "",
            imo_number: "",
            project_id: undefined
        }));
        setModalImoNumber("");
        setProjectNameOptions([]);
        setProjectId(null);
        setFormErrors(prev => ({ ...prev, vessel_type: "", vessel_name: "", project_name: "" }));

        loadTankTypes(0, vesselTypeText);
    };
    useEffect(() => {
        loadTankTypes(0, "");
    }, []);

    const handleVesselNameChange = async (selected: any) => {
        const imo = selected?.[0] ?? "";

        try {
            const res = await tankService.GetProjectListByIMO(imo);

            const projectData = Array.isArray(res.data)
                ? res.data
                : (res.data?.data || res.data?.result || res.data?.projects || []);

            const mappedProjects = projectData.map((p: any) => ({
                text: p.text,
                value: String(p.value)
            }));

            setModalImoNumber(imo);
            setModalFormData(prev => ({
                ...prev,
                vessel_name: imo,
                imo_number: imo,
                vessel_type: "",
            }));
            
            setFormErrors(prev => ({ ...prev, vessel_type: "", vessel_name: "", project_name: "" }));

            if (mappedProjects.length === 1) {
                const single = mappedProjects[0];

                setProjectNameOptions(mappedProjects);
                setProjectId(Number(single.value));
                setIsProjectDisabled(true);
                setModalFormData(prev => ({
                    ...prev,
                    project_name: single.value,
                    project_id: Number(single.value)
                }));

                loadTankTypes(Number(single.value), "");
            }
            else {
                setProjectNameOptions(mappedProjects);
                setIsProjectDisabled(false);
                setProjectId(null);
                if (!editingTankId) {
                    setModalFormData(prev => ({
                        ...prev,
                        project_name: ""
                    }));
                }
            }

        } catch (e) {
        }
    };

    const resetForm = () => {
        setModalFormData({
            tank_name: "",
            subheader_name: "",
            vessel_type: "",
            vessel_name: "",
            imo_number: "",
            tank_type: "",
            activation_status: null,
            project_name: "",
            project_id: undefined
        });

        setActivationStatus(true);
        setProjectNameOptions([]);
        setModalImoNumber("");
        setProjectId(null);
        setEditingTankId(null);
        setFormErrors({});
    };

    const handleAddNewTank = () => {
        resetForm();
        setOpen(true);
    };

    const handleStatusChangeDone = async () => {
        setSelectedTanks([]);
        setTableKey(k => k + 1);
        await refreshTankListWithFilters();
    };

    const validateForm = (): boolean => {
        const errors: { [key: string]: string } = {};

        if (!modalFormData.tank_name || modalFormData.tank_name.trim() === "") {
            errors.tank_name = "Tank Name Required";
        }

        if (!modalFormData.tank_type || modalFormData.tank_type === "") {
            errors.tank_type = "Tank Type Required";
        }

        if (!modalFormData.vessel_type && !modalFormData.vessel_name && !modalImoNumber) {
            errors.vessel_name = "Please select Vessel Type or Vessel Name";
        }

        if (modalImoNumber && !modalFormData.vessel_name && !modalFormData.vessel_type) {
            errors.vessel_name = "Please select Vessel Name";
        }

        if (modalFormData.vessel_name && modalImoNumber) {
            const vesselOption = vesselNameOptions.find(v => v.value === modalImoNumber);
            if (!vesselOption) {
                errors.imo_number = "Please select valid IMO number";
            }
        }

        if (!modalFormData.vessel_type && (!projectId || projectId === null || projectId === 0) && !modalFormData.project_name) {
            errors.project_name = "Please select Project Name";
        }

        setFormErrors(errors);
        return Object.keys(errors).length === 0;
    };
    const isDuplicateTank = () => {
        const dataToCheck = filteredTanklist.length > 0 ? filteredTanklist : tanks;

        return dataToCheck.some((t: any) => {
            const currentTankId = editingTankId || null;
            const tankId = t.tankId || t.TankId || t.id || t.Id || null;
            if (currentTankId && tankId && String(currentTankId) === String(tankId)) {
                return false;
            }

            const sameName =
                (t.tankName || t.TankName || t.tank_name || "").trim().toLowerCase() ===
                modalFormData.tank_name.trim().toLowerCase();

            const tankTypeId = t.tankTypeId || t.TankTypeId || t.tank_type_id || null;
            const tankTypeText = t.tankType || t.TankType || t.tank_type || "";
            const currentTankTypeId = Number(modalFormData.tank_type);
            const sameTankType = tankTypeId
                ? Number(tankTypeId) === currentTankTypeId
                : String(tankTypeText) === String(modalFormData.tank_type);

            const currentImo = modalImoNumber || modalFormData.imo_number || null;
            const tankImo = t.imoNumber || t.IMONumber || t.imo_number || null;

            const currentVesselType = modalFormData.vessel_type || null;
            const tankVesselType = t.vesselType || t.VesselType || t.vessel_type || null;
            return (
                sameName &&
                sameTankType &&
                String(tankImo || "") === String(currentImo || "") &&
                String(tankVesselType || "") === String(currentVesselType || "")
            );
        });
    };

    const handleSaveTank = async () => {
        if (!validateForm()) {
            return;
        }

        if (!editingTankId && isDuplicateTank()) {
            setFormErrors(prev => ({
                ...prev,
                tank_name: "This tank is already available in the section"
            }));
            return;
        }

        setIsSaving(true);

        try {

            const tankTypeId = Number(modalFormData.tank_type);

            const tankTypeOption = formTankTypeOptions.find(t => t.value === modalFormData.tank_type);
            const tankTypeText = tankTypeOption ? tankTypeOption.text : modalFormData.tank_type;


            let vesselTypeText: string | null = null;
            if (modalFormData.vessel_type) {
                const vesselTypeOption = vesselTypeOptions.find(v => v.value === modalFormData.vessel_type);
                vesselTypeText = vesselTypeOption ? vesselTypeOption.text : null;
            }

            const payload = {
                tankName: modalFormData.tank_name.trim(),
                subheader: modalFormData.subheader_name.trim(),
                vesselType: vesselTypeText,
                imoNumber: modalFormData.vessel_type ? null : (modalImoNumber || modalFormData.imo_number || null),
                projectId: projectId || 0,
                tankTypeId: tankTypeId,
                tankType: tankTypeText,
                status: activationStatus === true
            };

            const isEdit = !!editingTankId;
            const tankName = modalFormData.tank_name.trim();

            if (editingTankId) {
                await tankService.updateTank({
                    ...payload,
                    tankId: editingTankId
                });
            } else {
                await tankService.createTank(payload);
            }

            setOpen(false);
            resetForm();
            await refreshTankListWithFilters();
            popup.success({
                title: isEdit ? "Edit Tank" : "Create Tank",
                message: resultMessage(tankName || "Tank", isEdit ? "Updated" : "Created")
            });

        } catch (e: any) {
            popup.error({ message: e?.message || `Failed to ${editingTankId ? 'update' : 'create'} tank.` });
        } finally {
            setIsSaving(false);
        }
    };

    const handleCancel = () => {
        setOpen(false);
        resetForm();
    };

    const onDeleteTank = async (tank: any) => {
        if (!tank) return;

        const tankName = tank.tankName || tank.tank_name || "this tank";
        const confirmed = await popup.confirm({
            title: "Delete Tank",
            message: `Are you sure you want to delete '${tankName}'?`,
            confirmText: "Delete",
            danger: true
        });
        if (!confirmed) return;

        setIsSaving(true);
        try {
            const tankId = tank.tankId || tank.TankId || tank.id || "";
            const rowProjectId = tank.projectId || tank.ProjectId || projectId || 0;
            const imoValue = filterFormData.imo_number || tank.imoNumber || tank.IMONumber || modalImoNumber || "";

            await tankService.deleteTank(
                tankId,
                imoValue,
                rowProjectId
            );

            await refreshTankListWithFilters();
            popup.success({
                title: "Delete Tank",
                message: resultMessage(tankName || "Tank", "Deleted")
            });
        } catch (e: any) {
            popup.error({ message: e?.message || "Failed to delete tank." });
        } finally {
            setIsSaving(false);
        }
    };

    const cleanString = (str: any) =>
        str !== null && str !== undefined
            ? str
                .toString()
                .normalize("NFKD")
                .replace(/\s+/g, " ")
                .replace(/[\u200B-\u200D\uFEFF]/g, "")
                .trim()
                .toLowerCase()
            : "";

    const columnUniqueValues = React.useMemo(() => {
        const filters = appliedFilters.filters || {};

        const computeUniq = (targetKey: string) => {
            let src = filteredTanklist || [];

            if (hierarchicalFilters.vesselType) {
                src = src.filter((item: any) => item.vesselType === hierarchicalFilters.vesselType);
            }
            if (hierarchicalFilters.tankType) {
                src = src.filter((item: any) => item.tankType === hierarchicalFilters.tankType);
            }
            if (hierarchicalFilters.tankName) {
                src = src.filter((item: any) => item.tankName === hierarchicalFilters.tankName);
            }
            if (searchApplied && searchApplied.trim()) {
                const searchLower = searchApplied.toLowerCase().trim();
                src = src.filter((item: any) => {
                    const tankName = String(item.tankName || item.TankName || item.tank_name || "").toLowerCase();
                    const tankType = String(item.tankType || item.TankType || item.tank_type || "").toLowerCase();
                    return (tankName && tankName.includes(searchLower)) ||
                        (tankType && tankType.includes(searchLower));
                });
            }

            const filterEntries = Object.entries(filters);
            if (filterEntries.length > 0) {
                src = src.filter((tank: any) =>
                    filterEntries.every(([columnId, filter]) => {
                        if (columnId === targetKey) return true;

                        const { searchTerm, selectedFilters } = filter as { searchTerm?: string; selectedFilters?: any[] };

                        if (selectedFilters && selectedFilters.length > 0) {
                            const tankValue = tank[columnId];
                            if (typeof tankValue === 'boolean') {
                                return selectedFilters.some(f => {
                                    const fStr = String(f).toLowerCase();
                                    if (fStr === 'active' || fStr === 'true' || fStr === 'yes' || fStr === 'checked') return tankValue === true;
                                    if (fStr === 'inactive' || fStr === 'false' || fStr === 'no' || fStr === 'unchecked') return tankValue === false;
                                    return false;
                                });
                            }
                            return selectedFilters.some(f =>
                                cleanString(String(f)) === cleanString(String(tankValue))
                            );
                        }

                        if (!searchTerm) return true;

                        const tankValue = cleanString(tank[columnId]);
                        const normalizedSearchTerm = cleanString(searchTerm);

                        return tankValue.includes(normalizedSearchTerm);
                    })
                );
            }

            const vals = Array.from(new Set(src.map((r: any) => r[targetKey]).filter(Boolean)));
            return vals.map(String).sort();
        };

        return {
            tankName: computeUniq("tankName"),
            vesselType: computeUniq("vesselType"),
            vesselName: computeUniq("vesselName"),
            imoNumber: computeUniq("imoNumber"),
            tankType: computeUniq("tankType"),
            statusText: computeUniq("statusText")
        };
    }, [filteredTanklist, searchApplied, hierarchicalFilters, appliedFilters.filters]);

    const columns = React.useMemo<Array<any>>(
        () => [
            {
                label: "Tank Name",
                accessorKey: "tankName",
                accessor: "tankName",
                columnValues: { type: "both", data: columnUniqueValues.tankName },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },
            {
                label: "Subheader",
                accessorKey: "subheader",
                accessor: "subheader",
                filtering: false,
                sorting: false,
                cell: (info: { getValue: () => any }) => info.getValue(),

            },
            {
                label: "Vessel Type",
                accessorKey: "vesselType",
                accessor: "vesselType",
                columnValues: { type: "both", data: columnUniqueValues.vesselType },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },
            {
                label: "Vessel Name",
                accessorKey: "vesselName",
                accessor: "vesselName",
                columnValues: { type: "both", data: columnUniqueValues.vesselName },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },
            {
                label: "IMO Number",
                accessorKey: "imoNumber",
                accessor: "imoNumber",
                columnValues: { type: "both", data: columnUniqueValues.imoNumber },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },
            {
                label: "Tank  Type",
                accessorKey: "tankType",
                accessor: "tankType",
                columnValues: { type: "both", data: columnUniqueValues.tankType },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },
            {
                label: "Status",
                accessorKey: "statusText",
                accessor: "statusText",
                columnValues: { type: "both", data: columnUniqueValues.statusText },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },

            {
                label: "Action",
                accessorKey: "action",
                accessor: "action",
                filtering: false,
                sorting: false,
                Cell: ({ row }: { row: { original: any } }) => {
                    return (
                        <div className='d-flex justify-content-start gap-3'>
                            <Button
                                variant={BUTTONS.TABLEDOWNLOAD}
                                startIcon={<Icon.PencilFill />}
                                onClick={() => handleEditTank(row.original)}
                            ></Button>
                            <Button variant={BUTTONS.TABLEDOWNLOAD} onClick={() => {
                                onDeleteTank(row.original);
                            }} startIcon={<Icon.TrashFill />}></Button>
                        </div>
                    );
                }
            },
        ],
        [columnUniqueValues]
    );

    const handleHierarchicalFilterChange = useCallback((filters: {
        vesselType?: string;
        tankType?: string;
    }) => {
        setHierarchicalFilters(filters);

        setFilterFormData(prev => {
            const tankName = filters.tankType && filters.tankType !== prev.tank_type ? "" : prev.tank_name;

            applyGridFilters(
                filters.tankType || "",
                tankName,
                prev.activation_status,
                prev.project_name
            );

            return {
                ...prev,
                tank_type: filters.tankType || "",
                tank_name: tankName,
            };
        });

        setAppliedFilters(prev => ({
            ...prev,
            pageIndex: 0,
            resetPageIndex: true
        }));
    }, []);

    const onChangeSearchInput = useCallback((value: string) => {
        updateVal(value);

        if (value.trim() === "") {
            updateSearchApplied(null);
            setAppliedFilters(prev => ({
                ...prev,
                pageIndex: 0,
                resetPageIndex: true
            }));
        } else {
            updateSearchApplied(value);
            setAppliedFilters(prev => ({
                ...prev,
                pageIndex: 0,
                resetPageIndex: true
            }));
        }
    }, []);

    const headerTitle = useMemo(() => {
        const vesselType = hierarchicalFilters.vesselType || "";
        const tankType = hierarchicalFilters.tankType || filterFormData.tank_type || "";

        if (vesselType && tankType) {
            return `${vesselType} - ${tankType}`;
        } else if (vesselType) {
            return `${vesselType} - Tank`;
        } else if (tankType) {
            return `${tankType}`;
        }
        return "Project Tanks";
    }, [hierarchicalFilters.vesselType, hierarchicalFilters.tankType, filterFormData.tank_type]);

    const applyGridFilters = (
        tankType?: string,
        tankName?: string,
        status?: boolean | null,
        projectValue?: string,
        sourceData?: ManageTankRow[],
        projectsList?: SelectOption[]
    ) => {
        const sourceTanks = sourceData ?? tanklistRef.current ?? tanklist;
        let projectScoped = [...sourceTanks];

        if (projectValue) {
            projectScoped = filterTanksByProject(
                projectScoped,
                projectValue,
                projectsList ?? projectOptionsRef.current ?? projectOptions
            );
        }

        let result = [...projectScoped];

        if (tankType) {
            result = result.filter((t: any) => t.tankType === tankType);
        }

        if (tankName) {
            result = result.filter((t: any) => t.tankName === tankName);
        }

        if (status !== null && status !== undefined) {
            result = result.filter((t: any) => t.status === status);
        }

        setFilteredTanklist(result);
        setTankFilter(projectScoped);

        setAppliedFilters(prev => ({
            ...prev,
            pageIndex: 0,
            resetPageIndex: true
        }));
    };

    const getProjectScopedTanks = (
        projectValue?: string,
        sourceData?: ManageTankRow[]
    ): ManageTankRow[] => {
        const sourceTanks = sourceData ?? tanklistRef.current ?? tanklist;
        if (!projectValue) return [...sourceTanks];

        return filterTanksByProject(
            sourceTanks,
            projectValue,
            projectOptionsRef.current ?? projectOptions
        );
    };

    const refreshTankListWithFilters = async () => {
        const imo = filterFormData.imo_number;
        if (!imo) {
            await loadManageTanks(undefined);
            return;
        }

        const mappedData = await GetVesselTankbyImo(imo, false);
        const projectValue = filterFormData.project_name;
        const isMultipleProjects = showProjectDropdown;

        updateFilterDropdownOptions(
            mappedData,
            isMultipleProjects,
            projectValue || undefined,
            projectOptionsRef.current
        );

        applyGridFilters(
            filterFormData.tank_type,
            filterFormData.tank_name,
            filterFormData.activation_status,
            projectValue || undefined,
            mappedData,
            projectOptionsRef.current
        );
    };

    const searchTanks = useCallback((
        filters: any,
        pageIndex: number,
        pageSize: number,
        dataToPaginate?: ManageTankRow[]
    ) => {

        let dataToFilter = dataToPaginate || filteredTanklist;

        if (hierarchicalFilters.vesselType) {
            dataToFilter = dataToFilter.filter((item: any) =>
                item.vesselType === hierarchicalFilters.vesselType
            );
        }
        if (hierarchicalFilters.tankType) {
            dataToFilter = dataToFilter.filter((item: any) =>
                item.tankType === hierarchicalFilters.tankType
            );
        }
        if (hierarchicalFilters.tankName) {
            dataToFilter = dataToFilter.filter((item: any) =>
                item.tankName === hierarchicalFilters.tankName
            );
        }
        if (searchApplied && searchApplied.trim()) {
            const searchLower = searchApplied.toLowerCase().trim();
            dataToFilter = dataToFilter.filter((item: any) => {
                const tankName = String(
                    item.tankName ||
                    item.TankName ||
                    item.tank_name ||
                    ""
                ).toLowerCase();
                const tankType = String(
                    item.tankType ||
                    item.TankType ||
                    item.tank_type ||
                    ""
                ).toLowerCase();

                return (tankName && tankName.includes(searchLower)) ||
                    (tankType && tankType.includes(searchLower));
            });
        }

        const activeFilters = filters?.filters || appliedFilters.filters || {};
        const filterEntries = Object.entries(activeFilters);
        if (filterEntries.length > 0) {
            dataToFilter = dataToFilter.filter((tank: any) =>
                filterEntries.every(([columnId, filter]) => {
                    const { searchTerm, selectedFilters } = filter as { searchTerm?: string; selectedFilters?: any[] };

                    if (selectedFilters && selectedFilters.length > 0) {
                        const tankValue = tank[columnId];
                        if (typeof tankValue === 'boolean') {
                            return selectedFilters.some(f => {
                                const fStr = String(f).toLowerCase();
                                if (fStr === 'active' || fStr === 'true' || fStr === 'yes' || fStr === 'checked') {
                                    return tankValue === true;
                                }
                                if (fStr === 'inactive' || fStr === 'false' || fStr === 'no' || fStr === 'unchecked') {
                                    return tankValue === false;
                                }
                                return false;
                            });
                        }
                        return selectedFilters.some(f =>
                            cleanString(String(f)) === cleanString(String(tankValue))
                        );
                    }

                    if (!searchTerm) return true;

                    const tankValue = cleanString(tank[columnId]);
                    const normalizedSearchTerm = cleanString(searchTerm);

                    return tankValue.includes(normalizedSearchTerm);
                })
            );
        }

        const startIndex = pageIndex * pageSize;
        const endIndex = startIndex + pageSize;

        const paginatedData = dataToFilter.slice(startIndex, endIndex);
        setDataCount(dataToFilter.length);

        setPaginatedTanklist(paginatedData);
    }, [filteredTanklist, searchApplied, hierarchicalFilters, appliedFilters.filters]);

    useEffect(() => {
        searchTanks(
            appliedFilters,
            toPageIndex(appliedFilters.pageIndex),
            toPageSize(appliedFilters.pageSize)
        );
    }, [filteredTanklist, searchApplied, hierarchicalFilters, appliedFilters.pageIndex, appliedFilters.pageSize, searchTanks]);

    const applyFilter = useCallback(
        (filtersObj: any) => {
            const filteredData = Object.fromEntries(
                Object.entries(filtersObj.filters || {}).filter(
                    ([, value]) => (value as { searchTerm: string })?.searchTerm !== ""
                        || ((value as { selectedFilters?: string[] })?.selectedFilters?.length ?? 0) > 0
                        || (value as { isFiltersApplied?: boolean })?.isFiltersApplied
                )
            );

            setAppliedFilters(prev => {
                const prevFiltersStr = JSON.stringify(prev.filters || {});
                const newFiltersStr = JSON.stringify(filteredData);
                const filtersChanged = prevFiltersStr !== newFiltersStr;

                let pageIndexNum = typeof filtersObj.pageIndex === 'string'
                    ? parseInt(filtersObj.pageIndex, 10)
                    : Number(filtersObj.pageIndex) || 0;

                if (filtersChanged) {
                    pageIndexNum = 0;
                }

                const pageSizeNum = toPageSize(filtersObj.pageSize);
                const searchType = filtersObj.searchType || "list";

                const updatedFilters = {
                    ...filtersObj,
                    pageIndex: pageIndexNum,
                    filters: filteredData,
                    sort: filtersObj.sort,
                    searchType,
                    pageSize: String(pageSizeNum),
                    resetPageIndex: filtersChanged,
                };

                searchTanks(
                    { ...updatedFilters, filters: filteredData },
                    pageIndexNum,
                    pageSizeNum
                );

                return updatedFilters;
            });
        },
        [searchTanks]
    );

    const handleClearTopFilters = () => {
        setFilterFormData({
            imo_number: "",
            project_name: "",
            project_id: "",
            tank_type: "",
            tank_name: "",
            activation_status: null
        });

        setProjectOptions([]);
        projectOptionsRef.current = [];
        setShowProjectDropdown(false);

        setHierarchicalFilters({});
        setIsFilterEnabled(false);

        setTankList([]);
        setFilteredTanklist([]);
        setTankFilter([]);
        tanklistRef.current = [];

        setDataCount(0);
        setPaginatedTanklist([]);

        setTimeout(() => {
            loadManageTanks(undefined);
        }, 0);
    };

    const handleClearTableFilters = () => {
        updateVal("");
        updateSearchApplied(null);

        setAppliedFilters(prev => ({
            ...prev,
            pageIndex: 0,
            resetPageIndex: true,
            filters: {}
        }));
    };

    const handleEditTank = async (row: any) => {
        try {
            const tankName = row.tankName || "";
            const subheader = row.subheader || "";
            const imo = String(row.imoNumber || "");
            const tankTypeText = row.tankType || "";
            const projectIdValue = row.projectId ? String(row.projectId) : "";
            const vesselTypeText = row.vesselType || "";
            const status = row.status;

            const isActive =
                status === true ||
                status === "Active" ||
                status === 1 ||
                status === "true";

            setActivationStatus(isActive);
            setModalImoNumber(imo);

            const vesselRes = await gradingService.getVesselType();
            const vesselOptions = vesselRes.data.data.map((v: any) => ({
                text: v.vesselType,
                value: v.id.toString()
            }));
            setVesselTypeOptions(vesselOptions);

            const vesselTypeValue =
                vesselOptions.find(v =>
                    v.text.trim().toLowerCase() === vesselTypeText.trim().toLowerCase()
                )?.value || "";

            const res = await tankService.GetProjectListByIMO(imo);

            const projectOptions = (res.data || []).map((p: any) => ({
                text: p.text,
                value: String(p.value)
            }));

            setProjectNameOptions(projectOptions);

            const finalProject = projectOptions.find(
                p => String(p.value) === String(projectIdValue)
            );

            const finalProjectId = finalProject ? finalProject.value : "";

            setProjectId(finalProjectId ? Number(finalProjectId) : null);

            const tankTypeList = await loadTankTypes(Number(finalProjectId), "");

            const tankTypeValue =
                tankTypeList.find(
                    t => t.text.trim().toLowerCase() === tankTypeText.trim().toLowerCase()
                )?.value || "";

            setModalFormData({
                tank_name: tankName,
                subheader_name: subheader,
                vessel_type: vesselTypeValue,
                vessel_name: imo,
                imo_number: imo,
                tank_type: tankTypeValue,
                activation_status: isActive,
                project_name: finalProjectId,
                project_id: finalProjectId ? Number(finalProjectId) : undefined
            });

            setEditingTankId(row.tankId || row.id || null);
            setOpen(true);

        } catch (error) {
        }
    };

    const projectTanksForDropdowns = getProjectScopedTanks(filterFormData.project_name);

    const filteredForTankName = filterFormData.tank_type
        ? projectTanksForDropdowns.filter((t: any) => t.tankType === filterFormData.tank_type)
        : projectTanksForDropdowns;

    const dynamicTankNameOptions = Array.from(new Set(filteredForTankName.map((t: any) => t.tankName).filter(Boolean)))
        .map((name: any) => ({ text: name, value: name }));

    const filteredForStatus = filteredForTankName.filter((t: any) =>
        !filterFormData.tank_name || t.tankName === filterFormData.tank_name
    );

    const uniqueStatuses = Array.from(new Set(filteredForStatus.map((t: any) => t.status)));
    const dynamicStatusOptions = [];
    if (uniqueStatuses.includes(true)) dynamicStatusOptions.push({ text: "Active", value: "Active" });
    if (uniqueStatuses.includes(false)) dynamicStatusOptions.push({ text: "InActive", value: "InActive" });


    return (
        <>
            <div className="filter-section">
                <div className="row">
                    <div className="col-md-2 form-group">
                        <Label name="IMO Number" bold />
                        <AbsSelect
                            key={`imo-select-${filterFormData.imo_number || 'empty'}-${isFilterEnabled}-${imoOptions.length}`}
                            mult={false}
                            options={imoOptions.map(o => ({
                                text: o.text,
                                value: String(o.value)
                            }))}
                            selected={filterFormData.imo_number ? [filterFormData.imo_number] : []}
                            placeholder="Select IMO Number"
                            disabled={!!urlImo}
                            onChange={async (selected: string[] | any) => {
                                const imo = selected?.[0] ?? "";
                                if (imo === filterFormData.imo_number) return;
                                const requestId = ++imoRequestRef.current;

                                setFilterFormData(prev => ({
                                    ...prev,
                                    imo_number: imo,
                                    project_name: "",
                                    project_id: "",
                                    tank_type: "",
                                    tank_name: "",
                                    activation_status: null
                                }));

                                setHierarchicalFilters({});
                                updateVal("");
                                updateSearchApplied(null);

                                if (imo) {
                                    setIsFilterEnabled(true);
                                    setLoading(true);
                                    clearTankGridState();

                                    try {
                                        const mappedData = await GetVesselTankbyImo(imo, false);
                                        if (requestId !== imoRequestRef.current) return;

                                        const res = await tankService.ManageTankFilter_Project(imo);
                                        if (requestId !== imoRequestRef.current) return;

                                        const projects = mapProjectOptions(res.data);

                                        projectOptionsRef.current = projects;

                                        if (projects.length > 1) {
                                            setProjectOptions(projects);
                                            setShowProjectDropdown(true);
                                            updateFilterDropdownOptions(mappedData, true);
                                        } else if (projects.length === 1) {
                                            const singleProjectId = projects[0].value;

                                            setProjectOptions(projects);
                                            setShowProjectDropdown(false);
                                            setFilterFormData(prev => ({
                                                ...prev,
                                                project_name: singleProjectId,
                                                project_id: singleProjectId
                                            }));
                                            updateFilterDropdownOptions(mappedData, false);
                                            applyGridFilters("", "", null, singleProjectId, mappedData, projects);
                                        } else {
                                            setProjectOptions([]);
                                            projectOptionsRef.current = [];
                                            setShowProjectDropdown(false);
                                            updateFilterDropdownOptions(mappedData, false);
                                            setFilteredTanklist(mappedData);
                                            setTankFilter(mappedData);
                                        }
                                    } catch (e) {
                                        if (requestId === imoRequestRef.current) {
                                            clearTankGridState();
                                        }
                                    }
                                } else {
                                    imoRequestRef.current += 1;
                                    setIsFilterEnabled(false);
                                    setLoading(false);
                                    clearTankGridState();
                                }
                            }}
                        />


                    </div>
                    {showProjectDropdown ? <>
                        <div className="col-md-2 form-group">
                            <Label name="Select a Project Name" bold />
                            <AbsSelect
                                key={`project-select-${filterFormData.imo_number}-${projectOptions.length}`}
                                mult={false}
                                options={projectOptions}
                                selected={filterFormData.project_name ? [filterFormData.project_name] : []}
                                placeholder="Select Project Name"
                                disabled={!!urlProject}
                                onChange={(val) => {
                                    const value = val?.[0] ?? "";
                                    if (value === filterFormData.project_name) return;

                                    setFilterFormData(prev => ({
                                        ...prev,
                                        project_name: value,
                                        project_id: value,
                                        tank_type: "",
                                        tank_name: "",
                                        activation_status: null
                                    }));

                                    setHierarchicalFilters({});
                                    updateFilterDropdownOptions(
                                        tanklistRef.current,
                                        true,
                                        value,
                                        projectOptionsRef.current
                                    );

                                    applyGridFilters(
                                        "",
                                        "",
                                        null,
                                        value,
                                        tanklistRef.current
                                    );
                                }}
                            />

                        </div>
                    </>
                        : null}
                    <div className="col-md-2 form-group">
                        <Label name="Select a Tank Type" bold />
                        <AbsSelect
                            key={`tanktype-${filterFormData.imo_number}-${filterFormData.project_name || 'none'}-${filterFormData.tank_type || 'empty'}`}
                            mult={false}
                            options={tankTypeOptions}
                            disabled={!filterFormData.imo_number || (showProjectDropdown && !filterFormData.project_name)}
                            selected={filterFormData.tank_type ? [filterFormData.tank_type] : []}
                            placeholder="Select Tank Type"
                            onChange={(val) => {
                                const value = val?.[0] ?? "";
                                if (value === filterFormData.tank_type) return;

                                setFilterFormData(prev => {
                                    const projectTanks = getProjectScopedTanks(prev.project_name);
                                    const matchingTank = projectTanks.find(t => t.tankType === value);

                                    if (value) {
                                        setHierarchicalFilters({
                                            vesselType: matchingTank?.vesselType,
                                            tankType: value
                                        });
                                    } else {
                                        setHierarchicalFilters(prevFilters => ({
                                            ...prevFilters,
                                            tankType: undefined
                                        }));
                                    }

                                    const tankName = value !== prev.tank_type ? "" : prev.tank_name;

                                    applyGridFilters(
                                        value,
                                        tankName,
                                        prev.activation_status,
                                        prev.project_name
                                    );

                                    return {
                                        ...prev,
                                        tank_type: value,
                                        tank_name: tankName
                                    };
                                });
                            }}
                        />

                    </div>
                    <div className="col-md-2 form-group">
                        <Label name="Select a Tank Name" bold />
                        <AbsSelect
                            key={`tankname-${filterFormData.imo_number}-${filterFormData.project_name || 'none'}-${filterFormData.tank_name || 'empty'}`}
                            mult={false}
                            options={dynamicTankNameOptions}
                            disabled={!filterFormData.imo_number || (showProjectDropdown && !filterFormData.project_name)}
                            selected={filterFormData.tank_name ? [filterFormData.tank_name] : []}
                            placeholder="Select Tank Name"
                            onChange={(s: string[] | any) => {
                                const value = s?.[0] ?? "";
                                if (value === filterFormData.tank_name) return;

                                setFilterFormData(prev => ({ ...prev, tank_name: value }));
                                applyGridFilters(
                                    filterFormData.tank_type,
                                    value,
                                    filterFormData.activation_status,
                                    filterFormData.project_name
                                );
                            }}
                        />

                    </div>
                    <div className="col-md-2 form-group">
                        <Label name="  Status" bold />
                        <AbsSelect
                            key={`status-${filterFormData.imo_number}-${filterFormData.project_name || 'none'}-${filterFormData.activation_status === true ? 'active' : filterFormData.activation_status === false ? 'inactive' : 'empty'}`}
                            mult={false}
                            options={dynamicStatusOptions}
                            disabled={!filterFormData.imo_number || (showProjectDropdown && !filterFormData.project_name)}
                            selected={
                                filterFormData.activation_status === true
                                    ? ["Active"]
                                    : filterFormData.activation_status === false
                                        ? ["InActive"]
                                        : []
                            }
                            placeholder="Select Status"
                            onChange={(s: string[] | any) => {

                                const statusValue =
                                    s?.[0] === "Active"
                                        ? true
                                        : s?.[0] === "InActive"
                                            ? false
                                            : null;

                                setFilterFormData(prev => ({
                                    ...prev,
                                    activation_status: statusValue
                                }));

                                applyGridFilters(
                                    filterFormData.tank_type,
                                    filterFormData.tank_name,
                                    statusValue,
                                    filterFormData.project_name
                                );
                            }}

                        />

                    </div>


                    <div className="col-md-2 form-group align-self-end">
                        <div className="d-flex gap-2">
                            <Button variant={BUTTONS.SECONDARY} onClick={handleClearTopFilters}>Clear Filter</Button>
                        </div>
                    </div>

                </div>
            </div>

            <div className="grid-container position-relative">

                <Offcanvas show={showMobileSidebar} onHide={() => setShowMobileSidebar(false)} placement="end" className="d-md-none">
                    <Offcanvas.Header closeButton>
                        <Offcanvas.Title>Filters</Offcanvas.Title>
                    </Offcanvas.Header>
                    <Offcanvas.Body>
                        <ManageTankTemplateList
                            data={TankFilter as any}
                            onFilterChange={handleHierarchicalFilterChange}
                            selectedFilters={{
                                vesselType: hierarchicalFilters.vesselType,
                                tankType: hierarchicalFilters.tankType || filterFormData.tank_type || undefined
                            }} />
                    </Offcanvas.Body>
                </Offcanvas>
                <div className="grid-sidebar d-none d-md-block">
                    <ManageTankTemplateList
                        data={TankFilter as any}
                        onFilterChange={handleHierarchicalFilterChange}
                        selectedFilters={{
                            vesselType: hierarchicalFilters.vesselType,
                            tankType: hierarchicalFilters.tankType || filterFormData.tank_type || undefined
                        }} />
                </div>
                <div className="grid-content-body">
                    <div className="row align-items-center mb-3">
                        <div className="col-8 col-md-8">
                            <h5 className='_600 mb-0'>{headerTitle}</h5>
                        </div>
                        <div className="col-4 d-md-none text-end">
                            <Icon.Funnel 
                                size={24} 
                                onClick={() => setShowMobileSidebar(true)} 
                                style={{ cursor: 'pointer' }} 
                            />
                        </div>
                    </div>
                    <div className="row">
                        <div className="col-md-6 mb-3 mt-2">
                            <div className="d-flex justify-content-start align-items-center gap-2">
                                <AbsSearch
                                    handleClear={() => {
                                        updateVal("");
                                        updateSearchApplied(null);
                                        setAppliedFilters(prev => ({
                                            ...prev,
                                            pageIndex: 0,
                                            resetPageIndex: true
                                        }));
                                    }}
                                    value={val}
                                    handleChange={(e: any) => onChangeSearchInput(e.target.value)}
                                    handleSearch={() => {
                                        updateSearchApplied(val === "" ? null : val);
                                    }}
                                    placeholder="Search by Tank Name or Tank Type"
                                    searchApplied={searchApplied !== null}
                                />
                                <a href="#" className='text-link text-nowrap _500' onClick={(e) => { e.preventDefault(); handleClearTableFilters(); }}>Clear Filter</a>
                            </div>
                        </div>
                        <div className="col-md-6 d-flex justify-content-end gap-2">
                            <div>
                                <Button variant={BUTTONS.PRIMARY} onClick={handleAddNewTank}>Add New Tank</Button>
                            </div>
                            {canEdit && selectedTanks.length > 0 && (
                                <div>
                                    <Button variant={BUTTONS.PRIMARY} onClick={() => setStatusModel(true)}>Change Status</Button>
                                </div>
                            )}
                        </div>
                    </div>
                    <div className="custom-react-table">
                        <AbsReactTable
                            key={`project-tanks-table-${tableKey}-${canEdit ? 'edit' : 'view'}`}
                            loading={loading}
                            columns={columns}
                            data={paginatedTanklist.length ? paginatedTanklist : []}
                            pagination={true}
                            expand={false}
                            allowColumnsShowHide={false}
                            shadow={true}
                            rowSelect={canEdit}
                            onRowSelected={(rows: any[]) => {
                                setSelectedTanks(toSelectedTanks(rows));
                            }}
                            allDataProvided
                            dataCount={dataCount || filteredTanklist.length}
                            applyFilterCallback={applyFilter}
                            pageIndex={appliedFilters.pageIndex}
                            appliedFilters={{
                                ...appliedFilters,
                                pageSize: appliedFilters.pageSize.toString(),
                                searchType: "all"
                            }}
                            pageOptions={["20", "40", "60", "80"]}
                        />
                    </div>
                </div>
            </div>

            <Offcanvas show={open} placement={"end"} onHide={handleCancel} backdrop="static">
                <Offcanvas.Header closeButton>
                    {editingTankId ? "Edit Tank" : "Add New Tank"}
                </Offcanvas.Header>
                <Offcanvas.Body>
                    <div className="row">
                        <div className="col-md-12 form-group">
                            <Label name="Tank Name" bold required />
                            <Input
                                placeholder="Enter Tank Name"
                                value={modalFormData.tank_name}
                                onChange={(val: string) => {
                                    setModalFormData(prev => ({
                                        ...prev,
                                        tank_name: val
                                    }));
                                    setFormErrors(prev => ({ ...prev, tank_name: "" }));
                                }}
                            />

                            {formErrors.tank_name && <span className="text-danger small">{formErrors.tank_name}</span>}
                        </div>
                        <div className="col-md-12 form-group">
                            <Label name="Subheader" bold />
                            <Input
                                placeholder="Enter Subheader Name"
                                value={modalFormData.subheader_name}
                                onChange={(val: string) => {
                                    setModalFormData(prev => ({
                                        ...prev,
                                        subheader_name: val
                                    }));
                                    setFormErrors(prev => ({ ...prev, subheader_name: "" }));
                                }}
                            />

                            {formErrors.subheader_name && <span className="text-danger small">{formErrors.subheader_name}</span>}
                        </div>
                        <div className="col-md-12 form-group">
                            <Label name="Select a Vessel Type" bold required />
                            <AbsSelect
                                key={`vesselType-${modalFormData.vessel_type}`}
                                mult={false}
                                onChange={handleVesselTypeChange}
                                options={vesselTypeOptions}
                                selected={modalFormData.vessel_type ? [modalFormData.vessel_type] : []}
                                placeholder="Select Vessel Type"
                                disabled={!!editingTankId}
                            />
                            {formErrors.vessel_type && <span className="text-danger small">{formErrors.vessel_type}</span>}
                        </div>

                        <div className="col-md-12">
                            <div className="d-flex align-items-center my-2">
                                <hr className="flex-grow-1 center-divider" />
                                <span className="px-3 text-muted fw-semibold">OR</span>
                                <hr className="flex-grow-1 center-divider" />
                            </div>
                        </div>

                        <div className="col-md-12 form-group">
                            <Label name="Vessel Name" bold required />
                            <AbsSelect
                                key={`vessel-name-modal-${editingTankId || 'new'}-${modalImoNumber || 'empty'}-${vesselNameOptions.length}`}
                                mult={false}
                                onChange={handleVesselNameChange}
                                options={vesselNameOptions}
                                selected={modalImoNumber ? [String(modalImoNumber)] : []}
                                placeholder="Select Vessel Name"
                                disabled={!!editingTankId}
                            />
                            {formErrors.vessel_name && <span className="text-danger small">{formErrors.vessel_name}</span>}
                        </div>


                        <div className="col-md-12 form-group">
                            <Label name="IMO Number" bold />
                            <Input
                                placeholder="IMO Number"
                                value={modalImoNumber}
                                onChange={(value: string) => {
                                    setModalImoNumber(value);
                                    setFormErrors(prev => ({ ...prev, imo_number: "" }));
                                }}
                                disabled={!!editingTankId}
                            />
                            {formErrors.imo_number && <span className="text-danger small">{formErrors.imo_number}</span>}
                        </div>
                        {
                            modalImoNumber &&

                            <div className="col-md-12 form-group">
                                <Label name="Project Name" bold required />
                                <AbsSelect
                                    key={`project-${modalFormData.project_name}`}
                                    mult={false}
                                    options={projectNameOptions}
                                    selected={
                                        modalFormData.project_name
                                            ? [String(modalFormData.project_name)]
                                            : []
                                    }
                                    placeholder="Select Project Name"
                                    disabled={!!editingTankId || isProjectDisabled}
                                    onChange={(selected: any) => {
                                        const value = selected?.[0] ?? "";

                                        setProjectId(value ? Number(value) : null);

                                        setModalFormData(prev => ({
                                            ...prev,
                                            project_name: value,
                                            project_id: value ? Number(value) : undefined
                                        }));
                                        
                                        setFormErrors(prev => ({ ...prev, project_name: "" }));

                                        loadTankTypes(Number(value), "");
                                    }}
                                />
                                {formErrors.project_name && <span className="text-danger small">{formErrors.project_name}</span>}
                            </div>
                        }
                        <div className="col-md-12 form-group">
                            <Label name="Select a Tank Type" bold required />
                            <AbsSelect
                                key={`tank-type-${formTankTypeOptions.length}-${modalFormData.tank_type}`}
                                mult={false}
                                onChange={(selected: string[] | any) => {
                                    setModalFormData(prev => ({
                                        ...prev,
                                        tank_type: String(selected?.[0] || "")
                                    }));
                                    setFormErrors(prev => ({ ...prev, tank_type: "" }));
                                }}
                                options={formTankTypeOptions}
                                selected={
                                    modalFormData.tank_type
                                        ? [String(modalFormData.tank_type)]
                                        : []
                                }
                                placeholder="Select a Tank Type"
                                disabled={!!editingTankId}
                            />
                            {formErrors.tank_type && <span className="text-danger small">{formErrors.tank_type}</span>}
                        </div>
                        <div className="col-md-12 form-group">
                            <Label name="Activation Status" bold />
                            <div className='d-flex justify-content-start gap-3'>
                                <div className='d-flex justify-content-start gap-3'>
                                    <RadioInput
                                        label="Active"
                                        checked={activationStatus === true}
                                        onChange={() => setActivationStatus(true)}
                                    />

                                    <RadioInput
                                        label="Inactive"
                                        checked={activationStatus === false}
                                        onChange={() => setActivationStatus(false)}
                                    />
                                </div>

                            </div>
                            {formErrors.activationStatus && <span className="text-danger small d-block">{formErrors.activation_status}</span>}
                        </div>
                        <div className="col-md-12 form-group">
                            <Divider className="divider-1" />
                        </div>
                        <div className="col-md-12 form-group">
                            <div className="d-flex gap-2">
                                <Button
                                    variant={BUTTONS.PRIMARY}
                                    onClick={handleSaveTank}
                                    disabled={isSaving}
                                >
                                    {isSaving
                                        ? (editingTankId ? "Updating..." : "Saving...")
                                        : (editingTankId ? "Update" : "Create")}
                                </Button>
                                <Button variant={BUTTONS.SECONDARY} onClick={() => {
                                    setOpen(false);
                                    resetForm();
                                }}
                                > Cancel</Button>
                            </div>
                        </div>

                    </div>
                </Offcanvas.Body>
            </Offcanvas>
            <TankStatusChangeModal
                show={statusModel}
                onHide={() => setStatusModel(false)}
                selectedTanks={selectedTanks}
                imo={filterFormData.imo_number || ""}
                projectId={filterFormData.project_id || undefined}
                onDone={handleStatusChangeDone}
            />
        </>
    );
};

export default ProjectTankList;


