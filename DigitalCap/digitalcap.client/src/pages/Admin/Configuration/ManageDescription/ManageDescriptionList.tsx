import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { BreadcrumbBar, Button, AbsReactTable, BUTTONS, Divider, Input, AbsSelect, AbsSearch, RadioInput, Label } from "customer_portal-ui-shared";
import * as Icon from 'react-bootstrap-icons';
import ManageDescriptionTemplateList from './ManageDescTemplateList';
import { usePopup } from '../../../../Common/PopupProvider';
import { resultMessage } from '../../../../Common/UtilityMessage';
import { Offcanvas } from 'react-bootstrap';
import * as descriptionService from '../../../../service/DescriptionService';
import * as gradingService from '../../../../service/GradingService';

interface ManageDescriptionRow {

    id: number;
    sectionId: number;
    tanktypeId: number;
    vesselType: string;
    templateName: string;
    sectionName: string;
    description: string;
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

    description_name: string;
};

type SelectOption = {
    text: string;
    value: number | string;
};
const ManageDescriptionList: React.FC = () => {
    const popup = usePopup();
    const [showMobileSidebar, setShowMobileSidebar] = useState(false);
    const [val, updateVal] = React.useState<string>("");
    const [searchApplied, updateSearchApplied] = React.useState<string | null>(null);
    const [fullFilteredDescriptions, setFullFilteredDescriptions] = useState<ManageDescriptionRow[]>([]);
    const [descriptions, setDescriptions] = useState<ManageDescriptionRow[]>([]);
    const [loading, setLoading] = useState<boolean>(false);
    const [DescriptionFilter, setDescriptionFilter] = useState<ManageDescriptionRow[]>([]);
    const [hierarchicalFilters, setHierarchicalFilters] = useState<{
        vesselType?: string;
        templateName?: string;
        sectionName?: string;
        Description?: string;
    }>({});
    const [error, setError] = React.useState<boolean>();
    const [open, setOpen] = React.useState<boolean>();
    const [formData, setFormData] = useState<FormData>({
        vesselValue: null,
        vesselText: "",
        partValue: null,
        partText: "",
        sectionValue: "",
        sectionText: "",
        description_name: ""
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

    const getFirstVesselType = (rows: ManageDescriptionRow[]) => {
        for (const row of rows) {
            if (row.vesselType) {
                return row.vesselType;
            }
        }
        return undefined;
    };

    const mapDescriptionRows = (rows: ManageDescriptionRow[]) =>
        rows.map(item => ({
            ...item,
            statusText: item.isActive ? 'Active' : 'Inactive',
        }));

    const [appliedFilters, setAppliedFilters] = React.useState({
        ...initialFilterState,
    });
    const [vesselOptions, setVesselOptions] = useState<SelectOption[]>([]);
    const [partOptions, setPartOptions] = useState<SelectOption[]>([]);
    const [sectionOptions, setSectionOptions] = useState<SelectOption[]>([]);
    const [selectedVesselType, setselectedVesselType] = useState("");
    const [DescriptionName, setDescriptionName] = useState("");
    const [responseMsg, setResponseMsg] = React.useState<boolean>();
    const [activationStatus, setActivationStatus] = useState<boolean | null>(true);

    useEffect(() => {
        gradingService.getVesselType().then(res => {
            setVesselOptions(
                res.data.data.map((v: any) => ({
                    text: v.vesselType,
                    value: v.id
                }))
            );
        });
    }, []);


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
        if (!templateName || !selectedVesselType) return;

        descriptionService.getSectionsByTemplateAndVessel(templateName, selectedVesselType)
            .then(res => {
                setSectionOptions(

                    res.data.map((s: any) => ({
                        text: s.sectionName,
                        value: s.sectionId,
                    }))
                );
            })
            .catch(() => {
                setSectionOptions([]);
            });
    };


    const isDuplicateDescription = () => {
        return DescriptionFilter.some(g =>
            g.vesselType === formData.vesselText &&
            g.templateName === formData.partText &&
            g.sectionName === formData.sectionText &&
            g.description.trim().toLowerCase() === DescriptionName.trim().toLowerCase() &&
            g.id !== editingDescriptionId
        );
    };

    const handleCreate = async () => {
        if (!formData.vesselText || !formData.partText || !formData.sectionText || !DescriptionName.trim()) {
            setError(true);
            return;
        }
        if (isDuplicateDescription()) {
            setResponseMsg(true);
            return;
        }

        const payload = {
            id: Number(editingDescriptionId),
            VesselType: formData.vesselText,
            TemplateName: formData.partText,
            SectionName: formData.sectionText,
            DescriptionName: DescriptionName,
            isActive: activationStatus === true,
        };

        const descName = DescriptionName;
        try {
            if (editingDescriptionId) {
                await descriptionService.getEditDescriptionById({
                    descriptionId: Number(editingDescriptionId),
                    sectionId: String(formData.sectionValue ?? ''),
                } as any);
            } else {
                await descriptionService.addNewDescription(payload as any);
            }
            await reloadAllData();

            resetForm();
            setOpen(false);
            popup.success({
                title: "Create Description",
                message: resultMessage(descName || "Description", "Created")
            });
        } catch (err: any) {
            popup.error({ message: err?.message || "Failed to create description." });
        }
    };

    const handleUpdate = async () => {
        const payload = {
            id: Number(editingDescriptionId),
            vesselType: formData.vesselText,
            templateName: formData.partText,
            sectionId: formData.sectionValue,
            sectionName: formData.sectionText,
            descriptionName: DescriptionName,
            isActive: activationStatus ?? false
        };

        const descName = DescriptionName;
        try {
            await descriptionService.updateDescription(payload as any);

            setOpen(false);
            setEditingDescriptionId(null);
            await reloadAllData();
            resetForm();
            popup.success({
                title: "Edit Description",
                message: resultMessage(descName || "Description", "Updated")
            });
        } catch (err: any) {
            popup.error({ message: err?.message || "Failed to update description." });
        }
    };


    const cleanString = useCallback((str: string) =>
        str
            ? str
                .toString()
                .normalize("NFKD")
                .replace(/\s+/g, " ")
                .replace(/[\u200B-\u200D\uFEFF]/g, "")
                .trim()
                .toLowerCase()
            : "", []);

    const applyColumnFilters = useCallback((
        data: ManageDescriptionRow[],
        activeFilters: Record<string, unknown>,
        skipKey: string | null = null
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
                            if (fStr === 'active' || fStr === 'true' || fStr === 'yes') {
                                return rowValue === true;
                            }
                            if (fStr === 'inactive' || fStr === 'false' || fStr === 'no') {
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

                const rowValue = columnId === 'isActive'
                    ? ((row as any)[columnId] ? 'active' : 'inactive')
                    : cleanString((row as any)[columnId] || '');

                return rowValue.includes(cleanString(searchTerm));
            })
        );
    }, [cleanString]);

    const searchVessels = useCallback((
        filters: any,
        _searchType: string,
        _pageIndex: number,
        pageSize: any,
        searchText?: string | null,
        hierarchicalOverride?: typeof hierarchicalFilters
    ) => {
        const activeHierarchical = hierarchicalOverride ?? hierarchicalFilters;

        const currentSearchText = searchText !== undefined ? searchText : searchApplied;

        let dataToPaginate = DescriptionFilter;

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

        if (activeHierarchical.Description) {
            dataToPaginate = dataToPaginate.filter(item =>
                item.description === activeHierarchical.Description
            );
        }

        if (currentSearchText) {
            dataToPaginate = dataToPaginate.filter((item) => {
                const searchLower = currentSearchText.toLowerCase();
                return (
                    item.templateName?.toLowerCase().includes(searchLower) ||
                    item.sectionName?.toLowerCase().includes(searchLower) ||
                    item.description?.toLowerCase().includes(searchLower) ||
                    item.vesselType?.toLowerCase().includes(searchLower)
                );
            });
        }

        const activeFilters = filters?.filters || {};
        const filteredAllData = applyColumnFilters(dataToPaginate, activeFilters);
        setFullFilteredDescriptions(filteredAllData);

        const pageIndexParsed = typeof filters.pageIndex === "string"
            ? parseInt(filters.pageIndex, 10)
            : Number(filters.pageIndex) || 0;

        const pageSizeParsed = typeof pageSize === "string"
            ? parseInt(pageSize, 10)
            : Number(pageSize) || 50;

        const startIndex = pageIndexParsed * pageSizeParsed;
        const endIndex = startIndex + pageSizeParsed;
        const pageData = filteredAllData.slice(startIndex, endIndex);

        setDescriptions(pageData);
    }, [DescriptionFilter, searchApplied, hierarchicalFilters, applyColumnFilters]);


    const reloadAllData = useCallback(async () => {
        try {
            setLoading(true);
            const res = await descriptionService.getDescriptions(1, 0);

            let rows: ManageDescriptionRow[] = [];

            if (res?.data?.data) {
                const apiData = res.data.data;
                if (apiData.data && Array.isArray(apiData.data)) {
                    rows = apiData.data;
                }
            }

            setDescriptionFilter(mapDescriptionRows(rows));
            const pageSize = typeof appliedFilters.pageSize === 'string'
                ? parseInt(appliedFilters.pageSize, 10)
                : Number(appliedFilters.pageSize) || 50;

            const pageIndex = appliedFilters.pageIndex || 0;
            const activeHierarchical = hierarchicalFilters.vesselType
                ? hierarchicalFilters
                : { vesselType: getFirstVesselType(rows) };

            if (!hierarchicalFilters.vesselType && activeHierarchical.vesselType) {
                setHierarchicalFilters(activeHierarchical);
            }

            searchVessels({}, "list", pageIndex, pageSize, undefined, activeHierarchical);
        } catch (error) {
        } finally {
            setLoading(false);
        }
    }, [appliedFilters.pageSize, appliedFilters.pageIndex, hierarchicalFilters, searchApplied, searchVessels]);


    const applyFilter = useCallback((filtersObj: any) => {
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

            let pageIndexNum = typeof filtersObj.pageIndex === "string"
                ? parseInt(filtersObj.pageIndex, 10)
                : Number(filtersObj.pageIndex) || 0;

            if (filtersChanged) {
                pageIndexNum = 0;
            }

            const pageSizeNum = typeof filtersObj.pageSize === "string"
                ? parseInt(filtersObj.pageSize, 10)
                : Number(filtersObj.pageSize) || 50;

            const searchType = filtersObj.searchType || "list";

            const updatedFilters = {
                ...filtersObj,
                pageIndex: pageIndexNum,
                filters: filteredData,
                sort: filtersObj.sort,
                searchType,
                pageSize: filtersObj.pageSize,
                resetPageIndex: filtersChanged,
            };

            searchVessels(
                { ...updatedFilters, filters: filteredData },
                searchType,
                pageIndexNum,
                pageSizeNum
            );

            return updatedFilters;
        });
    }, [searchVessels]);


    useEffect(() => {
        const loadInitialData = async () => {
            try {
                setLoading(true);
                const res = await descriptionService.getDescriptions(1, 0);

                let rows: ManageDescriptionRow[] = [];

                if (res?.data?.data) {
                    const apiData = res.data.data;
                    if (apiData.data && Array.isArray(apiData.data)) {
                        rows = apiData.data;
                    }
                }

                const mappedRows = mapDescriptionRows(rows);
                setDescriptionFilter(mappedRows);

                const pageSize = typeof appliedFilters.pageSize === 'string'
                    ? parseInt(appliedFilters.pageSize, 10)
                    : Number(appliedFilters.pageSize) || 50;

                const firstVesselType = getFirstVesselType(mappedRows);
                const initialHierarchical = firstVesselType
                    ? { vesselType: firstVesselType }
                    : {};

                if (firstVesselType) {
                    setHierarchicalFilters(initialHierarchical);
                }

                const filteredRows = firstVesselType
                    ? mappedRows.filter(item => item.vesselType === firstVesselType)
                    : mappedRows;

                setDescriptions(filteredRows.slice(0, pageSize));
            } catch (error) {
            } finally {
                setLoading(false);
            }
        };

        loadInitialData();
    }, []);

    const onChangeSearchInput = useCallback((value: string) => {
        updateVal(value);

        if (value.trim() === "") {
            updateSearchApplied(null);
            if (DescriptionFilter && DescriptionFilter.length > 0) {
                const pagesize = appliedFilters.pageSize != null && appliedFilters.pageSize != ""
                    ? (typeof appliedFilters.pageSize === 'string' ? parseInt(appliedFilters.pageSize, 10) : Number(appliedFilters.pageSize))
                    : 50;

                setAppliedFilters(prev => ({
                    ...prev,
                    pageIndex: 0,
                    resetPageIndex: true
                }));
                searchVessels({ filters: appliedFilters.filters }, "list", 0, pagesize, null);
            }
        } else {

            updateSearchApplied(value);
            if (DescriptionFilter && DescriptionFilter.length > 0) {
                const pagesize = appliedFilters.pageSize != null && appliedFilters.pageSize != ""
                    ? (typeof appliedFilters.pageSize === 'string' ? parseInt(appliedFilters.pageSize, 10) : Number(appliedFilters.pageSize))
                    : 50;

                setAppliedFilters(prev => ({
                    ...prev,
                    pageIndex: 0,
                    resetPageIndex: true
                }));
                searchVessels({ filters: appliedFilters.filters }, "list", 0, pagesize, value);
            }
        }
    }, [DescriptionFilter, appliedFilters.pageSize, appliedFilters.filters, searchVessels]);


    const resetForm = () => {
        setFormData({
            vesselValue: null,
            vesselText: "",
            partValue: null,
            partText: "",
            sectionValue: "",
            sectionText: "",
            description_name: ""
        });

        setDescriptionName("");
        setActivationStatus(true);
        setError(false);

        setPartOptions([]);
        setSectionOptions([]);
        setselectedVesselType("");

        setEditingDescriptionId(null);
    };

    const [editingDescriptionId, setEditingDescriptionId] = useState<number | null>(null);

    const handleEditDescription = async (row: ManageDescriptionRow) => {
        try {
            setEditingDescriptionId(row.id);

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
                description_name: row.description
            });

            setDescriptionName(row.description || "");
            setActivationStatus(row.isActive);
            setOpen(true);
        } catch (error) { }
    };

    const columnUniqueValues = useMemo(() => {
        const filters = appliedFilters.filters || {};

        const computeUniq = (targetKey: keyof ManageDescriptionRow) => {
            let src = DescriptionFilter || [];

            if (hierarchicalFilters.vesselType) src = src.filter(item => item.vesselType === hierarchicalFilters.vesselType);
            if (hierarchicalFilters.templateName) src = src.filter(item => item.templateName === hierarchicalFilters.templateName);
            if (hierarchicalFilters.sectionName) src = src.filter(item => item.sectionName === hierarchicalFilters.sectionName);
            if (searchApplied) {
                src = src.filter(item =>
                    String(item.templateName).toLowerCase().includes(searchApplied.toLowerCase()) ||
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
            description: computeUniq('description'),
            isActive: computeUniq('isActive'),
        };
    }, [DescriptionFilter, appliedFilters.filters, hierarchicalFilters, searchApplied, applyColumnFilters]);

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
                label: "Description",
                accessorKey: "description",
                accessor: "description",
                columnValues: { type: "both", data: columnUniqueValues.description },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },
            {
                label: "Status",
                accessorKey: "statusText",
                accessor: "statusText",
                columnValues: { type: "both", data: ['Active', 'Inactive'] },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },

            {
                label: "Action",
                accessorKey: "action",
                accessor: "action",
                filtering: false,
                sorting: false,
                Cell: ({ row }: { row: { original: ManageDescriptionRow } }) => {
                    return (
                        <div className='d-flex justify-content-start gap-3'>
                            <Button
                                variant={BUTTONS.TABLEDOWNLOAD}
                                startIcon={<Icon.PencilFill />}
                                onClick={() => handleEditDescription(row.original)}
                            ></Button>
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
        localStorage.removeItem("SearchDescriptionFilter");
        localStorage.removeItem("DescriptionFilter");

        const firstVesselType = getFirstVesselType(DescriptionFilter);
        const resetHierarchical = firstVesselType ? { vesselType: firstVesselType } : {};
        setHierarchicalFilters(resetHierarchical);

        setAppliedFilters(prev => ({
            ...prev,
            pageIndex: 0,
            resetPageIndex: true,
            filters: {},
            searchType: "list",
        }));

        if (DescriptionFilter && DescriptionFilter.length > 0) {
            const pagesize = appliedFilters.pageSize != null && appliedFilters.pageSize != ""
                ? (typeof appliedFilters.pageSize === 'string' ? parseInt(appliedFilters.pageSize, 10) : Number(appliedFilters.pageSize))
                : 50;

            searchVessels({}, "list", 0, pagesize, null, resetHierarchical);
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

        const pageSize = typeof appliedFilters.pageSize === 'string'
            ? parseInt(appliedFilters.pageSize, 10)
            : Number(appliedFilters.pageSize) || 50;

        searchVessels({ filters: {} }, "all", 0, pageSize, undefined, filters);
    }, [appliedFilters.pageSize, searchVessels]);

    const DescriptionHeaderTitle = useMemo(() => {
        if (hierarchicalFilters.sectionName) {
            return `${hierarchicalFilters.sectionName} - Description`;
        }
        if (hierarchicalFilters.templateName) {
            return `${hierarchicalFilters.templateName} - Description`;
        }
        if (hierarchicalFilters.vesselType) {
            return `${hierarchicalFilters.vesselType} - Description`;
        }
        return "Description";
    }, [hierarchicalFilters]);

    return (
        <>
            <div className="page-title">
                <BreadcrumbBar
                    pageName="Manage Description"
                    parentPages={[
                        {
                            name: "Project Configuration",
                            link: ""
                        }
                    ]}>
                    <Button onClick={() => { resetForm(); setOpen(true); }}>Add New Description</Button>
                </BreadcrumbBar>
            </div>
            <div className="page-content">
                <div className="grid-container">
                    <Offcanvas show={showMobileSidebar} onHide={() => setShowMobileSidebar(false)} placement="end" className="d-md-none">
                        <Offcanvas.Header closeButton>
                            <Offcanvas.Title>Filters</Offcanvas.Title>
                        </Offcanvas.Header>
                        <Offcanvas.Body>
                            <ManageDescriptionTemplateList
                                data={DescriptionFilter as any}
                                onFilterChange={handleHierarchicalFilterChange}
                                selectedFilters={hierarchicalFilters}
                            />
                        </Offcanvas.Body>
                    </Offcanvas>
                    <div className="grid-sidebar d-none d-md-block">
                        <ManageDescriptionTemplateList
                            data={DescriptionFilter as any}
                            onFilterChange={handleHierarchicalFilterChange}
                            selectedFilters={hierarchicalFilters}
                        />
                    </div>
                    <div className="grid-content-body">
                        <div className="row align-items-center mb-3">
                            <div className="col-8 col-md-8">
                                <h5 className='_600 mb-0'>{DescriptionHeaderTitle}</h5>
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
                                        placeholder="Search by Part Name, Section Name, Description"
                                        searchApplied={searchApplied !== null}
                                    />
                                    <a onClick={() => handleClearFilter()} href="#" className='text-link text-nowrap _500'>Clear Filters</a>
                                </div>
                            </div>
                        </div>
                        <div className="custom-react-table" style={{ position: 'relative' }}>
                            <AbsReactTable
                                loading={loading}
                                columns={columns}
                                data={descriptions || []}
                                pagination={true}
                                expand={false}
                                allowColumnsShowHide={true}
                                shadow={true}
                                allDataProvided
                                dataCount={fullFilteredDescriptions ? fullFilteredDescriptions.length : 0}
                                applyFilterCallback={applyFilter}
                                pageIndex={appliedFilters.pageIndex}
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
            <Offcanvas show={open} placement={"end"} onHide={() => { resetForm(); setOpen(false); }} backdrop="static">
                <Offcanvas.Header closeButton>
                    <Offcanvas.Title>{editingDescriptionId ? "Edit Description" : "Add New Description"}</Offcanvas.Title>

                </Offcanvas.Header>
                <Offcanvas.Body>
                    <div className="row">
                        <div className="col-md-12 form-group">
                            <div className="d-flex align-items-center gap-1 mb-2">
                                <Label name="Vessel Template " bold />
                                <span className="required" style={{ color: 'red' }}>*</span>
                            </div>
                            <AbsSelect
                                mult={false}
                                disabled={!!editingDescriptionId}
                                options={vesselOptions}
                                selected={formData.vesselValue ? [formData.vesselValue] : []}

                                onChange={(selected: any[]) => {
                                    const selectedValue = selected?.[0];
                                    if (!selectedValue) return;

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

                                    onVesselChange(opt.text);
                                }}
                                placeholder="Select Vessel Template"
                            />

                            {(error && (!formData.vesselValue || formData.vesselValue == null)) ?
                                <div><label style={{ color: 'red' }}> Please Select Vessel Type</label></div> :
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
                                disabled={!!editingDescriptionId}
                                selected={formData.partValue ? [formData.partValue] : []}
                                onChange={(selected: any[]) => {
                                    const selectedValue = selected?.[0];
                                    if (!selectedValue) return;

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
                                    onTemplateChange(opt.text);
                                }}
                                placeholder="Select Part Name"
                            />
                            {(error && (!formData.partValue || formData.partValue == null)) ?
                                <div><label style={{ color: 'red' }}> Please Select Part Name</label></div> :
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
                                disabled={!!editingDescriptionId}
                                selected={formData.sectionValue ? [formData.sectionValue] : []}
                                onChange={(selected: any[]) => {
                                    const selectedValue = selected?.[0];
                                    if (!selectedValue) return;

                                    const opt = sectionOptions.find(option =>
                                        String(option.value) === String(selectedValue)
                                    );
                                    if (!opt) return;

                                    setFormData(prev => ({
                                        ...prev,
                                        sectionValue: opt.value,
                                        sectionText: opt.text
                                    }));

                                }}
                                placeholder="Select Section Name"
                            />
                            {(error && (!formData.sectionValue || formData.sectionValue == null)) ?
                                <div><label style={{ color: 'red' }}> Please Select Section Name</label></div> :
                                <></>
                            }

                        </div>

                        <div className="col-md-12 form-group">
                            <div className="d-flex align-items-center gap-1 mb-2">
                                <Label name="Description" bold />
                                <span className="required" style={{ color: 'red' }}>*</span>
                            </div>
                            <Input
                                placeholder="Enter the Description"
                                value={DescriptionName}
                                onChange={(value) => {
                                    setDescriptionName(value);
                                }}
                            />
                            {(error && (!DescriptionName || DescriptionName == null)) ?
                                <div><label style={{ color: 'red' }}> Description is  required</label></div> :
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
                            <Divider className="divider-1" />
                        </div>
                        <div className="col-md-12 form-group">
                            <div className="d-flex gap-2">

                                <Button variant={BUTTONS.PRIMARY} onClick={editingDescriptionId ? handleUpdate : handleCreate} > {editingDescriptionId ? "Update" : "Save"}</Button>
                                <Button variant={BUTTONS.SECONDARY} onClick={() => {
                                    resetForm();
                                    setOpen(false);
                                }}> Cancel</Button>

                            </div>
                            {(responseMsg) ?
                                <div><label style={{ color: 'red' }}>Description Already Exist</label></div> :
                                <></>
                            }

                        </div>

                    </div>
                </Offcanvas.Body>
            </Offcanvas>
        </>
    );
};

export default ManageDescriptionList;