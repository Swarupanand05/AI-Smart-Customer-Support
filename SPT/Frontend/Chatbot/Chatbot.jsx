import React, { useState, useRef, useEffect, useCallback } from "react";
import { chatAPI, ticketAPI, chatSessionAPI, getUser } from "../services/api";
import "./Chatbot.css";

// ── Lightweight, safe markdown formatter for bot responses ─────────────────
function renderFormattedMessage(text) {
  if (!text) return null;

  const lines = text.split("\n");
  const elements = [];
  let listItems = [];
  let listType = null; // 'ul' or 'ol'

  const flushList = (keyPrefix) => {
    if (listItems.length > 0) {
      if (listType === "ol") {
        elements.push(<ol key={`${keyPrefix}-ol`} className="chat-ol">{listItems}</ol>);
      } else {
        elements.push(<ul key={`${keyPrefix}-ul`} className="chat-ul">{listItems}</ul>);
      }
      listItems = [];
      listType = null;
    }
  };

  const formatInline = (str) => {
    const parts = [];
    const regex = /(\*\*.*?\*\*|`.*?`)/g;
    let lastIdx = 0;
    let match;

    while ((match = regex.exec(str)) !== null) {
      if (match.index > lastIdx) {
        parts.push(str.substring(lastIdx, match.index));
      }
      const token = match[0];
      if (token.startsWith("**") && token.endsWith("**")) {
        parts.push(<strong key={match.index} className="chat-bold">{token.slice(2, -2)}</strong>);
      } else if (token.startsWith("`") && token.endsWith("`")) {
        parts.push(<code key={match.index} className="chat-code">{token.slice(1, -1)}</code>);
      }
      lastIdx = regex.lastIndex;
    }
    if (lastIdx < str.length) {
      parts.push(str.substring(lastIdx));
    }
    return parts.length > 0 ? parts : str;
  };

  lines.forEach((line, idx) => {
    const trimmed = line.trim();
    if (!trimmed) {
      flushList(`flush-${idx}`);
      elements.push(<div key={`space-${idx}`} className="chat-line-break" />);
      return;
    }

    // Unordered bullet (* or -)
    if (/^[-*•]\s+/.test(trimmed)) {
      if (listType && listType !== "ul") flushList(`sw-${idx}`);
      listType = "ul";
      const content = trimmed.replace(/^[-*•]\s+/, "");
      listItems.push(<li key={`li-${idx}`}>{formatInline(content)}</li>);
      return;
    }

    // Numbered list (1. or 1))
    const numMatch = trimmed.match(/^(\d+)[.)]\s+(.*)/);
    if (numMatch) {
      if (listType && listType !== "ol") flushList(`sw-${idx}`);
      listType = "ol";
      listItems.push(<li key={`li-${idx}`}>{formatInline(numMatch[2])}</li>);
      return;
    }

    flushList(`line-${idx}`);
    elements.push(<p key={`p-${idx}`} className="chat-p">{formatInline(trimmed)}</p>);
  });

  flushList("final");
  return elements;
}

// ── Default greeting message ──────────────────────────────────────────────
const WELCOME = {
  id: "welcome-system-msg",
  from: "bot",
  text: "Hello! I am your AI E-Commerce Support Assistant powered by AI Smart Support. How can I assist you with your orders, refunds, shipments, or returns today?",
  timestamp: new Date(),
};

