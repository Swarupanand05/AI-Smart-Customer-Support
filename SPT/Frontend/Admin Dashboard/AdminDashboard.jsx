import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { ticketAPI, authAPI, getUser, removeToken, removeUser } from "../services/api";
import "./AdminDashboard.css";

const RESOLUTION_TEMPLATES = [
  {
    label: "💳 Full Refund (3-5 Days)",
    text: "We have verified your issue and initiated a 100% full refund to your original payment method. It will reflect in your account within 3-5 business days. Your transaction reference has been logged."
  },
  {
    label: "📦 Replacement Dispatched",
    text: "We apologize for the defective or damaged item received. A replacement order has been dispatched via expedited courier. Tracking updates will follow shortly via SMS and email."
  },
  {
    label: "🚚 Logistics Escalated",
    text: "We have contacted our courier partner's local hub manager regarding your delayed delivery. The shipment has been prioritized for delivery today before 8:00 PM."
  },
  {
    label: "🛡️ Payment Gateway Settled",
    text: "Our billing team verified your payment gateway transaction. As the payment failed at checkout, the bank's automated reversal will return the funds to your account within 24-48 hours."
  },
  {
    label: "✅ Issue Resolved",
    text: "Thank you for contacting customer support. We have verified and resolved your query. Please let us know if you need any additional assistance!"
  }
];

