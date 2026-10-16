using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportAPI.Models;
using SupportAPI.Services;

namespace SupportAPI.Controllers
{
    // ─── DTOs ────────────────────────────────────────────────────────────────────
    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? AdminReply { get; set; }
        public string? Priority { get; set; }
    }

    public class UpdatePriorityRequest
    {
        public string Priority { get; set; } = string.Empty;
    }

    public class UserReplyRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // TicketController
    // POST   api/ticket              → create ticket
    // GET    api/ticket              → all tickets
    // GET    api/ticket/{id}         → single ticket
    // GET    api/ticket/analytics    → analytics data
    // PUT    api/ticket/{id}         → update status + admin reply (appends to thread)
    // POST   api/ticket/{id}/reply   → user follow-up reply (appends to thread)
    // ─────────────────────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketController : ControllerBase
    {
        private readonly MongoService _mongoService;
        private readonly EmailService _emailService;

        public TicketController(MongoService mongoService, EmailService emailService)
        {
            _mongoService = mongoService;
            _emailService = emailService;
        }

        // ── POST api/ticket ───────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] Ticket ticket)
        {
            if (ticket == null)
                return BadRequest(new { error = "Ticket data is required." });

            if (string.IsNullOrWhiteSpace(ticket.UserEmail))
            {
                var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                                ?? User.FindFirst("email")?.Value
                                ?? User.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(emailClaim))
                    ticket.UserEmail = emailClaim;
                else
                    ticket.UserEmail = "guest@portal.com";
            }

            if (string.IsNullOrWhiteSpace(ticket.Message))
                return BadRequest(new { error = "Ticket message cannot be empty." });

            if (string.IsNullOrWhiteSpace(ticket.Category))
                ticket.Category = "General";

            if (string.IsNullOrWhiteSpace(ticket.Priority))
                ticket.Priority = "Medium";

            if (string.IsNullOrWhiteSpace(ticket.Status))
                ticket.Status = "Open";

            if (string.IsNullOrWhiteSpace(ticket.Id))
                ticket.Id = null;

            // Duplicate ticket prevention: check if identical open ticket was submitted within 15s
            var duplicate = await _mongoService.FindRecentDuplicateTicket(ticket.UserEmail, ticket.Message);
            if (duplicate != null)
            {
                return Ok(new { 
                    message = "A ticket with identical details was recently submitted.", 
                    ticketId = duplicate.Id,
                    isDuplicate = true 
                });
            }

            if (ticket.CreatedAt == default)
                ticket.CreatedAt = DateTime.UtcNow;

            await _mongoService.CreateTicket(ticket);

            // Asynchronously dispatch confirmation email via Gmail SMTP
            if (!string.IsNullOrWhiteSpace(ticket.UserEmail) && ticket.UserEmail.Contains('@'))
            {
                var recipient = ticket.UserEmail;
                var tId       = ticket.Id ?? "NEW";
                var subj      = ticket.Message.Length > 50 ? ticket.Message[..50] + "…" : ticket.Message;
                var cat       = ticket.Category ?? "General";
                var prio      = ticket.Priority ?? "Medium";
                _ = Task.Run(() => _emailService.SendTicketCreatedEmailAsync(recipient, tId, subj, cat, prio));
            }

            return Ok(new { message = "Ticket created successfully", ticketId = ticket.Id });
        }

        // ── GET api/ticket ────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            var tickets = await _mongoService.GetTickets();
            return Ok(tickets);
        }

        // ── GET api/ticket/analytics ──────────────────────────────────────────
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var tickets = await _mongoService.GetTickets();

            // Status breakdown
            var total    = tickets.Count;
            var open     = tickets.Count(t => t.Status == "Open");
            var resolved = tickets.Count(t => t.Status == "Resolved");
            var closed   = tickets.Count(t => t.Status == "Closed");

            // Priority breakdown
            var high   = tickets.Count(t => t.Priority == "High");
            var medium = tickets.Count(t => t.Priority == "Medium");
            var low    = tickets.Count(t => t.Priority == "Low");

            // Category breakdown
            var byCategory = tickets
                .GroupBy(t => t.Category ?? "General")
                .Select(g => new { category = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            // Last 7 days trend (tickets created per day)
            var today = DateTime.UtcNow.Date;
            var trend = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(-i))
                .Select(day => new
                {
                    date  = day.ToString("MMM dd"),
                    count = tickets.Count(t => t.CreatedAt.Date == day)
                })
                .Reverse()
                .ToList();

            // Avg resolution time (resolved tickets only)
            var resolvedTickets = tickets
                .Where(t => (t.Status == "Resolved" || t.Status == "Closed") && t.ResolvedAt.HasValue)
                .ToList();

            double avgResolutionHours = 0;
            if (resolvedTickets.Any())
            {
                avgResolutionHours = resolvedTickets
                    .Average(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours);
            }

            return Ok(new
            {
                total,
                open,
                resolved,
                closed,
                highPriority   = high,
                mediumPriority = medium,
                lowPriority    = low,
                byCategory,
                trend,
                avgResolutionHours = Math.Round(avgResolutionHours, 1)
            });
        }

        // ── GET api/ticket/{id} ───────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(string id)
        {
            var ticket = await _mongoService.GetTicketById(id);
            if (ticket == null)
                return NotFound(new { error = "Ticket not found." });
            return Ok(ticket);
        }

        // ── PUT api/ticket/{id} ───────────────────────────────────────────────
        // Admin updates status and optionally appends a reply to the thread
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(
            string id,
            [FromBody] UpdateStatusRequest req)
        {
            var existingTicket = await _mongoService.GetTicketById(id);

            await _mongoService.UpdateTicket(id, req.Status, req.AdminReply, req.Priority);

            // Asynchronously dispatch status update email via Gmail SMTP
            if (existingTicket != null && !string.IsNullOrWhiteSpace(existingTicket.UserEmail) && existingTicket.UserEmail.Contains('@'))
            {
                var recipient = existingTicket.UserEmail;
                var stat      = req.Status;
                var reply     = req.AdminReply;
                var prio      = req.Priority ?? existingTicket.Priority;
                _ = Task.Run(() => _emailService.SendTicketStatusUpdateEmailAsync(recipient, id, stat, reply, prio));
            }

            return Ok(new { message = "Ticket updated successfully" });
        }

        // ── PATCH api/ticket/{id}/priority ────────────────────────────────────
        // Admin sets the priority of a ticket (High / Medium / Low)
        [HttpPatch("{id}/priority")]
        public async Task<IActionResult> UpdatePriority(
            string id,
            [FromBody] UpdatePriorityRequest req)
        {
            var allowed = new[] { "High", "Medium", "Low" };
            if (!allowed.Contains(req.Priority))
                return BadRequest(new { error = "Priority must be High, Medium, or Low." });

            await _mongoService.UpdatePriority(id, req.Priority);
            return Ok(new { message = $"Priority set to {req.Priority}" });
        }

        // ── POST api/ticket/{id}/reply ────────────────────────────────────────
        // User posts a follow-up message to an existing conversation thread
        [HttpPost("{id}/reply")]
        public async Task<IActionResult> UserReply(
            string id,
            [FromBody] UserReplyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Text))
                return BadRequest(new { error = "Reply text cannot be empty." });

            var ticket = await _mongoService.GetTicketById(id);
            if (ticket == null)
                return NotFound(new { error = "Ticket not found." });

            await _mongoService.AddUserReply(id, req.Text);
            return Ok(new { message = "Reply sent successfully" });
        }
    }
}
