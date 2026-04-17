using Microsoft.AspNetCore.Mvc;
using SupportAPI.Models;
using SupportAPI.Services;

namespace SupportAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly MongoService _mongoService;
        private readonly AIService _aiService;

        public ChatController(MongoService mongoService, AIService aiService)
        {
            _mongoService = mongoService;
            _aiService = aiService;
        }

        [HttpPost]
        public async Task<IActionResult> Chat(string message, string email)
        {
            var reply = _aiService.GetReply(message);
            var category = _aiService.GetCategory(message);
            var sentiment = _aiService.GetSentiment(message);

            if (reply != null)
            {
                return Ok(new { reply, category, sentiment });
            }

            var ticket = new Ticket
            {
                UserEmail = email,
                Message = message,
                Category = category,
                Priority = sentiment == "Negative" ? "High" : "Medium"
            };

            await _mongoService.CreateTicket(ticket);

            return Ok(new
            {
                reply = "Ticket created. Support will contact you.",
                category,
                sentiment
            });
        }
    }
}