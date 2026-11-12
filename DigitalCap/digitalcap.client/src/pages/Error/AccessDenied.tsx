import React from "react";
import { Container, Card, ListGroup, Row, Col } from "react-bootstrap";
import * as Icon from "react-bootstrap-icons";

const AccessDenied: React.FC = () => {
    return (
        <Container className="d-flex justify-content-center align-items-center" style={{ minHeight: '75vh' }}>
            <Row className="justify-content-center w-100">
                <Col lg={8} md={10}>
                    <Card className="border-0">
                        <Card.Body className="p-5">
                            <div className="d-flex flex-column align-items-center mb-4">
                                <div className="icon-section">
                                    <Icon.ExclamationTriangleFill className="text-danger" size={32} />
                                </div>
                                <h2 className="text-danger">Not Authorized</h2>
                                <p className="tx-14 _500">You do not have access to the requested resource.</p>
                            </div>
                            <h5 className="text-muted mb-3 mt-4">Some steps you can try:</h5>

                            <ListGroup variant="flush" className="border-top">
                                <ListGroup.Item className="px-0 py-3 d-flex align-items-center">
                                    <Icon.ArrowRepeat className="me-3 text-primary" size={20} />
                                    <span>Logout, then log back in to your account to update your login credentials.</span>
                                </ListGroup.Item>
                                <ListGroup.Item className="px-0 py-3 d-flex align-items-center">
                                    <Icon.PersonBadge className="me-3 text-primary" size={20} />
                                    <span>Contact your organizational Admin for appropriate permissions.</span>
                                </ListGroup.Item>
                                <ListGroup.Item className="px-0 py-3 d-flex align-items-center">
                                    <Icon.CheckCircle className="me-3 text-primary" size={20} />
                                    <span>Make sure you are logged in to your account.</span>
                                </ListGroup.Item>
                                <ListGroup.Item className="px-0 py-3 d-flex align-items-center">
                                    <Icon.Headset className="me-3 text-primary" size={20} />
                                    <span>Contact a member of ABS administrators for help.</span>
                                </ListGroup.Item>
                            </ListGroup>
                        </Card.Body>
                    </Card>
                </Col>
            </Row>
        </Container>
    );
};

export default AccessDenied;
