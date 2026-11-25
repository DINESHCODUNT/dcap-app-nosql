import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Login from "../pages/Login/Login";
import ForgotPassword from "../pages/Login/ForgotPassword";
import ResetPassword from "../pages/Login/ResetPassword";
import ManageDescriptionList from "../pages/Admin/Configuration/ManageDescription/ManageDescriptionList";
import AppLayout from "../layouts/AppLayout";
import ManageGradingList from "../pages/Admin/Configuration/ManageGrading/ManageGradingList";
import Tanks from "../pages/Admin/Configuration/ManageTanks/Tanks";
import ManageTemplate from "../pages/Admin/Configuration/ManageTemplate/ManageTemplate";
import ManageExport from "../pages/Admin/Configuration/ManageExport/ManageExport";
import AdminOverview from "../pages/Admin/AdminSettings/AdminOverview";
import Overview from "../pages/Dashboard/Overview";
import Success from "../Success";
import ReportOverview from "../pages/SurveyReport/ReportOverview";
import RecentActivityOverview from "../pages/Activity/RecentActivityOverview";
import AccessDenied from "../pages/Error/AccessDenied";
import ClientList from "../pages/Admin/AdminSettings/Client/ClientList";
import PermissionList from "../pages/Admin/AdminSettings/Permissions/PermissionList";
import UsersList from "../pages/Admin/AdminSettings/Users/UsersList";
import RoleProtectedRoute from "./RoleProtectedRoute";
import { PERMISSIONS } from "../constants/permissions";
import { AuthRedirectWrapper } from "../components/AuthRedirectWrapper";

const AppRoutes = () => (
    <BrowserRouter>
        <AuthRedirectWrapper>
            <Routes>
                <Route path="/" element={<Login />} />
                <Route path="/Login" element={<Login />} />
                <Route path="/internal" element={<Login />} />
                <Route path="/forgot-password" element={<ForgotPassword />} />
                <Route path="/reset-password" element={<ResetPassword />} />

                <Route path="/success" element={<Success />} />
                <Route path="/signout" element={<Navigate to="/" replace />} />

                <Route path="/app" element={<AppLayout />}>
                    <Route
                        index
                        element={<Navigate to="Project" replace />}
                    />
                    <Route path="access-denied" element={<AccessDenied />} />
                    <Route path="Project" element={
                        <RoleProtectedRoute requireAnyAccess={true}>
                            <Overview />
                        </RoleProtectedRoute>
                    } />
                    <Route path="Configuration/Managetemplate" element={<ManageTemplate />} />
                    <Route path="Configuration/Managedescriptions" element={<ManageDescriptionList />} />
                    <Route path="Configuration/ManageTanks" element={<Tanks />} />
                    <Route path="Configuration/Manageabsgrading" element={<ManageGradingList />} />
                    <Route path="Configuration/ManageExport" element={<ManageExport />} />
                    <Route path="Admin/Settings" element={
                        <RoleProtectedRoute allowedPermissionAny={[PERMISSIONS.VIEW_CLIENT_PROFILES, PERMISSIONS.MANAGE_ABS_USERS]}>
                            <AdminOverview />
                        </RoleProtectedRoute>
                    } />
                    <Route path="Activity/ActivityLog" element={<RecentActivityOverview />} />
                    <Route path="Client/ClientList" element={<ClientList />} />
                    <Route path="Permissions/PermissionList" element={<PermissionList />} />
                    <Route path="Users/UsersList" element={<UsersList />} />

                    <Route path="project/report" element={<ReportOverview />} />
                </Route>

                <Route path="/Layout" element={<AppLayout />} />
                <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
        </AuthRedirectWrapper>
    </BrowserRouter>
);

export default AppRoutes;