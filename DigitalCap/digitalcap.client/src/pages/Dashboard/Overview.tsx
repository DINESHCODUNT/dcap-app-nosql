import React, { useContext, useState, useEffect, useCallback } from 'react';
import { Button, BUTTONS, SideDialogManagerContext, BreadcrumbBar } from "customer_portal-ui-shared";
import CreateProject from '../Project/Createproject';
import _uniqueId from "lodash/uniqueId";
import ActivityLog from '../Activity/ActivityLog';
import ProjectList, { type SidebarProjectRow } from '../Project/ProjectList';
import DashboardWidgets from './DashboardWidgets';
import ProjectLanding from '../Project/ProjectLanding';
import * as projectService from '../../service/ProjectService';
import { usePopup } from '../../Common/PopupProvider';
import { toFriendlyErrorMessage } from '../../utils/errorMessage';
import * as userPermissionService from "../../service/UserPermissionService";
import { useLocation } from 'react-router-dom';

function downloadCsv(rows: Array<Record<string, string>>, filename: string): void {
    if (rows.length === 0) return;

    const headers = Array.from(
        new Set(rows.flatMap(row => Object.keys(row)))
    );

    const escapeCell = (v: string) =>
        `"${String(v ?? '').replace(/"/g, '""')}"`;

    const lines = [
        headers.join(','),
        ...rows.map((row) =>
            headers.map((h) => escapeCell(String(row[h] ?? ''))).join(',')
        )
    ];

    const blob = new Blob([lines.join('\r\n')], {
        type: 'text/csv;charset=utf-8;'
    });

    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
}

