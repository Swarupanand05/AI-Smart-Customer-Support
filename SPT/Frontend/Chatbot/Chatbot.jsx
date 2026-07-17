import React, { useState, useRef, useEffect } from "react";
import { chatAPI, getUser } from "../services/api";
import "./Chatbot.css";

function Chatbot() {
  const [messages, setMessages] = useState([
    {
      from: "bot",
      text: "👋 Hello! I'm your AI support assistant. How can I help you today?",
      timestamp: new Date(),
    },
  ]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const bottomRef = useRef(null);

  const user = getUser();

  // Auto-scroll to bottom on new messages
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const sendMessage = async () => {
    const text = input.trim();
    if (!text || loading) return;

    // Add user message
    const userMsg = { from: "user", text, timestamp: new Date() };
    setMessages((prev) => [...prev, userMsg]);
    setInput("");
    setLoading(true);

    try {
      const data = await chatAPI.sendMessage(text, user?.email || "guest@portal.com");

      const botMsg = {
        from: "bot",
        text: data.reply,
        timestamp: new Date(),
        meta: {
          category: data.category,
          sentiment: data.sentiment,
          ticketCreated: data.ticketCreated,
        },
      };
      setMessages((prev) => [...prev, botMsg]);
    } catch (err) {
      setMessages((prev) => [
        ...prev,
        {
          from: "bot",
          text: "⚠️ Sorry, I couldn't process your request. Please try again.",
          timestamp: new Date(),
        },
      ]);
    } finally {
      setLoading(false);
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

  const sentimentBadge = (sentiment) => {
    const map = {
      Positive: { color: "#22c55e", icon: "😊" },
      Negative: { color: "#ef4444", icon: "😠" },
      Neutral: { color: "#6b7280", icon: "😐" },
    };
    return map[sentiment] || map.Neutral;
  };

  return (
    <div className="chatbot-wrapper">
      {/* Header */}
      <div className="chatbot-header">
        <div className="chatbot-avatar">🤖</div>
        <div>
          <h3>AI Support Assistant</h3>
          <span className="chatbot-status">● Online</span>
        </div>
      </div>

      {/* Messages */}
      <div className="chatbot-messages">
        {messages.map((msg, i) => (
          <div key={i} className={`chat-bubble-row ${msg.from}`}>
            <div className={`chat-bubble ${msg.from}`}>
              <p>{msg.text}</p>

              {/* Metadata badge for bot messages */}
              {msg.from === "bot" && msg.meta && (
                <div className="chat-meta">
                  <span className="meta-tag">📁 {msg.meta.category}</span>
                  <span
                    className="meta-tag"
                    style={{ color: sentimentBadge(msg.meta.sentiment).color }}
                  >
                    {sentimentBadge(msg.meta.sentiment).icon} {msg.meta.sentiment}
                  </span>
                  {msg.meta.ticketCreated && (
                    <span className="meta-tag ticket-created">🎫 Ticket Created</span>
                  )}
                </div>
              )}

              <span className="chat-time">{formatTime(msg.timestamp)}</span>
            </div>
          </div>
        ))}

        {loading && (
          <div className="chat-bubble-row bot">
            <div className="chat-bubble bot typing">
              <span></span><span></span><span></span>
            </div>
          </div>
        )}

        <div ref={bottomRef} />
      </div>

      {/* Input */}
      <div className="chatbot-input-area">
        <input
          className="chatbot-input"
          type="text"
          placeholder="Type your message..."
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={loading}
        />
        <button
          className="chatbot-send-btn"
          onClick={sendMessage}
          disabled={loading || !input.trim()}
        >
          ➤
        </button>
      </div>
    </div>
  );
}

export default Chatbot;
