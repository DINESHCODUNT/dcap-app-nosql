import React, { useState, useEffect, useCallback, useContext } from 'react';
import { Button, AbsReactTable, BUTTONS, Divider, Input, TextArea, AbsSearch, Label, SideDialogManagerContext } from "customer_portal-ui-shared";
import * as Icon from 'react-bootstrap-icons';
import _uniqueId from "lodash/uniqueId";
import * as clientProfileService from '../../../../service/ClientProfileService';
import { Offcanvas } from 'react-bootstrap';
import { usePopup } from '../../../../Common/PopupProvider';
import { resultMessage } from '../../../../Common/UtilityMessage';
import ClientDetails from './ClientDetails';
import { usePermissions } from '../../../../context/PermissionContext';
import { PERMISSIONS } from '../../../../constants/permissions';
interface ManageClientRow {
    id: string;
    client_name: string;
    wcn: string | string[];
    no_of_projects: string;
    billing_url?: string;
    company_address?: string;
    company_email?: string;
    status: string;
    action?: unknown;
}
type FormData = {
    id: string;
    client_name: string;
    wcn: string;
    no_of_projects: string;
    billing_url: string;
    company_name: string;
    company_address: string;
    company_email: string;
    status: string;
    activation_status: boolean;

};


const ClientList: React.FC = () => {
    const popup = usePopup();
    const [val, updateVal] = React.useState<string>("");
    const [searchApplied, updateSearchApplied] = React.useState<string | null>(null);
    const [createclient, setCreateclient] = React.useState<boolean>();
    const [data, setData] = React.useState<ManageClientRow[]>([]);
    const [loading, setLoading] = React.useState<boolean>(true);
    const [dataCount, setDataCount] = useState(0);
    const [clientFilter, setClientFilter] = useState<ManageClientRow[]>([]);
    const [isSaving, setIsSaving] = React.useState<boolean>(false);
    const [editingClientId, setEditingClientId] = React.useState<string | null>(null);
    const [formErrors, setFormErrors] = React.useState<{ [key: string]: string }>({});
    const { hasGlobalPermission } = usePermissions();
    const hasManageClientProfilesPermission = hasGlobalPermission(PERMISSIONS.MANAGE_CLIENT_PROFILES);
    const sideDialog = useContext(SideDialogManagerContext) as any;
    const [formData, setFormData] = React.useState<FormData>({
        id: "",
        client_name: "",
        wcn: "",
        no_of_projects: "",
        billing_url: "",
        company_name: "",
        company_address: "",
        company_email: "",
        status: "Active",
        activation_status: true
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
        pageSize: "10"
    };
    const [appliedFilters, setAppliedFilters] = React.useState({
        ...initialFilterState,
    });

    useEffect(() => {
        fetchClientProfiles();
        restoreFilters();
    }, []);


    useEffect(() => {
        if (clientFilter && clientFilter.length > 0) {
            const pagesize =
                typeof appliedFilters.pageSize === 'string'
                    ? parseInt(appliedFilters.pageSize, 10)
                    : Number(appliedFilters.pageSize) || 20;

            searchClients({}, 0, pagesize);
        }
    }, [clientFilter]);


    const resetForm = () => {
        setFormData({
            id: "",
            client_name: "",
            wcn: "",
            no_of_projects: "",
            billing_url: "",
            company_name: "",
            company_address: "",
            company_email: "",
            status: "Active",
            activation_status: true
        });

        setEditingClientId(null);

        setFormErrors({});
        setIsSaving(false);
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

    const searchClients = useCallback((
        filters: any,
        pageIndex: number,
        pageSize: any,
        searchText?: string | null,
        clientsData?: ManageClientRow[]
    ) => {
        const filterEntries = Object.entries(filters?.filters || {});

        const pageSizeNum = typeof pageSize === 'string' ? parseInt(pageSize, 10) : Number(pageSize);
        const pageIndexNum = typeof pageIndex === 'string' ? parseInt(pageIndex, 10) : Number(pageIndex);

        let dataToPaginate = clientsData || clientFilter;
        const currentSearchText = searchText !== undefined ? searchText : searchApplied;

        if (currentSearchText) {
            const searchLower = currentSearchText.toLowerCase();
            dataToPaginate = dataToPaginate.filter((item) =>
                item.client_name?.toLowerCase().includes(searchLower) ||
                (Array.isArray(item.wcn) ? item.wcn.join(' ') : (item.wcn || '')).toLowerCase().includes(searchLower) ||
                item.company_address?.toLowerCase().includes(searchLower) ||
                item.company_email?.toLowerCase().includes(searchLower) ||
                String(item.no_of_projects ?? '').toLowerCase().includes(searchLower)
            );
        }

        if (filterEntries.length > 0) {
            dataToPaginate = dataToPaginate.filter(client =>
                filterEntries.every(([columnId, filter]: any) => {
                    if (!filter) return true;
                    const { searchTerm, selectedFilters, isFiltersApplied } = filter;
                    if (!isFiltersApplied) return true;

                    if (selectedFilters && selectedFilters.length > 0) {
                        const rawValue = (client as any)[columnId];
                        const clientValue = Array.isArray(rawValue)
                            ? rawValue.join(', ')
                            : String(rawValue ?? "");
                        return selectedFilters.includes(clientValue);
                    }
                    if (searchTerm && searchTerm.trim() !== "") {
                        const rawValue = (client as any)[columnId];
                        const clientValue = cleanString(
                            Array.isArray(rawValue) ? rawValue.join(' ') : String(rawValue ?? "")
                        );
                        return clientValue.includes(cleanString(searchTerm));
                    }
                    return true;
                })
            );
        }

        const startIndex = pageIndexNum * pageSizeNum;
        const endIndex = startIndex + pageSizeNum;
        const filteredPageData = dataToPaginate.slice(startIndex, endIndex);

        setDataCount(dataToPaginate.length);
        setData(filteredPageData);
    }, [clientFilter, searchApplied]);

    const applyFilter = useCallback(
        (filtersObj: any) => {
            const filteredData = Object.fromEntries(
                Object.entries(filtersObj.filters || {}).filter(
                    ([, value]) =>
                        (value as any)?.searchTerm !== "" ||
                        ((value as any)?.selectedFilters?.length ?? 0) > 0 ||
                        (value as any)?.isFiltersApplied
                )
            );

            const {
                pageIndex: newPageIndex,
                pageSize: newPageSize,
                searchType: newSearchType,
            } = filtersObj;

            const pageIndexNum = typeof newPageIndex === 'string'
                ? parseInt(newPageIndex, 10)
                : Number(newPageIndex) || 0;

            const pageSizeNum = typeof newPageSize === 'string'
                ? parseInt(newPageSize, 10)
                : Number(newPageSize) || 10;

            setAppliedFilters(() => {
                const updatedFilters = {
                    ...filtersObj,
                    pageIndex: pageIndexNum,
                    filters: filteredData,
                    sort: filtersObj.sort,
                    searchType: newSearchType,
                    pageSize: newPageSize,
                    resetPageIndex: false,
                };

                searchClients(
                    { filters: filteredData },
                    pageIndexNum,
                    pageSizeNum
                );

                return updatedFilters;
            });
        },

        [searchClients]
    );

    const onChangeSearchInput = useCallback((value: string) => {
        updateVal(value);

        const pagesize = appliedFilters.pageSize != null && appliedFilters.pageSize != ""
            ? (typeof appliedFilters.pageSize === 'string' ? parseInt(appliedFilters.pageSize, 10) : Number(appliedFilters.pageSize))
            : 20;

        const currentPageIndex = typeof appliedFilters.pageIndex === 'string'
            ? parseInt(appliedFilters.pageIndex, 10)
            : Number(appliedFilters.pageIndex) || 0;

        if (value.trim() === "") {
            updateSearchApplied(null);
            if (clientFilter && clientFilter.length > 0) {
                searchClients({ filters: appliedFilters.filters }, currentPageIndex, pagesize, null);
            }
        } else {
            updateSearchApplied(value);
            if (clientFilter && clientFilter.length > 0) {
                searchClients({ filters: appliedFilters.filters }, currentPageIndex, pagesize, value);
            }
        }
    }, [clientFilter, appliedFilters.pageSize, appliedFilters.pageIndex, appliedFilters.filters, searchClients]);


    const fetchClientProfiles = async () => {
        try {
            setLoading(true);
            const response = await clientProfileService.getClientProfiles();
            if (response.status === 204) {
                setData([]);
                setClientFilter([]);
                setDataCount(0);
                return;
            }

            const payload = response.data && response.data.data ? response.data.data : response.data;

            if (Array.isArray(payload)) {
                const mappedData = payload.map((client: any) => {
                    return {


                        id: client.id || "",
                        client_name: client.companyName || client.client_name || "",
                        wcn: client.wcn || "",
                        no_of_projects: (client.projectCount ?? 0).toString(),
                        billing_url: client.companyBillingSystemURL ?? "",
                        company_address: client.companyAddress ?? "",
                        company_email: client.companyBillingEmail ?? "",
                        status: client.status || "Active",
                        action: client.id || client.clientId || ""
                    }
                });
                setClientFilter(mappedData);
                setDataCount(mappedData.length);
                setData(mappedData);
            } else {
                setData([]);
                setClientFilter([]);
                setDataCount(0);
            }
        } catch (err) {
            setData([]);
            setClientFilter([]);
            setDataCount(0);
        } finally {
            setLoading(false);
        }
    };

    const restoreFilters = () => {
        const searchFilter = localStorage.getItem("SearchClientProfileFilter");
        if (searchFilter) {
            updateVal(searchFilter);
            updateSearchApplied(searchFilter);
        }
    };


    const handleDeleteClick = async (row: ManageClientRow) => {
        const confirmed = await popup.confirm({
            title: "Delete Client",
            message: `Are you sure you want to delete ${row?.client_name} Client?`,
            confirmText: "Delete",
            danger: true
        });
        if (!confirmed) return;

        setIsSaving(true);
        try {
            await clientProfileService.deleteClientProfile(row.id);
            await fetchClientProfiles();
            popup.success({
                title: "Delete Client",
                message: resultMessage(row.client_name || "Client", "Deleted")
            });
        } catch (err: any) {
            popup.error({ message: err?.message || "Failed to delete client." });
        } finally {
            setIsSaving(false);
        }
    };

    const handleEditClient = async (row: ManageClientRow) => {
        setEditingClientId(row.id);
        setFormData({
            id: row.id,
            client_name: row.client_name ?? "",
            wcn: typeof row.wcn === "string" ? row.wcn : row.wcn.join(", "),
            no_of_projects: row.no_of_projects,
            billing_url: row.billing_url ?? "",
            company_name: row.client_name,
            company_address: row.company_address ?? "",
            company_email: row.company_email ?? "",
            status: row.status,
            activation_status: row.status === "Active"
        });
        setCreateclient(true);
    }

    const handleCreateClient = async () => {
        setFormErrors({});
        if (!validateForm()) {
            return;
        }

        setIsSaving(true);

        const isEdit = !!editingClientId;
        const clientName = formData.company_name || formData.client_name;

        try {
            if (editingClientId) {
                await clientProfileService.editClientProfile({
                    id: editingClientId,
                    wcn: formData.wcn,
                    companyName: formData.company_name,
                    companyAddress: formData.company_address,
                    companyBillingEmail: formData.company_email?.trim() || undefined,
                    companyBillingSystemURL: formData.billing_url?.trim() || undefined,
                });
            } else {
                await clientProfileService.createClientProfile({
                    wcn: formData.wcn,
                    companyName: formData.company_name,
                    companyAddress: formData.company_address,
                    companyBillingEmail: formData.company_email,
                    companyBillingSystemURL: formData.billing_url,
                });
            }

            setCreateclient(false);
            resetForm();
            await fetchClientProfiles();
            popup.success({
                title: isEdit ? "Edit Client" : "Create Client",
                message: resultMessage(clientName || "Client", isEdit ? "Updated" : "Created")
            });

        } catch (err: any) {
            if (err.response?.data?.message === "Duplicate WCN") {
                setFormErrors({ wcn: "This WCN already exists" });
            } else {
                popup.error({ message: err?.message || `Failed to ${isEdit ? 'update' : 'create'} client.` });
            }
        } finally {
            setIsSaving(false);
        }
    };

    const handleCancelCreate = () => {
        setCreateclient(false);
        resetForm();
    };

    const validateForm = (): boolean => {
        const errors: { [key: string]: string } = {};
        if (formData.wcn && formData.wcn.trim() !== "" && !/^\d+$/.test(formData.wcn.trim())) {
            errors.wcn = "WCN must contain only numbers";
        }
        if (!formData.company_name || formData.company_name.trim() === "") {
            errors.companyName = "Company Name is required";
        }
        if (!formData.company_address || formData.company_address.trim() === "") {
            errors.companyAddress = "Company Address is required";
        } else if (formData.company_address.length > 120) {
            errors.companyAddress = "Company Address must not exceed 120 characters";
        }
        setFormErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const columnUniqueValues = React.useMemo(() => {
        const filters = appliedFilters.filters || {};

        const uniq = (targetKey: string) => {
            let src = clientFilter || [];

            if (searchApplied && searchApplied.trim()) {
                const searchLower = searchApplied.toLowerCase().trim();
                src = src.filter((item) =>
                    item.client_name?.toLowerCase().includes(searchLower) ||
                    (Array.isArray(item.wcn) ? item.wcn.join(' ') : (item.wcn || '')).toLowerCase().includes(searchLower) ||
                    item.company_address?.toLowerCase().includes(searchLower) ||
                    item.company_email?.toLowerCase().includes(searchLower) ||
                    String(item.no_of_projects ?? '').toLowerCase().includes(searchLower)
                );
            }

            const filterEntries = Object.entries(filters);
            if (filterEntries.length > 0) {
                src = src.filter((client: any) =>
                    filterEntries.every(([columnId, filter]: any) => {
                        if (columnId === targetKey) return true;

                        if (!filter) return true;
                        const { searchTerm, selectedFilters, isFiltersApplied } = filter;
                        if (!isFiltersApplied) return true;

                        if (selectedFilters && selectedFilters.length > 0) {
                            const rawValue = client[columnId];
                            const clientValue = Array.isArray(rawValue)
                                ? rawValue.join(', ')
                                : String(rawValue ?? "");
                            return selectedFilters.includes(clientValue);
                        }
                        if (searchTerm && searchTerm.trim() !== "") {
                            const rawValue = client[columnId];
                            const clientValue = cleanString(
                                Array.isArray(rawValue) ? rawValue.join(' ') : String(rawValue ?? "")
                            );
                            return clientValue.includes(cleanString(searchTerm));
                        }
                        return true;
                    })
                );
            }

            const vals = [...new Set(src.map((r: any) => {
                const rawValue = r[targetKey];
                return Array.isArray(rawValue) ? rawValue.join(', ') : String(rawValue ?? '');
            }).filter(v => v !== ''))];
            return vals.sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'base' }));
        };

        return {
            client_name: uniq('client_name'),
            wcn: uniq('wcn'),
            no_of_projects: uniq('no_of_projects'),
        };
    }, [clientFilter, searchApplied, appliedFilters.filters]);

    const columns = React.useMemo<Array<any>>(
        () => [

            {
                label: "Client Name",
                accessorKey: "client_name",
                accessor: "client_name",
                columnValues: { type: "both", data: columnUniqueValues.client_name },
                Cell: ({ row: { original } }: { row: { original: ManageClientRow } }) => {
                    return (
                        <div>
                            {original.client_name ? (
                                <p
                                    className="mr-2 _600 mb-0 text-primary text-link text-decoration-underline cursor-pointer"
                                    onClick={() => loadClientDetails(original.id)}
                                >
                                    {" "}
                                    {original.client_name}
                                </p>
                            ) : (
                                "-"
                            )}
                        </div>
                    );
                },
            },
            {
                label: "WCN",
                accessorKey: "wcn",
                accessor: "wcn",
                columnValues: { type: "both", data: columnUniqueValues.wcn },
                cell: (info: { getValue: () => any }) => {
                    const values = info.getValue();
                    return Array.isArray(values) ? values.join(", ") : values;
                },
            },
            {
                label: "Number of Projects",
                accessorKey: "no_of_projects",
                accessor: "no_of_projects",
                columnValues: { type: "both", data: columnUniqueValues.no_of_projects },
                cell: (info: { getValue: () => any }) => info.getValue(),
            },
            ...((hasManageClientProfilesPermission ? [{
                label: "Action",
                accessorKey: "action",
                accessor: "action",
                filtering: false,
                sorting: false,
                Cell: ({ row: { original } }: { row: { original: ManageClientRow }, cell: { value: any } }) => {
                    return (
                        <div className='d-flex justify-content-start gap-3'>
                            <Button variant={BUTTONS.TABLEDOWNLOAD} onClick={() => handleEditClient(original)} startIcon={<Icon.PencilFill />}></Button>
                            <Button variant={BUTTONS.TABLEDOWNLOAD} onClick={() => {
                                handleDeleteClick(original);
                            }} startIcon={<Icon.TrashFill />}></Button>


                        </div>
                    );
                }
            }] : []) as any)
        ],
        [handleDeleteClick, hasManageClientProfilesPermission]
    );

    const loadClientDetails = (clientId: string) => {
        const dialogId = _uniqueId("sideDialog_");
        sideDialog.addDialog({
            id: dialogId,
            component: (
                <ClientDetails
                    id={dialogId}
                    clientId={clientId}
                    onClientUpdated={() => fetchClientProfiles()}
                />
            ),
        });
    };




    const handleClearFilter = () => {
        updateVal("");
        updateSearchApplied(null);

        localStorage.removeItem("SearchClientProfileFilter");
        localStorage.removeItem("ClientProfileFilter");

        const currentPageSize = appliedFilters.pageSize;

        setAppliedFilters({
            ...initialFilterState,
            pageSize: currentPageSize,
            resetFilters: true,
            resetPageIndex: true,
        });

        searchClients(
            { filters: {} },
            0,
            currentPageSize,
            null
        );
    };




    return (
        <>
            <div className="row">
                <div className="col-md-8">
                    <h5 className='_600'> Client Profiles</h5>
                </div>
                <div className="col-md-6 mb-3 mt-2">
                    <div className="d-flex justify-content-start align-items-center gap-2">
                        <AbsSearch
                            handleClear={handleClearFilter}
                            value={val}
                            handleChange={(e: any) => onChangeSearchInput(e.target.value)}
                            handleSearch={() => {
                                updateSearchApplied(val === "" ? null : val);
                            }}
                            placeholder="Search by Client Name, WCN, Address, Email"
                            searchApplied={searchApplied !== null}
                        />

                        <a href="#" className='text-link text-nowrap _500'
                            onClick={(e) => {
                                e.preventDefault();
                                handleClearFilter();
                            }}>Clear Filter</a>
                    </div>
                </div>
                <div className="col-md-6 d-flex justify-content-end">
                    <div>
                        {hasManageClientProfilesPermission && (
                            <Button variant={BUTTONS.PRIMARY} onClick={() => { resetForm(); setCreateclient(true) }}>Add New Client</Button>
                        )}
                    </div>
                </div>
            </div>
            <div className="custom-react-table">
                <AbsReactTable
                    loading={loading}
                    columns={columns}
                    data={data}
                    pagination={true}
                    expand={false}
                    allowColumnsShowHide={false}
                    shadow={true}
                    dataCount={dataCount || clientFilter.length}
                    applyFilterCallback={applyFilter}
                    appliedFilters={{
                        ...appliedFilters,
                        pageSize: appliedFilters.pageSize.toString(),
                        pageIndex: appliedFilters.pageIndex.toString(),
                    }}
                    pageOptions={["10", "20", "30", "40"]}
                    allDataProvided

                />
            </div>
            <Offcanvas show={createclient} placement={"end"} onHide={() => setCreateclient(false)} backdrop="static">
                <Offcanvas.Header closeButton>
                    <Offcanvas.Title>{editingClientId ? "Edit Client Profile" : "Add Client Profile"}</Offcanvas.Title>
                </Offcanvas.Header>
                <Offcanvas.Body>
                    <div className="row">
                        <div className="col-md-12 form-group">
                            <Label name="WCN" bold />
                            <Input
                                label=""
                                placeholder="Enter WCN"
                                value={formData.wcn}
                                onChange={(val: string) => {
                                    const parsed = val.replace(/\D/g, '').slice(0, 8);
                                    setFormData({ ...formData, wcn: parsed });
                                    if (formErrors.wcn) {
                                        setFormErrors(prev => {
                                            const newErrors = { ...prev };
                                            delete newErrors.wcn;
                                            return newErrors;
                                        });
                                    }
                                }}
                            />
                            {formErrors.wcn && (
                                <div className="text-danger small mt-1">
                                    {formErrors.wcn}
                                </div>
                            )}
                        </div>
                        <div className="col-md-12 form-group">
                            <Label name="Company Name" bold required />
                            <Input
                                placeholder="Enter Company Name"
                                value={formData.company_name}
                                onChange={(val: string) => {
                                    setFormData({ ...formData, company_name: val });
                                    if (formErrors.companyName) {
                                        setFormErrors(prev => {
                                            const newErrors = { ...prev };
                                            delete newErrors.companyName;
                                            return newErrors;
                                        });
                                    }
                                }}
                            />
                            {formErrors.companyName && (
                                <div className="text-danger small mt-1">
                                    {formErrors.companyName}
                                </div>
                            )}
                        </div>
                        <div className="col-md-12 form-group">
                            <Label name="Company Address" bold required />
                            <TextArea
                                placeholder="Enter Company Address"
                                value={formData.company_address}
                                maxLength={120}
                                onChange={(val: string) => {
                                    setFormData({ ...formData, company_address: val });
                                    if (formErrors.companyAddress) {
                                        setFormErrors(prev => {
                                            const newErrors = { ...prev };
                                            delete newErrors.companyAddress;
                                            return newErrors;
                                        });
                                    }
                                }}
                            />
                            <div className="d-flex justify-content-between align-items-center">
                                {formErrors.companyAddress && (
                                    <div className="text-danger small mt-1">
                                        {formErrors.companyAddress}</div>
                                )}
                                <div className="text-muted small mt-1">
                                    {formData.company_address.length}/120 characters</div>
                            </div>
                        </div>



                        <div className="col-md-12 form-group">
                            <Label name="Company Billing E-mail" bold />

                            <Input
                                placeholder="Enter Company Billing Email"
                                value={formData.company_email}
                                onChange={(val: string) => setFormData({ ...formData, company_email: val })}
                            />
                        </div>

                        <div className="col-md-12 form-group">
                            <Label name="Company Billing System URL" bold />
                            <Input
                                placeholder="Enter Company Billing System URL"
                                value={formData.billing_url}
                                onChange={(val: string) => setFormData({ ...formData, billing_url: val })}
                            />
                        </div>


                        <div className="col-md-12 form-group">
                            <Divider className="divider-1" />
                        </div>
                        <div className="col-md-12 form-group">
                            <div className="d-flex gap-2">
                                <Button variant={BUTTONS.PRIMARY} onClick={handleCreateClient} disabled={isSaving}>{editingClientId ? "Update" : "Create"}</Button>
                                <Button variant={BUTTONS.SECONDARY} onClick={handleCancelCreate}>Cancel</Button>
                            </div>
                        </div>

                    </div>
                </Offcanvas.Body>
            </Offcanvas>
        </>
    );
};

export default ClientList;