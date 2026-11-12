import React, { useState, useEffect, useContext, useMemo, useCallback } from 'react';
import { AbsReactTable, AbsSearch, Button, BUTTONS, Divider, Label, SideDialog, SideDialogManagerContext } from "customer_portal-ui-shared";
import * as Icon from 'react-bootstrap-icons';
import AppLoader from '../../../../Common/AppLoader';
import { MemoryRouter } from 'react-router-dom';
import * as clientProfileService from '../../../../service/ClientProfileService';
import AddUser from '../Users/AddUser';
import { ROLES } from '../../../File/appConstants';
import { usePopup } from '../../../../Common/PopupProvider';
import { resultMessage } from '../../../../Common/UtilityMessage';
import { usePermissions } from '../../../../context/PermissionContext';
import { PERMISSIONS } from '../../../../constants/permissions';

interface ClientDetailsProps {
    id: string;
    clientId: string;
    onClientUpdated?: () => void;
}

const PROJECT_HISTORY_PAGE_SIZE = "25";

const initialProjectFilterState = {
    searchType: "list",
    resetPageIndex: false,
    expandAll: false,
    resetFilters: false,
    deselectRows: false,
    filters: {} as Record<string, any>,
    sort: null,
    pageIndex: 0,
    pageSize: PROJECT_HISTORY_PAGE_SIZE,
};

const cleanString = (str: string) =>
    str
        ? str
            .toString()
            .normalize("NFKD")
            .replace(/\s+/g, " ")
            .replace(new RegExp("[\\u200B-\\u200D\\uFEFF]", "g"), "")
            .trim()
            .toLowerCase()
        : "";

const applyProjectSearchFilter = (data: any[], searchText: string | null) => {
    if (!searchText) return data;

    const searchLower = cleanString(searchText);
    return data.filter((item) =>
        cleanString(item?.name || "").includes(searchLower) ||
        cleanString(String(item?.pidNumber ?? "")).includes(searchLower) ||
        cleanString(item?.statusName || "").includes(searchLower)
    );
};

const applyProjectColumnFilters = (data: any[], activeFilters: Record<string, unknown>) => {
    const filterEntries = Object.entries(activeFilters || {});
    if (filterEntries.length === 0) {
        return data;
    }

    return data.filter((row) =>
        filterEntries.every(([columnId, filter]) => {
            const { searchTerm, selectedFilters } = filter as {
                searchTerm?: string;
                selectedFilters?: unknown[];
            };

            if (selectedFilters && selectedFilters.length > 0) {
                const rowValue = (row as any)[columnId];
                return selectedFilters.some(f => cleanString(String(f)) === cleanString(String(rowValue)));
            }

            if (!searchTerm) return true;

            const rowValue = cleanString((row as any)[columnId] || "");
            return rowValue.includes(cleanString(searchTerm));
        })
    );
};

