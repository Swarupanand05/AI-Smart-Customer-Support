using MongoDB.Bson;
using MongoDB.Driver;
using SupportAPI.Models;

namespace SupportAPI.Services
{
    // ──────────────────────────────────────────────────────────────────────────
    // MongoService — handles all MongoDB CRUD operations
    // Collections: Tickets, Chats, ChatSessions
    // ──────────────────────────────────────────────────────────────────────────
    public class MongoService
    {
        private readonly IMongoCollection<ChatMessage>  _chats;
        private readonly IMongoCollection<Ticket>       _tickets;
        private readonly IMongoCollection<ChatSession>  _sessions;

        public MongoService(IConfiguration config)
        {
            var client   = new MongoClient(config["MongoDB:ConnectionString"]);
            var database = client.GetDatabase(config["MongoDB:DatabaseName"]);

            _tickets  = database.GetCollection<Ticket>("Tickets");
            _chats    = database.GetCollection<ChatMessage>("Chats");
            _sessions = database.GetCollection<ChatSession>("ChatSessions");
        }

        /// <summary>
        /// Idempotently ensures all optimized indexes exist on MongoDB collections.
        /// Safe to run on startup without duplicating indexes or dropping collections.
        /// </summary>
        public async Task CreateIndexesAsync()
        {
            try
            {
                // 1. ChatSessions: { UserEmail: 1, UpdatedAt: -1 }
                var sessionIndexKeys = Builders<ChatSession>.IndexKeys
                    .Ascending(s => s.UserEmail)
                    .Descending(s => s.UpdatedAt);
                await _sessions.Indexes.CreateOneAsync(
                    new CreateIndexModel<ChatSession>(sessionIndexKeys, new CreateIndexOptions { Name = "idx_sessions_email_updated" })
                );

                // 2. Chats: { UserEmail: 1, CreatedAt: -1 }
                var chatIndexKeys = Builders<ChatMessage>.IndexKeys
                    .Ascending(c => c.UserEmail)
                    .Descending(c => c.CreatedAt);
                await _chats.Indexes.CreateOneAsync(
                    new CreateIndexModel<ChatMessage>(chatIndexKeys, new CreateIndexOptions { Name = "idx_chats_email_created" })
                );

                // 3. Tickets indexes:
                // - Sort by CreatedAt descending
                var ticketCreatedKeys = Builders<Ticket>.IndexKeys.Descending(t => t.CreatedAt);
                await _tickets.Indexes.CreateOneAsync(
                    new CreateIndexModel<Ticket>(ticketCreatedKeys, new CreateIndexOptions { Name = "idx_tickets_created" })
                );

                // - Filter by UserEmail
                var ticketUserKeys = Builders<Ticket>.IndexKeys.Ascending(t => t.UserEmail);
                await _tickets.Indexes.CreateOneAsync(
                    new CreateIndexModel<Ticket>(ticketUserKeys, new CreateIndexOptions { Name = "idx_tickets_user_email" })
                );

                // - Compound: Status + CreatedAt descending
                var ticketStatusCreatedKeys = Builders<Ticket>.IndexKeys
                    .Ascending(t => t.Status)
                    .Descending(t => t.CreatedAt);
                await _tickets.Indexes.CreateOneAsync(
                    new CreateIndexModel<Ticket>(ticketStatusCreatedKeys, new CreateIndexOptions { Name = "idx_tickets_status_created" })
                );

                Console.WriteLine("✅ [MongoService] MongoDB compound and single indexes ensured successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [MongoService] Index creation note (non-blocking): {ex.Message}");
            }
        }

        // ── Tickets ───────────────────────────────────────────────────────────
        public async Task CreateTicket(Ticket ticket)
            => await _tickets.InsertOneAsync(ticket);

        public async Task<Ticket?> FindRecentDuplicateTicket(string email, string message, int windowSeconds = 15)
        {
            var threshold = DateTime.UtcNow.AddSeconds(-windowSeconds);
            return await _tickets.Find(t => t.UserEmail == email && t.Message == message && t.CreatedAt >= threshold && t.Status == "Open")
                                 .FirstOrDefaultAsync();
        }

        public async Task<List<Ticket>> GetTickets()
            => await _tickets.Find(_ => true).SortByDescending(t => t.CreatedAt).ToListAsync();

