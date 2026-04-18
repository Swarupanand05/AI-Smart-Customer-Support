import { Navigate } from "react-router-dom";
import { isAuthenticated, getUser } from "../services/api";

/**
 * AdminRoute — only allows users with role === "Admin".
 * - Not logged in  → redirect to /
 * - Logged in but not Admin → redirect to /dashboard
 */
function AdminRoute({ children }) {
  if (!isAuthenticated()) return <Navigate to="/" replace />;
  const user = getUser();
  if (user?.role !== "Admin") return <Navigate to="/dashboard" replace />;
  return children;
}

export default AdminRoute;
