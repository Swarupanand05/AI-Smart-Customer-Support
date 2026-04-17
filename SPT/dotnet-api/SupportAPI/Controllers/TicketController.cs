using Microsoft.AspNetCore.Mvc;
using SupportAPI.Models;
using SupportAPI.Services;

namespace SupportAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketController : ControllerBase
    {
        private readonly MongoService _mongoService;

        public TicketController(MongoService mongoService)
        {
            _mongoService = mongoService;
        }

        // CREATE TICKET
        [HttpPost]
        public async Task<IActionResult> CreateTicket(Ticket ticket)
        {
            await _mongoService.CreateTicket(ticket);
            return Ok("Ticket created successfully");
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
        public async Task<IActionResult> UpdateStatus(string id, string status)
        {
            await _mongoService.UpdateStatus(id, status);
            return Ok("Status updated");
        }
    }
}