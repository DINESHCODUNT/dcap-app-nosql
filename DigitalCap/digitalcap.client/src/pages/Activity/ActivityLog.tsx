import React, { useState, useEffect, useMemo } from 'react';
import { Link } from 'react-router-dom';
import AppLoader from '../../Common/AppLoader';
import * as projectService from '../../service/ProjectService';
import { Button, BUTTONS } from 'customer_portal-ui-shared';
import * as Icon from "react-bootstrap-icons";
import { getRelativeTime } from '../../utils/dateTime';
interface ActivityUser {
    name?: string;
    initials?: string;
    avatarColor?: string;
}

interface ActivityItem {
    id: number;
    type: 'create' | 'modify' | 'assigned' | 'status' | 'complete' | 'delete' | 'alert';
    text: string;
    time: string;
    tag: string;
    user?: ActivityUser | string;
    userName?: string;
    userEmail?: string;
    email?: string;
    createdBy?: string;
    modifiedBy?: string;
    entity?: string;
    metadata?: any[];
    [key: string]: any;
}

interface ActivityGroup {
    dateLabel: string;
    dateText: string;
    activities: ActivityItem[];
}

interface ActivityLogProps {
    projectId?: number;
    activities?: any[];
    source?: "overview" | "recentActivity" | "projectLanding";
    onArrowClick?: (activity: any) => void;
    refreshTrigger?: number;
    isLoading?: boolean;
    errorMessage?: string | null;
}
export default function ActivityLog({
    projectId,
    activities = [],
    source = "overview",
    onArrowClick,
    refreshTrigger = 0,
    isLoading = false,
    errorMessage = null,
}: ActivityLogProps) {
    const [overviewGroups, setOverviewGroups] = useState<ActivityGroup[]>([]);
    const [overviewLoading, setOverviewLoading] = useState<boolean>(true);
    const [overviewError, setOverviewError] = useState<string | null>(null);

    const isControlled = source === "recentActivity" || source === "projectLanding";
    const loading = isControlled ? isLoading : overviewLoading;
    const error = isControlled ? errorMessage : overviewError;

    const controlledGroups = useMemo<ActivityGroup[]>(() => {
        if (!isControlled) return [];
        return activities.length > 0 ? [{ dateLabel: "", dateText: "", activities }] : [];
    }, [activities, isControlled]);

    const activityGroups = isControlled ? controlledGroups : overviewGroups;

    const headingText = source === "recentActivity" ? "Recent Activity" : "Activity Log";

    useEffect(() => {
        if (source === "overview") {
            loadActivities();
        }
    }, [projectId, source, refreshTrigger]);

    const loadActivities = async () => {
        try {
            setOverviewLoading(true);
            setOverviewError(null);

            let groups: ActivityGroup[] = [];

            const response = await projectService.getRecentActivities(false, projectId);

            if (response.data) {
                const data = response.data as any;

                if (Array.isArray(data)) {
                    groups = [
                        {
                            dateLabel: "",
                            dateText: "",
                            activities: data.map((item: any) => ({
                                id: item.id || item.ID,
                                text: item.text || item.Text || (item.description ? item.description.split(" - ")[0] : ''),
                                vesselName: [item.vesselName || item.VesselName, (item.pidNumber || item.PIDNumber) ? `Project ID #${item.pidNumber || item.PIDNumber}` : null].filter(Boolean).join(', '),
                                time: getRelativeTime(item.createDttm || item.CreateDttm),
                                type: "modify",
                                tag: "",
                                userName: item.createBy || item.CreateBy,
                                userEmail: item.createBy || item.CreateBy,
                                createBy: item.createBy || item.CreateBy,
                                createDttm: item.createDttm || item.CreateDttm
                            }))
                        }
                    ];
                }
            }

            setOverviewGroups(groups);
        } catch (err: any) {
            setOverviewError('Failed to load activities. Please try again.');
        } finally {
            setOverviewLoading(false);
        }
    };

    const getUserInfo = (activity: ActivityItem) => {
        let userName = '';
        let userEmail = '';
        let userInitials: string | undefined;
        let avatarColor: string | undefined;

        if (activity.user && typeof activity.user === 'object') {
            const userObj = activity.user as ActivityUser;

            userName = userObj.name || '';
            avatarColor = userObj.avatarColor;

            if (userName.includes('@')) {
                const prefix = userName.split('@')[0];

                userEmail = userName;
                userInitials = prefix.substring(0, 2).toUpperCase();
            } else {
                userInitials = userObj.initials;
            }
        } else if (activity.user && typeof activity.user === 'string') {
            userName = activity.user.trim();
            if (userName.includes('@')) {
                userEmail = userName;
            }
        }

        if (!userName) {
            const userNameValue = activity.userName || activity.createdBy || activity.modifiedBy;
            userName = (userNameValue && typeof userNameValue === 'string') ? userNameValue.trim() : '';
        }

        if (!userEmail) {
            const emailValue = activity.userEmail || activity.email || activity.createdByEmail || activity.modifiedByEmail;
            userEmail = (emailValue && typeof emailValue === 'string') ? emailValue.trim() : '';
        }

        if (!userInitials) {
            if (userName && userName.length > 0) {
                const nameParts = userName.split(/\s+/).filter(part => part.length > 0);
                if (nameParts.length >= 2) {
                    userInitials = (nameParts[0][0] + nameParts[1][0]).toUpperCase();
                } else if (nameParts.length === 1) {
                    userInitials = nameParts[0].substring(0, 2).toUpperCase();
                }
            } else if (userEmail && userEmail.length > 0) {
                const emailPrefix = userEmail.split('@')[0];
                userInitials = emailPrefix.substring(0, 2).toUpperCase();
            }
        }

        return { userName, userEmail, userInitials, avatarColor };
    };

    const getActivityIcon = (type: ActivityItem['type']) => {
        switch (type) {
            case 'create':
                return (
                    <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path d="M12 5v14M5 12h14" />
                    </svg>
                );
            case 'modify':
                return (
                    <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                        <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4z" />
                    </svg>
                );
            case 'assigned':
                return (
                    <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                        <circle cx="12" cy="7" r="4" />
                    </svg>
                );
            case 'status':
                return (
                    <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                        <polyline points="17 8 12 3 7 8" />
                        <line x1="12" y1="3" x2="12" y2="15" />
                    </svg>
                );
            case 'complete':
                return (
                    <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
                        <polyline points="22 4 12 14.01 9 11.01" />
                    </svg>
                );
            case 'delete':
                return (
                    <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <polyline points="3 6 5 6 21 6" />
                        <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                        <path d="M10 11v6M14 11v6" />
                    </svg>
                );
            case 'alert':
                return (
                    <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <circle cx="12" cy="12" r="10" />
                        <line x1="12" y1="8" x2="12" y2="12" />
                        <line x1="12" y1="16" x2="12.01" y2="16" />
                    </svg>
                );
            default:
                return (
                    <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                        <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                        <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4z" />
                    </svg>
                );
        }
    };

    if (loading) {
        return (
            <div className="h-100 d-flex flex-column flex-grow-1">
                <div className="section-title">
                    <h5 className='_600'>{headingText}</h5>
                    <div className="section-actions">
                        {source === "overview" && (
                            <Link to="/app/Activity/ActivityLog" className="text-primary text-decoration-underline text-sm fw-medium">
                                View All
                            </Link>
                        )}
                    </div>
                </div>
                <div className="feed flex-grow-1">
                    <AppLoader inline />
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <>
                <div className="section-title">
                    <h5 className='_600'>{headingText}</h5>
                    <div className="section-actions">
                        {source === "overview" && (
                            <Link to="/app/Activity/ActivityLog" className="text-primary text-decoration-underline text-sm fw-medium">
                                View All
                            </Link>
                        )}
                    </div>
                </div>


                <div className="feed">
                    <div className="text-center p-4 text-danger">{error}</div>
                </div>
            </>
        );
    }

    if (activityGroups.length === 0) {
        return (
            <>
                <div className="section-title">
                    <h5 className='_600'>{headingText}</h5>
                    <div className="section-actions">
                        {source === "overview" && (
                            <Link to="/app/Activity/ActivityLog" className="text-primary text-decoration-underline text-sm fw-medium">
                                View All
                            </Link>
                        )}
                    </div>
                </div>
                <div className="feed">
                    <div className="text-center p-4 text-muted">No activities found</div>
                </div>
            </>
        );
    }

    return (
        <>
            <div className="section-title">
                <h5 className='_600'>{headingText}</h5>
                <div className="section-actions">
                    {source === "overview" && (
                        <Link to="/app/Activity/ActivityLog" className="text-primary text-decoration-underline text-sm fw-medium">
                            View All
                        </Link>
                    )}
                </div>
            </div>
            <div className="feed">
                {activityGroups.map((group, groupIndex) => (
                    <React.Fragment key={group.dateLabel || groupIndex}>
                        {group.dateText && (
                            <div className="date-label">
                                <span className="date-text">{group.dateText}</span>
                                <div className="date-line"></div>
                            </div>
                        )}

                        {group.activities && group.activities.map((activity, index) => {
                            const { userName, userEmail, userInitials, avatarColor } = getUserInfo(activity);
                            const displayUser = userName || userEmail;
                            const avatarStyle = avatarColor ? { backgroundColor: avatarColor } : {};

                            return (
                                <div key={`${activity.id}-${index}`} className={`act-item act-${activity.type}`}>
                                    <div className="act-icon-col">
                                        <div className={`act-icon ${activity.type === 'alert' ? 'alert-icon' : activity.type === 'assigned' ? 'assign-icon' : ''}`}>
                                            {getActivityIcon(activity.type)}
                                        </div>
                                    </div>
                                    <div className="act-body">
                                        <div className="act-top">
                                            <div className="act-text">
                                                {(source === "recentActivity" || source === "projectLanding") ? (
                                                    <div>
                                                        <span className="_600">
                                                            {activity.VesselProject ? `${activity.VesselProject} - ${activity.text}` : activity.text}
                                                        </span>
                                                    </div>
                                                ) : (
                                                    <></>
                                                )}
                                            </div>
                                            <div className="d-flex justify-content-end gap-3">

                                                {source === "recentActivity" && (
                                                    <Button
                                                        variant={BUTTONS.TABLEDOWNLOAD}
                                                        startIcon={<Icon.ArrowRight />}
                                                        onClick={() => onArrowClick?.(activity)}
                                                    />
                                                )}
                                            </div>
                                        </div>
                                        {
                                            (source === "recentActivity" || source === "projectLanding") &&
                                            <div className="act-meta">
                                                {activity.tag && (
                                                    <span className="act-tag">{activity.tag}</span>
                                                )}
                                                {displayUser && (
                                                    <div className="act-user">
                                                        <span
                                                            className={`user-avatar user-bg`}
                                                            style={avatarStyle}
                                                        >
                                                            {userInitials || displayUser.substring(0, 2).toUpperCase()}
                                                        </span>
                                                        {userName && userEmail && userName !== userEmail ? (
                                                            <>
                                                                <span>
                                                                    {userName}
                                                                </span>

                                                                <span className="text-muted">({userEmail})</span>

                                                            </>
                                                        ) : (
                                                            <span>{displayUser}</span>
                                                        )}
                                                        <span><Icon.Dot size={14} /></span>
                                                        <span className="act-time">
                                                            {activity.time}
                                                        </span>
                                                    </div>
                                                )}
                                            </div>
                                        }
                                        {
                                            source == "overview" &&
                                            <div className="act-meta">
                                                {activity.tag && (
                                                    <span className="act-tag">{activity.tag}</span>
                                                )}

                                                <div className="act-user-column">

                                                    <span className="_600">
                                                        {activity.text?.split(" - ")[0]}
                                                        {activity.vesselName ? ` — ${activity.vesselName}` : ''}
                                                    </span>

                                                    {displayUser && (
                                                        <div className="act-user">
                                                            <span
                                                                className="user-avatar user-bg"
                                                                style={avatarStyle}
                                                            >
                                                                {userInitials || displayUser.substring(0, 2).toUpperCase()}
                                                            </span>

                                                            <span>{userEmail || displayUser}</span>

                                                            <Icon.Dot size={14} />

                                                            <span
                                                                className="act-time"
                                                            >
                                                                {activity.time}
                                                            </span>
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        }
                                    </div>
                                </div>
                            );
                        })}
                    </React.Fragment>
                ))}
            </div>

        </>
    );
}
