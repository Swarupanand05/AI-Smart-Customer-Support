import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { ticketAPI, authAPI, getUser, removeToken, removeUser } from "../services/api";
import Chatbot from "./Chatbot";
import ChatSidebar from "./ChatSidebar";
import "./Dashboard.css";

function Dashboard() {
  const [active, setActive]               = useState("home");
  const [tickets, setTickets]             = useState([]);
  const [ticketsLoading, setTicketsLoading] = useState(false);
  const [ticketsError, setTicketsError]   = useState("");
  const [ticketsSuccess, setTicketsSuccess] = useState("");
  const [activeSessionId, setActiveSessionId] = useState(null);
  const [newChatTrigger, setNewChatTrigger]   = useState(0);

  const handleNewChat = () => {
    setActiveSessionId(null);
    setNewChatTrigger(prev => prev + 1);
  };

  // Per-ticket reply panel state: { [ticketId]: { open: bool, text: string } }
  const [replyPanels, setReplyPanels] = useState({});

  const navigate = useNavigate();
  const user = getUser();

  // Fetch tickets when History tab is opened
  useEffect(() => {
    if (active === "history") {
      fetchTickets();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [active]);

  const fetchTickets = async () => {
    setTicketsLoading(true);
    setTicketsError("");
    try {
      const data = await ticketAPI.getAll();
      const currentEmail = user?.email?.toLowerCase();
      setTickets(data.filter((t) => !currentEmail || t.userEmail?.toLowerCase() === currentEmail));
    } catch (err) {
      setTicketsError("Failed to load tickets. " + err.message);
    } finally {
      setTicketsLoading(false);
    }
  };



  const toggleReplyPanel = (ticketId) => {
    setReplyPanels(prev => ({
      ...prev,
      [ticketId]: {
        open: !prev[ticketId]?.open,
        text: prev[ticketId]?.text || "",
      },
    }));
  };

  const updateReplyText = (ticketId, text) => {
    setReplyPanels(prev => ({
      ...prev,
      [ticketId]: { ...(prev[ticketId] || {}), text },
    }));
  };

  const handleUserReply = async (ticketId) => {
    const panel = replyPanels[ticketId] || {};
    const text = (panel.text || "").trim();

    if (!text) {
      setTicketsError("Reply cannot be empty.");
      setTimeout(() => setTicketsError(""), 3000);
      return;
    }

    try {
      await ticketAPI.addUserReply(ticketId, text);
      setTicketsSuccess("Your reply has been sent!");
      setReplyPanels(prev => ({
        ...prev,
        [ticketId]: { open: false, text: "" },
      }));
      fetchTickets();
      setTimeout(() => setTicketsSuccess(""), 3000);
    } catch (err) {
      setTicketsError("Failed to send reply: " + err.message);
    }
  };

  const handleLogout = async () => {
    try { await authAPI.logout(); } catch (_) { }
    removeToken();
    removeUser();
    navigate("/");
  };

  const statusColor = (status) => {
    if (status === "Open")     return "#f59e0b";
    if (status === "Resolved") return "#22c55e";
    if (status === "Closed")   return "#64748b";
    return "#14b8a6";
  };

  const priorityBg = (priority) => {
    if (priority === "High")   return "#7f1d1d";
    if (priority === "Medium") return "#78350f";
    return "#14532d";
  };

  // ── Render conversation thread ────────────────────────────────────────────
  const renderThread = (ticket) => {
    const hasThread   = ticket.messages && ticket.messages.length > 0;
    const hasLegacy   = ticket.adminReply && !hasThread;

    return (
      <div className="ticket-thread">
        {hasLegacy && (
          <div className="thread-msg admin">
            <div className="thread-msg-header">
              <span className="thread-sender-label admin-label">&#128736; Support Agent</span>
            </div>
            <p className="thread-msg-text">{ticket.adminReply}</p>
          </div>
        )}

        {hasThread && ticket.messages.map((msg, idx) => (
          <div key={idx} className={`thread-msg ${msg.sender}`}>
            <div className="thread-msg-header">
              {msg.sender === "admin" ? (
                <span className="thread-sender-label admin-label">&#128736; Support Agent</span>
              ) : (
                <span className="thread-sender-label user-label">&#128100; You</span>
              )}
              <span className="thread-msg-time">
                {new Date(msg.sentAt).toLocaleString([], {
                  month: "short", day: "numeric",
                  hour: "2-digit", minute: "2-digit"
                })}
              </span>
            </div>
            <p className="thread-msg-text">{msg.text}</p>
          </div>
        ))}
      </div>
    );
  };

  const renderContent = () => {
    switch (active) {
      case "home":
        return (
          <div className="dash-welcome">
            {/* Welcome Banner */}
            <div className="welcome-banner">
              <div className="welcome-banner-text">
                <h2>Welcome back, {user?.fullName || "User"} 👋</h2>
                <p>Here's a quick overview of your support workspace.</p>
              </div>
              <div className="welcome-badge">
                <span className="welcome-badge-dot" />
                AI Online
              </div>
            </div>

            <div className="dash-stats">
              <div className="stat-card" onClick={() => setActive("chat")}>
                <div className="stat-icon">
                  <svg viewBox="0 0 24 24" fill="none" stroke="#14b8a6" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
                  </svg>
                </div>
                <h4>AI Chatbot</h4>
                <p>Instant AI-powered support</p>
              </div>

              <div className="stat-card" onClick={() => setActive("history")}>
                <div className="stat-icon">
                  <svg viewBox="0 0 24 24" fill="none" stroke="#0ea5e9" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M15 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V9z"/>
                    <polyline points="15 3 15 9 21 9"/>
                  </svg>
                </div>
                <h4>My Tickets</h4>
                <p>View your ticket history</p>
              </div>
            </div>
          </div>
        );

      case "chat":
        return (
          <div className="chat-section-layout">
            <ChatSidebar
              activeSessionId={activeSessionId}
              onSelectSession={id => setActiveSessionId(id)}
              onNewChat={handleNewChat}
            />
            <div className="chat-main-area">
              <Chatbot
                key={newChatTrigger}
                activeSessionId={activeSessionId}
                onSessionCreated={id => setActiveSessionId(id)}
                onNewChat={handleNewChat}
              />
            </div>
          </div>
        );

      case "history":
        return (
          <div className="tickets-section">
            <div className="section-header">
              <h2>My Ticket History</h2>
              <button className="refresh-btn" onClick={fetchTickets}>
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round">
                  <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4.04"/>
                </svg>
                Refresh
              </button>
            </div>

            {ticketsLoading && <p className="loading-text">Loading tickets…</p>}
            {ticketsError   && <p className="error-msg">&#9888; {ticketsError}</p>}
            {ticketsSuccess && <p className="success-msg">&#10003; {ticketsSuccess}</p>}

            {!ticketsLoading && !ticketsError && tickets.length === 0 && (
              <p className="empty-text">No tickets found. Start a chat to create one!</p>
            )}

            {tickets.map((ticket, i) => {
              const ticketId = ticket.id?.toString() || ticket._id?.toString();
              const panel    = replyPanels[ticketId] || {};
              const hasConversation =
                (ticket.messages && ticket.messages.length > 0) || ticket.adminReply;

              return (
                <div key={i} className="ticket-card">
                  {/* ── Header ──────────────────────────────────────────── */}
                  <div className="ticket-header">
                    <span
                      className="ticket-priority"
                      style={{ background: priorityBg(ticket.priority) }}
                    >
                      {ticket.priority} Priority
                    </span>
                    <span
                      className="ticket-status"
                      style={{ color: statusColor(ticket.status) }}
                    >
                      &#9679; {ticket.status}
                    </span>
                  </div>

                  {/* ── Original user message ────────────────────────── */}
                  <div className="ticket-original-msg">
                    <p className="ticket-message">{ticket.message}</p>
                  </div>

                  {/* ── Conversation thread ──────────────────────────── */}
                  {hasConversation && renderThread(ticket)}

                  {/* ── User Reply Panel ─────────────────────────────── */}
                  {hasConversation && ticket.status !== "Closed" && (
                    <div className="user-reply-area">
                      {!panel.open ? (
                        <button
                          className="user-reply-toggle-btn"
                          onClick={() => toggleReplyPanel(ticketId)}
                        >
                          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                            <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
                          </svg>
                          Reply to Support
                        </button>
                      ) : (
                        <div className="user-reply-panel">
                          <div className="user-reply-panel-header">
                            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                              <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
                            </svg>
                            Your Reply
                          </div>
                          <textarea
                            className="user-reply-textarea"
                            placeholder="Type your follow-up question or message..."
                            value={panel.text || ""}
                            onChange={e => updateReplyText(ticketId, e.target.value)}
                            rows={3}
                          />
                          <div className="user-reply-panel-actions">
                            <button
                              className="user-reply-cancel-btn"
                              onClick={() => toggleReplyPanel(ticketId)}
                            >
                              Cancel
                            </button>
                            <button
                              className="user-reply-send-btn"
                              onClick={() => handleUserReply(ticketId)}
                            >
                              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                                <line x1="22" y1="2" x2="11" y2="13"/>
                                <polygon points="22 2 15 22 11 13 2 9 22 2"/>
                              </svg>
                              Send
                            </button>
                          </div>
                        </div>
                      )}
                    </div>
                  )}

                  {/* ── Footer ──────────────────────────────────────────── */}
                  <div className="ticket-footer">
                    <span className="ticket-category">&#128193; {ticket.category}</span>
                    <span className="ticket-date">
                      {new Date(ticket.createdAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>
              );
            })}
          </div>
        );

      default:
        return <h2>Welcome</h2>;
    }
  };

  return (
    <div className="dashboard">
      {/* Sidebar */}
      <div className="sidebar">
        {/* Logo */}
        <div className="logo">
          <div className="logo-icon">
            <svg viewBox="0 0 24 24" fill="white" xmlns="http://www.w3.org/2000/svg">
              <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
            </svg>
          </div>
          <span className="logo-text">AI Smart <span>Support</span></span>
        </div>

        {/* User Info */}
        <div className="user-info">
          <div className="user-avatar">{user?.fullName?.[0]?.toUpperCase() || "U"}</div>
          <div>
            <p className="user-name">{user?.fullName || "User"}</p>
            <p className="user-role">{user?.role || "Customer"}</p>
          </div>
        </div>

        {/* Nav */}
        <div className="nav-section-label">Workspace</div>
        <ul>
          <li
            className={active === "home" ? "active" : ""}
            onClick={() => setActive("home")}
          >
            <svg className="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/>
            </svg>
            Home
          </li>
          <li
            className={active === "chat" ? "active" : ""}
            onClick={() => setActive("chat")}
          >
            <svg className="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
            </svg>
            AI Chatbot
          </li>
          <li
            className={active === "history" ? "active" : ""}
            onClick={() => setActive("history")}
          >
            <svg className="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/>
            </svg>
            Ticket History
          </li>
        </ul>

        {/* Logout */}
        <div className="sidebar-divider" />
        <button className="logout-btn" onClick={handleLogout}>
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
            <polyline points="16 17 21 12 16 7"/>
            <line x1="21" y1="12" x2="9" y2="12"/>
          </svg>
          Sign out
        </button>
      </div>

      {/* Main Content */}
      <div className={`main-content ${active === "chat" ? "chat-view-mode" : ""}`}>
        {renderContent()}
      </div>
    </div>
  );
}

export default Dashboard;