const ClientDetails: React.FC<ClientDetailsProps> = ({ id, clientId }) => {
    const popup = usePopup();
    const sideDialog = useContext(SideDialogManagerContext);
    const [clientDetails, setClientDetails] = useState<any>(null);
    const [projectHistory, setProjectHistory] = useState<any[]>([]);
    const [displayedProjectHistory, setDisplayedProjectHistory] = useState<any[]>([]);
    const [projectHistoryCount, setProjectHistoryCount] = useState(0);
    const [projectAppliedFilters, setProjectAppliedFilters] = useState(initialProjectFilterState);
    const [projectSearchVal, setProjectSearchVal] = useState<string>("");
    const [projectSearchApplied, setProjectSearchApplied] = useState<string | null>(null);
    const [detailsLoading, setDetailsLoading] = useState(false);
    const [showAddUser, setShowAddUser] = useState(false);
    const [addUserContext, setAddUserContext] = useState<{ company: string; clientId: string } | null>(null);
    const { hasGlobalPermission } = usePermissions();
    const hasManageClientAdminsPermission = hasGlobalPermission(PERMISSIONS.MANAGE_CLIENT_ADMINS);

    useEffect(() => {
        if (clientId) {
            loadClientDetails(clientId);
        }
    }, [clientId]);

    const paginateProjectHistory = useCallback((sourceData: any[], filtersObj: typeof initialProjectFilterState, searchText?: string | null) => {
        const effectiveSearchText = searchText !== undefined ? searchText : projectSearchApplied;
        const pageSizeNum = typeof filtersObj.pageSize === 'string' ? parseInt(filtersObj.pageSize, 10) : Number(filtersObj.pageSize) || 25;
        const pageIndexNum = typeof filtersObj.pageIndex === 'string' ? parseInt(filtersObj.pageIndex as any, 10) : Number(filtersObj.pageIndex) || 0;
        const startIndex = pageIndexNum * pageSizeNum;
        let pageData = sourceData.slice(startIndex, startIndex + pageSizeNum);

        pageData = applyProjectSearchFilter(pageData, effectiveSearchText);
        pageData = applyProjectColumnFilters(pageData, filtersObj.filters || {});

        setDisplayedProjectHistory(pageData);
        setProjectHistoryCount(sourceData.length);
    }, [projectSearchApplied]);

    const onChangeProjectSearchInput = useCallback((value: string) => {
        setProjectSearchVal(value);

        const searchText = value.trim() === "" ? null : value;
        setProjectSearchApplied(searchText);
        setProjectAppliedFilters(prev => {
            const resetFilters = { ...prev, pageIndex: 0 };
            paginateProjectHistory(projectHistory, resetFilters, searchText);
            return resetFilters;
        });
    }, [projectHistory, paginateProjectHistory]);

    const handleProjectSearch = useCallback(() => {
        const searchText = projectSearchVal.trim() === "" ? null : projectSearchVal;
        setProjectSearchApplied(searchText);
        setProjectAppliedFilters(prev => {
            const resetFilters = { ...prev, pageIndex: 0 };
            paginateProjectHistory(projectHistory, resetFilters, searchText);
            return resetFilters;
        });
    }, [projectSearchVal, projectHistory, paginateProjectHistory]);

    const handleClearProjectSearch = useCallback(() => {
        setProjectSearchVal("");
        setProjectSearchApplied(null);
        setProjectAppliedFilters(initialProjectFilterState);
        paginateProjectHistory(projectHistory, initialProjectFilterState, null);
    }, [projectHistory, paginateProjectHistory]);

    const applyProjectFilter = useCallback((filtersObj: any) => {
        const filteredEntries = Object.fromEntries(
            Object.entries(filtersObj.filters || {}).filter(
                ([, value]) => (value as any)?.searchTerm !== ""
                    || ((value as any)?.selectedFilters?.length ?? 0) > 0
                    || (value as any)?.isFiltersApplied
            )
        );

        const pageIndexNum = typeof filtersObj.pageIndex === "string" ? parseInt(filtersObj.pageIndex, 10) : Number(filtersObj.pageIndex) || 0;
        const pageSizeNum = typeof filtersObj.pageSize === "string" ? parseInt(filtersObj.pageSize, 10) : Number(filtersObj.pageSize) || 25;

        const updatedFilters = {
            ...initialProjectFilterState,
            ...filtersObj,
            pageIndex: pageIndexNum,
            pageSize: String(pageSizeNum),
            filters: filteredEntries,
            resetPageIndex: false,
        };

        setProjectAppliedFilters(updatedFilters);
        paginateProjectHistory(projectHistory, updatedFilters);
    }, [projectHistory, paginateProjectHistory]);

    const loadClientDetails = async (cId: string) => {
        try {
            setDetailsLoading(true);
            setClientDetails(null);
            setProjectHistory([]);
            setDisplayedProjectHistory([]);
            setProjectHistoryCount(0);
            setProjectAppliedFilters(initialProjectFilterState);
            setProjectSearchVal("");
            setProjectSearchApplied(null);

            const response = await clientProfileService.getClientProfileWithDetails(cId, false);
            const payload = response.data?.data ?? response.data;

            const client =
                payload?.client ??
                payload?.clientProfile ??
                payload;
            const projectsRaw =
                payload?.projects ??
                payload?.projectHistory ??
                payload?.projectHistories ??
                [];

            const normalizedProjects = Array.isArray(projectsRaw) ? projectsRaw : [];

            setClientDetails(client);
            setProjectHistory(normalizedProjects);
            paginateProjectHistory(normalizedProjects, initialProjectFilterState);
        } catch (err) {
            setClientDetails({});
            setProjectHistory([]);
            setDisplayedProjectHistory([]);
            setProjectHistoryCount(0);
        } finally {
            setDetailsLoading(false);
        }
    };

    const handleClose = () => {
        if (sideDialog && typeof sideDialog === 'object' && 'removeDialog' in sideDialog) {
            (sideDialog as any).removeDialog(id);
        }
    };

    const handleAddClientAdmin = () => {
        const companyName = clientDetails?.companyName;
        if (!companyName || !clientId) return;

        setAddUserContext({
            company: companyName,
            clientId: clientId,
        });
        setShowAddUser(true);
    };

    const handleAddUserSaved = (action: "CreateUser" | "UpdateUser") => {
        setShowAddUser(false);
        setAddUserContext(null);
        popup.success({
            title: action === "CreateUser" ? "Create User" : "Edit User",
            message: resultMessage("User", action === "CreateUser" ? "Created" : "Updated")
        });
    };

    const handleAddUserError = (message: string) => {
        popup.error({ message });
    };

    const handleAddUserHide = () => {
        setShowAddUser(false);
        setAddUserContext(null);
    };

    const projectHistoryColumns = useMemo(() => [
        {
            label: "Project Name",
            accessorKey: "name",
            accessor: "name",
            sorting: true,
            cell: (info: { getValue: () => any }) => info.getValue(),
        },
        {
            label: "Project ID",
            accessorKey: "pidNumber",
            accessor: "pidNumber",
            sorting: true,
            cell: (info: { getValue: () => any }) => info.getValue(),
        },
        {
            label: "Visit Dates",
            accessorKey: "visitDates",
            accessor: "visitDates",
            sorting: false,
            filtering: false,
            cell: (info: { getValue: () => any }) => info.getValue(),
        },
        {
            label: "Status",
            accessorKey: "statusName",
            accessor: "statusName",
            sorting: true,
            cell: (info: { getValue: () => any }) => info.getValue(),
        },
    ], []);

    return (
        <>
            <SideDialog
                id={id}
                confirmOnClose={false}
                className="right-side-dialog"
                showWarningMinimize={() => false}
                title={
                    <div className='side-panel-title'>
                        <h5>Client Details</h5>
                        <div className="side-header-actions">
                            <Button className="close-button" onClick={handleClose}><Icon.X /></Button>
                        </div>
                    </div>
                }
                minimizedTitle="Client Details"
            >
                {() => (
                    <>
                        {detailsLoading ? (
                            <AppLoader inline />
                        ) : (
                            <>
                                {/* Client Info */}
                                <div className="mb-4">


                                    <div className="row mb-2">
                                        <div className="col-md-6">
                                            <Label name="WCN" bold />
                                            <div>{clientDetails?.wcn}</div>
                                        </div>

                                        <div className="col-md-6">
                                            <Label name="Company Name" bold />
                                            <div>{clientDetails?.companyName}</div>
                                        </div>
                                    </div>

                                    <div className="row mb-2">
                                        <div className="col-md-6">
                                            <Label name="Company Address" bold />
                                            <div>{clientDetails?.companyAddress}</div>
                                        </div>

                                        <div className="col-md-6">
                                            <Label name="Billing Email" bold />
                                            <div>{clientDetails?.companyBillingEmail}</div>
                                        </div>
                                    </div>
                                </div>

                                {/* Project History */}
                                <Divider className="divider-1" />

                                <div className="row">
                                    <div className="col-md-8">
                                        <h6 className="_600 mb-3">Project History</h6>
                                    </div>
                                    <div className="col-md-9 mb-3 mt-2">
                                        <div className="d-flex justify-content-start align-items-center gap-2">
                                            <AbsSearch
                                                handleClear={handleClearProjectSearch}
                                                value={projectSearchVal}
                                                handleChange={(e: any) => onChangeProjectSearchInput(e.target.value)}
                                                handleSearch={handleProjectSearch}
                                                placeholder="Search by Project Name, Project ID, Status"
                                                searchApplied={projectSearchApplied !== null}
                                            />

                                            <a href="#" className='text-link text-nowrap _500'
                                                onClick={(e) => {
                                                    e.preventDefault();
                                                    handleClearProjectSearch();
                                                }}>Clear Filter</a>
                                        </div>
                                    </div>
                                </div>

                                <div className="custom-react-table">
                                    <AbsReactTable
                                        loading={detailsLoading}
                                        columns={projectHistoryColumns}
                                        data={displayedProjectHistory}
                                        pagination={true}
                                        expand={false}
                                        allowColumnsShowHide={false}
                                        shadow={true}
                                        dataCount={projectHistoryCount}
                                        applyFilterCallback={applyProjectFilter}
                                        appliedFilters={{
                                            ...projectAppliedFilters,
                                            pageSize: projectAppliedFilters.pageSize.toString(),
                                            pageIndex: projectAppliedFilters.pageIndex.toString(),
                                        }}
                                        pageOptions={["25", "50", "100"]}
                                        allDataProvided
                                    />
                                </div>
                                <Divider className="divider-1" />

                                <div className="mt-4">
                                    <h6 className="_600 mb-3">Client Admins</h6>

                                    <div className="d-flex align-items-center gap-3 p-3 border rounded bg-white">
                                        {hasManageClientAdminsPermission && (
                                            <Button
                                                variant={BUTTONS.PRIMARY}
                                                onClick={handleAddClientAdmin}
                                                disabled={!clientDetails?.companyName}
                                            >
                                                Add Client Admin
                                            </Button>
                                        )}

                                        <span className="text-muted">
                                            No Client Admins have been added.
                                        </span>
                                    </div>
                                </div>
                            </>
                        )}
                    </>
                )}
            </SideDialog>

            {addUserContext && (
                <MemoryRouter>
                    <AddUser
                        show={showAddUser}
                        onHide={handleAddUserHide}
                        onSaved={handleAddUserSaved}
                        onError={handleAddUserError}
                        isClientUsers={false}
                        company={addUserContext.company}
                        clientId={addUserContext.clientId}
                        userRole={ROLES.CLIENT_ADMIN}
                        className="add-client-admin-offcanvas"
                        backdropClassName="add-client-admin-offcanvas-backdrop"
                    />
                </MemoryRouter>
            )}
        </>
    );
};

export default ClientDetails;
