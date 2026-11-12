import { useContext, type JSX } from "react";
import { Navigate } from "react-router-dom";
import { Spinner } from "react-bootstrap";
import { AuthContext } from "../context/AuthContext";

const ProtectedRoute = ({ children }: { children: JSX.Element }) => {
  const { user, loading } = useContext(AuthContext);

  if (loading) {
    return (
      <div className="d-flex flex-column justify-content-center align-items-center min-vh-100">
        <Spinner animation="border" role="status" variant="primary">
          <span className="visually-hidden">Loading...</span>
        </Spinner>
        <div className="mt-3 text-muted">Loading...</div>
      </div>
    );
  }
  if (!user) return <Navigate to="/" replace />;

  return children;
};

export default ProtectedRoute;
