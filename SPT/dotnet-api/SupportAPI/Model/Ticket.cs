using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace SupportAPI.Models
{
    // ── Embedded conversation message ────────────────────────────────────────
    public class TicketMessage
    {
        public string Sender { get; set; } = string.Empty;  // "admin" | "user"
        public string Text   { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

    // ── Ticket document ──────────────────────────────────────────────────────
    public class Ticket
    {
        [BsonId]
        [BsonIgnoreIfNull]
        [BsonIgnoreIfDefault]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string  UserEmail  { get; set; } = string.Empty;
        public string  Message    { get; set; } = string.Empty;
        public string  Category   { get; set; } = string.Empty;
        public string  Priority   { get; set; } = string.Empty;
        public string  Status     { get; set; } = "Open";

        // ML-predicted sentiment stored at ticket creation time
        public string? Sentiment  { get; set; }

        // Legacy single-reply field — kept so old tickets still display
        public string? AdminReply { get; set; }

        // New conversation thread — list of messages in order
        public List<TicketMessage> Messages { get; set; } = new List<TicketMessage>();

        public DateTime  CreatedAt  { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}