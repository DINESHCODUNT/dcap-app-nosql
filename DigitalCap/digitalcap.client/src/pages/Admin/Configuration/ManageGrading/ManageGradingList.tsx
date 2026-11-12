import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { BreadcrumbBar, Button, AbsReactTable, BUTTONS, Divider, Input, AbsSelect, AbsSearch, CheckboxInput, RadioInput, Label } from "customer_portal-ui-shared";
import * as Icon from 'react-bootstrap-icons';
import ManageGradingTemplateList from './ManageGradingTemplateList';
import { usePopup } from '../../../../Common/PopupProvider';
import { resultMessage } from '../../../../Common/UtilityMessage';
import { Offcanvas } from 'react-bootstrap';
import * as gradingService from '../../../../service/GradingService';

interface ManageGradingRow {
    gradingId: number;
    sectionId: string;
    tanktypeId: number;
    vesselType: string;
    templateName: string;
    sectionName: string;
    gradingName: string;
    isActive: boolean;
    requiredInReport: boolean;
    statusText?: string;
}

type FormData = {
    vesselValue: number | string | null;
    vesselText: string;

    partValue: number | null;
    partText: string;

    sectionValue: string | number;
    sectionText: string;

    grading_name: string;
};
type SelectOption = {
    text: string;
    value: number | string;
};
const ManageGradingList: React.FC = () => {
    const [showMobileSidebar, setShowMobileSidebar] = useState(false);
    const [val, updateVal] = React.useState<string>("");
    const [searchApplied, updateSearchApplied] = React.useState<string | null>(null);
    const [gradings, setGradings] = useState<ManageGradingRow[]>([]);
    const [loading, setLoading] = useState<boolean>(false);
    const [dataCount, setDataCount] = useState(0);
    const [GradingFilter, setGradingFilter] = useState<ManageGradingRow[]>([]);
    const [hierarchicalFilters, setHierarchicalFilters] = useState<{
        vesselType?: string;
        templateName?: string;
        sectionName?: string;
        gradingName?: string;
    }>({});
    const [error, setError] = React.useState<boolean>();
    const [open, setOpen] = React.useState<boolean>();
    const popup = usePopup();


    const [isSaving, setIsSaving] = useState(false);
    const [responseMsg, setResponseMsg] = React.useState<boolean>();
    const [formErrors, setFormErrors] = React.useState<{ [key: string]: string }>({});
    const [formData, setFormData] = useState<FormData>({
        vesselValue: null,
        vesselText: "",
        partValue: null,
        partText: "",
        sectionValue: "",
        sectionText: "",
        grading_name: ""
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
        pageSize: "50"
    };

    const getFirstVesselType = (rows: ManageGradingRow[]) => {
        for (const row of rows) {
            if (row.vesselType) {
                return row.vesselType;
            }
        }
        return undefined;
    };

    const getPageSizeNum = (pageSize?: string | number) => {
        if (pageSize != null && pageSize !== "") {
            return typeof pageSize === 'string' ? parseInt(pageSize, 10) : Number(pageSize);
        }
        return 50;
    };

    const [appliedFilters, setAppliedFilters] = React.useState({
        ...initialFilterState,
    });
    const [vesselOptions, setVesselOptions] = useState<SelectOption[]>([]);
    const [partOptions, setPartOptions] = useState<SelectOption[]>([]);
    const [sectionOptions, setSectionOptions] = useState<SelectOption[]>([]);
    const [selectedVesselType, setselectedVesselType] = useState("");
    const [GradingName, setGradingName] = useState("");
    const [activationStatus, setActivationStatus] = useState<boolean | null>(true);
    const [reportStatus, setReportStatus] = useState<boolean | null>(true);



    useEffect(() => {
        loadGradings();
        restoreFilters();
    }, []);

    const loadVesselOptions = useCallback(() => {
        gradingService.getVesselType().then((res) => {
            setVesselOptions(
                (res.data.data || []).map((v: { vesselType: string; id: number }) => ({
                    text: v.vesselType,
                    value: v.id,
                }))
            );
        });
    }, []);

    useEffect(() => {
        loadVesselOptions();
    }, [loadVesselOptions]);

    const onVesselChange = (vesselType: string) => {

        setselectedVesselType(vesselType);
        gradingService.getTemplateName(vesselType).then(res => {

            setPartOptions(
                res.data.data.map((p: any) => ({
                    text: p.templateName,
                    value: p.templateId
                }))
            );
        });
    };


    const onTemplateChange = (templateName: string) => {
        if (!templateName || !selectedVesselType) {
            setSectionOptions([]);

            setFormData(prev => ({
                ...prev,
                sectionValue: "",
                sectionText: ""
            }));

            if (formErrors.sectionName) {
                setFormErrors(prev => {
                    const newErrors = { ...prev };
                    delete newErrors.sectionName;
                    return newErrors;
                });
            }
            return;
        }

        gradingService.GetSectionNameByTemplateNameAndVesselType(templateName, selectedVesselType)
            .then(res => {
                const sections = res.data.data || [];

                setSectionOptions(
                    sections.map((s: any) => ({
                        text: s.sectionName,
                        value: s.sectionId
                    }))
                );

                setFormData(prev => ({
                    ...prev,
                    sectionValue: "",
                    sectionText: ""
                }));

                if (formErrors.sectionName) {
                    setFormErrors(prev => {
                        const newErrors = { ...prev };
                        delete newErrors.sectionName;
                        return newErrors;
                    });
                }
            })
            .catch(() => {
                setSectionOptions([]);
                setFormData(prev => ({
                    ...prev,
                    sectionValue: "",
                    sectionText: ""
                }));
            });
    };

    const isDuplicateGrading = () => {
        return GradingFilter.some(g =>
            g.vesselType === formData.vesselText &&
            g.templateName === formData.partText &&
            g.sectionName === formData.sectionText &&
            g.gradingName.trim().toLowerCase() === GradingName.trim().toLowerCase() &&
            (editingGradingId ? g.gradingId !== editingGradingId : true)
        );
    };

    const validateForm = (): boolean => {
        const errors: { [key: string]: string } = {};

        if (editingGradingId) {
            if (!GradingName || GradingName.trim() === "") {
                errors.gradingName = "Grading Name Required";
            }
        } else {
            if (!formData.vesselText || formData.vesselText.trim() === "") {
                errors.vesselType = "Please select valid Vessel Type";
            }

            if (!GradingName || GradingName.trim() === "") {
                errors.gradingName = "Grading Name Required";
            }

            if (!formData.partText || formData.partText.trim() === "") {
                errors.templateName = "Please select valid Part Name";
            }

            if (!formData.sectionText || formData.sectionText.trim() === "") {
                errors.sectionName = "Please select valid Section Name";
            }
            if (formData.partText && formData.vesselText) {
                if (!sectionOptions || sectionOptions.length === 0) {
                    errors.sectionName = "Invalid section";
                } else if (formData.sectionText && formData.sectionValue) {
                    const section = sectionOptions.find(s =>
                        s.text === formData.sectionText
                    );

                    if (!section) {
                        errors.sectionName = "Please select valid Section";
                    } else if (!section.value || section.value === "" || section.value === null || String(section.value).trim() === "") {
                        errors.sectionName = "Please select valid Section";
                    } else {
                        if (String(section.value) !== String(formData.sectionValue)) {
                            errors.sectionName = "Please select valid Section";
                        }
                    }
                }
            }
        }

        setFormErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const handleCreate = async () => {
        setFormErrors({});
        setError(false);
        setResponseMsg(false);
        if (!validateForm()) {
            if (!editingGradingId) {
                setError(true);
            }
            return;
        }

        if (isDuplicateGrading()) {
            setFormErrors(prev => ({
                ...prev,
                gradingName: "Grading Already Exist"
            }));
            setResponseMsg(true);
            return;
        }

        setIsSaving(true);

        try {
            if (editingGradingId && editingRowData) {
                const formdata = {
                    gradingId: editingGradingId,
                    vesselType: formData.vesselText || editingRowData.vesselType,
                    templateName: formData.partText || editingRowData.templateName,
                    sectionName: formData.sectionText || editingRowData.sectionName,
                    sectionId: formData.sectionValue ? String(formData.sectionValue) : (editingSectionId != null ? String(editingSectionId) : undefined),
                    gradingName: GradingName,
                    isActive: activationStatus === true,
                    requiredInReport: reportStatus === true,
                    templateId: formData.partValue && formData.partValue !== -1 ? Number(formData.partValue) : undefined,
                };

                await gradingService.updateGrading(formdata);

                handleCloseOffcanvas();
                await popup.success({
                    title: "Edit Grading",
                    message: resultMessage(GradingName || "Grading", "Updated")
                });
            } else {
                const formdata = {
                    vesselType: formData.vesselText,
                    templateName: formData.partText,
                    sectionName: formData.sectionText,
                    sectionId: formData.sectionValue ? String(formData.sectionValue) : undefined,
                    gradingName: GradingName,
                    isActive: activationStatus === true,
                    requiredInReport: reportStatus === true,
                };

                await gradingService.createGrading(formdata);

                handleCloseOffcanvas();
                await popup.success({
                    title: "Create Grading",
                    message: resultMessage(GradingName || "Grading", "Created")
                });
            }


            loadGradings();
        } catch (err) {
        } finally {
            setIsSaving(false);
        }
    };


    const cleanString = (str: string) =>
        str
            ? str
                .toString()
                .normalize("NFKD")
                .replace(/\s+/g, " ")
                .replace(/[\u200B-\u200D\uFEFF]/g, "")
                .trim()
                .toLowerCase()
            : "";

    const applyColumnFilters = (
        data: ManageGradingRow[],
        activeFilters: Record<string, unknown>,
        skipKey?: string | null
    ) => {
        const filterEntries = Object.entries(activeFilters);
        if (filterEntries.length === 0) {
            return data;
        }

        return data.filter((row) =>
            filterEntries.every(([columnId, filter]) => {
                if (columnId === skipKey) return true;

                const { searchTerm, selectedFilters } = filter as {
                    searchTerm?: string;
                    selectedFilters?: unknown[];
                };

                if (selectedFilters && selectedFilters.length > 0) {
                    const rowValue = (row as any)[columnId];
                    if (typeof rowValue === 'boolean') {
                        return selectedFilters.some(f => {
                            const fStr = String(f).toLowerCase();
                            if (fStr === 'active' || fStr === 'true' || fStr === 'yes' || fStr === 'checked') {
                                return rowValue === true;
                            }
                            if (fStr === 'inactive' || fStr === 'false' || fStr === 'no' || fStr === 'unchecked') {
                                return rowValue === false;
                            }
                            return false;
                        });
                    }
                    return selectedFilters.some(f =>
                        cleanString(String(f)) === cleanString(String(rowValue))
                    );
                }

                if (!searchTerm) return true;

                const rowValue = cleanString((row as any)[columnId] || '');
                return rowValue.includes(cleanString(searchTerm));
            })
        );
    };

    const searchVessels = useCallback((
        filters: any,
        pageIndex: number,
        pageSize: any,
        searchText?: string | null,
        hierarchicalOverride?: typeof hierarchicalFilters,
        dataOverride?: ManageGradingRow[]
    ) => {
        const pageSizeNum = getPageSizeNum(pageSize);
        const pageIndexNum = typeof pageIndex === 'string' ? parseInt(pageIndex, 10) : Number(pageIndex);

        const currentSearchText = searchText !== undefined ? searchText : searchApplied;
        const activeHierarchical = hierarchicalOverride ?? hierarchicalFilters;

        let dataToPaginate = dataOverride ?? GradingFilter;

        if (activeHierarchical.vesselType) {
            dataToPaginate = dataToPaginate.filter(item =>
                item.vesselType === activeHierarchical.vesselType
            );
        }
        if (activeHierarchical.templateName) {
            dataToPaginate = dataToPaginate.filter(item =>
                item.templateName === activeHierarchical.templateName
            );
        }
        if (activeHierarchical.sectionName) {
            dataToPaginate = dataToPaginate.filter(item =>
                item.sectionName === activeHierarchical.sectionName
            );
        }
        if (activeHierarchical.gradingName) {
            dataToPaginate = dataToPaginate.filter(item =>
                item.gradingName === activeHierarchical.gradingName
            );
        }
        if (currentSearchText) {
            dataToPaginate = dataToPaginate.filter((item) => {
                const searchLower = currentSearchText.toLowerCase();
                return (
                    item.templateName?.toLowerCase().includes(searchLower) ||
                    item.sectionName?.toLowerCase().includes(searchLower) ||
                    item.gradingName?.toLowerCase().includes(searchLower) ||
                    item.vesselType?.toLowerCase().includes(searchLower)
                );
            });
        }

        const activeFilters = filters?.filters || appliedFilters.filters || {};

        const fullyFilteredData = applyColumnFilters(dataToPaginate, activeFilters);

        const startIndex = pageIndexNum * pageSizeNum;
        const endIndex = startIndex + pageSizeNum;
        const paginatedData = fullyFilteredData.slice(startIndex, endIndex);

        setGradings(paginatedData);
        setDataCount(fullyFilteredData.length);
    }, [GradingFilter, searchApplied, hierarchicalFilters, appliedFilters]);

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

                const pageSizeNum = getPageSizeNum(filtersObj.pageSize);
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

                searchVessels(
                    { ...updatedFilters, filters: filteredData },
                    pageIndexNum,
                    pageSizeNum
                );

                return updatedFilters;
            });
        },
        [searchVessels]
    );

    const onChangeSearchInput = useCallback((value: string) => {
        updateVal(value);

        if (value.trim() === "") {
            updateSearchApplied(null);
            if (GradingFilter && GradingFilter.length > 0) {
                const pagesize = getPageSizeNum(appliedFilters.pageSize);
                setAppliedFilters(prev => ({
                    ...prev,
                    pageIndex: 0,
                    resetPageIndex: true
                }));
                searchVessels({ filters: appliedFilters.filters }, 0, pagesize, null);
            }
        } else {
            updateSearchApplied(value);
            if (GradingFilter && GradingFilter.length > 0) {
                const pagesize = getPageSizeNum(appliedFilters.pageSize);
                setAppliedFilters(prev => ({
                    ...prev,
                    pageIndex: 0,
                    resetPageIndex: true
                }));
                searchVessels({ filters: appliedFilters.filters }, 0, pagesize, value);
            }
        }
    }, [GradingFilter, appliedFilters.pageSize, appliedFilters.filters, searchVessels]);

    const loadGradings = async () => {
        try {
            setLoading(true);

            const res = await gradingService.getGradings();
            let dataArray: ManageGradingRow[] = [];
            if (Array.isArray(res.data)) {
                dataArray = res.data;
            } else if (res.data?.data && Array.isArray(res.data.data)) {
                dataArray = res.data.data;
            } else if (res.data?.Data && Array.isArray(res.data.Data)) {
                dataArray = res.data.Data;
            }

            const mappedData = dataArray.map(item => ({
                ...item,
                statusText: item.isActive ? 'Active' : 'Inactive'
            }));

            setGradingFilter(mappedData);

            const pageSize = getPageSizeNum(appliedFilters.pageSize);
            const pageIndex = appliedFilters.pageIndex || 0;

            const activeHierarchical = hierarchicalFilters.vesselType
                ? hierarchicalFilters
                : (() => {
                    const firstVesselType = getFirstVesselType(mappedData);
                    return firstVesselType ? { vesselType: firstVesselType } : {};
                })();

            if (!hierarchicalFilters.vesselType && activeHierarchical.vesselType) {
                setHierarchicalFilters(activeHierarchical);
            }

            searchVessels(
                { filters: appliedFilters.filters },
                pageIndex,
                pageSize,
                searchApplied,
                activeHierarchical,
                mappedData
            );

            sessionStorage.setItem("GradingList", JSON.stringify(mappedData));
        } catch (error) {
        } finally {
            setLoading(false);
        }
    };

    const restoreFilters = () => {
        const searchFilter = localStorage.getItem("SearchGradingFilter");
        if (searchFilter) {
            updateVal(searchFilter);
            updateSearchApplied(searchFilter);
        }
    };


    const [editingGradingId, setEditingGradingId] = useState<number | null>(null);
    const [editingSectionId, setEditingSectionId] = useState<string | null>(null);
    const [editingRowData, setEditingRowData] = useState<ManageGradingRow | null>(null);


    const handleEditGrading = async (row: ManageGradingRow) => {
        try {
            setFormErrors({});
            setError(false);
            setResponseMsg(false);

            setEditingGradingId(row.gradingId);
            setEditingSectionId(row.sectionId);
            setEditingRowData(row);

            const vesselVal = row.vesselType;

            const vesselType = row.vesselType;

            setselectedVesselType(vesselType);

            setVesselOptions([{
                text: row.vesselType,
                value: vesselVal
            }]);

            const partVal = (row as any).templateId || -1;
            setPartOptions([{
                text: row.templateName,
                value: partVal
            }]);

            const sectionVal = row.sectionId;
            setSectionOptions([{
                text: row.sectionName,
                value: sectionVal
            }]);

            setFormData({
                vesselText: row.vesselType,
                vesselValue: vesselVal,
                partText: row.templateName,
                partValue: partVal,
                sectionText: row.sectionName,
                sectionValue: sectionVal,
                grading_name: row.gradingName
            });
            setGradingName(row.gradingName || "");
            setActivationStatus(row.isActive);
            setReportStatus(row.requiredInReport);
            setOpen(true);
        } catch (error) { }
    };


    const resetForm = () => {
        setFormData({
            vesselValue: null,
            vesselText: "",
            partValue: null,
            partText: "",
            sectionValue: "",
            sectionText: "",
            grading_name: "",
        });
        setGradingName("");
        setActivationStatus(true);
        setReportStatus(true);
        setPartOptions([]);
        setSectionOptions([]);
        setselectedVesselType("");

        setEditingGradingId(null);
        setEditingSectionId(null);
        setEditingRowData(null);

        loadVesselOptions();

        setFormErrors({});
        setError(false);
        setResponseMsg(false);
    };

    const handleCloseOffcanvas = () => {
        setOpen(false);
        resetForm();
    };

    const handleOpenAddNewGrading = () => {
        resetForm();
        setOpen(true);
    };


    const columnUniqueValues = useMemo(() => {
        const filters = appliedFilters.filters || {};

        const computeUniq = (targetKey: keyof ManageGradingRow) => {
            let src = GradingFilter || [];

            if (hierarchicalFilters.vesselType) src = src.filter(item => item.vesselType === hierarchicalFilters.vesselType);
            if (hierarchicalFilters.templateName) src = src.filter(item => item.templateName === hierarchicalFilters.templateName);
            if (searchApplied) {
                src = src.filter(item =>
                    String(item.gradingName).toLowerCase().includes(searchApplied.toLowerCase()) ||
                    String(item.sectionName).toLowerCase().includes(searchApplied.toLowerCase())
                );
            }

            const filteredSrc = applyColumnFilters(src, filters, targetKey);
            const vals = Array.from(new Set(filteredSrc.map(r => r[targetKey]).filter(v => v != null && v !== '')));
            return vals.sort();
        };

        return {
            vesselType: computeUniq('vesselType'),
            templateName: computeUniq('templateName'),
            sectionName: computeUniq('sectionName'),
            gradingName: computeUniq('gradingName'),
            statusText: computeUniq('statusText'),
        };
    }, [GradingFilter, appliedFilters.filters, hierarchicalFilters, searchApplied]);

    const columns = useMemo<Array<any>>(
        () => [
            {
                label: "Vessel Template",
                accessorKey: "vesselType",
                accessor: "vesselType",
                columnValues: { type: "both", data: columnUniqueValues.vesselType },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },
            {
                label: "Part Name",
                accessorKey: "templateName",
                accessor: "templateName",
                columnValues: { type: "both", data: columnUniqueValues.templateName },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },
            {
                label: "Section Name",
                accessorKey: "sectionName",
                accessor: "sectionName",
                columnValues: { type: "both", data: columnUniqueValues.sectionName },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },
            {
                label: "Grading Name",
                accessorKey: "gradingName",
                accessor: "gradingName",
                columnValues: { type: "both", data: columnUniqueValues.gradingName },
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
                label: "Required in Export",
                accessorKey: "requiredInReport",
                accessor: "requiredInReport",
                filtering: false,
                sorting: false,
                Cell: ({ cell: { value } }: { cell: { value: any } }) => {
                    return (
                        <div className='d-flex justify-content-center gap-3'>
                            <CheckboxInput
                                label=" "
                                checked={value || false}
                                onChange={() => { }}
                                disabled
                            />
                        </div>
                    );
                }
            },
            {
                label: "Action",
                accessorKey: "action",
                accessor: "action",
                filtering: false,
                sorting: false,
                Cell: ({ row }: { row: { original: ManageGradingRow } }) => {
                    return (
                        <div className='d-flex justify-content-start gap-3'>
                            <Button
                                variant={BUTTONS.TABLEDOWNLOAD}
                                startIcon={<Icon.PencilFill />}
                                onClick={() => handleEditGrading(row.original)}
                            ></Button>
                            <Button variant={BUTTONS.TABLEDOWNLOAD} onClick={() => {
                                handleDeleteClick(row.original);
                            }} startIcon={<Icon.TrashFill />}></Button>
                        </div>
                    );
                }
            },
        ],
        [columnUniqueValues]
    );

    const handleClearFilter = () => {
        updateVal("");
        updateSearchApplied(null);
        localStorage.removeItem("SearchGradingFilter");
        localStorage.removeItem("GradingFilter");

        const firstVesselType = getFirstVesselType(GradingFilter);
        const resetHierarchical = firstVesselType ? { vesselType: firstVesselType } : {};
        setHierarchicalFilters(resetHierarchical);

        setAppliedFilters(prev => ({
            ...prev,
            pageIndex: 0,
            resetPageIndex: true,
            filters: {},
            searchType: "list",
        }));

        if (GradingFilter && GradingFilter.length > 0) {
            const pagesize = getPageSizeNum(appliedFilters.pageSize);
            searchVessels({ filters: {} }, 0, pagesize, null, resetHierarchical);
        }
    };

    const handleHierarchicalFilterChange = useCallback((filters: {
        vesselType?: string;
        templateName?: string;
        sectionName?: string
    }) => {
        setHierarchicalFilters(filters);
        setAppliedFilters(prev => ({
            ...prev,
            pageIndex: 0,
            resetPageIndex: true,
            filters: {},
            searchType: "all",
        }));

        const pageSize = getPageSizeNum(appliedFilters.pageSize);
        searchVessels({ filters: {} }, 0, pageSize, undefined, filters);
    }, [appliedFilters.pageSize, searchVessels]);

    const handleDeleteClick = async (row: ManageGradingRow) => {
        const confirmed = await popup.confirm({
            title: "Delete Grading",
            message: `Are you sure you want to delete ${row.gradingName} Grading?`,
            confirmText: "Delete",
            danger: true
        });

        if (!confirmed) return;

        setIsSaving(true);
        try {
            await gradingService.deleteGrading(row.gradingId ?? 0, row.tanktypeId ?? 0);
            await popup.success({
                title: "Delete Grading",
                message: resultMessage(row.gradingName || "Grading", "Deleted")
            });
            loadGradings();
        } catch (err: any) {
            popup.error({ message: err?.message || "Failed to delete grading" });
        } finally {
            setIsSaving(false);
        }
    };


    const gradingHeaderTitle = useMemo(() => {
        if (hierarchicalFilters.sectionName) {
            return `${hierarchicalFilters.sectionName} - Grading`;
        }
        if (hierarchicalFilters.templateName) {
            return `${hierarchicalFilters.templateName} - Grading`;
        }
        if (hierarchicalFilters.vesselType) {
            return `${hierarchicalFilters.vesselType} - Grading`;
        }
        return "Grading";
    }, [hierarchicalFilters]);


    return (
        <>
            <div className="page-title">
                <BreadcrumbBar
                    pageName="Manage Grading"
                    parentPages={[
                        {
                            name: "Project Configuration",
                            link: ""
                        }
                    ]}>
                    <Button onClick={handleOpenAddNewGrading}>Add New Grading</Button>
                </BreadcrumbBar>
            </div>
            <div className="page-content">
                <div className="grid-container">
                    <Offcanvas show={showMobileSidebar} onHide={() => setShowMobileSidebar(false)} placement="end" className="d-md-none">
                        <Offcanvas.Header closeButton>
                            <Offcanvas.Title>Filters</Offcanvas.Title>
                        </Offcanvas.Header>
                        <Offcanvas.Body>
                            <ManageGradingTemplateList
                                data={GradingFilter}
                                onFilterChange={handleHierarchicalFilterChange}
                                selectedFilters={hierarchicalFilters}
                            />
                        </Offcanvas.Body>
                    </Offcanvas>
                    <div className="grid-sidebar d-none d-md-block">
                        <ManageGradingTemplateList
                            data={GradingFilter}
                            onFilterChange={handleHierarchicalFilterChange}
                            selectedFilters={hierarchicalFilters}
                        />
                    </div>
                    <div className="grid-content-body">
                        <div className="row align-items-center mb-3">
                            <div className="col-8 col-md-8">
                                <h5 className='_600 mb-0'>{gradingHeaderTitle}</h5>
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
                                            handleClearFilter();
                                        }}
                                        value={val}
                                        handleChange={(e: any) => onChangeSearchInput(e.target.value)}
                                        handleSearch={() => {
                                            updateSearchApplied(val === "" ? null : val);
                                        }}
                                        placeholder="Search by Part Name, Section Name,Grading Name"
                                        searchApplied={searchApplied !== null}
                                    />
                                    <a onClick={() => handleClearFilter()} href="#" className='text-link text-nowrap _500'>Clear Filters</a>
                                </div>
                            </div>
                        </div>
                        <div className="custom-react-table">
                            <AbsReactTable
                                loading={loading}
                                columns={columns}
                                data={gradings}
                                pagination={true}
                                expand={false}
                                allowColumnsShowHide={false}
                                shadow={true}
                                allDataProvided
                                dataCount={dataCount}
                                applyFilterCallback={applyFilter}
                                appliedFilters={{
                                    ...appliedFilters,
                                    pageSize: appliedFilters.pageSize.toString(),
                                    pageIndex: appliedFilters.pageIndex.toString(),
                                    searchType: "all"
                                }}
                                pageOptions={["50", "100", "150", "200"]}
                            />
                        </div>
                    </div>
                </div>
            </div>
            <Offcanvas show={!!open} placement={"end"} onHide={handleCloseOffcanvas} backdrop="static">
                <Offcanvas.Header closeButton>
                    <Offcanvas.Title>
                        {editingGradingId ? "Edit ABS Grading" : "Add New ABS Grading"}
                    </Offcanvas.Title>
                </Offcanvas.Header>
                <Offcanvas.Body>
                    <div className="row">
                        <div className="col-md-12 form-group">
                            <div className="d-flex align-items-center gap-1 mb-2">
                                <Label name="Vessel Template" bold />
                                <span className="required" style={{ color: 'red' }}>*</span>
                            </div>
                            <AbsSelect
                                mult={false}
                                options={vesselOptions}
                                disabled={!!editingGradingId}
                                selected={formData.vesselValue ? [formData.vesselValue] : []}

                                onChange={(selected: any[]) => {
                                    const selectedValue = selected?.[0];
                                    if (!selectedValue) {
                                        setFormData(prev => ({
                                            ...prev,
                                            vesselValue: null,
                                            vesselText: "",
                                            partValue: null,
                                            partText: "",
                                            sectionValue: "",
                                            sectionText: ""
                                        }));
                                        setPartOptions([]);
                                        setSectionOptions([]);
                                        if (formErrors.vesselType) {
                                            setFormErrors(prev => {
                                                const newErrors = { ...prev };
                                                delete newErrors.vesselType;
                                                return newErrors;
                                            });
                                        }
                                        return;
                                    }

                                    const opt = vesselOptions.find(option => option.value === selectedValue);
                                    if (!opt) return;

                                    setFormData(prev => ({
                                        ...prev,
                                        vesselValue: opt.value,
                                        vesselText: opt.text,
                                        partValue: null,
                                        partText: "",
                                        sectionValue: "",
                                        sectionText: ""
                                    }));

                                    setPartOptions([]);
                                    setSectionOptions([]);

                                    if (formErrors.vesselType) {
                                        setFormErrors(prev => {
                                            const newErrors = { ...prev };
                                            delete newErrors.vesselType;
                                            return newErrors;
                                        });
                                    }

                                    onVesselChange(opt.text);
                                }}
                                placeholder="Select Vessel Template"
                            />
                            {(error && !editingGradingId && (!formData.vesselValue || formData.vesselValue == null)) ?
                                <div><label style={{ color: 'red' }}>{formErrors.vesselType || "Please select Vessel Type"}</label></div> :
                                <></>
                            }
                        </div>
                        <div className="col-md-12 form-group">
                            <div className="d-flex align-items-center gap-1 mb-2">
                                <Label name="Part Name" bold />
                                <span className="required" style={{ color: 'red' }}>*</span>
                            </div>
                            <AbsSelect
                                mult={false}
                                options={partOptions}
                                disabled={!!editingGradingId}
                                selected={formData.partValue ? [formData.partValue] : []}
                                onChange={(selected: any[]) => {
                                    const selectedValue = selected?.[0];
                                    if (!selectedValue) {
                                        setFormData(prev => ({
                                            ...prev,
                                            partValue: null,
                                            partText: "",
                                            sectionValue: "",
                                            sectionText: ""
                                        }));
                                        setSectionOptions([]);
                                        if (formErrors.templateName) {
                                            setFormErrors(prev => {
                                                const newErrors = { ...prev };
                                                delete newErrors.templateName;
                                                return newErrors;
                                            });
                                        }
                                        return;
                                    }

                                    const opt = partOptions.find(option => option.value === selectedValue);
                                    if (!opt) return;

                                    setFormData(prev => ({
                                        ...prev,
                                        partValue: Number(opt.value) as number,
                                        partText: opt.text,
                                        sectionValue: "",
                                        sectionText: ""
                                    }));
                                    setSectionOptions([]);

                                    if (formErrors.templateName) {
                                        setFormErrors(prev => {
                                            const newErrors = { ...prev };
                                            delete newErrors.templateName;
                                            return newErrors;
                                        });
                                    }

                                    onTemplateChange(opt.text);
                                }}
                                placeholder="Select Part Name"
                            />
                            {(error && !editingGradingId && (!formData.partValue || formData.partValue == null || formData.partValue === -1)) ?
                                <div><label style={{ color: 'red' }}>{formErrors.templateName || "Please select Template Name"}</label></div> :
                                <></>
                            }
                        </div>
                        <div className="col-md-12 form-group">
                            <div className="d-flex align-items-center gap-1 mb-2">
                                <Label name="Section Name" bold />
                                <span className="required" style={{ color: 'red' }}>*</span>
                            </div>
                            <AbsSelect
                                mult={false}
                                options={sectionOptions}
                                disabled={!!editingGradingId}
                                selected={formData.sectionValue ? [formData.sectionValue] : []}
                                onChange={(selected: any[]) => {
                                    const selectedValue = selected?.[0];
                                    if (!selectedValue) {
                                        setFormData(prev => ({
                                            ...prev,
                                            sectionValue: "",
                                            sectionText: ""
                                        }));
                                        if (formErrors.sectionName) {
                                            setFormErrors(prev => {
                                                const newErrors = { ...prev };
                                                delete newErrors.sectionName;
                                                return newErrors;
                                            });
                                        }
                                        return;
                                    }

                                    const opt = sectionOptions.find(option =>
                                        String(option.value) === String(selectedValue)
                                    );
                                    if (!opt) return;

                                    setFormData(prev => ({
                                        ...prev,
                                        sectionValue: opt.value,
                                        sectionText: opt.text
                                    }));

                                    if (formErrors.sectionName) {
                                        setFormErrors(prev => {
                                            const newErrors = { ...prev };
                                            delete newErrors.sectionName;
                                            return newErrors;
                                        });
                                    }
                                }}
                                placeholder="Select Section Name"
                            />
                            {(error && !editingGradingId && (!formData.sectionValue || formData.sectionValue == null || formData.sectionValue === "")) ?
                                <div><label style={{ color: 'red' }}>{formErrors.sectionName || "Please select Section Name"}</label></div> :
                                <></>
                            }
                        </div>

                        <div className="col-md-12 form-group">
                            <div className="d-flex align-items-center gap-1 mb-2">
                                <Label name="Grading Name" bold />
                                <span className="required" style={{ color: 'red' }}>*</span>
                            </div>
                            <Input
                                placeholder="Enter Grading Name"
                                value={GradingName}
                                onChange={(value) => {
                                    setGradingName(value);
                                    if (formErrors.gradingName && value.trim() !== "") {
                                        setFormErrors(prev => {
                                            const newErrors = { ...prev };
                                            delete newErrors.gradingName;
                                            return newErrors;
                                        });
                                    }
                                }}
                            />
                            {(error && (!GradingName || GradingName == null || GradingName.trim() === "")) ?
                                <div><label style={{ color: 'red' }}>{formErrors.gradingName || "Grading Name Required"}</label></div> :
                                <></>
                            }
                            {(responseMsg) ?
                                <div><label style={{ color: 'red' }}>Grading Already Exist</label></div> :
                                <></>
                            }
                        </div>

                        <div className="col-md-12 form-group">
                            <Label name="Activation Status" bold />
                            <div className='d-flex justify-contentn-start gap-3'>
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
                        <div className="col-md-12 form-group">
                            <Label name="Report Configuration" bold />
                            <div className='d-flex justify-contentn-start gap-3'>
                                <RadioInput
                                    label="Required in Export"
                                    checked={reportStatus === true}
                                    onChange={() => setReportStatus(true)}
                                />

                                <RadioInput
                                    label="Exclude in Export"
                                    checked={reportStatus === false}
                                    onChange={() => setReportStatus(false)}
                                />

                            </div>
                        </div>
                        <div className="col-md-12 form-group">
                            <Divider className="divider-1" />
                        </div>
                        <div className="col-md-12 form-group">
                            <div className="d-flex gap-2">
                                <Button variant={BUTTONS.PRIMARY} onClick={handleCreate} disabled={isSaving}>{editingGradingId ? "Update" : "Save"}</Button>
                                <Button variant={BUTTONS.SECONDARY} onClick={handleCloseOffcanvas}>
                                    Cancel
                                </Button>

                            </div>


                        </div>

                    </div>
                </Offcanvas.Body>
            </Offcanvas>


        </>
    );
};

export default ManageGradingList;