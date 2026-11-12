import React from 'react';
import { BreadcrumbBar, Tab } from "customer_portal-ui-shared";
import ClientList from './Client/ClientList';
import UsersList from './Users/UsersList';
import PermissionList from './Permissions/PermissionList';
import { useSearchParams } from 'react-router-dom';
import { usePermissions } from '../../../context/PermissionContext';
import { PERMISSIONS } from '../../../constants/permissions';
import { ROLES } from '../../../constants/roles';

const AdminOverview: React.FC = () => {
    const [activeTab, setActiveTab] = React.useState<number>(0);
    const { hasGlobalPermission, hasRole } = usePermissions();
    const [searchParams, setSearchParams] = useSearchParams();

    const availableTabs = React.useMemo(() => {
        const tabs = [];
        tabs.push({
            id: 'clients',
            content: <><ClientList /></>,
            header: 'Client Profiles'
        });

        if (hasGlobalPermission(PERMISSIONS.VIEW_ABS_USERS)) {
            tabs.push({
                id: 'users',
                content: <><UsersList /></>,
                header: 'Manage ABS Users'
            });
        }

        if (hasRole(ROLES.ADMIN_CAP_HQ)) {
            tabs.push({
                id: 'permissions',
                content: <><PermissionList /></>,
                header: 'Manage Permission'
            });
        }

        return tabs;
    }, [hasGlobalPermission, hasRole]);

    React.useEffect(() => {
        const tab = searchParams.get("tab");
        let index = availableTabs.findIndex(t => t.id === tab);
        if (index === -1) index = 0;

        setActiveTab(index);
    }, [searchParams, availableTabs]);

    const handleTabClick = (tabIndex: number) => {
        setActiveTab(tabIndex);

        let tabName = "clients";
        if (tabIndex === 1) tabName = "users";
        else if (tabIndex === 2) tabName = "permissions";

        setSearchParams({ tab: tabName });
    };

    const getPageDescription = () => {
        switch (activeTab) {
            case 1:
                return "Add ABS users and assign their role within the application.";
            case 2:
                return "Restriction a page or edit existing Restriction Page.";
            default:
                return "Create a client profile and create their admin accounts. Client profiles will be available when creating a project.";
        }
    };

    const getPageName = () => {
        switch (activeTab) {
            case 1:
                return "Manage ABS Users";
            case 2:
                return "Manage Permission";
            default:
                return "Client Profiles";
        }
    };

    return (
        <>
            <div className="page-title">
                <BreadcrumbBar
                    pageName={getPageName()}
                    parentPages={[
                        {
                            name: "ABS Admin",
                            link: ""
                        }
                    ]}
                >
                    <p className="text-muted mt-2 mb-0">{getPageDescription()}</p>
                </BreadcrumbBar>
            </div>
            <div className="page-content p-4">

                <div className="row">
                    <div className="col-md-12">
                        <div className="nav-vertical">
                            <Tab
                                activeTab={activeTab}
                                onTabChange={handleTabClick}
                                tabData={availableTabs}
                            />
                        </div>
                    </div>
                </div>
            </div>

        </>

    );
};

export default AdminOverview;