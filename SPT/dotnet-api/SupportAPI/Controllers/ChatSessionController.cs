using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportAPI.Models;
using SupportAPI.Services;

namespace SupportAPI.Controllers
{
    // ── DTOs ─────────────────────────────────────────────────────────────────────
    public class AppendMessageRequest
    {
        public string From { get; set; } = "user";   // "user" | "bot"
        public string Text { get; set; } = string.Empty;

        // Bot metadata (optional)
        public string? Category      { get; set; }
        public string? Sentiment     { get; set; }
        public string? Priority      { get; set; }
        public double? CategoryConf  { get; set; }
        public double? SentimentConf { get; set; }
        public bool    TicketCreated { get; set; } = false;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // ChatSessionController
    // GET    api/chatsession              → list user's sessions (newest first)
    // POST   api/chatsession              → create new session
    // GET    api/chatsession/{id}         → get session with all messages
    // DELETE api/chatsession/{id}         → delete session
    // POST   api/chatsession/{id}/message → append a message to a session
    // ─────────────────────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatSessionController : ControllerBase
    {
        private readonly MongoService _mongo;

        public ChatSessionController(MongoService mongo)
        {
            _mongo = mongo;
        }

        private string CurrentEmail =>
            User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.Identity?.Name
            ?? "guest@portal.com";

        // ── GET api/chatsession ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetSessions()
        {
            var sessions = await _mongo.GetSessions(CurrentEmail);

            // Return lightweight list (no messages array — just metadata)
            var result = sessions.Select(s => new
            {
                id        = s.Id,
                title     = s.Title,
                createdAt = s.CreatedAt,
                updatedAt = s.UpdatedAt,
                // Latest bot message metadata for the sidebar preview
                lastCategory  = s.Messages.LastOrDefault(m => m.From == "bot")?.Category,
                lastSentiment = s.Messages.LastOrDefault(m => m.From == "bot")?.Sentiment,
                lastPriority  = s.Messages.LastOrDefault(m => m.From == "bot")?.Priority,
                messageCount  = s.Messages.Count,
            });

            return Ok(result);
        }

        // ── POST api/chatsession ──────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CreateSession()
        {
            var session = await _mongo.CreateSession(CurrentEmail);
            return Ok(new { id = session.Id, title = session.Title, createdAt = session.CreatedAt });
        }

        private bool CanAccess(ChatSession session)
        {
            if (User.IsInRole("Admin")) return true;
            return string.Equals(session.UserEmail, CurrentEmail, StringComparison.OrdinalIgnoreCase);
        }

        // ── GET api/chatsession/{id} ──────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSession(string id)
        {
            var session = await _mongo.GetSession(id);
            if (session == null)
                return NotFound(new { error = "Session not found." });

            // Only the owning user or admin can read their session
            if (!CanAccess(session))
                return Forbid();

            return Ok(session);
        }

        // ── DELETE api/chatsession/{id} ───────────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSession(string id)
        {
            var session = await _mongo.GetSession(id);
            if (session == null)
                return NotFound(new { error = "Session not found." });

            if (!CanAccess(session))
                return Forbid();

            await _mongo.DeleteSession(id);
            return Ok(new { message = "Session deleted." });
        }

        // ── POST api/chatsession/{id}/message ─────────────────────────────────
        [HttpPost("{id}/message")]
        public async Task<IActionResult> AppendMessage(string id, [FromBody] AppendMessageRequest req)
        {
            var session = await _mongo.GetSession(id);
            if (session == null)
                return NotFound(new { error = "Session not found." });

            if (!CanAccess(session))
                return Forbid();

            var msg = new SessionMessage
            {
                From         = req.From,
                Text         = req.Text,
                SentAt       = DateTime.UtcNow,
                Category     = req.Category,
                Sentiment    = req.Sentiment,
                Priority     = req.Priority,
                CategoryConf = req.CategoryConf,
                SentimentConf = req.SentimentConf,
                TicketCreated = req.TicketCreated,
            };

            // Set title from first user message (max 60 chars)
            string? newTitle = null;
            if (req.From == "user" && session.Messages.Count == 0)
                newTitle = req.Text.Length > 60 ? req.Text[..60] + "…" : req.Text;

            await _mongo.AppendMessage(id, msg, newTitle);
            return Ok(new { message = "Appended." });
        }
    }
}
