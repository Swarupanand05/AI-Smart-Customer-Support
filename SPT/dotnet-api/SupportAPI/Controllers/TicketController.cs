using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportAPI.Models;
using SupportAPI.Services;

namespace SupportAPI.Controllers
{
    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketController : ControllerBase
    {
        private readonly MongoService _mongoService;

        public TicketController(MongoService mongoService)
        {
            _mongoService = mongoService;
        }


        // CREATE TICKET
        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] Ticket ticket)
        {

            await _mongoService.CreateTicket(ticket);
            return Ok(new { message = "Ticket created successfully" });
        }
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics()
        {
            var tickets = await _mongoService.GetTickets();

            var total = tickets.Count;
            var open = tickets.Count(t => t.Status == "Open");
            var closed = tickets.Count(t => t.Status == "Closed");
            var high = tickets.Count(t => t.Priority == "High");

            return Ok(new
            {
                total,
                open,
                closed,
                highPriority = high
            });
        }

        // GET ALL TICKETS
        [HttpGet]
        public async Task<IActionResult> GetTickets()
        {
            var tickets = await _mongoService.GetTickets();
            return Ok(tickets);
        }

        // UPDATE STATUS (Admin)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest req)
        {
            await _mongoService.UpdateStatus(id, req.Status);
            return Ok(new { message = "Status updated" });
        }
    }
}