using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportAPI.Models;
using SupportAPI.Services;

namespace SupportAPI.Controllers
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly MongoService _mongoService;
        private readonly AIService _aiService;
        private readonly MLService _mlService;

        public ChatController(MongoService mongoService, AIService aiService, MLService mlService)
        {
            _mongoService = mongoService;
            _aiService = aiService;
            _mlService = mlService;
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest req)
        {
            // quick replies (FAQ)
            var reply = _aiService.GetReply(req.Message);

            // ML prediction (always)
            var (category, sentiment) = await _mlService.Predict(req.Message);
            var priority = sentiment == "Negative" ? "High" : "Medium";

            if (reply != null)
            {
                await _mongoService.SaveChat(new ChatMessage
                {
                    UserEmail = req.Email,
                    Message = req.Message,
                    Reply = reply,
                    Sentiment = sentiment
                });

                return Ok(new { reply, category, sentiment, ticketCreated = false });
            }

            var ticket = new Ticket
            {
                UserEmail = req.Email,
                Message = req.Message,
                Category = category,
                Priority = priority,
                Status = "Open"
            };

            await _mongoService.CreateTicket(ticket);

            await _mongoService.SaveChat(new ChatMessage
            {
                UserEmail = req.Email,
                Message = req.Message,
                Reply = "Ticket created",
                Sentiment = sentiment
            });

            return Ok(new
            {
                reply = "Your issue has been escalated. Ticket created.",
                category,
                sentiment,
                ticketCreated = true
            });
        }


    }
}