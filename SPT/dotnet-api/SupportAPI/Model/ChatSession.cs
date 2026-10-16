using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace SupportAPI.Models
{
    // ── Embedded message in a chat session ───────────────────────────────────
    public class SessionMessage
    {
        public string   From   { get; set; } = "user";   // "user" | "bot"
        public string   Text   { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Bot-only metadata (null for user messages)
        public string? Category      { get; set; }
        public string? Sentiment     { get; set; }
        public string? Priority      { get; set; }
        public double? CategoryConf  { get; set; }
        public double? SentimentConf { get; set; }
        public bool    TicketCreated { get; set; } = false;
    }

    // ── ChatSession document ─────────────────────────────────────────────────
    public class ChatSession
    {
        [BsonId]
        [BsonIgnoreIfNull]
        [BsonIgnoreIfDefault]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        /// <summary>Auto-generated from the first user message (max 60 chars).</summary>
        public string Title { get; set; } = "New Chat";

        public List<SessionMessage> Messages { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
