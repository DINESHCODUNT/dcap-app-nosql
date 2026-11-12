import React, { useState, useEffect, useCallback } from 'react';
import { DragDropContext, Droppable, Draggable } from '@hello-pangea/dnd';
import Modal from 'react-bootstrap/Modal';
import { AbsSearch, Button, BUTTONS, AbsReactTable, Toggle } from 'customer_portal-ui-shared';
import * as Icon from "react-bootstrap-icons";
import * as projectService from '../../service/ProjectService';
import { usePopup } from '../../Common/PopupProvider';
import { resultMessage } from '../../Common/UtilityMessage';
import moment from 'moment';
import { isEmptyDate } from '../../utils/dateTime';

export interface SidebarProjectRow {
    name: string;
    imo: string;
    date: string;
    pct: number;
    status: string;
    id?: number;
    wcn?: string;
    clientName?: string;
    capRegion?: string;
    pidNumber?: string;
    capType?: string;
    capScope?: string;
    projectStatusColor?: string;
    projectStatusName?: string;
    statusName?: string;
    createdDate?: string;
    createdBy?: string;
    lastModifiedDate?: string;
    lastModifiedBy?: string;
    priority?: number;
}

export interface ProjectListProps {
    selectedProject: SidebarProjectRow | null;
    onSelectProject: (project: SidebarProjectRow | null) => void;
    refreshTrigger?: number;
    onSidebarStats?: (stats: projectService.ProjectSidebarStatsAggregate) => void;
    onStatsError?: () => void;
    onStatusChartStats?: (stats: projectService.ProjectSidebarStatsAggregate | null) => void;
    onRegionChartStats?: (stats: projectService.ProjectSidebarStatsAggregate | null) => void;
    onProjectsLoaded?: (projects: SidebarProjectRow[]) => void;
    externalSearchTerm?: string;
    externalStatusFilter?: string | null;
    externalRegionFilter?: string | null;
    navigatedProjectId?: number;
    onClearExternalFilters?: () => void;
}

const LOAD_COUNT = 20;

const getProjectStatusTag = (statusName?: string): string => {
    if (!statusName) return '';
    return statusName.trim().toLowerCase().replace(/\s+/g, '-');
};

const isGlobalSearchMatch = (p: SidebarProjectRow, trimmed: string) => {
    return (
        p.name?.toLowerCase().includes(trimmed) ||
        p.imo?.toLowerCase().includes(trimmed) ||
        p.pidNumber?.toLowerCase().includes(trimmed) ||
        p.capRegion?.toLowerCase().includes(trimmed) ||
        p.clientName?.toLowerCase().includes(trimmed) ||
        p.wcn?.toLowerCase().includes(trimmed) || false
    );
};