const Overview: React.FC = () => {
    const popup = usePopup();
    const location = useLocation();
    const sideDialog = useContext(SideDialogManagerContext) as any;
    const [selectedProject, setSelectedProject] = React.useState<SidebarProjectRow | null>(null);
    const [refreshTrigger, setRefreshTrigger] = React.useState<number>(0);
    const [cancelSubmitting, setCancelSubmitting] = React.useState(false);
    const [sidebarStats, setSidebarStats] = React.useState<projectService.ProjectSidebarStatsAggregate | null>(null);
    const [statusChartStats, setStatusChartStats] = React.useState<projectService.ProjectSidebarStatsAggregate | null>(null);
    const [regionChartStats, setRegionChartStats] = React.useState<projectService.ProjectSidebarStatsAggregate | null>(null);
    const [statsFailed, setStatsFailed] = React.useState<boolean>(false);
    const [exporting, setExporting] = useState(false);
    const [buttonPermissions, setButtonPermissions] = useState({
        addProject: true,
        exportProject: true,
        cancelProject: true,
        deleteProject: true
    });
    const [permissions, setPermissions] = React.useState<string[]>([]);
    const [initialTab, setInitialTab] = React.useState<number>(0);
    const [sidebarSearchTerm, setSidebarSearchTerm] = React.useState<string>('');
    const [dashboardStatusFilter, setDashboardStatusFilter] = React.useState<string | null>(null);
    const [dashboardRegionFilter, setDashboardRegionFilter] = React.useState<string | null>(null);

    useEffect(() => {
        const nav = location.state as {
            projectId?: number;
            projectName?: string;
            focusTab?: number;
            showOverview?: boolean;
        } | null;

        if (nav?.showOverview) {
            setSelectedProject(null);
            setInitialTab(0);
            setSidebarSearchTerm('');
            window.history.replaceState({}, '');
            return;
        }

        if (!nav?.projectId) return;

        setSelectedProject({
            id: nav.projectId,
            name: nav.projectName ?? '',
            imo: '',
            date: '',
            pct: 0,
            status: ''
        } as any);

        const loadNavigatedProject = async () => {
            try {
                const response = await projectService.getProjectById(nav.projectId!);
                const data = response.data as any;
                const pd = data?.projectDetails ?? {};

                setSelectedProject({
                    id: nav.projectId,
                    name: nav.projectName ?? pd.vesselName ?? '',
                    imo: pd.imo ? String(pd.imo) : '',
                    date: pd.potentialDrydockDate ?? '',
                    pct: data?.percentComplete ?? 0,
                    status: projectService.getSidebarConnectionStatus
                        ? projectService.getSidebarConnectionStatus(pd)
                        : (pd.status ?? ''),
                    wcn: pd.wcn ?? '',
                    capRegion: pd.capRegion ?? '',
                    pidNumber: pd.projectId ? String(pd.projectId) : '',
                    capType: pd.capType ?? '',
                    capScope: pd.capScope ?? '',
                    statusName: pd.statusName ?? '',
                    projectStatusName: pd.projectStatusName ?? '',
                    createdDate: pd.createdDate ?? '',
                    createdBy: pd.createdBy ?? '',
                    lastModifiedDate: pd.statusChangedDate ?? '',
                    lastModifiedBy: pd.lastModifiedBy ?? pd.statusChangedBy ?? ''
                } as any);
            } catch (e) {
                setSelectedProject({
                    id: nav.projectId,
                    name: nav.projectName ?? '',
                    imo: '',
                    date: '',
                    pct: 0,
                    status: ''
                });
            }
        };

        void loadNavigatedProject();
        setInitialTab(nav.focusTab ?? 0);
        setSidebarSearchTerm('');

        window.history.replaceState({}, '');
    }, [location.state]);

    useEffect(() => {
        const loadPermissions = async () => {
            try {
                const res = await projectService.GetList();

                const data = res.data || {};

                setButtonPermissions({
                    addProject: data.addProject ?? false,
                    exportProject: data.exportProject ?? false,
                    cancelProject: data.cancelProject ?? false,
                    deleteProject: data.deleteProject ?? false
                });
                try {
                    const response = await userPermissionService.GetUserMenuPermissions();
                    const data = response.data || {};

                    setPermissions(Array.isArray(data.permissions) ? data.permissions : []);
                } catch (e) {
                }


            } catch (e) {
            }
        };

        loadPermissions();
    }, []);

    const handleExport = async () => {
        setExporting(true);
        try {
            const countRes = await projectService.getProjectCount();
            const totalCount = (countRes.data as any)?.data?.[0]?.count ?? 0;
            const rows = await projectService.exportAllProjectsGridPaginated(totalCount);
            const now = new Date();
            const stamp = `${now.getMonth() + 1}_${now.getDate()}_${now.getFullYear()}_${now.getHours()}_${now.getMinutes()}_${now.getSeconds()}`;

            downloadCsv(rows, `Digital CAP - All Projects - ${stamp}.csv`);
        } catch (err) {
            popup.error(toFriendlyErrorMessage((err as any)?.message, "Unable to export projects. Please try again later."), "Export");
        } finally {
            setExporting(false);
        }
    };

    const id = _uniqueId("sideDialog_");
    const handleClick = () => {
        sideDialog.addDialog({
            id,
            component: <CreateProject id={id} onSuccess={() => setRefreshTrigger(n => n + 1)} />
        });
    };

    const handleEditProject = useCallback((projectId: number) => {
        const editId = _uniqueId("sideDialog_");
        sideDialog.addDialog({
            id: editId,
            component: <CreateProject id={editId} projectId={projectId} onSuccess={() => setRefreshTrigger(n => n + 1)} />
        });
    }, [sideDialog]);

    const hasPermission = useCallback((perm: string) => {
        return buttonPermissions?.[perm] === true;
    }, [buttonPermissions]);

    const hasGlobalPermission = useCallback((perm: string) =>
        Array.isArray(permissions) && permissions.includes(perm), [permissions]);

    const handleProjectsChanged = useCallback(() => setRefreshTrigger(n => n + 1), []);

    useEffect(() => {
        if (selectedProject?.id && refreshTrigger > 0) {
            projectService.getAllProjectsGridData().then(res => {
                const items = projectService.normalizeAllProjectsGridPayload(res.data);
                const updated = items.find((p: any) => p.id === selectedProject.id);
                if (updated) {
                    setSelectedProject(prev => {
                        if (prev && prev.id === selectedProject.id) {
                            return { ...prev, ...updated };
                        }
                        return prev;
                    });
                }
            }).catch(e => {
                console.error("Failed to refresh project data", e);
                popup.error({ message: "Failed to refresh project data." });
            });
        }
    }, [refreshTrigger, selectedProject?.id, popup]);

    const handleBack = useCallback(() => setSelectedProject(null), []);

    const handleCancelProject = async () => {
        if (!selectedProject?.id) return;
        const confirmed = await popup.confirm({
            title: "Cancel Project",
            message: "Are you sure you want to proceed with cancelling this project?",
            confirmText: "Yes, Proceed"
        });
        if (!confirmed) return;

        setCancelSubmitting(true);
        try {
            await projectService.postProjectLifecycleAction("cancel", selectedProject.id);
            handleProjectsChanged();
            setSelectedProject({ ...selectedProject, projectStatusName: "Cancelled" } as any);
        } catch (e: any) {
            popup.error({ message: e?.message || "Failed to cancel project." });
        } finally {
            setCancelSubmitting(false);
        }
    };

    return (
        <>
            <div className="page-title">
                {selectedProject ? (
                    <BreadcrumbBar
                        pageName={selectedProject.name}
                        parentPages={[{ name: 'All Projects', onClick: () => setSelectedProject(null), link: '' }]}
                    >
                        <div className="section-actions d-flex flex-column flex-md-row justify-content-center justify-content-md-end align-items-end align-items-md-center gap-2 mt-2 mt-md-0 flex-grow-1">
                            <Button variant={BUTTONS.SECONDARY} onClick={() => setSelectedProject(null)}>
                                ← Back
                            </Button>
                            {String(selectedProject.projectStatusName).toLowerCase() !== "cancelled" && (
                                <Button onClick={handleCancelProject} disabled={cancelSubmitting}>
                                    Cancel Project
                                </Button>
                            )}
                            {["closed", "cancelled"].includes(String(selectedProject.projectStatusName).toLowerCase()) && selectedProject.id != null && hasGlobalPermission("AbsProjectLandingPageReassessmentButton") && (
                                <Button onClick={() => handleEditProject(selectedProject.id as number)}>
                                    Create Reassessment
                                </Button>
                            )}
                        </div>
                    </BreadcrumbBar>
                ) : (
                    <BreadcrumbBar
                        pageName="All Projects"
                    >
                        <div className="section-actions d-flex flex-column flex-md-row justify-content-center justify-content-md-end align-items-end align-items-md-center gap-2 mt-2 mt-md-0 flex-grow-1">

                            {hasPermission("exportProject") && (
                                <Button disabled={exporting} onClick={() => void handleExport()}>
                                    {exporting ? 'Exporting…' : 'Export'}
                                </Button>
                            )}

                            {hasPermission("addProject") && (
                                <Button onClick={handleClick}>
                                    Add New Project
                                </Button>
                            )}

                        </div>
                    </BreadcrumbBar>
                )}
            </div>
            <div className="page-content p-4">
                {!selectedProject ? (
                    <>
                        <div className="row g-4 mb-4">
                            <div className="col-12 col-xl-8">
                                <DashboardWidgets
                                    stats={sidebarStats}
                                    statusChartStats={statusChartStats}
                                    regionChartStats={regionChartStats}
                                    statsFailed={statsFailed}
                                    onStatusClick={setDashboardStatusFilter}
                                    onRegionClick={setDashboardRegionFilter}
                                />
                            </div>
                            <div className="col-12 col-xl-4">
                                <div className="card-area h-100 mb-0 d-flex flex-column">
                                    <ActivityLog source="overview" refreshTrigger={refreshTrigger} />
                                </div>
                            </div>
                        </div>
                        <div className="row mt-4">
                            <div className="col-md-12">
                                <ProjectList
                                    selectedProject={selectedProject}
                                    onSelectProject={setSelectedProject}
                                    refreshTrigger={refreshTrigger}
                                    onSidebarStats={(stats) => {
                                        setSidebarStats(stats);
                                        setStatsFailed(false);
                                    }}
                                    onStatsError={() => setStatsFailed(true)}
                                    onStatusChartStats={setStatusChartStats}
                                    onRegionChartStats={setRegionChartStats}
                                    externalSearchTerm={sidebarSearchTerm}
                                    externalStatusFilter={dashboardStatusFilter}
                                    externalRegionFilter={dashboardRegionFilter}
                                    onClearExternalFilters={() => {
                                        setDashboardStatusFilter(null);
                                        setDashboardRegionFilter(null);
                                    }}
                                    navigatedProjectId={selectedProject?.id ?? undefined}
                                    onProjectsLoaded={(projects) => {
                                        setSelectedProject(prev => {
                                            if (prev) {
                                                const updated = projects.find(p => p.id === prev.id);
                                                if (updated) return updated;
                                            }
                                            return prev;
                                        });
                                    }}
                                />
                            </div>
                        </div>
                    </>
                ) : (
                    <div className="row">
                        <div className="col-md-12">
                            <ProjectLanding
                                project={selectedProject}
                                onEdit={handleEditProject}
                                onProjectsChanged={handleProjectsChanged}
                                globalPermission={hasGlobalPermission}
                                initialTab={initialTab}
                                onBack={handleBack}
                            />
                        </div>
                    </div>
                )}
            </div>
        </>

    );
};

export default Overview;