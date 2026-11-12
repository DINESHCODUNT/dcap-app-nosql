import React from 'react';
import ABSLogo from '../assets/images/ABSLogoBlue.svg';
import Dropdown from 'react-bootstrap/esm/Dropdown';
import styled from 'styled-components'
import Navbar from 'react-bootstrap/esm/Navbar';
import { NavItem, Container } from 'react-bootstrap';
import * as Icon from 'react-bootstrap-icons';
import { MsalContext, type IMsalContext } from '@azure/msal-react';
import { Modal } from 'react-bootstrap';
import { Button, BUTTONS } from 'customer_portal-ui-shared';
import ChangePassword from '../Common/ChangePassword';
import AddUser, { type CapUser } from '../pages/Admin/AdminSettings/Users/AddUser';
import { usePopup } from '../Common/PopupProvider';
import * as absUserService from '../service/AbsUserService';
import { usePermissions } from '../context/PermissionContext';

const Header: React.FC = () => {
    const msalContext = React.useContext(MsalContext);
    const account = msalContext.accounts?.[0];
    const { reloadPermissions, permissions, roles, pagePermissions } = usePermissions();
    const isAuthorized = permissions.length > 0 || roles.length > 0 || Object.keys(pagePermissions).length > 0;
    const userName =
        account?.name ||
        account?.username ||
        'User';
    const popup = usePopup();
    const [showContactModal, setShowContactModal] = React.useState(false);
    const [showMyProfile, setShowMyProfile] = React.useState(false);
    const [myProfileUser, setMyProfileUser] = React.useState<CapUser | null>(null);
    const [loadingMyProfile, setLoadingMyProfile] = React.useState(false);

    const getCurrentUserEmail = () =>
        account?.username ||
        (account?.idTokenClaims as { preferred_username?: string })?.preferred_username ||
        "";

    const handleMyProfile = async () => {
        const email = getCurrentUserEmail();
        if (!email) {
            popup.error("Unable to identify current user");
            return;
        }

        setLoadingMyProfile(true);
        try {
            const response = await absUserService.getCapUsers({ isClientUsers: false });
            const raw = response.data?.data ?? response.data;

            let users: CapUser[] = [];
            if (Array.isArray(raw?.capUsers)) {
                users = raw.capUsers;
            } else if (Array.isArray(raw)) {
                users = raw;
            }

            const match = users.find(
                (user) => String(user.email ?? "").toLowerCase() === email.toLowerCase()
            );

            if (!match) {
                popup.error("Failed to load profile");
                return;
            }

            setMyProfileUser(match);
            setShowMyProfile(true);
        } catch (error) {
            popup.error("Failed to load profile");
        } finally {
            setLoadingMyProfile(false);
        }
    };
    const [showChangePassword, setShowChangePassword] = React.useState(false);
    const userEmail = account?.username || '';
    async function handleLogout(e: React.MouseEvent<HTMLElement, MouseEvent>, value?: IMsalContext | null): Promise<void> {
        e.preventDefault();
        if (!value || !(value as any).instance) return;
        const instance = (value as any).instance;
        try {
            if (typeof instance.logoutRedirect === 'function') {
                await instance.logoutRedirect();
            } else if (typeof instance.logoutPopup === 'function') {
                await instance.logoutPopup();
            } else {
            }
        } catch (err) {
        }
    }

    return (
        <>
            <StyledToBar fixed='top' className='top-nav-bar px-2 px-md-3'>
                <Container fluid className="d-flex justify-content-between align-items-center flex-nowrap p-0 m-0">
                    <NavItem className='top-nav-item border-0'>
                        <div className='d-flex align-items-center'>
                            <div className="abs-logo-container">
                                <div className="img-logo">
                                    <img src={ABSLogo} alt="DCAP Logo" height="40" className="login-logo" />
                                </div>
                            </div>
                        </div>
                    </NavItem>
                    <div className='d-flex align-items-center gap-2 gap-md-3'>
                        <NavItem className='top-nav-item border-0'>
                            {isAuthorized ? (
                                <Dropdown align="end">
                                    <Dropdown.Toggle id="dropdown-basic" variant='secondary' className='btn btn-transparent p-1 p-md-2'>
                                        <div className='d-flex align-items-center'>
                                            <div className='action-icons'>
                                                <Icon.PersonCircle size={20} />
                                            </div>
                                            <div className='d-none d-md-inline pl-2 pr-2'>{userName}</div>
                                        </div>
                                    </Dropdown.Toggle>

                                    <Dropdown.Menu>
                                        <Dropdown.Item onClick={() => setShowChangePassword(true)}>Change Password</Dropdown.Item>
                                        <Dropdown.Item
                                            onClick={handleMyProfile}
                                            disabled={loadingMyProfile}
                                        >
                                            {loadingMyProfile ? "Loading..." : "My Profile"}
                                        </Dropdown.Item>
                                        <Dropdown.Item
                                            onClick={() => setShowContactModal(true)}
                                        >
                                            Contact Us
                                        </Dropdown.Item>
                                    </Dropdown.Menu>
                                </Dropdown>
                            ) : (
                                <div className='btn btn-transparent d-flex align-items-center p-1 p-md-2' style={{ cursor: 'default' }}>
                                    <div className='action-icons'>
                                        <Icon.PersonCircle size={20} />
                                    </div>
                                    <div className='d-none d-md-inline pl-2 pr-2 text-white'>{userName}</div>
                                </div>
                            )}
                        </NavItem>
                        <NavItem className='top-nav-item border-0'>
                            <a className='nav-link p-1 p-md-2'
                                aria-current='false'
                                onClick={(e) => handleLogout(e, msalContext)}
                                style={{ cursor: 'pointer' }}
                            >
                                <div className='d-flex align-items-center'>
                                    <div className='action-icons'>
                                        <Icon.BoxArrowRight size={20} />
                                    </div>
                                    <div className='d-none d-md-inline pl-2'>Logout</div>
                                </div>
                            </a>
                        </NavItem>
                    </div>
                </Container>
            </StyledToBar>
            <Modal
                show={showContactModal}
                size="lg"
                centered
                backdrop="static"
                onHide={() => setShowContactModal(false)}
            >
                <Modal.Header closeButton>
                    <Modal.Title>Contact Us</Modal.Title>
                </Modal.Header>

                <Modal.Body>

                    <div className="mb-4">
                        <h5>CAP Coordinators - Americas/Europe Region: Jim Woling</h5>
                        <ul>
                            <li>Phone: +1-281-877-6368</li>
                            <li>Mobile: +1-282-380-3295</li>
                            <li>
                                <a href="mailto:jwolling@eagle.org">
                                    jwolling@eagle.org
                                </a>
                            </li>
                        </ul>
                    </div>

                    <div className="mb-4">
                        <h5>CAP Coordinators - Asia Region: Zenn Tan</h5>
                        <ul>
                            <li>Phone: +6563717844</li>
                            <li>Mobile: +6583833824</li>
                            <li>
                                <a href="mailto:zetan@eagle.org">
                                    zetan@eagle.org
                                </a>
                            </li>
                        </ul>
                    </div>

                    <div className="mb-4">
                        <h5>CAP Coordinators - Greece Region: George Lountzis</h5>
                        <ul>
                            <li>Phone: +302109441000</li>
                            <li>Mobile: +306949204757</li>
                            <li>
                                <a href="mailto:glountzis@eagle.org">
                                    glountzis@eagle.org
                                </a>
                            </li>
                        </ul>
                    </div>

                    <div className="mb-4">
                        <h5>CAP Coordinators - Middle East Region: Youssef Boulares</h5>
                        <ul>
                            <li>Phone: +1-281-877-6314</li>
                            <li>Mobile: +1-346-380-0649</li>
                            <li>
                                <a href="mailto:yboulares@eagle.org">
                                    yboulares@eagle.org
                                </a>
                            </li>
                        </ul>
                    </div>

                    <div className="mb-4">
                        <h5>CAP Manager: Sumeet Sahnan</h5>
                        <ul>
                            <li>Phone: +1-281-673-2934</li>
                            <li>Mobile: +1-832-683-9847</li>
                            <li>
                                <a href="mailto:ssahnan@eagle.org">
                                    ssahnan@eagle.org
                                </a>
                            </li>
                        </ul>
                    </div>

                    <div>
                        <h5>Technical Assistance: Customer Service Center</h5>
                        <ul>
                            <li>Phone: +1-281-877-6499</li>
                            <li>
                                <a href="mailto:csc@eagle.org">
                                    csc@eagle.org
                                </a>
                            </li>
                        </ul>
                    </div>

                </Modal.Body>

                <Modal.Footer>
                    <Button
                        variant={BUTTONS.SECONDARY}
                        onClick={() => setShowContactModal(false)}
                    >
                        Close
                    </Button>
                </Modal.Footer>
            </Modal>
            <ChangePassword
                show={showChangePassword}
                onHide={() => setShowChangePassword(false)}
                userEmail={userEmail}
            />

            <AddUser
                show={showMyProfile}
                onHide={() => {
                    setShowMyProfile(false);
                    setMyProfileUser(null);
                }}
                onSaved={async () => {
                    setShowMyProfile(false);
                    setMyProfileUser(null);
                    popup.success("User updated successfully.", "Update User");
                    await reloadPermissions();
                }}
                onError={(message) => {
                    popup.error(message || "Failed to update profile.");
                }}
                isClientUsers={false}
                isMyProfile={true}
                editUser={myProfileUser}
            />
        </>
    );
};
export default Header;
const StyledToBar = styled(Navbar)`
  background-color: #002a4e;
  color: #fff;
  margin: 0px !important;
  .nav-item {
    & .nav-link {
      color: #fff !important;
    }
  }
  .abs-logo-container {
    
    background-size: contain;
    height: 64px;
    width: 150px;
    background-repeat: no-repeat;
    position: relative;
    background-position: 0 0;
  }`;