const ProjectList: React.FC<ProjectListProps> = ({
    onSelectProject,
    refreshTrigger = 0,
    onSidebarStats,
    onStatsError,
    onStatusChartStats,
    onRegionChartStats,
    onProjectsLoaded,
    externalSearchTerm = '',
    externalStatusFilter = null,
    externalRegionFilter = null,
    navigatedProjectId,
}) => {
    const popup = usePopup();
    const [val, updateVal] = useState<string>("");
    const [statusFilter, setStatusFilter] = useState<string | null>(null);
    const [regionFilter, setRegionFilter] = useState<string | null>(null);
    const [connectionFilter, setConnectionFilter] = useState<'online' | 'offline' | null>(null);

    const [projects, setProjects] = useState<SidebarProjectRow[]>([]);
    const [allProjects, setAllProjects] = useState<SidebarProjectRow[]>([]);
    const [modalProject, setModalProject] = React.useState<SidebarProjectRow | null>(null);
    const [targetProjectToUpdate, setTargetProjectToUpdate] = React.useState<SidebarProjectRow | null>(null);
    const [selectedPriority, setSelectedPriority] = React.useState<number | null>(null);
    const [modalItems, setModalItems] = useState<{ id: string, originalPriority: number, priority: number, project: SidebarProjectRow | null, isCurrent: boolean }[]>([]);
    const [loading, setLoading] = useState(false);
    const [pageAppliedFilters, setPageAppliedFilters] = useState<{
        pageIndex: number;
        pageSize: string;
        resetPageIndex: boolean;
        filters: Record<string, unknown>;
        searchType?: string;
    }>({
        pageIndex: 0,
        pageSize: "50",
        resetPageIndex: false,
        filters: {},
        searchType: "list"
    });

    const [, setVisibleCount] = useState(LOAD_COUNT);


    useEffect(() => {
        if (modalProject) {
            setTargetProjectToUpdate(modalProject);
            const items = [1, 2, 3, 4, 5, 6, 7, 8, 9].map(p => ({
                id: `priority-${p}`,
                originalPriority: p,
                priority: p,
                project: allProjects.find(x => x.priority === p) || null,
                isCurrent: modalProject.priority === p
            }));

            if (!modalProject.priority) {
                items.push({
                    id: `priority-unassigned`,
                    originalPriority: 10,
                    priority: 10,
                    project: modalProject,
                    isCurrent: true
                });
            }

            setModalItems(items);
            setSelectedPriority(modalProject.priority || null);
        }
    }, [modalProject, allProjects]);

    const handleDragEnd = (result: any) => {
        if (!result.destination) return;

        const sourceIndex = result.source.index;
        const destinationIndex = result.destination.index;

        if (sourceIndex === destinationIndex) return;

        const draggedItem = modalItems[sourceIndex];
        const targetPriority = destinationIndex + 1;

        if (targetPriority > 9) return;
        if (!draggedItem || !draggedItem.project) return;

        setTargetProjectToUpdate(draggedItem.project);
        const updatedProjects = modalItems
            .filter(item => item.project)
            .map(item => {
                if (item.project?.id === draggedItem.project?.id) {
                    return { ...item, computedPriority: targetPriority, isCurrent: true };
                } else {
                    let computed = item.originalPriority;
                    if (computed >= targetPriority) {
                        computed = computed + 1;
                    }
                    return { ...item, computedPriority: computed, isCurrent: false };
                }
            });

        const newItems = [1, 2, 3, 4, 5, 6, 7, 8, 9].map(p => {
            const found = updatedProjects.find(x => x.computedPriority === p);
            return {
                id: `priority-${p}`,
                originalPriority: found ? found.originalPriority : p,
                priority: p,
                project: found ? found.project : null,
                isCurrent: found ? found.isCurrent : false
            };
        });

        setModalItems(newItems);
        setSelectedPriority(targetPriority);
    };


    const loadProjects = useCallback(async () => {

        setLoading(true);

        try {

            const response = await projectService.getAllProjectsGridData();

            const raw = response.data;

            const items = projectService.normalizeAllProjectsGridPayload(raw);

            const mapped: SidebarProjectRow[] = items.map((project: any) => {

                let formattedDate = '';

                if (
                    !isEmptyDate(project.commentLastModifiedDate)
                ) {
                    const parsed = moment(project.commentLastModifiedDate);

                    formattedDate = parsed.isValid()
                        ? parsed.format('DD-MMM-YYYY')
                        : project.commentLastModifiedDate;
                }

                let formattedLastModifiedDate = '';
                if (project.projectLastUpdatedDate) {
                    const parsedLMD = moment(project.projectLastUpdatedDate);
                    formattedLastModifiedDate = parsedLMD.isValid() ? parsedLMD.format('DD-MMM-YYYY hh:mm A') : project.projectLastUpdatedDate;
                }

                return {
                    name: (project.name || '').trim().replace(/\t/g, ''),
                    imo: project.imo || '',
                    date: formattedDate,
                    pct: project.percentComplete || 0,
                    status: projectService.getSidebarConnectionStatus(project),
                    id: project.id,
                    wcn: project.wcn || '',
                    clientName: project.companyName || '',
                    capRegion: project.capRegion || '',
                    pidNumber: project.pidNumber || '',
                    projectStatusName: project.projectStatusName || '',
                    projectStatusColor: project.projectStatusColor || '',
                    lastModifiedDate: formattedLastModifiedDate,
                    lastModifiedBy: (() => {
                        const isGuid = (val?: string) => Boolean(val && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(val.trim()));
                        const val = project.lastModifiedBy || project.modifiedBy || project.commentCreatedBy || '';
                        return (!val || isGuid(val)) ? '-' : val;
                    })(),
                    priority: project.projectPriority ? parseInt(project.projectPriority, 10) : undefined
                };
            });


            mapped.sort((a, b) => {
                if (a.priority !== undefined && b.priority !== undefined) {
                    return a.priority - b.priority;
                }
                if (a.priority !== undefined) return -1;
                if (b.priority !== undefined) return 1;
                return 0;
            });


            setProjects(mapped);

            setAllProjects(mapped);

            onProjectsLoaded?.(mapped);

        } catch (error) {

        } finally {

            setLoading(false);
        }

    }, [onProjectsLoaded]);

    useEffect(() => {
        if (!navigatedProjectId || allProjects.length === 0) return;

        const idx = allProjects.findIndex(p => p.id === navigatedProjectId);
        if (idx === -1) return;

        setVisibleCount(Math.max(idx + 1, LOAD_COUNT));

        setTimeout(() => {
            const el = document.getElementById(`project-item-${navigatedProjectId}`);
            el?.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }, 300);

    }, [navigatedProjectId, allProjects]);

    useEffect(() => {
        if (externalSearchTerm === undefined) return;

        updateVal(externalSearchTerm);
        const trimmed = externalSearchTerm.trim().toLowerCase();

        if (trimmed === '') {
            setProjects(allProjects);
        } else {
            const filtered = allProjects.filter(p =>
                isGlobalSearchMatch(p, trimmed)
            );
            setProjects(filtered);
        }
        setVisibleCount(LOAD_COUNT);

    }, [externalSearchTerm, allProjects]);

    useEffect(() => {
        if (externalStatusFilter !== undefined) {
            setStatusFilter(externalStatusFilter);
        }
    }, [externalStatusFilter]);

    useEffect(() => {
        if (externalRegionFilter !== undefined) {
            setRegionFilter(externalRegionFilter);
        }
    }, [externalRegionFilter]);

    useEffect(() => {

        void loadProjects();

    }, [refreshTrigger]);

    useEffect(() => {
        const handleRefresh = () => {
            void loadProjects();
        };
        window.addEventListener('refreshProjectList', handleRefresh);
        return () => {
            window.removeEventListener('refreshProjectList', handleRefresh);
        };
    }, [loadProjects]);

    useEffect(() => {

        const fetchStats = async () => {

            try {

                const stats = await projectService.getProjectStats();

                onSidebarStats?.(stats);

            } catch (err) {
                onStatsError?.();
            }
        };

        void fetchStats();

    }, [refreshTrigger]);

    const onChangeSearchInput = useCallback((value: string) => {

        updateVal(value);

        const trimmed = value.trim().toLowerCase();

        if (!trimmed) {

            setProjects(allProjects);

        } else {

            const filtered = allProjects.filter(p =>
                isGlobalSearchMatch(p, trimmed)
            );

            setProjects(filtered);
        }

        setVisibleCount(LOAD_COUNT);

    }, [allProjects]);

    const handleClear = useCallback(() => {
        updateVal("");
        setProjects(allProjects);
        setVisibleCount(LOAD_COUNT);
        setStatusFilter(null);
        setRegionFilter(null);
        setConnectionFilter(null);
        setPageAppliedFilters(prev => ({
            ...prev,
            pageIndex: 0,
            resetPageIndex: true,
            filters: {}
        }));
    }, [allProjects]);

    const cleanString = (str?: string | null) =>
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
        data: SidebarProjectRow[],
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
                    return selectedFilters.some(f => {
                        const fStr = cleanString(String(f));
                        const rStr = cleanString(String(rowValue));
                        if (fStr === "n/a") {
                            return rStr === "" || rStr === "n/a" || rStr === "-";
                        }
                        return fStr === rStr;
                    });
                }

                if (!searchTerm) return true;

                const rowValue = cleanString((row as any)[columnId] || '');
                const searchStr = cleanString(searchTerm);
                if (searchStr === "n/a" && (rowValue === "" || rowValue === "-")) return true;
                return rowValue.includes(searchStr);
            })
        );
    };

    const columnUniqueValues = React.useMemo(() => {
        const filters = pageAppliedFilters.filters || {};

        const computeUniq = (targetKey: keyof SidebarProjectRow) => {
            let src = allProjects || [];

            if (val.trim() !== "") {
                const trimmed = val.trim().toLowerCase();
                src = src.filter(p =>
                    isGlobalSearchMatch(p, trimmed)
                );
            }
            if (statusFilter) {
                src = src.filter(p => p.status?.toLowerCase() === statusFilter.toLowerCase() || p.projectStatusName?.toLowerCase() === statusFilter.toLowerCase() || getProjectStatusTag(p.projectStatusName) === statusFilter.toLowerCase());
            }
            if (regionFilter) {
                const cleanFilterRegion = regionFilter.toLowerCase().replace(/\s+/g, '');
                src = src.filter(p => (p.capRegion?.toLowerCase().replace(/\s+/g, '') || '') === cleanFilterRegion);
            }

            const filteredSrc = applyColumnFilters(src, filters, targetKey);
            const vals = Array.from(new Set(filteredSrc.map(r => {
                const v = r[targetKey];
                return (v == null || String(v).trim() === '') ? "N/A" : String(v);
            })));
            vals.sort();
            const naIndex = vals.indexOf("N/A");
            if (naIndex > 0) {
                vals.splice(naIndex, 1);
                vals.unshift("N/A");
            }
            return vals;
        };

        return {
            name: computeUniq('name'),
            pidNumber: computeUniq('pidNumber'),
            capRegion: computeUniq('capRegion'),
            date: computeUniq('date'),
            clientName: computeUniq('clientName'),
            wcn: computeUniq('wcn'),
            pct: computeUniq('pct')
        };
    }, [allProjects, pageAppliedFilters.filters, val, statusFilter, regionFilter, connectionFilter]);

    const columns = React.useMemo<Array<any>>(
        () => [
            {
                label: "Project Name",
                accessorKey: "name",
                accessor: "name",
                columnValues: { type: "both", data: columnUniqueValues.name },
                Cell: ({ row }: any) => {
                    const { original } = row;
                    return (
                        <div className="d-flex align-items-center justify-content-start gap-3 rank-list flex-row">
                            <div className="d-flex gap-2">
                                <div className={`project-dot priority ${!original.priority ? 'empty' : ''}`} onClick={(e) => { e.stopPropagation(); setModalProject(original); setSelectedPriority(null); }}>
                                    {original.priority || ''}
                                </div>
                            </div>
                            <div className="d-flex gap-2 flex-column">
                                <p className="mr-2 _600 mb-0 text-primary text-link cursor-pointer" role="button" onClick={() => onSelectProject(original)}>
                                    {original.name}
                                </p>
                                <p className="text-muted small mb-0">
                                    IMO <span className="_600 ">{original.imo}</span>
                                </p>
                            </div>
                        </div>
                    );
                },
            },
            {
                label: "Project ID",
                accessorKey: "pidNumber",
                accessor: "pidNumber",
                columnValues: { type: "both", data: columnUniqueValues.pidNumber },
                Cell: ({ value }: any) => (value && value.toString().trim() !== '') ? value : '-',
            },
            {
                label: "CAP Coordinator",
                accessorKey: "capRegion",
                accessor: "capRegion",
                columnValues: { type: "both", data: columnUniqueValues.capRegion },
                Cell: ({ value }: any) => (value && value.toString().trim() !== '') ? value : '-',
            },
            {
                label: "Potential Drydock Date",
                accessorKey: "date",
                accessor: "date",
                columnValues: { type: "both", data: columnUniqueValues.date },
                Cell: ({ value }: any) => (value && value.toString().trim() !== '') ? value : '-',
            },
            {
                label: "Client Name",
                accessorKey: "clientName",
                accessor: "clientName",
                columnValues: { type: "both", data: columnUniqueValues.clientName },
                Cell: ({ value }: any) => (value && value.toString().trim() !== '') ? value : '-',
            },
            {
                label: "WCN Number",
                accessorKey: "wcn",
                accessor: "wcn",
                columnValues: { type: "both", data: columnUniqueValues.wcn },
                Cell: ({ value }: any) => (value && value.toString().trim() !== '') ? value : '-',
            },
            {
                label: "Completion %",
                accessorKey: "pct",
                accessor: "pct",
                columnValues: { type: "both", data: columnUniqueValues.pct },
                Cell: ({ value }: any) => `${value ?? 0}%`,
            },
            {
                label: "Status",
                accessorKey: "status",
                accessor: "status",
                filtering: false,
                sorting: false,
                Cell: ({ row: { original } }: any) => (
                    <div className="d-flex flex-column gap-1">
                        <span className={`project-conn tag-${original.status}`}>
                            {original.status.charAt(0).toUpperCase() + original.status.slice(1)}
                        </span>
                        {original.projectStatusName && (
                            <span className={`project-conn tag-${getProjectStatusTag(original.projectStatusName)}`} style={{ color: original.projectStatusColor || 'inherit' }}>
                                {original.projectStatusName}
                            </span>
                        )}
                    </div>
                ),
            },
            {
                label: "Modified by",
                accessorKey: "lastModifiedBy",
                accessor: "lastModifiedBy",
                filtering: false,
                sorting: false,
                Cell: ({ row: { original } }: any) => {
                    const isGuid = (val?: string) => Boolean(val && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(val.trim()));
                    const displayModifiedBy = (!original.lastModifiedBy || isGuid(original.lastModifiedBy) || original.lastModifiedBy === 'N/A') ? '-' : original.lastModifiedBy;
                    return (
                        <div className="d-flex flex-column gap-1">
                            <span>{displayModifiedBy}</span>
                            <span className="text-muted small">{original.lastModifiedDate || ''}</span>
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
                Cell: ({ row: { original } }: any) => (
                    <div className='d-flex justify-content-start gap-3'>
                        <Button
                            variant={BUTTONS.TABLEDOWNLOAD}
                            startIcon={<Icon.ArrowRight />}
                            onClick={() => onSelectProject(original)}
                        ></Button>
                    </div>
                )
            },
        ],
        [projects, onSelectProject, columnUniqueValues]
    );

    useEffect(() => {
        if (!onStatusChartStats || !onRegionChartStats || projects.length === 0) return;

        const statusChartProjects = projects.filter(p => {
            if (externalRegionFilter) {
                return p.capRegion?.toLowerCase() === externalRegionFilter.toLowerCase();
            }
            return true;
        });

        const regionChartProjects = projects.filter(p => {
            if (externalStatusFilter) {
                return p.status?.toLowerCase() === externalStatusFilter.toLowerCase() ||
                    p.projectStatusName?.toLowerCase() === externalStatusFilter.toLowerCase() ||
                    getProjectStatusTag(p.projectStatusName) === externalStatusFilter.toLowerCase();
            }
            return true;
        });

        onStatusChartStats(projectService.aggregateProjectStatsFromGridRows(statusChartProjects as any));
        onRegionChartStats(projectService.aggregateProjectStatsFromGridRows(regionChartProjects as any));
    }, [projects, externalStatusFilter, externalRegionFilter, onStatusChartStats, onRegionChartStats]);

    const allFilteredProjects = projects.filter(p => {
        let matchStatus = true;
        if (statusFilter) {
            matchStatus = p.status?.toLowerCase() === statusFilter.toLowerCase() ||
                p.projectStatusName?.toLowerCase() === statusFilter.toLowerCase() ||
                getProjectStatusTag(p.projectStatusName) === statusFilter.toLowerCase();
        }

        let matchRegion = true;
        if (regionFilter) {
            matchRegion = p.capRegion?.toLowerCase() === regionFilter.toLowerCase();
        }

        let matchConnection = true;
        if (connectionFilter) {
            matchConnection = p.status?.toLowerCase() === connectionFilter;
        }

        return matchStatus && matchRegion && matchConnection;
    });

    const fullyFilteredProjects = applyColumnFilters(allFilteredProjects, pageAppliedFilters.filters);

    const getPageSizeNum = (pageSize?: string | number) => {
        if (pageSize != null && pageSize !== "") {
            return typeof pageSize === 'string' ? parseInt(pageSize, 10) : Number(pageSize);
        }
        return 50;
    };

    const applyFilter = React.useCallback((filtersObj: any) => {
        const filteredData = Object.fromEntries(
            Object.entries(filtersObj.filters || {}).filter(
                ([, value]) => (value as { searchTerm: string })?.searchTerm !== ""
                    || ((value as { selectedFilters?: string[] })?.selectedFilters?.length ?? 0) > 0
                    || (value as { isFiltersApplied?: boolean })?.isFiltersApplied
            )
        );

        setPageAppliedFilters(prev => {
            const prevFiltersStr = JSON.stringify(prev.filters || {});
            const newFiltersStr = JSON.stringify(filteredData);
            const filtersChanged = prevFiltersStr !== newFiltersStr;

            let pageIndexNum = typeof filtersObj.pageIndex === 'string'
                ? parseInt(filtersObj.pageIndex, 10)
                : Number(filtersObj?.pageIndex ?? 0);

            if (filtersChanged) {
                pageIndexNum = 0;
            }

            return {
                pageIndex: pageIndexNum,
                pageSize: String(filtersObj?.pageSize ?? "50"),
                resetPageIndex: filtersChanged,
                filters: filteredData,
                searchType: "list"
            };
        });
    }, []);

    React.useEffect(() => {
        setPageAppliedFilters(prev => ({ ...prev, pageIndex: 0, resetPageIndex: true }));
    }, [statusFilter, regionFilter, connectionFilter, projects]);

    const dataCount = fullyFilteredProjects.length;
    const pageSizeNum = getPageSizeNum(pageAppliedFilters.pageSize);
    const startIdx = pageAppliedFilters.pageIndex * pageSizeNum;
    const pagedProjects = fullyFilteredProjects.slice(startIdx, startIdx + pageSizeNum);

    return (
        <>
            <div className="container-fluid p-0 bg-transparent">
                <div className="section-title mt-4">
                    <h5 className='_700'>Projects List</h5>
                    <div className="section-actions"></div>
                </div>

                <div className="row align-items-center mb-3">
                    <div className="col-12 col-md-4 mt-2">
                        <div className="d-flex justify-content-start align-items-center gap-2">
                            <AbsSearch
                                handleClear={handleClear}
                                value={val}
                                handleChange={(e: any) => onChangeSearchInput(e.target.value)}
                                placeholder="Search"
                                searchApplied={val.trim() !== ""}
                            />
                            <a href="#" className='text-link text-nowrap _500' onClick={(e) => { e.preventDefault(); handleClear(); }}>Clear Filter</a>
                        </div>
                    </div>

                    <div className="col-12 col-md-8 d-flex justify-content-md-end gap-2 mt-3 mt-md-2">
                        <div className="d-flex justify-content-start justify-content-md-end align-items-center gap-2 overflow-x-auto flex-nowrap w-100 pb-1 hide-scrollbar">
                            <div
                                className="d-inline-flex align-items-center me-3 connection-toggle-container flex-shrink-0"
                                onClick={() => {
                                    setConnectionFilter(connectionFilter === 'online' ? 'offline' : 'online');
                                }}
                            >
                                <span className={`mb-0 _600 connection-toggle-label offline-label ${connectionFilter === 'offline' ? 'active' : ''}`}>Offline</span>
                                <Toggle
                                    id="connectionToggle"
                                    checked={connectionFilter === 'online'}
                                    onChange={() => { }}
                                />
                                <span className={`mb-0 _600 connection-toggle-label online-label ${connectionFilter === 'online' ? 'active' : ''}`}>Online</span>
                            </div>
                            {(() => {
                                const dynamicStatuses = (() => {
                                    const statusSet = new Set<string>();

                                    projects.forEach(p => {
                                        if (p.projectStatusName) {
                                            statusSet.add(p.projectStatusName.trim());
                                        }
                                    });
                                    const arr = Array.from(statusSet).sort().map(s => ({
                                        label: s,
                                        value: s.toLowerCase()
                                    }));

                                    return [{ label: 'All', value: 'all' }, ...arr];
                                })();

                                const getCount = (val: string) => {
                                    if (val === 'all') return projects.length;
                                    return projects.filter(p => p.status?.toLowerCase() === val || p.projectStatusName?.toLowerCase() === val || getProjectStatusTag(p.projectStatusName) === val).length;
                                };

                                return dynamicStatuses.map((option) => {
                                    const isAll = option.value === 'all';
                                    const isSelected = isAll ? statusFilter === null : statusFilter === option.value;

                                    return (
                                        <span
                                            key={option.value}
                                            role="button"
                                            className={`project-status-badge flex-shrink-0 ${option.value} ${isSelected ? 'is-selected' : ''}`}
                                            onClick={() => setStatusFilter(isAll || isSelected ? null : option.value)}
                                        >
                                            {option.label} - {getCount(option.value)}
                                        </span>
                                    );
                                });
                            })()}
                        </div>
                    </div>
                </div>

                <div className="row">
                    <div className="col-12 overflow-x-auto custom-table-responsive">
                        <AbsReactTable
                            columns={columns}
                            data={pagedProjects}
                            pagination={true}
                            expand={false}
                            allowColumnsShowHide={true}
                            shadow={true}
                            allDataProvided
                            dataCount={dataCount}
                            loading={loading}
                            pageOptions={["50", "100", "150", "200"]}
                            applyFilterCallback={applyFilter}
                            appliedFilters={{
                                ...pageAppliedFilters,
                                pageIndex: pageAppliedFilters.pageIndex.toString(),
                                pageSize: pageAppliedFilters.pageSize,
                                resetPageIndex: pageAppliedFilters.resetPageIndex,
                                searchType: "all"
                            }}
                        />
                    </div>
                </div>
            </div>

            {modalProject && (
                <Modal show={!!modalProject} onHide={() => setModalProject(null)}>
                    <Modal.Header closeButton>
                        <Modal.Title>Assign Priority</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <p>Select a priority position below. Lower priority positions will be updated as a result.</p>

                        <div className="priority-list-view">
                            <DragDropContext onDragEnd={handleDragEnd}>
                                <Droppable droppableId="priority-list">
                                    {(provided) => (
                                        <div
                                            {...provided.droppableProps}
                                            ref={provided.innerRef}
                                        >
                                            {modalItems.map((item, index) => {
                                                const existingProject = item.project;
                                                const isCurrent = item.isCurrent;
                                                const isSelected = selectedPriority === item.priority;

                                                return (
                                                    <Draggable
                                                        key={item.id}
                                                        draggableId={item.id}
                                                        index={index}
                                                        isDragDisabled={!existingProject}
                                                    >
                                                        {(provided, snapshot) => (
                                                            <div
                                                                ref={provided.innerRef}
                                                                {...provided.draggableProps}
                                                                {...provided.dragHandleProps}
                                                                className={`priority-list-item ${isCurrent ? 'current' : ''} ${isSelected ? 'selected' : ''}`}
                                                                onClick={() => setSelectedPriority(item.priority)}
                                                                style={{
                                                                    display: 'flex',
                                                                    alignItems: 'center',
                                                                    padding: '10px 15px',
                                                                    marginBottom: '8px',
                                                                    border: `1px solid ${isCurrent || isSelected ? '#007bff' : '#ddd'}`,
                                                                    borderRadius: '5px',
                                                                    backgroundColor: snapshot.isDragging ? '#cce5ff' : (isCurrent ? '#e6f2ff' : isSelected ? '#f8f9fa' : '#fff'),
                                                                    cursor: existingProject ? (snapshot.isDragging ? 'grabbing' : 'pointer') : 'pointer',
                                                                    transition: 'background-color 0.2s',
                                                                    boxShadow: snapshot.isDragging ? '0 5px 15px rgba(0,0,0,0.1)' : 'none',
                                                                    ...provided.draggableProps.style
                                                                }}
                                                            >
                                                                <div style={{ marginRight: '15px', color: '#888', cursor: existingProject ? 'inherit' : 'default' }}>
                                                                    <svg width="12" height="16" viewBox="0 0 12 16" fill="currentColor">
                                                                        <path d="M4 14c0 1.1-.9 2-2 2s-2-.9-2-2 .9-2 2-2 2 .9 2 2zm8 0c0 1.1-.9 2-2 2s-2-.9-2-2 .9-2 2-2 2 .9 2 2zM4 8c0 1.1-.9 2-2 2s-2-.9-2-2 .9-2 2-2 2 .9 2 2zm8 0c0 1.1-.9 2-2 2s-2-.9-2-2 .9-2 2-2 2 .9 2 2zM4 2c0 1.1-.9 2-2 2S0 3.1 0 2 0 0 2 0s2 .9 2 2zm8 0c0 1.1-.9 2-2 2s-2-.9-2-2 .9-2 2-2 2 .9 2 2z" />
                                                                    </svg>
                                                                </div>
                                                                <div
                                                                    className="project-dot priority"
                                                                    style={{
                                                                        marginRight: '15px',
                                                                        position: 'static',
                                                                        width: '24px',
                                                                        height: '24px',
                                                                        backgroundColor: isCurrent || isSelected ? '#007bff' : '#eee',
                                                                        color: isCurrent || isSelected ? '#fff' : '#333'
                                                                    }}
                                                                >
                                                                    {item.priority === 10 ? '-' : item.priority}
                                                                </div>
                                                                <div style={{ flex: 1, fontWeight: isCurrent ? 'bold' : 'normal' }}>
                                                                    {existingProject ? existingProject.name : 'Empty Position'}
                                                                </div>
                                                                {isCurrent && (
                                                                    <div style={{
                                                                        fontSize: '11px',
                                                                        color: '#007bff',
                                                                        border: '1px solid #007bff',
                                                                        borderRadius: '4px',
                                                                        padding: '2px 6px'
                                                                    }}>
                                                                        current position
                                                                    </div>
                                                                )}
                                                            </div>
                                                        )}
                                                    </Draggable>
                                                );
                                            })}
                                            {provided.placeholder}
                                        </div>
                                    )}
                                </Droppable>
                            </DragDropContext>
                        </div>

                    </Modal.Body>
                    <Modal.Footer>
                        <div className="d-flex gap-2">
                            <Button
                                variant={BUTTONS.PRIMARY}
                                onClick={async () => {
                                    if (targetProjectToUpdate?.id) {
                                        try {
                                            await projectService.updatePriority(targetProjectToUpdate.id, selectedPriority);
                                            setModalProject(null);
                                            popup.success(resultMessage(targetProjectToUpdate.name || "Project", "Updated"), "Change Priority");
                                            loadProjects();
                                        } catch (error: any) {
                                            popup.error(error?.response?.data || error?.message || "Unable to update priority. Please try again.", "Change Priority");
                                        }
                                    }
                                }}
                            >
                                Confirm
                            </Button>
                            <Button variant={BUTTONS.SECONDARY} onClick={() => setModalProject(null)}>Cancel</Button>
                        </div>
                    </Modal.Footer>

                </Modal>
            )}
        </>
    );
};

export default ProjectList;
