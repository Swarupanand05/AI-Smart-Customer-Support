import { Navigate } from "react-router-dom";
import { isAuthenticated } from "../services/api";

/**
 * Wraps a route so only authenticated users can access it.
 * Redirects to "/" (login) if no token is found.
 */
function ProtectedRoute({ children }) {
  return isAuthenticated() ? children : <Navigate to="/" replace />;
}

export default ProtectedRoute;
