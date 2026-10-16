import React from "react";
import { BrowserRouter as Router, Routes, Route, Navigate } from "react-router-dom";
import { isAuthenticated, getUser } from "./services/api";

import Login from "./components/Login";
import Register from "./components/Register";
import Dashboard from "./components/Dashboard";
import AdminDashboard from "./components/AdminDashboard";
import ProtectedRoute from "./components/ProtectedRoute";
import AdminRoute from "./components/AdminRoute";

// ─── Role-aware root redirect ────────────────────────────────────────────────
function RootRedirect() {
  if (!isAuthenticated()) return <Login />;
  const user = getUser();
  return user?.role === "Admin"
    ? <Navigate to="/admin" replace />
    : <Navigate to="/dashboard" replace />;
}

function App() {
  return (
    <Router>
      <Routes>
        {/* Root: show login or redirect based on role */}
        <Route path="/" element={<RootRedirect />} />

        {/* Public */}
        <Route path="/register" element={<Register />} />

        {/* User portal — requires any valid JWT */}
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <Dashboard />
            </ProtectedRoute>
          }
        />

        {/* Admin portal — requires Admin role */}
        <Route
          path="/admin"
          element={
            <AdminRoute>
              <AdminDashboard />
            </AdminRoute>
          }
        />
      </Routes>
    </Router>
  );
}

export default App;