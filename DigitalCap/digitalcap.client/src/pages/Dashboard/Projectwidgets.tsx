import React from "react";
import type { ProjectSidebarStatsAggregate } from "../../service/ProjectService";

export interface ProjectwidgetsProps {
    /** From `ProjectList` / same grid as sidebar — not `GetProjectByStatuses`. */
    stats: ProjectSidebarStatsAggregate | null;
}

const Projectwidgets: React.FC<ProjectwidgetsProps> = ({ stats }) => {
    const fmt = (n: number | undefined) =>
        stats == null ? "—" : String(n ?? 0);

    return (
        <>
            <div id="overview-view">
                <div className="stat-strip">
                    <div className="stat-card">
                        <div className="stat-num">{fmt(stats?.total)}</div>
                        <div className="stat-label">Total Projects</div>
                    </div>
                    <div className="stat-card">
                        <div className="stat-num">{fmt(stats?.statuses?.['Open'])}</div>
                        <div className="stat-label">Open</div>
                    </div>
                    <div className="stat-card">
                        <div className="stat-num">{fmt(stats?.statuses?.['Active'])}</div>
                        <div className="stat-label">Active</div>
                    </div>
                    <div className="stat-card">
                        <div className="stat-num">{fmt(stats?.statuses?.['Closed'])}</div>
                        <div className="stat-label">Closed</div>
                    </div>
                    <div className="stat-card">
                        <div className="stat-num">{fmt(stats?.online)}</div>
                        <div className="stat-label">Online</div>
                    </div>
                    <div className="stat-card">
                        <div className="stat-num">{fmt(stats?.offline)}</div>
                        <div className="stat-label">Offline</div>
                    </div>
                </div>
            </div>
        </>
    );
};

export default Projectwidgets;
