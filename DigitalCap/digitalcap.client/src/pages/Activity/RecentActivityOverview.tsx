import React, { useState, useEffect, useRef } from 'react';
import { BreadcrumbBar, Button, BUTTONS } from "customer_portal-ui-shared";
import { getRelativeTime } from '../../utils/dateTime';
import ActivityLog from '../Activity/ActivityLog';
import * as projectService from '../../service/ProjectService';
import { useNavigate, useLocation } from 'react-router-dom';
import { toFriendlyErrorMessage } from '../../utils/errorMessage';

const RecentActivityOverview: React.FC = () => {
    const navigate = useNavigate();

    const [activities, setActivities] = useState<any[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [loadError, setLoadError] = useState<string | null>(null);
    const requestIdRef = useRef<number>(0);

    const location = useLocation();
    const [filteredProjectId, setFilteredProjectId] = useState<number>(0);
    const [filteredProjectName, setFilteredProjectName] = useState<string>("");

    useEffect(() => {
        const state = location.state as { projectId?: any, projectName?: string } | null;
        const pId = state?.projectId ? Number(state.projectId) : 0;
        setFilteredProjectId(pId);
        setFilteredProjectName(state?.projectName || "");
        loadRecentActivities(pId);
    }, [location.state]);

    const loadRecentActivities = async (projectId: number = 0) => {
        const currentRequestId = ++requestIdRef.current;
        setLoading(true);
        setLoadError(null);
        try {
            let response;
            if (projectId > 0) {
                response = await projectService.GetAllActivities2ByProjectId(false, projectId);
            } else {
                response = await projectService.GetRecentActivities2(false, 0);
            }

            if (requestIdRef.current !== currentRequestId) return;

            const mappedActivities = (response.data || []).map((item: any) => ({
                id: item.id,
                ProjectId: item.projectId,
                VesselProject: item.vesselProject,
                text: item.text,
                time: getRelativeTime(item.changeTime),
                userName: item.changedBy,
                userEmail: item.changedBy,
                type: "modify",
                tag: ""
            }));

            setActivities(mappedActivities);
        } catch (err: any) {
            if (requestIdRef.current !== currentRequestId) return;
            setActivities([]);
            setLoadError(toFriendlyErrorMessage(
                err?.response?.data?.message ?? err?.response?.data ?? err?.message,
                "Failed to load activities. Please try again."
            ));
        } finally {
            if (requestIdRef.current === currentRequestId) {
                setLoading(false);
            }
        }
    };

    const handleArrowClick = (activity: any) => {
        navigate('/app/Project', {
            state: {
                projectId: activity.ProjectId,
                projectName: activity.VesselProject,
                focusTab: 0
            }
        });
    };

    return (
        <>
            <div className="page-title">
                <BreadcrumbBar pageName={filteredProjectName ? `Recent Activity - ${filteredProjectName}` : "Recent Activity"}>
                    <div className="section-actions d-flex flex-wrap align-items-center gap-2">
                        {filteredProjectId > 0 && (
                            <Button
                                variant={BUTTONS.SECONDARY}
                                onClick={() => {
                                    navigate('/app/Project', {
                                        state: {
                                            projectId: filteredProjectId,
                                            projectName: filteredProjectName,
                                            focusTab: 99
                                        }
                                    });
                                }}
                            >
                                &#8592; Back to Project ({filteredProjectName || 'Project'})
                            </Button>
                        )}
                    </div>
                </BreadcrumbBar>
            </div>
            <div className="page-content p-4 recent-activity-page">
                <div className="row">
                    <div className="col-md-12">
                        <div className="grid-container">
                            <div className="grid-content-body">
                                <div className="card-area">
                                    <ActivityLog
                                        source="recentActivity"
                                        activities={activities}
                                        isLoading={loading}
                                        errorMessage={loadError}
                                        onArrowClick={handleArrowClick}
                                    />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </>
    );
};

export default RecentActivityOverview;