function AdminDashboard() {
  const [active, setActive]         = useState("overview");
  const [tickets, setTickets]       = useState([]);
  const [loading, setLoading]       = useState(false);
  const [error, setError]           = useState("");
  const [successMsg, setSuccessMsg] = useState("");

  // Filter & sort state
  const [search,      setSearch]      = useState("");
  const [filterCat,   setFilterCat]   = useState("All");
  const [filterPri,   setFilterPri]   = useState("All");
  const [filterSent,  setFilterSent]  = useState("All");
  const [sortOrder,   setSortOrder]   = useState("newest");

  // Per-ticket inline reply panel state: { [ticketId]: { open: bool, text: string, status: string } }
  const [replyPanels, setReplyPanels] = useState({});

  // Ticket Investigation Drawer / Modal State
  const [inspectTicket, setInspectTicket]     = useState(null);
  const [modalReplyText, setModalReplyText]   = useState("");
  const [modalStatus, setModalStatus]         = useState("Resolved");
  const [modalPriority, setModalPriority]     = useState("Medium");

  const navigate = useNavigate();
  const user = getUser();

  useEffect(() => {
    fetchTickets();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchTickets = async () => {
    setLoading(true);
    setError("");
    try {
      const data = await ticketAPI.getAll();
      setTickets(data);
      // If a ticket is currently open in modal, refresh its data
      if (inspectTicket) {
        const updated = data.find(t => (t.id || t._id) === (inspectTicket.id || inspectTicket._id));
        if (updated) setInspectTicket(updated);
      }
    } catch (err) {
      setError("Failed to load tickets: " + err.message);
    } finally {
      setLoading(false);
    }
  };

  // Open inspection drawer for a ticket
  const openInspectModal = (ticket) => {
    setInspectTicket(ticket);
    setModalReplyText("");
    setModalStatus(ticket.status === "Closed" ? "Closed" : "Resolved");
    setModalPriority(ticket.priority || "Medium");
  };

  const closeInspectModal = () => {
    setInspectTicket(null);
    setModalReplyText("");
  };

  // Simple status-only update (quick action buttons)
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

  // Admin reply + optional status update
  const handleReplySubmit = async (ticketId, currentStatus) => {
    const panel = replyPanels[ticketId] || {};
    const replyText = panel.text || "";
    const newStatus = panel.status || currentStatus;

    if (!replyText.trim()) {
      setError("Reply cannot be empty.");
      setTimeout(() => setError(""), 3000);
      return;
    }

    try {
      await ticketAPI.updateStatus(ticketId, newStatus, replyText.trim());
      setSuccessMsg(`Reply sent & ticket marked as "${newStatus}"`);
      setReplyPanels(prev => ({ ...prev, [ticketId]: { open: false, text: "", status: "" } }));
      fetchTickets();
      setTimeout(() => setSuccessMsg(""), 3000);
    } catch (err) {
      setError("Failed to send reply: " + err.message);
    }
  };

  // Handle resolution submission from inside the Inspection Modal
  const handleModalSubmit = async () => {
    if (!inspectTicket) return;
    const ticketId = inspectTicket.id?.toString() || inspectTicket._id?.toString();
    const text = modalReplyText.trim();

    if (!text) {
      setError("Resolution reply cannot be empty.");
      setTimeout(() => setError(""), 3000);
      return;
    }

    try {
      await ticketAPI.updateStatus(ticketId, modalStatus, text);
      if (modalPriority && modalPriority !== inspectTicket.priority) {
        await ticketAPI.updatePriority(ticketId, modalPriority);
      }
      setSuccessMsg(`Resolution submitted & ticket updated to "${modalStatus}"`);
      closeInspectModal();
      fetchTickets();
      setTimeout(() => setSuccessMsg(""), 3000);
    } catch (err) {
      setError("Failed to submit resolution: " + err.message);
    }
  };

  const toggleReplyPanel = (ticketId, currentStatus) => {
    setReplyPanels(prev => {
      const existing = prev[ticketId] || {};
      return {
        ...prev,
        [ticketId]: {
          open: !existing.open,
          text: existing.text || "",
          status: existing.status || currentStatus,
        },
      };
    });
  };

  const updatePanelField = (ticketId, field, value) => {
    setReplyPanels(prev => ({
      ...prev,
      [ticketId]: { ...(prev[ticketId] || {}), [field]: value },
    }));
  };

  const handleLogout = async () => {
    try { await authAPI.logout(); } catch (_) {}
    removeToken();
    removeUser();
    navigate("/");
  };

  // ─── Stats ────────────────────────────────────────────────────────────────────
  const stats = {
    total:    tickets.length,
    open:     tickets.filter(t => t.status === "Open").length,
    resolved: tickets.filter(t => t.status === "Resolved").length,
    high:     tickets.filter(t => t.priority === "High").length,
  };

  // ─── Helpers ──────────────────────────────────────────────────────────────────
  const statusColor   = (s) => ({ Open:"#f59e0b", Resolved:"#10b981", Closed:"#6b7280" }[s] || "#6366f1");
  const priorityColor = (p) => ({ High:"#ef4444", Medium:"#f59e0b",   Low:"#10b981"   }[p] || "#94a3b8");

  // ─── Filter + Sort pipeline ───────────────────────────────────────────────────
  const tabFiltered = active === "all"      ? tickets
                    : active === "open"     ? tickets.filter(t => t.status === "Open")
                    : active === "resolved" ? tickets.filter(t => t.status === "Resolved")
                    : tickets;

  const categories = ["All", ...new Set(tickets.map(t => t.category).filter(Boolean))];

  const filtered = (() => {
    let result = tabFiltered
      .filter(t => filterCat  === "All" || t.category === filterCat)
      .filter(t => filterPri  === "All" || t.priority === filterPri)
      .filter(t => filterSent === "All" || (t.sentiment || "Neutral") === filterSent)
      .filter(t => !search
        || t.message?.toLowerCase().includes(search.toLowerCase())
        || t.userEmail?.toLowerCase().includes(search.toLowerCase()));

    const PRIORITY_ORDER = ["High", "Medium", "Low"];
    if      (sortOrder === "oldest")  result = [...result].sort((a, b) => new Date(a.createdAt) - new Date(b.createdAt));
    else if (sortOrder === "newest")  result = [...result].sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
    else if (sortOrder === "priHigh") result = [...result].sort((a, b) => PRIORITY_ORDER.indexOf(a.priority) - PRIORITY_ORDER.indexOf(b.priority));
    else if (sortOrder === "priLow")  result = [...result].sort((a, b) => PRIORITY_ORDER.indexOf(b.priority) - PRIORITY_ORDER.indexOf(a.priority));

    return result;
  })();

  const activeFilterCount = [filterCat, filterPri, filterSent].filter(f => f !== "All").length + (search ? 1 : 0);
  const clearAllFilters = () => { setSearch(""); setFilterCat("All"); setFilterPri("All"); setFilterSent("All"); setSortOrder("newest"); };

  const pageTitle = {
    overview: "Dashboard Overview",
    all:      "All Tickets",
    open:     "Open Tickets",
    resolved: "Resolved Tickets",
  }[active];

  return (
    <div className="admin-layout">
      {/* ── Sidebar ──────────────────────────────────────────────────────── */}
      <aside className="admin-sidebar">
        <div className="admin-logo">
          <div className="admin-logo-icon">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/>
            </svg>
          </div>
          <div className="admin-logo-text">
            <span>Admin Center</span>
            <span className="admin-logo-sub">Enterprise Support</span>
          </div>
        </div>

        <nav className="admin-nav">
          <button
            className={`admin-nav-item ${active === "overview" ? "active" : ""}`}
            onClick={() => setActive("overview")}
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/>
              <rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/>
            </svg>
            Overview
          </button>

          <button
            className={`admin-nav-item ${active === "all" ? "active" : ""}`}
            onClick={() => setActive("all")}
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
              <polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/>
              <line x1="16" y1="17" x2="8" y2="17"/><polyline points="10 9 9 9 8 9"/>
            </svg>
            All Tickets
            <span className="nav-badge">{stats.total}</span>
          </button>

          <button
            className={`admin-nav-item ${active === "open" ? "active" : ""}`}
            onClick={() => setActive("open")}
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
            </svg>
            Open Tickets
            {stats.open > 0 && <span className="nav-badge open">{stats.open}</span>}
          </button>

          <button
            className={`admin-nav-item ${active === "resolved" ? "active" : ""}`}
            onClick={() => setActive("resolved")}
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/>
            </svg>
            Resolved
            <span className="nav-badge resolved">{stats.resolved}</span>
          </button>
        </nav>

        <div className="admin-sidebar-footer">
          <div className="admin-user-info">
            <div className="admin-avatar">
              {(user?.fullName || user?.email || "A").charAt(0).toUpperCase()}
            </div>
            <div className="admin-user-meta">
              <span className="admin-user-name">{user?.fullName || "Support Agent"}</span>
              <span className="admin-user-email">{user?.email || "admin@portal.com"}</span>
            </div>
          </div>
          <button className="admin-logout-btn" onClick={handleLogout} title="Log out">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
              <polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/>
            </svg>
          </button>
        </div>
      </aside>

      {/* ── Main Area ────────────────────────────────────────────────────── */}
      <div className="admin-main">
        <header className="admin-header">
          <div className="admin-header-title">
            <h2>{pageTitle}</h2>
            <span className="admin-header-sub">Diagnose customer issues and manage resolution workflows</span>
          </div>

          <div className="admin-header-actions">
            <button className="admin-refresh-btn" onClick={fetchTickets} disabled={loading}>
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/>
                <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"/>
              </svg>
              {loading ? "Refreshing..." : "Refresh"}
            </button>
          </div>
        </header>

        {error && (
          <div className="admin-alert error">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
            </svg>
            {error}
            <button onClick={() => setError("")}>✕</button>
          </div>
        )}

        {successMsg && (
          <div className="admin-alert success">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/>
            </svg>
            {successMsg}
          </div>
        )}

        <div className="admin-content">
          {/* ── Stats Row ─────────────────────────────────────────────────── */}
          <div className="admin-stats-row">
            <div className="stat-card" onClick={() => setActive("all")}>
              <div className="stat-icon-wrap blue">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                  <polyline points="14 2 14 8 20 8"/>
                </svg>
              </div>
              <div className="stat-body">
                <span className="stat-num">{stats.total}</span>
                <span className="stat-label">Total Tickets</span>
              </div>
            </div>

            <div className="stat-card" onClick={() => setActive("open")}>
              <div className="stat-icon-wrap amber">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
                </svg>
              </div>
              <div className="stat-body">
                <span className="stat-num">{stats.open}</span>
                <span className="stat-label">Open / Pending</span>
              </div>
            </div>

            <div className="stat-card" onClick={() => setActive("resolved")}>
              <div className="stat-icon-wrap green">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/>
                </svg>
              </div>
              <div className="stat-body">
                <span className="stat-num">{stats.resolved}</span>
                <span className="stat-label">Resolved</span>
              </div>
            </div>

            <div className="stat-card">
              <div className="stat-icon-wrap red">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
                </svg>
              </div>
              <div className="stat-body">
                <span className="stat-num">{stats.high}</span>
                <span className="stat-label">High Priority</span>
              </div>
            </div>
          </div>

          {/* ── Overview: Recent Tickets ──────────────────────────────────── */}
          {active === "overview" && (
            <>
              <p className="section-label">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                  <circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>
                </svg>
                Recent Tickets (Click to inspect customer problem)
              </p>
              {renderTickets(
                tickets.slice(0, 6),
                handleStatusUpdate,
                handleReplySubmit,
                toggleReplyPanel,
                updatePanelField,
                replyPanels,
                statusColor,
                priorityColor,
                openInspectModal
              )}
            </>
          )}

          {/* ── Ticket List with Filter Bar ──────────────────────────────── */}
          {active !== "overview" && (
            <>
              {/* ── Filter Bar ────────────────────────────────────────── */}
              <div className="admin-filter-bar">
                {/* Search */}
                <input
                  className="admin-filter-search"
                  type="text"
                  placeholder="🔍 Search message or email..."
                  value={search}
                  onChange={e => setSearch(e.target.value)}
                />
                {/* Category */}
                <select className="admin-filter-select" value={filterCat} onChange={e => setFilterCat(e.target.value)}>
                  {categories.map(c => <option key={c}>{c}</option>)}
                </select>
                {/* Priority */}
                <select className="admin-filter-select" value={filterPri} onChange={e => setFilterPri(e.target.value)}>
                  {["All", "High", "Medium", "Low"].map(p => <option key={p}>{p}</option>)}
                </select>
                {/* Sentiment */}
                <select className="admin-filter-select" value={filterSent} onChange={e => setFilterSent(e.target.value)}>
                  <option value="All">😐 All Sentiment</option>
                  <option value="Negative">😠 Negative</option>
                  <option value="Neutral">😐 Neutral</option>
                  <option value="Positive">😊 Positive</option>
                </select>
                {/* Sort Order */}
                <select className="admin-filter-select admin-filter-sort" value={sortOrder} onChange={e => setSortOrder(e.target.value)}>
                  <option value="newest">↓ Newest First</option>
                  <option value="oldest">↑ Oldest First</option>
                  <option value="priHigh">🔴 High Priority First</option>
                  <option value="priLow">🟢 Low Priority First</option>
                </select>
                {/* Count */}
                <span className="admin-filter-count">
                  {filtered.length} ticket{filtered.length !== 1 ? "s" : ""}
                </span>
                {/* Clear */}
                {activeFilterCount > 0 && (
                  <button className="admin-filter-clear" onClick={clearAllFilters}>
                    ✕ Clear ({activeFilterCount})
                  </button>
                )}
              </div>

              {loading && <p className="admin-loading">Loading tickets...</p>}
              {!loading && filtered.length === 0 && (
                <p className="admin-empty">No tickets match your filters.</p>
              )}
              {renderTickets(
                filtered,
                handleStatusUpdate,
                handleReplySubmit,
                toggleReplyPanel,
                updatePanelField,
                replyPanels,
                statusColor,
                priorityColor,
                openInspectModal
              )}
            </>
          )}
        </div>
      </div>

      {/* ════════════════════════════════════════════════════════════════════════
         TICKET INVESTIGATION & PROBLEM DIAGNOSIS DRAWER / MODAL
         ════════════════════════════════════════════════════════════════════════ */}
      {inspectTicket && (
        <div className="admin-modal-overlay" onClick={closeInspectModal}>
          <div className="admin-modal-content" onClick={e => e.stopPropagation()}>
            {/* Modal Header */}
            <div className="admin-modal-header">
              <div className="admin-modal-header-info">
                <div className="admin-modal-badge-row">
                  <span className="modal-ticket-id">#{inspectTicket.id || inspectTicket._id}</span>
                  <span className="admin-status-tag" style={{ color: statusColor(inspectTicket.status), background: `${statusColor(inspectTicket.status)}22` }}>
                    ● {inspectTicket.status}
                  </span>
                  <span className="admin-priority-tag" style={{ background: priorityColor(inspectTicket.priority) }}>
                    {inspectTicket.priority} Priority
                  </span>
                  {inspectTicket.sentiment && (
                    <span className="admin-sentiment-tag">
                      {inspectTicket.sentiment === "Negative" ? "😠" : inspectTicket.sentiment === "Positive" ? "😊" : "😐"} {inspectTicket.sentiment}
                    </span>
                  )}
                </div>
                <div className="modal-customer-info">
                  <span className="modal-customer-email">👤 {inspectTicket.userEmail}</span>
                  <span className="modal-created-time">🕒 {new Date(inspectTicket.createdAt).toLocaleString()}</span>
                </div>
              </div>
              <button className="admin-modal-close" onClick={closeInspectModal} title="Close inspection">✕</button>
            </div>

            {/* Modal Body */}
            <div className="admin-modal-body">
              {/* Highlighted Customer Problem Statement */}
              <div className="modal-problem-section">
                <div className="modal-section-title">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#ef4444" strokeWidth="2.2" strokeLinecap="round">
                    <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
                  </svg>
                  <h4>Customer's Actual Problem Statement</h4>
                </div>
                <div className="modal-problem-box">
                  <p>{inspectTicket.message}</p>
                </div>
              </div>

              {/* AI Diagnostics Banner */}
              <div className="modal-ai-diagnosis-box">
                <div className="ai-diag-item">
                  <span className="ai-diag-label">Category:</span>
                  <span className="ai-diag-val">📁 {inspectTicket.category}</span>
                </div>
                <div className="ai-diag-item">
                  <span className="ai-diag-label">Detected Sentiment:</span>
                  <span className="ai-diag-val">
                    {inspectTicket.sentiment === "Negative" ? "😠 Negative (High Escalation Risk)"
                     : inspectTicket.sentiment === "Positive" ? "😊 Positive (Pleased Customer)"
                     : "😐 Neutral (Information Request)"}
                  </span>
                </div>
                <div className="ai-diag-item">
                  <span className="ai-diag-label">Assigned Priority:</span>
                  <span className="ai-diag-val" style={{ color: priorityColor(inspectTicket.priority) }}>
                    {inspectTicket.priority} Priority
                  </span>
                </div>
              </div>

              {/* Chronological Chat & Interaction Thread */}
              <div className="modal-thread-section">
                <div className="modal-section-title">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#14b8a6" strokeWidth="2" strokeLinecap="round">
                    <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
                  </svg>
                  <h4>Full Conversation Transcript & Timeline</h4>
                </div>
                <div className="modal-thread-scroll">
                  {renderModalThread(inspectTicket)}
                </div>
              </div>

              {/* Resolution Center */}
              <div className="modal-resolution-section">
                <div className="modal-section-title">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#0ea5e9" strokeWidth="2" strokeLinecap="round">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                  </svg>
                  <h4>Official Agent Resolution Response</h4>
                </div>

                {/* 1-Click Fast Templates */}
                <div className="modal-template-chips">
                  <span className="template-label">1-Click Templates:</span>
                  {RESOLUTION_TEMPLATES.map((tmpl, idx) => (
                    <button
                      key={idx}
                      type="button"
                      className="modal-tmpl-btn"
                      onClick={() => setModalReplyText(tmpl.text)}
                    >
                      {tmpl.label}
                    </button>
                  ))}
                </div>

                <textarea
                  className="modal-reply-textarea"
                  placeholder="Type your official resolution response to the customer..."
                  value={modalReplyText}
                  onChange={e => setModalReplyText(e.target.value)}
                  rows={4}
                />

                <div className="modal-resolution-actions">
                  <div className="modal-action-selectors">
                    <div className="modal-sel-wrap">
                      <label>Set Status:</label>
                      <select
                        value={modalStatus}
                        onChange={e => setModalStatus(e.target.value)}
                        className="modal-select"
                      >
                        <option value="Open">Open</option>
                        <option value="Resolved">Resolved</option>
                        <option value="Closed">Closed</option>
                      </select>
                    </div>

                    <div className="modal-sel-wrap">
                      <label>Priority:</label>
                      <select
                        value={modalPriority}
                        onChange={e => setModalPriority(e.target.value)}
                        className="modal-select"
                      >
                        <option value="High">High</option>
                        <option value="Medium">Medium</option>
                        <option value="Low">Low</option>
                      </select>
                    </div>
                  </div>

                  <button
                    className="modal-send-btn"
                    onClick={handleModalSubmit}
                  >
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round">
                      <line x1="22" y1="2" x2="11" y2="13"/>
                      <polygon points="22 2 15 22 11 13 2 9 22 2"/>
                    </svg>
                    Submit Resolution
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Modal Thread Renderer ──────────────────────────────────────────────────
function renderModalThread(ticket) {
  const hasThread = ticket.messages && ticket.messages.length > 0;
  const hasLegacy = ticket.adminReply && !hasThread;

  if (!hasThread && !hasLegacy) {
    return <p className="modal-thread-empty">No conversation messages recorded.</p>;
  }

  return (
    <div className="modal-thread-list">
      {hasLegacy && (
        <div className="modal-thread-msg admin">
          <div className="modal-thread-msg-header">
            <span className="modal-thread-sender admin-sender">🛠️ Support Agent</span>
          </div>
          <p className="modal-thread-msg-text">{ticket.adminReply}</p>
        </div>
      )}
      {hasThread && ticket.messages.map((msg, idx) => (
        <div key={idx} className={`modal-thread-msg ${msg.sender}`}>
          <div className="modal-thread-msg-header">
            {msg.sender === "admin" ? (
              <span className="modal-thread-sender admin-sender">🛠️ Support Agent / AI Resolution</span>
            ) : (
              <span className="modal-thread-sender user-sender">👤 {ticket.userEmail}</span>
            )}
            <span className="modal-thread-msg-time">
              {new Date(msg.sentAt).toLocaleString([], {
                month: "short", day: "numeric",
                hour: "2-digit", minute: "2-digit"
              })}
            </span>
          </div>
          <p className="modal-thread-msg-text">{msg.text}</p>
        </div>
      ))}
    </div>
  );
}



// ─── Ticket Card Renderer ─────────────────────────────────────────────────────
function renderTickets(tickets, onUpdate, onReplySubmit, toggleReplyPanel, updatePanelField, replyPanels, statusColor, priorityColor, onInspect) {
  return (
    <div className="admin-ticket-list">
      {tickets.map((ticket, i) => {
        const ticketId = ticket.id?.toString() || ticket._id?.toString();
        const panel    = replyPanels[ticketId] || {};

        return (
          <div
            key={i}
            className={`admin-ticket-card ${panel.open ? "reply-open" : ""}`}
            onClick={() => onInspect(ticket)}
          >
            <div className="admin-ticket-top">
              <div className="admin-ticket-meta">
                <span className="admin-user-tag">&#128100; {ticket.userEmail}</span>
                <span className="admin-category-tag">&#128193; {ticket.category}</span>
                <span className="admin-priority-tag" style={{ background: priorityColor(ticket.priority) }}>
                  {ticket.priority}
                </span>
                {ticket.sentiment && (
                  <span className="admin-sentiment-tag" style={{
                    color: ticket.sentiment === "Negative" ? "#ef4444"
                         : ticket.sentiment === "Positive" ? "#10b981"
                         : "#94a3b8"
                  }}>
                    {ticket.sentiment === "Negative" ? "😠"
                   : ticket.sentiment === "Positive" ? "😊" : "😐"} {ticket.sentiment}
                  </span>
                )}
              </div>
              <span className="admin-status-tag" style={{ color: statusColor(ticket.status) }}>
                &#9679; {ticket.status}
              </span>
            </div>

            {/* Customer Original Problem Statement Preview */}
            <div className="admin-ticket-problem-preview">
              <span className="problem-preview-label">Customer Problem:</span>
              <p className="admin-ticket-msg">{ticket.message}</p>
            </div>

            {ticket.messages && ticket.messages.length > 0 && (
              <div className="admin-thread-preview-pill">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
                </svg>
                <span>{ticket.messages.length} message{ticket.messages.length > 1 ? "s" : ""} in thread</span>
                {ticket.adminReply && <span className="thread-replied-tag">✓ Agent replied</span>}
              </div>
            )}

            <div className="admin-ticket-footer" onClick={e => e.stopPropagation()}>
              <span className="admin-ticket-date">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
                  <line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/>
                  <line x1="3" y1="10" x2="21" y2="10"/>
                </svg>
                {new Date(ticket.createdAt).toLocaleString()}
              </span>

              <div className="admin-actions">
                <button
                  className="action-btn inspect-btn"
                  onClick={() => onInspect(ticket)}
                  title="Inspect customer problem and conversation details"
                >
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                    <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
                  </svg>
                  Inspect
                </button>
                <button
                  className={`action-btn reply-btn ${panel.open ? "reply-active" : ""}`}
                  onClick={() => toggleReplyPanel(ticketId, ticket.status)}
                >
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
                  </svg>
                  {panel.open ? "Cancel" : "Quick Reply"}
                </button>
                {ticket.status !== "Resolved" && (
                  <button className="action-btn resolve" onClick={() => onUpdate(ticketId, "Resolved")}>
                    &#10003; Resolve
                  </button>
                )}
                {ticket.status !== "Closed" && (
                  <button className="action-btn close" onClick={() => onUpdate(ticketId, "Closed")}>
                    Close
                  </button>
                )}
                {ticket.status !== "Open" && (
                  <button className="action-btn reopen" onClick={() => onUpdate(ticketId, "Open")}>
                    Reopen
                  </button>
                )}
              </div>
            </div>

            {panel.open && (
              <div className="reply-panel" onClick={e => e.stopPropagation()}>
                <div className="reply-panel-header">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
                  </svg>
                  Write Quick Reply
                </div>
                <textarea
                  className="reply-textarea"
                  placeholder="Type your reply to the customer..."
                  value={panel.text || ""}
                  onChange={e => updatePanelField(ticketId, "text", e.target.value)}
                  rows={3}
                />
                <div className="reply-panel-footer">
                  <div className="reply-status-select-wrap">
                    <label className="reply-status-label">Set Status:</label>
                    <select
                      className="reply-status-select"
                      value={panel.status || ticket.status}
                      onChange={e => updatePanelField(ticketId, "status", e.target.value)}
                    >
                      <option value="Open">Open</option>
                      <option value="Resolved">Resolved</option>
                      <option value="Closed">Closed</option>
                    </select>
                  </div>
                  <button className="reply-send-btn" onClick={() => onReplySubmit(ticketId, ticket.status)}>
                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                      <line x1="22" y1="2" x2="11" y2="13"/>
                      <polygon points="22 2 15 22 11 13 2 9 22 2"/>
                    </svg>
                    Send Reply
                  </button>
                </div>
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}

export default AdminDashboard;
