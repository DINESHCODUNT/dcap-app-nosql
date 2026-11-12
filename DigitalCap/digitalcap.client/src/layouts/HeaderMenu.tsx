import React, { useEffect } from 'react';
import { Container, Nav, Navbar } from 'react-bootstrap';
import Offcanvas from 'react-bootstrap/Offcanvas';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from "../store/hooks";
import { usePermissions } from '../context/PermissionContext';
import { PERMISSIONS } from '../constants/permissions';
import { ROLES } from '../constants/roles';
import { fetchAppConfiguration } from "../store/slices/configurationSlice";
import GlobalSearch from './GlobalSearch';

const HeaderMenu: React.FC = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const { hasRole, hasGlobalPermission, hasPagePermission, permissions, roles, pagePermissions } = usePermissions();
    const isAuthorized = permissions.length > 0 || roles.length > 0 || Object.keys(pagePermissions).length > 0;

    const dispatch = useAppDispatch();
    const capPortalUrl = useAppSelector(
        (state) => state.configuration.data?.capPortalUrl
    );

    const [expanded, setExpanded] = React.useState(false);
    const [openMobileMenus, setOpenMobileMenus] = React.useState<string[]>([]);

    const toggleMobileMenu = (id: string) => {
        setOpenMobileMenus(prev => prev.includes(id) ? prev.filter(m => m !== id) : [...prev, id]);
    };

    useEffect(() => {
        if (!capPortalUrl) {
            dispatch(fetchAppConfiguration());
        }
    }, [dispatch, capPortalUrl]);

    type SidebarChildItem = {
        id: string;
        name: string;
        to: string;
        navlink: string;
        refresh: boolean;
    };

    type SidebarMenuItem = {
        id: string;
        title: string;
        link: string;
        enabled: boolean;
        sidebarEnabled: boolean;
        megaMenu: boolean;
        refresh: boolean;
        permission?: string;
        permissionAny?: string[];
        role?: string;
        roleAny?: string[];
        items: SidebarChildItem[];
    };

    const sidebarData: any[] = [
        {
            id: "Allprojects",
            title: "All Projects",
            link: "/app/Project",
            permission: "ViewAllProjects",
            items: [],
        },
        {
            id: "Activity",
            title: "Recent Activity",
            link: "/app/Activity/ActivityLog",
            permissionAny: [
                "ViewRecentActivityAllProjectsAllRegions",
                "ViewRecentActivityAllProjectsOwnRegion"
            ],
            items: [],
        },
        {
            id: "CapPortal",
            title: "Cap Portal",
            link: "",
            roleAny: [ROLES.ADMIN_CAP_HQ, "Contributor_CAP_Coordinator"],
            items: [],
        },
        {
            id: "ProjectConfiguration",
            title: "Project Configuration",
            link: "",
            items: [
                {
                    id: "ManageTemplate",
                    name: "Manage Templates",
                    navlink: "/app/Configuration/Managetemplate",
                    permission: "manageTemplate"
                },
                {
                    id: "ManageGrading",
                    name: "Manage ABS Grading",
                    navlink: "/app/Configuration/Manageabsgrading",
                    permission: "manageGrading"
                },
                {
                    id: "ManageDescription",
                    name: "Manage Description",
                    navlink: "/app/Configuration/Managedescriptions",
                    permission: "manageDescription"
                },
                {
                    id: "ManageTanks",
                    name: "Manage Tanks",
                    navlink: "/app/Configuration/ManageTanks",
                    permission: "manageTank"
                },
                {
                    id: "ManageExport",
                    name: "Manage Export",
                    navlink: "/app/Configuration/ManageExport",
                    permission: "manageExport"
                },
            ],
        },
        {
            id: "ABSAdmin",
            title: "ABS Admin",
            link: "",
            permissionAny: [PERMISSIONS.VIEW_CLIENT_PROFILES, PERMISSIONS.MANAGE_ABS_USERS],
            items: [
                {
                    id: "Clients",
                    name: "Client Profiles",
                    navlink: "/app/Admin/Settings?tab=clients"
                },
                {
                    id: "Users",
                    name: "Manage ABS Users",
                    navlink: "/app/Admin/Settings?tab=users",
                    permission: PERMISSIONS.VIEW_ABS_USERS
                },
                {
                    id: "Permissions",
                    name: "Manage Permission",
                    navlink: "/app/Admin/Settings?tab=permissions",
                    role: ROLES.ADMIN_CAP_HQ
                }
            ]
        }
    ];

    const handleNavClick = (e: React.MouseEvent, navlink: string, menuId?: string) => {

        e.preventDefault();
        setExpanded(false);
        if (navlink && navlink !== "#") {
            if (menuId === "Allprojects" || navlink === "/app/Project") {
                navigate(navlink, { state: { showOverview: true } });
            } else {
                navigate(navlink);
            }
        }
    };

    const isMenuItemActive = (navlink: string) => {
        if (!navlink || navlink === "#") return false;

        const [navPath, navSearch] = navlink.split('?');
        const isPathMatch = location.pathname === navPath || location.pathname.startsWith(navPath + '/');

        if (!isPathMatch) return false;

        if (navSearch) {
            const currentParams = new URLSearchParams(location.search);
            const navParams = new URLSearchParams(navSearch);
            let isMatch = true;
            navParams.forEach((value, key) => {
                if (currentParams.get(key) !== value) {
                    isMatch = false;
                }
            });
            if (!isMatch) return false;
        }

        return true;
    };

    const isParentMenuActive = (menu: SidebarMenuItem) => {
        if (menu.link && (location.pathname === menu.link || location.pathname.startsWith(menu.link + '/'))) {
            return true;
        }
        return menu.items.some(child => isMenuItemActive(child.navlink));
    };


    const menuData = sidebarData
        .map((menu) =>
            menu.id === "CapPortal"
                ? { ...menu, link: capPortalUrl ?? "" }
                : menu
        )
        .filter((menu) => menu.id !== "CapPortal" || !!menu.link);

    const menuList = menuData.filter(menu => {

        if (menu.permission && !hasGlobalPermission(menu.permission)) return false;

        if (menu.permissionAny && !menu.permissionAny.some((p: string) => hasGlobalPermission(p))) return false;

        if (menu.role && !hasRole(menu.role)) return false;

        if (menu.roleAny && !menu.roleAny.some((r: string) => hasRole(r))) return false;

        if (menu.items && menu.items.length > 0) {
            const visibleItems = menu.items.filter(child => {
                if (child.permission && !(hasPagePermission(child.permission) || hasGlobalPermission(child.permission))) return false;
                if (child.role && !hasRole(child.role)) return false;
                if (child.roleAny && !child.roleAny.some((r: string) => hasRole(r))) return false;
                return true;
            });
            if (visibleItems.length === 0) return false;
        }

        return true;
    });
    return (
        <>
            <div className='top-secondary-menu'>
                <Navbar expanded={expanded} onToggle={setExpanded} expand="lg" className='bg-transparent shadow-none w-100' >
                    <Container fluid>
                        <Navbar.Brand href="#" className="d-lg-none">DCAP</Navbar.Brand>
                        <Navbar.Toggle aria-controls={`offcanvasNavbar-expand-lg`} />
                        <Navbar.Offcanvas
                            id={`offcanvasNavbar-expand-lg`}
                            aria-labelledby={`offcanvasNavbarLabel-expand-lg`}
                            placement="end"
                        >
                            <Offcanvas.Header closeButton>
                                <Offcanvas.Title id={`offcanvasNavbarLabel-expand-lg`}>
                                    DCAP
                                </Offcanvas.Title>
                            </Offcanvas.Header>
                            <Offcanvas.Body className="w-100 d-flex">
                                <div className='secondary-menu d-flex justify-content-between w-100 flex-grow-1'>
                                    <div className="secondary-menu-nav">
                                        <ul>
                                            {menuList.map((menu: SidebarMenuItem) => {
                                                const hasChildren = menu.items?.length > 0;
                                                const isParentActive = hasChildren ? isParentMenuActive(menu) : isMenuItemActive(menu.link);

                                                return (
                                                    <li
                                                        key={menu.id}
                                                        className={`${hasChildren ? "nav-item has-child" : "nav-item"} ${isParentActive ? "active" : ""} ${openMobileMenus.includes(menu.id) ? "mobile-open" : ""}`}
                                                    >
                                                        {menu.id === "CapPortal" ? (
                                                            <a
                                                                href={capPortalUrl}
                                                                className="nav-link"
                                                                target="_blank"
                                                                rel="noopener noreferrer"
                                                                onClick={() => setExpanded(false)}
                                                            >
                                                                {menu.title}
                                                            </a>
                                                        ) : menu.link ? (
                                                            <Link
                                                                to={menu.link}
                                                                className="nav-link"
                                                                onClick={(e) => handleNavClick(e, menu.link, menu.id)}
                                                            >
                                                                {menu.title}
                                                            </Link>
                                                        ) : (
                                                            <Nav.Link href="#" onClick={(e) => { e.preventDefault(); toggleMobileMenu(menu.id); }}>
                                                                {menu.title}
                                                            </Nav.Link>
                                                        )}

                                                        {hasChildren && (
                                                            <div className="child-dropdown">
                                                                <ul className="child-items">
                                                                    {menu.items
                                                                        .filter((child: any) => {
                                                                            if (child.permission && !(hasPagePermission(child.permission) || hasGlobalPermission(child.permission))) return false;
                                                                            if (child.role && !hasRole(child.role)) return false;
                                                                            if (child.roleAny && !child.roleAny.some((r: string) => hasRole(r))) return false;
                                                                            return true;
                                                                        })
                                                                        .map((child: any) => {
                                                                            const isActive = isMenuItemActive(child.navlink);

                                                                            return (
                                                                                <li key={child.id} className={isActive ? "child-nav-item active" : "child-nav-item"}>
                                                                                    {child.navlink && child.navlink !== "#" ? (
                                                                                        <Link
                                                                                            to={child.navlink}
                                                                                            className="child-nav-link"
                                                                                            onClick={(e) => handleNavClick(e, child.navlink)}
                                                                                        >
                                                                                            {child.name}
                                                                                        </Link>
                                                                                    ) : (
                                                                                        <Nav.Link
                                                                                            href={child.to || child.navlink || "#"}
                                                                                            className="child-nav-link"
                                                                                        >
                                                                                            {child.name}
                                                                                        </Nav.Link>
                                                                                    )}
                                                                                </li>
                                                                            );
                                                                        })}
                                                                </ul>
                                                            </div>
                                                        )}
                                                    </li>
                                                );
                                            })}
                                        </ul>
                                    </div>

                                    {isAuthorized && (
                                        <div className="ms-auto d-none d-lg-block">
                                            <GlobalSearch />
                                        </div>
                                    )}
                                </div>
                            </Offcanvas.Body>
                        </Navbar.Offcanvas>
                    </Container>
                </Navbar>
            </div>
        </>
    );
};

export default HeaderMenu;