function Chatbot({ activeSessionId, onSessionCreated, onNewChat }) {
  const [messages, setMessages] = useState([WELCOME]);
  const [input, setInput]     = useState("");
  const [loading, setLoading] = useState(false);
  const [sessionId, setSessionId] = useState(activeSessionId || null);
  const [copiedIdx, setCopiedIdx] = useState(null);
  const bottomRef = useRef(null);
  const user = getUser();

  // Auto-scroll to bottom on new messages
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, loading]);

  // Load a session when activeSessionId changes
  const loadSession = useCallback(async (id) => {
    if (!id) {
      setMessages([WELCOME]);
      setSessionId(null);
      return;
    }
    try {
      const session = await chatSessionAPI.get(id);
      const rawMsgs = session?.messages || session?.Messages || [];
      const rebuilt = rawMsgs.map((m, idx, arr) => {
        const from   = m.from || m.From || "user";
        const text   = m.text || m.Text || "";
        const sentAt = m.sentAt || m.SentAt || new Date();
        let uQuery   = undefined;
        if (from === "bot") {
          for (let j = idx - 1; j >= 0; j--) {
            const prevFrom = arr[j]?.from || arr[j]?.From;
            const prevText = arr[j]?.text || arr[j]?.Text;
            if (prevFrom === "user" && prevText) {
              uQuery = prevText;
              break;
            }
          }
        }
        return {
          id: m.id || m.Id || `msg-${id}-${idx}`,
          from,
          text,
          userQuery: uQuery,
          timestamp: new Date(sentAt),
          meta: from === "bot" ? {
            category:      m.category || m.Category,
            sentiment:     m.sentiment || m.Sentiment,
            priority:      m.priority || m.Priority,
            categoryConf:  (m.categoryConf ?? m.CategoryConf) != null ? Math.round((m.categoryConf ?? m.CategoryConf) * 100) : null,
            sentimentConf: (m.sentimentConf ?? m.SentimentConf) != null ? Math.round((m.sentimentConf ?? m.SentimentConf) * 100) : null,
            ticketCreated: m.ticketCreated || m.TicketCreated,
            isGreeting:    false,
          } : undefined,
        };
      });
      setMessages(rebuilt.length > 0 ? rebuilt : [WELCOME]);
      setSessionId(id);
    } catch (err) {
      console.error("[Chatbot] Error loading session:", err);
    }
  }, []);

  useEffect(() => {
    if (activeSessionId) {
      loadSession(activeSessionId);
    } else {
      setMessages([WELCOME]);
      setSessionId(null);
      setInput("");
    }
  }, [activeSessionId, loadSession]);

  const QUICK_ACTIONS = [
    { label: "📦 Track Order", query: "Where is my order? Need tracking update." },
    { label: "💳 Payment / Refund", query: "My payment failed and money was deducted via UPI." },
    { label: "↩️ Return / Replace", query: "I want to return an item and request a replacement." },
    { label: "⚠️ Damaged Item", query: "I received a damaged product with broken packaging." },
    { label: "🏷️ Offers & Deals", query: "Promo code or discount not applying to my order." },
  ];

  const handleCopyMessage = (idx, text) => {
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(() => {
        setCopiedIdx(idx);
        setTimeout(() => setCopiedIdx(null), 2000);
      }).catch(() => {});
    }
  };

  const sendCustomMessage = async (customText) => {
    const text = (customText || input).trim();
    if (!text || loading) return;

    const userMsg = {
      id: `user-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`,
      from: "user",
      text,
      timestamp: new Date()
    };
    setMessages(prev => [...prev, userMsg]);
    if (!customText) setInput("");
    setLoading(true);

    // Ensure we have an active session
    let sid = sessionId;
    if (!sid) {
      try {
        const s = await chatSessionAPI.create();
        sid = s.id;
        setSessionId(sid);
        if (onSessionCreated) onSessionCreated(sid);
      } catch (_) { }
    }

    // Persist user message to session
    if (sid) {
      chatSessionAPI.appendMessage(sid, { from: "user", text }).catch(() => {});
    }

    try {
      const data = await chatAPI.sendMessage(text, user?.email || "guest@portal.com", sid);

      const botMsg = {
        id: `bot-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`,
        from: "bot",
        text: data.reply,
        userQuery: text,
        timestamp: new Date(),
        meta: {
          category:      data.category,
          sentiment:     data.sentiment,
          categoryConf:  data.categoryConf,
          sentimentConf: data.sentimentConf,
          priority:      data.priority,
          ticketCreated: data.ticketCreated,
          isFaq:         data.isFaq,
          isGreeting:    data.isGreeting,
        },
      };
      setMessages(prev => [...prev, botMsg]);

      // Persist bot message to session
      if (sid) {
        chatSessionAPI.appendMessage(sid, {
          from:         "bot",
          text:         data.reply,
          category:     data.category,
          sentiment:    data.sentiment,
          priority:     data.priority,
          categoryConf:  data.categoryConf  != null ? data.categoryConf  / 100 : null,
          sentimentConf: data.sentimentConf != null ? data.sentimentConf / 100 : null,
          ticketCreated: !!data.ticketCreated,
        }).catch(() => {});
      }
    } catch (err) {
      console.error("[Chatbot] sendMessage error:", err.message, err);
      setMessages((prev) => [
        ...prev,
        {
          from: "bot",
          text: `⚠️ ${err.message || "Sorry, I couldn't process your request. Please try again."}`,
          timestamp: new Date(),
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const sendMessage = () => sendCustomMessage();

  const handleSatisfaction = async (msgIndex, isSatisfied, msgObj) => {
    if (isSatisfied) {
      setMessages((prev) =>
        prev.map((m, idx) =>
          idx === msgIndex ? { ...m, feedbackGiven: "satisfied" } : m
        )
      );
    } else {
      // User is NOT satisfied -> create ticket for user with full context
      try {
        setMessages((prev) =>
          prev.map((m, idx) =>
            idx === msgIndex ? { ...m, feedbackGiven: "ticket_created" } : m
          )
        );

        // Find customer's actual query (search backwards from this message)
        let customerQuery = msgObj.userQuery;
        if (!customerQuery) {
          for (let i = msgIndex - 1; i >= 0; i--) {
            if (messages[i]?.from === "user" && messages[i]?.text) {
              customerQuery = messages[i].text;
              break;
            }
          }
        }
        if (!customerQuery) {
          customerQuery = "Customer requested escalation for support assistance.";
        }

        // Build complete conversation thread up to this point
        const threadHistory = messages.slice(0, msgIndex + 1).map(m => ({
          sender: m.from === "bot" ? "admin" : "user",
          text: m.text,
          sentAt: m.timestamp ? new Date(m.timestamp) : new Date(),
        }));

        const ticketData = {
          userEmail: user?.email || "guest@portal.com",
          message: customerQuery,
          category: msgObj.meta?.category || "General",
          priority: msgObj.meta?.priority || "Medium",
          sentiment: msgObj.meta?.sentiment || "Neutral",
          status: "Open",
          messages: threadHistory,
        };

        await ticketAPI.create(ticketData);

        // Add follow-up confirmation from bot
        setMessages((prev) => [
          ...prev,
          {
            id: `ticket-confirm-${Date.now()}`,
            from: "bot",
            text: "🎫 **Support Ticket Created Successfully!**\n\nYour issue has been escalated to our specialist support team along with your complete chat details. An agent will review the problem statement and update your ticket shortly.",
            timestamp: new Date(),
            meta: {
              category: ticketData.category,
              priority: ticketData.priority,
              sentiment: ticketData.sentiment,
              ticketCreated: true,
            }
          }
        ]);
      } catch (err) {
        console.error("Error creating ticket:", err);
      }
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  };

  const formatTime = (date) =>
    date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });

  const sentimentConfig = (sentiment) => {
    const map = {
      Positive: { color: "#10b981", bg: "rgba(16,185,129,0.12)", border: "rgba(16,185,129,0.25)", icon: "😊" },
      Negative: { color: "#ef4444", bg: "rgba(239,68,68,0.12)",  border: "rgba(239,68,68,0.25)",  icon: "😠" },
      Neutral:  { color: "#94a3b8", bg: "rgba(148,163,184,0.1)", border: "rgba(148,163,184,0.2)", icon: "😐" },
    };
    return map[sentiment] || map.Neutral;
  };

  const priorityConfig = (priority) => {
    const map = {
      Critical: { color: "#a855f7", bg: "rgba(168,85,247,0.12)", border: "rgba(168,85,247,0.3)" },
      High:     { color: "#ef4444", bg: "rgba(239,68,68,0.12)",  border: "rgba(239,68,68,0.25)"  },
      Medium:   { color: "#f59e0b", bg: "rgba(245,158,11,0.12)", border: "rgba(245,158,11,0.25)" },
      Low:      { color: "#10b981", bg: "rgba(16,185,129,0.12)", border: "rgba(16,185,129,0.25)" },
    };
    return map[priority] || map.Medium;
  };

  return (
    <div className="chatbot-wrapper">
      {/* Header */}
      <div className="chatbot-header">
        <div className="chatbot-header-left">
          <div className="chatbot-avatar">
            <svg viewBox="0 0 24 24" fill="none" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
              <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
            </svg>
          </div>
          <div className="chatbot-header-info">
            <div className="chatbot-header-title-row">
              <h3>AI Support Assistant</h3>
              <span className="ecommerce-tag">E-Commerce Intelligent Agent</span>
            </div>
            <div className="chatbot-status">
              <span className="status-dot" />
              Online · Multi-LLM Active (Groq + Gemini · Amazon/Flipkart Trained)
            </div>
          </div>
        </div>

        {onNewChat && (
          <button
            className="chatbot-newchat-header-btn"
            onClick={onNewChat}
            title="Start fresh conversation"
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round">
              <line x1="12" y1="5" x2="12" y2="19"/>
              <line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            New Chat
          </button>
        )}
      </div>

      {/* Messages */}
      <div className="chatbot-messages">
        {messages.map((msg, i) => (
          <div key={msg.id || `msg-${i}-${msg.timestamp ? new Date(msg.timestamp).getTime() : i}`} className={`chat-bubble-row ${msg.from}`}>
            {msg.from === "bot" && (
              <div className="bot-avatar-mini">
                <svg viewBox="0 0 24 24" fill="none" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                </svg>
              </div>
            )}
            <div className={`chat-bubble ${msg.from}`}>
              {/* Message Header Action for bot */}
              {msg.from === "bot" && (
                <div className="chat-bubble-actions">
                  <button
                    className="chat-action-icon-btn"
                    onClick={() => handleCopyMessage(i, msg.text)}
                    title="Copy response"
                  >
                    {copiedIdx === i ? (
                      <span className="copied-badge">✓ Copied</span>
                    ) : (
                      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                        <rect x="9" y="9" width="13" height="13" rx="2" ry="2"/>
                        <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
                      </svg>
                    )}
                  </button>
                </div>
              )}

              <div className="chat-bubble-content">
                {msg.from === "bot" ? renderFormattedMessage(msg.text) : <p className="chat-p">{msg.text}</p>}
              </div>

              {/* Metadata badge for bot messages */}
              {msg.from === "bot" && msg.meta && (
                <div className="chat-meta">
                  {/* Category */}
                  {msg.meta.category && (
                    <span className="meta-tag">
                      📁 {msg.meta.category}
                      {msg.meta.categoryConf != null && (
                        <span className="conf-score">{msg.meta.categoryConf}%</span>
                      )}
                    </span>
                  )}

                  {/* Sentiment */}
                  {msg.meta.sentiment && (
                    <span
                      className="meta-tag"
                      style={{
                        color:           sentimentConfig(msg.meta.sentiment).color,
                        backgroundColor: sentimentConfig(msg.meta.sentiment).bg,
                        borderColor:     sentimentConfig(msg.meta.sentiment).border,
                      }}
                    >
                      {sentimentConfig(msg.meta.sentiment).icon} {msg.meta.sentiment}
                      {msg.meta.sentimentConf != null && (
                        <span className="conf-score"> {msg.meta.sentimentConf}%</span>
                      )}
                    </span>
                  )}

                  {/* Priority */}
                  {msg.meta.priority && (
                    <span
                      className="meta-tag"
                      style={{
                        color:           priorityConfig(msg.meta.priority).color,
                        backgroundColor: priorityConfig(msg.meta.priority).bg,
                        borderColor:     priorityConfig(msg.meta.priority).border,
                      }}
                    >
                      {msg.meta.priority} Priority
                    </span>
                  )}

                  {/* Ticket Created */}
                  {msg.meta.ticketCreated && (
                    <span className="meta-tag ticket-created">🎫 Ticket Created</span>
                  )}

                  {/* Satisfaction Feedback — shown for ALL non-greeting responses */}
                  {msg.from === "bot" && msg.meta && !msg.meta.isGreeting && !msg.meta.ticketCreated && !msg.feedbackGiven && (
                    <div className="faq-feedback-box">
                      <span className="feedback-label">Was this resolution helpful?</span>
                      <div className="feedback-action-btns">
                        <button
                          className="feedback-btn btn-yes"
                          onClick={() => handleSatisfaction(i, true, msg)}
                        >
                          👍 Yes, satisfied
                        </button>
                        <button
                          className="feedback-btn btn-no"
                          onClick={() => handleSatisfaction(i, false, msg)}
                        >
                          👎 No, escalate to human agent
                        </button>
                      </div>
                    </div>
                  )}

                  {msg.feedbackGiven === "satisfied" && (
                    <span className="meta-tag ticket-satisfied">✅ Resolved (Self-Service)</span>
                  )}

                  {msg.feedbackGiven === "ticket_created" && (
                    <span className="meta-tag ticket-created">🎫 Escalated to Support Ticket</span>
                  )}
                </div>
              )}

              <span className="chat-time">{formatTime(msg.timestamp)}</span>
            </div>
          </div>
        ))}

        {loading && (
          <div className="chat-bubble-row bot">
            <div className="bot-avatar-mini">
              <svg viewBox="0 0 24 24" fill="none" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>
              </svg>
            </div>
            <div className="chat-bubble bot typing">
              <span /><span /><span />
            </div>
          </div>
        )}

        <div ref={bottomRef} />
      </div>

      {/* Quick E-Commerce Action Chips */}
      <div className="chatbot-quick-chips">
        {QUICK_ACTIONS.map((chip, idx) => (
          <button
            key={idx}
            className="quick-chip-btn"
            onClick={() => sendCustomMessage(chip.query)}
            disabled={loading}
          >
            {chip.label}
          </button>
        ))}
      </div>

      {/* Input */}
      <div className="chatbot-input-area">
        <input
          id="chat-input"
          className="chatbot-input"
          type="text"
          placeholder="Type your question (e.g., track order, UPI refund, return item)…"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={loading}
        />
        <button
          id="chat-send"
          className="chatbot-send-btn"
          onClick={sendMessage}
          disabled={loading || !input.trim()}
          title="Send message"
        >
          <svg viewBox="0 0 24 24" fill="none" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <line x1="22" y1="2" x2="11" y2="13"/>
            <polygon points="22 2 15 22 11 13 2 9 22 2"/>
          </svg>
        </button>
      </div>
    </div>
  );
}

export default Chatbot;
