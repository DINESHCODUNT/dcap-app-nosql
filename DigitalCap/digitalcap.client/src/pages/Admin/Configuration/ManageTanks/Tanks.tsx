import React from 'react';
import { BreadcrumbBar, Tab, Button, BUTTONS } from "customer_portal-ui-shared";
import { useSearchParams, useNavigate } from "react-router-dom";
import ManageTankList from './ManageTankList';
import ProjectTankList from './ProjectTanksList';


const Tanks: React.FC = () => {
    const [activeTab, setActiveTab] = React.useState<number>(0);
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const projectId = searchParams.get("ProjectId");

    const handleTabClick = (tabIndex: number) => {
        setActiveTab(tabIndex);
    };

    return (
        <>
            <div className="page-title">
                <BreadcrumbBar
                    pageName="Manage Tank"
                    parentPages={[
                        {
                            name: "Project Configuration",
                            link: ""
                        }
                    ]}>
                    <p className="text-muted mt-2 mb-0">Create a new Tank or edit existing Tanks.</p>
                    {projectId && (
                        <div className="d-flex gap-2">
                            <Button
                                variant={BUTTONS.SECONDARY}
                                onClick={() => navigate('/app/project/report', { state: { projectId: projectId } })}
                            >
                                Back to Project
                            </Button>
                        </div>
                    )}
                </BreadcrumbBar>
            </div>
            <div className="page-content p-4">
                <div className="row">
                    <div className="col-md-12">
                        <Tab
                            activeTab={activeTab}
                            onTabChange={handleTabClick}
                            tabData={[
                                {
                                    content: (
                                        <>
                                            <ProjectTankList />

                                        </>
                                    ),
                                    header: 'Project Tanks'
                                },
                                {
                                    content: (
                                        <>
                                            <ManageTankList />
                                        </>
                                    ),
                                    header: 'Master Tanks'
                                }
                            ]}
                        />
                    </div>
                </div>

            </div>
        </>
    );
};

export default Tanks;