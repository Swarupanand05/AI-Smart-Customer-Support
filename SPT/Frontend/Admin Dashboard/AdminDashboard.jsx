import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { ticketAPI, authAPI, getUser, removeToken, removeUser } from "../services/api";
import "./AdminDashboard.css";

function AdminDashboard() {
  const [active, setActive]         = useState("overview");
  const [tickets, setTickets]       = useState([]);
  const [loading, setLoading]       = useState(false);
  const [error, setError]           = useState("");
  const [successMsg, setSuccessMsg] = useState("");

  const navigate = useNavigate();
  const user = getUser();

  useEffect(() => {
    fetchTickets();
  }, []);

  const fetchTickets = async () => {
    setLoading(true);
    setError("");
    try {
      const data = await ticketAPI.getAll();
      setTickets(data);
    } catch (err) {
      setError("Failed to load tickets: " + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleStatusUpdate = async (id, status) => {
    try {
      await ticketAPI.updateStatus(id, status);
      setSuccessMsg(`Ticket marked as "${status}"`);
      fetchTickets();
      setTimeout(() => setSuccessMsg(""), 3000);
    } catch (err) {
      setError("Failed to update status: " + err.message);
    }
  };

  const handleLogout = async () => {
    try { await authAPI.logout(); } catch (_) {}
    removeToken();
    removeUser();
    navigate("/");
  };

  // ─── Stats ───────────────────────────────────────────────────────────────────
  const stats = {
    total:    tickets.length,
    open:     tickets.filter(t => t.status === "Open").length,
    resolved: tickets.filter(t => t.status === "Resolved").length,
    high:     tickets.filter(t => t.priority === "High").length,
  };

  // ─── Helpers ─────────────────────────────────────────────────────────────────
  const statusColor = (s) => ({ Open:"#f59e0b", Resolved:"#22c55e", Closed:"#6b7280" }[s] || "#3b82f6");
  const priorityColor = (p) => ({ High:"#ef4444", Medium:"#f59e0b", Low:"#22c55e" }[p] || "#94a3b8");

  // ─── Filter tickets by tab ────────────────────────────────────────────────
  const filtered = active === "all"      ? tickets
                 : active === "open"     ? tickets.filter(t => t.status === "Open")
                 : active === "resolved" ? tickets.filter(t => t.status === "Resolved")
                 : tickets;

  return (
    <div className="admin-layout">

      {/* ── Sidebar ─────────────────────────────────────────────────────── */}
      <div className="admin-sidebar">
        <div className="admin-logo">🛡️ Admin Portal</div>

        <div className="admin-user-info">
          <div className="admin-avatar">A</div>
          <div>
            <p className="admin-user-name">{user?.fullName || "Admin"}</p>
            <p className="admin-user-role">System Administrator</p>
          </div>
        </div>

        <nav className="admin-nav">
          <button
            className={`admin-nav-item ${active === "overview" ? "active" : ""}`}
            onClick={() => setActive("overview")}
          >
            📊 Overview
          </button>
          <button
            className={`admin-nav-item ${active === "all" ? "active" : ""}`}
            onClick={() => setActive("all")}
          >
            📋 All Tickets
            <span className="badge">{stats.total}</span>
          </button>
          <button
            className={`admin-nav-item ${active === "open" ? "active" : ""}`}
            onClick={() => setActive("open")}
          >
            🟡 Open
            <span className="badge badge-yellow">{stats.open}</span>
          </button>
          <button
            className={`admin-nav-item ${active === "resolved" ? "active" : ""}`}
            onClick={() => setActive("resolved")}
          >
            ✅ Resolved
            <span className="badge badge-green">{stats.resolved}</span>
          </button>
        </nav>

        <button className="admin-logout-btn" onClick={handleLogout}>
          🚪 Logout
        </button>
      </div>

      {/* ── Main Content ────────────────────────────────────────────────── */}
      <div className="admin-main">

        {/* Top Bar */}
        <div className="admin-topbar">
          <h1>
            {active === "overview" ? "📊 Dashboard Overview"
            : active === "open"    ? "🟡 Open Tickets"
            : active === "resolved"? "✅ Resolved Tickets"
            :                        "📋 All Tickets"}
          </h1>
          <button className="refresh-btn" onClick={fetchTickets}>🔄 Refresh</button>
        </div>

        {/* Alerts */}
        {error      && <div className="admin-alert error">{error}</div>}
        {successMsg && <div className="admin-alert success">✅ {successMsg}</div>}

        {/* ── Overview Stats ─────────────────────────────────────────────── */}
        {active === "overview" && (
          <>
            <div className="stats-grid">
              <div className="stat-box" onClick={() => setActive("all")}>
                <span className="stat-num">{stats.total}</span>
                <span className="stat-label">Total Tickets</span>
              </div>
              <div className="stat-box open" onClick={() => setActive("open")}>
                <span className="stat-num">{stats.open}</span>
                <span className="stat-label">Open</span>
              </div>
              <div className="stat-box resolved" onClick={() => setActive("resolved")}>
                <span className="stat-num">{stats.resolved}</span>
                <span className="stat-label">Resolved</span>
              </div>
              <div className="stat-box high">
                <span className="stat-num">{stats.high}</span>
                <span className="stat-label">High Priority</span>
              </div>
            </div>

            {/* Recent tickets preview */}
            <h3 className="section-subtitle">🕑 Recent Tickets</h3>
            {renderTickets(tickets.slice(0, 5), handleStatusUpdate, statusColor, priorityColor)}
          </>
        )}

        {/* ── Ticket List ────────────────────────────────────────────────── */}
        {active !== "overview" && (
          <>
            {loading && <p className="admin-loading">Loading tickets...</p>}
            {!loading && filtered.length === 0 && (
              <p className="admin-empty">No tickets in this category.</p>
            )}
            {renderTickets(filtered, handleStatusUpdate, statusColor, priorityColor)}
          </>
        )}
      </div>
    </div>
  );
}

// ─── Ticket Card Renderer ─────────────────────────────────────────────────────
function renderTickets(tickets, onUpdate, statusColor, priorityColor) {
  return (
    <div className="admin-ticket-list">
      {tickets.map((ticket, i) => (
        <div key={i} className="admin-ticket-card">
          <div className="admin-ticket-top">
            <div className="admin-ticket-meta">
              <span className="admin-user-tag">👤 {ticket.userEmail}</span>
              <span className="admin-category-tag">📁 {ticket.category}</span>
              <span
                className="admin-priority-tag"
                style={{ background: priorityColor(ticket.priority) }}
              >
                {ticket.priority}
              </span>
            </div>
            <span
              className="admin-status-tag"
              style={{ color: statusColor(ticket.status) }}
            >
              ● {ticket.status}
            </span>
          </div>

          <p className="admin-ticket-msg">{ticket.message}</p>

          <div className="admin-ticket-footer">
            <span className="admin-ticket-date">
              🗓 {new Date(ticket.createdAt).toLocaleString()}
            </span>

            {/* Action buttons */}
            <div className="admin-actions">
              {ticket.status !== "Resolved" && (
                <button
                  className="action-btn resolve"
                  onClick={() => onUpdate(ticket.id?.toString() || ticket._id?.toString(), "Resolved")}
                >
                  ✅ Mark Resolved
                </button>
              )}
              {ticket.status !== "Closed" && (
                <button
                  className="action-btn close"
                  onClick={() => onUpdate(ticket.id?.toString() || ticket._id?.toString(), "Closed")}
                >
                  🔒 Close
                </button>
              )}
              {ticket.status !== "Open" && (
                <button
                  className="action-btn reopen"
                  onClick={() => onUpdate(ticket.id?.toString() || ticket._id?.toString(), "Open")}
                >
                  🔄 Reopen
                </button>
              )}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

export default AdminDashboard;
