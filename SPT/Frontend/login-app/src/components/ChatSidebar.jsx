import React, { useState, useEffect, useCallback } from "react";
import { chatSessionAPI } from "../services/api";
import "./ChatSidebar.css";

// ── Date grouping helper ──────────────────────────────────────────────────────
function groupByDate(sessions) {
  const now   = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const yesterday = new Date(today); yesterday.setDate(today.getDate() - 1);
  const weekAgo   = new Date(today); weekAgo.setDate(today.getDate() - 7);

  const groups = { Today: [], Yesterday: [], "This Week": [], Older: [] };

  for (const s of sessions) {
    const d = new Date(s.updatedAt);
    const day = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    if (day >= today)          groups.Today.push(s);
    else if (day >= yesterday) groups.Yesterday.push(s);
    else if (day >= weekAgo)   groups["This Week"].push(s);
    else                       groups.Older.push(s);
  }

  return Object.entries(groups).filter(([, v]) => v.length > 0);
}

const categoryColor = {
  Billing: "#6366f1", Shipping: "#f59e0b", Returns: "#ec4899",
  Account: "#8b5cf6", Technical: "#ef4444", Product: "#22c55e",
  Promo: "#f97316",   General: "#94a3b8",
};

const sentimentIcon = { Positive: "😊", Negative: "😠", Neutral: "😐" };
const priorityColor = { High: "#ef4444", Medium: "#f59e0b", Low: "#22c55e" };

// ─── ChatSidebar ─────────────────────────────────────────────────────────────
function ChatSidebar({ activeSessionId, onSelectSession, onNewChat }) {
  const [sessions, setSessions]       = useState([]);
  const [loading, setLoading]         = useState(false);
  const [deletingId, setDeletingId]   = useState(null);
  const [search, setSearch]           = useState("");
  const [collapsed, setCollapsed]     = useState(false);

  const loadSessions = useCallback(async () => {
    setLoading(true);
    try {
      const data = await chatSessionAPI.list();
      setSessions(data);
    } catch (_) {
      // silently fail — sidebar is non-critical
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadSessions(); }, [loadSessions]);

  // Re-fetch when active session changes (new messages appended)
  useEffect(() => {
    if (activeSessionId) loadSessions();
  }, [activeSessionId, loadSessions]);

  const handleDelete = async (e, id) => {
    e.stopPropagation();
    setDeletingId(id);
    try {
      await chatSessionAPI.delete(id);
      setSessions(prev => prev.filter(s => s.id !== id));
      if (activeSessionId === id) onNewChat();
    } catch (_) { }
    setDeletingId(null);
  };

  const filtered = sessions.filter(s =>
    !search || s.title.toLowerCase().includes(search.toLowerCase())
  );

  const grouped = groupByDate(filtered);

  if (collapsed) {
    return (
      <aside className="chat-sidebar collapsed">
        <button
          className="cs-expand-btn"
          onClick={() => setCollapsed(false)}
          title="Expand Chat History"
        >
          💬 ▶
        </button>
        <button
          className="cs-mini-new-btn"
          onClick={onNewChat}
          title="Start fresh conversation"
        >
          ✏️
        </button>
      </aside>
    );
  }

  return (
    <aside className="chat-sidebar">
      {/* Header */}
      <div className="cs-header">
        <div className="cs-header-left">
          <span className="cs-logo">💬 Chat History</span>
          <button
            className="cs-collapse-btn"
            onClick={() => setCollapsed(true)}
            title="Collapse history"
          >
            ◀
          </button>
        </div>
        <button
          className="cs-new-btn"
          onClick={onNewChat}
          title="Start a new conversation"
        >
          ✏️ New Chat
        </button>
      </div>

      {/* Search */}
      <div className="cs-search-wrap">
        <input
          className="cs-search"
          type="text"
          placeholder="🔍 Search conversations..."
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
      </div>

      {/* Session list */}
      <div className="cs-list">
        {loading && <p className="cs-loading">Loading...</p>}

        {!loading && sessions.length === 0 && (
          <div className="cs-empty">
            <span className="cs-empty-icon">💬</span>
            <p>No conversations yet.</p>
            <p>Start chatting to see history here.</p>
          </div>
        )}

        {grouped.map(([group, items]) => (
          <div key={group} className="cs-group">
            <p className="cs-group-label">{group}</p>
            {items.map(session => (
              <div
                key={session.id}
                className={`cs-item ${activeSessionId === session.id ? "active" : ""}`}
                onClick={() => onSelectSession(session.id)}
                title={session.title}
              >
                <div className="cs-item-top">
                  <span className="cs-item-title">{session.title}</span>
                  <button
                    className="cs-delete-btn"
                    onClick={e => handleDelete(e, session.id)}
                    disabled={deletingId === session.id}
                    title="Delete conversation"
                  >
                    {deletingId === session.id ? "⏳" : "🗑️"}
                  </button>
                </div>

                {/* Metadata badges */}
                <div className="cs-item-meta">
                  {session.lastCategory && (
                    <span
                      className="cs-badge"
                      style={{ background: (categoryColor[session.lastCategory] || "#6b7280") + "22",
                               color: categoryColor[session.lastCategory] || "#6b7280" }}
                    >
                      {session.lastCategory}
                    </span>
                  )}
                  {session.lastSentiment && (
                    <span className="cs-badge cs-sentiment">
                      {sentimentIcon[session.lastSentiment]} {session.lastSentiment}
                    </span>
                  )}
                  {session.lastPriority && (
                    <span
                      className="cs-badge"
                      style={{ color: priorityColor[session.lastPriority] || "#94a3b8" }}
                    >
                      ● {session.lastPriority}
                    </span>
                  )}
                  <span className="cs-msg-count">
                    {session.messageCount} msg{session.messageCount !== 1 ? "s" : ""}
                  </span>
                </div>

                <span className="cs-item-date">
                  {new Date(session.updatedAt).toLocaleString([], {
                    month: "short", day: "numeric",
                    hour: "2-digit", minute: "2-digit"
                  })}
                </span>
              </div>
            ))}
          </div>
        ))}
      </div>
    </aside>
  );
}

export default ChatSidebar;