        /// <summary>
        /// Admin updates status and optionally sends a reply message.
        /// The reply is appended to the Messages thread AND stored in the legacy
        /// AdminReply field so old clients still work.
        /// </summary>
        public async Task UpdateTicket(string id, string status, string? adminReply, string? priority = null)
        {
            var filter    = Builders<Ticket>.Filter.Eq(t => t.Id, id);
            var updateDef = Builders<Ticket>.Update.Set(t => t.Status, status);

            if (!string.IsNullOrWhiteSpace(priority))
                updateDef = updateDef.Set(t => t.Priority, priority);

            if (!string.IsNullOrWhiteSpace(adminReply))
            {
                var msg = new TicketMessage
                {
                    Sender = "admin",
                    Text   = adminReply,
                    SentAt = DateTime.UtcNow
                };

                // Legacy field (for old UI still reading AdminReply)
                updateDef = updateDef.Set(t => t.AdminReply, adminReply);

                // New thread — push to Messages array
                updateDef = updateDef.Push(t => t.Messages, msg);
            }

            if (status == "Resolved" || status == "Closed")
                updateDef = updateDef.Set(t => t.ResolvedAt, DateTime.UtcNow);

            await _tickets.UpdateOneAsync(filter, updateDef);
        }

        /// <summary>Admin updates only the priority of a ticket (High / Medium / Low).</summary>
        public async Task UpdatePriority(string id, string priority)
        {
            var filter    = Builders<Ticket>.Filter.Eq(t => t.Id, id);
            var updateDef = Builders<Ticket>.Update.Set(t => t.Priority, priority);
            await _tickets.UpdateOneAsync(filter, updateDef);
        }

        /// <summary>
        /// User posts a follow-up reply to an existing conversation.
        /// Only appends to the Messages thread; does NOT change status.
        /// </summary>
        public async Task AddUserReply(string id, string text)
        {
            var filter = Builders<Ticket>.Filter.Eq(t => t.Id, id);
            var msg    = new TicketMessage
            {
                Sender = "user",
                Text   = text,
                SentAt = DateTime.UtcNow
            };
            var updateDef = Builders<Ticket>.Update.Push(t => t.Messages, msg);
            await _tickets.UpdateOneAsync(filter, updateDef);
        }

        // Keep for backward compatibility
        public async Task UpdateStatus(string id, string status)
            => await UpdateTicket(id, status, null);

        public async Task<Ticket?> GetTicketById(string id)
            => await _tickets.Find(t => t.Id == id).FirstOrDefaultAsync();

        // ── Chats ─────────────────────────────────────────────────────────────
        public async Task SaveChat(ChatMessage chat)
            => await _chats.InsertOneAsync(chat);

        /// <summary>Returns the last 50 chat messages for a user.</summary>
        public async Task<List<ChatMessage>> GetChatsByEmail(string email)
            => await _chats
                .Find(c => c.UserEmail == email)
                .SortByDescending(c => c.CreatedAt)
                .Limit(50)
                .ToListAsync();

        // ── Chat Sessions ─────────────────────────────────────────────────────

        /// <summary>Create a new empty chat session for a user.</summary>
        public async Task<ChatSession> CreateSession(string userEmail, string title = "New Chat")
        {
            var session = new ChatSession
            {
                UserEmail = userEmail,
                Title     = title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            await _sessions.InsertOneAsync(session);
            return session;
        }

        /// <summary>Get all sessions for a user, newest first.</summary>
        public async Task<List<ChatSession>> GetSessions(string userEmail)
            => await _sessions
                .Find(s => s.UserEmail == userEmail)
                .SortByDescending(s => s.UpdatedAt)
                .ToListAsync();

        /// <summary>Get a single session by ID with full messages.</summary>
        public async Task<ChatSession?> GetSession(string id)
            => await _sessions.Find(s => s.Id == id).FirstOrDefaultAsync();

        /// <summary>Append a message to a session; optionally update the session title.</summary>
        public async Task AppendMessage(string sessionId, SessionMessage message, string? newTitle = null)
        {
            var filter    = Builders<ChatSession>.Filter.Eq(s => s.Id, sessionId);
            var updateDef = Builders<ChatSession>.Update
                .Push(s => s.Messages, message)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrWhiteSpace(newTitle))
                updateDef = updateDef.Set(s => s.Title, newTitle);

            await _sessions.UpdateOneAsync(filter, updateDef);
        }

        /// <summary>Delete a session by ID.</summary>
        public async Task DeleteSession(string id)
            => await _sessions.DeleteOneAsync(s => s.Id == id);
    }
}