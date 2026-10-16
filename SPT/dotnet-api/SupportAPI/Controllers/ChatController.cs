using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportAPI.Models;
using SupportAPI.Services;

namespace SupportAPI.Controllers
{
    // ── Request DTO ───────────────────────────────────────────────────────────
    public class ChatRequest
    {
        public string Message   { get; set; } = string.Empty;
        public string Email     { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ChatController
    // POST api/chat  → send message, get AI reply
    // GET  api/chat  → retrieve chat history for logged-in user
    // ──────────────────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly MongoService _mongoService;
        private readonly AIService   _aiService;
        private readonly MLService   _mlService;

        public ChatController(
            MongoService mongoService,
            AIService    aiService,
            MLService    mlService)
        {
            _mongoService = mongoService;
            _aiService    = aiService;
            _mlService    = mlService;
        }

        // ── POST api/chat ─────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Message))
                return BadRequest(new { error = "Message cannot be empty." });

            // Ensure valid user email
            var userEmail = req.Email;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                         ?? User.FindFirst("email")?.Value
                         ?? User.Identity?.Name;
            }
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                userEmail = "guest@portal.com";
            }

            var trimmedMsg = req.Message.Trim();

            // ── Step 1: Detect greeting early ───────────────────────────────────────
            bool isGreeting = IsCasualGreeting(trimmedMsg);

            // ── Step 1b: Retrieve Session History & Multi-Turn Context ─────────────
            var conversationHistory = new List<(string role, string content)>();
            string? previousCategory = null;
            string? previousPriority = null;
            string? lastUserQuery = null;

            if (!string.IsNullOrWhiteSpace(req.SessionId))
            {
                try
                {
                    var session = await _mongoService.GetSession(req.SessionId);
                    if (session != null && session.Messages != null && session.Messages.Count > 0)
                    {
                        // Look for the last non-General bot classification in this session
                        var lastClassifiedBotMsg = session.Messages
                            .LastOrDefault(m => m.From == "bot" && !string.IsNullOrWhiteSpace(m.Category) && !m.Category.Equals("General", StringComparison.OrdinalIgnoreCase));

                        if (lastClassifiedBotMsg != null)
                        {
                            previousCategory = lastClassifiedBotMsg.Category;
                            previousPriority = lastClassifiedBotMsg.Priority;
                        }

                        // Last user query before this one
                        var lastUserMsg = session.Messages
                            .Where(m => m.From == "user" && m.Text != req.Message)
                            .LastOrDefault();
                        if (lastUserMsg != null)
                        {
                            lastUserQuery = lastUserMsg.Text;
                        }

                        // Extract prior conversation turns for LLM (exclude the current message if already saved)
                        var priorMessages = session.Messages
                            .Where(m => !(m.From == "user" && m.Text == req.Message && m == session.Messages.LastOrDefault()))
                            .TakeLast(6);

                        foreach (var m in priorMessages)
                        {
                            if (!string.IsNullOrWhiteSpace(m.Text))
                            {
                                conversationHistory.Add((m.From, m.Text));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChatController] Error fetching session context: {ex.Message}");
                }
            }

            // Detect Order ID format: OD-12345, ORD-12345, ORDER12345, or isolated digits (5 to 14 digits)
            bool isOrderId = System.Text.RegularExpressions.Regex.IsMatch(trimmedMsg, @"\b(OD|ORD|ORDER)[-_#]?\d+\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                          || System.Text.RegularExpressions.Regex.IsMatch(trimmedMsg, @"^\d{5,14}$")
                          || (trimmedMsg.Length < 30 && System.Text.RegularExpressions.Regex.IsMatch(trimmedMsg, @"\b\d{5,14}\b"));

            int wordCount = trimmedMsg.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            bool isFollowUpPhrase = System.Text.RegularExpressions.Regex.IsMatch(trimmedMsg, @"\b(next\s+steps?|guide\s+me|what\s+next|how\s+to\s+proceed|what\s+should\s+i\s+do|help\s+me\s+with\s+that)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            bool isShortFollowUp = (isOrderId || wordCount <= 4 || isFollowUpPhrase) && !isGreeting && !string.IsNullOrWhiteSpace(previousCategory);

            // ── Step 2: ML Prediction & Contextual Continuity ────────────────
            string category, sentiment, mlPriority;
            double catConf, sentConf;

            if (isGreeting)
            {
                // Greetings are always General/Neutral/Low — skip the ML call
                category   = "General";
                sentiment  = "Neutral";
                mlPriority = "Low";
                catConf    = 1.0;
                sentConf   = 1.0;
            }
            else if ((isOrderId || isShortFollowUp) && !string.IsNullOrWhiteSpace(previousCategory))
            {
                // Context Continuity: inherit category and priority from preceding turn in session
                category   = previousCategory;
                sentiment  = "Neutral";
                mlPriority = !string.IsNullOrWhiteSpace(previousPriority) ? previousPriority : "Medium";
                catConf    = 0.95;
                sentConf   = 0.95;
            }
            else
            {
                // Predict using ML. If follow-up has few words, append last user query for rich semantic context
                string queryToPredict = (wordCount <= 3 && !string.IsNullOrWhiteSpace(lastUserQuery))
                    ? $"{lastUserQuery} {req.Message}"
                    : req.Message;

                (category, sentiment, mlPriority, catConf, sentConf) =
                    await _mlService.Predict(queryToPredict, previousCategory, previousPriority);

                // Fallback guard: If ML predicted "General" but we have an active domain category from this session
                if (category.Equals("General", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(previousCategory))
                {
                    category = previousCategory;
                    catConf  = 0.88;
                    if (!string.IsNullOrWhiteSpace(previousPriority)) mlPriority = previousPriority;
                }
            }

            // ── Step 3: Priority — prefer ML-derived; fallback to rule matrix ────────────
            var priority = !string.IsNullOrWhiteSpace(mlPriority)
                ? mlPriority
                : GetSmartPriority(sentiment, category);

            // ── Step 4: Get Reply from FAQ or Groq ───────────────────────────
            // For Order IDs or follow-ups, skip FAQ and let contextual Groq handle the conversation
            string? faqReply = null;
            if (!isOrderId && !isShortFollowUp)
            {
                faqReply = _aiService.GetReply(req.Message);
            }

            string finalReply;
            if (faqReply != null)
            {
                finalReply = faqReply;
            }
            else
            {
                finalReply = await _aiService.GetGroqReply(req.Message, category, sentiment, conversationHistory);
            }

            // ── Step 5: Determine if greeting (no feedback needed for greetings) ──
            bool isFaq = faqReply != null;

            // Save chat message in MongoDB — fire-and-forget; don't let DB errors crash the response
            try
            {
                await _mongoService.SaveChat(new ChatMessage
                {
                    UserEmail = userEmail,
                    Message   = req.Message,
                    Reply     = finalReply,
                    Sentiment = sentiment
                });
            }
            catch (Exception dbEx)
            {
                // Log but don't crash — MongoDB may be unavailable
                Console.WriteLine($"[ChatController] MongoDB SaveChat failed: {dbEx.Message}");
            }

            // ── Step 6: Return response ───────────────────────────────────────
            return Ok(new
            {
                reply          = finalReply,
                category,
                sentiment,
                categoryConf   = Math.Round(catConf  * 100, 1),
                sentimentConf  = Math.Round(sentConf * 100, 1),
                priority,
                ticketCreated  = false,   // ticket only created on user request via frontend
                isFaq,
                isGreeting
            });
        }

        // ── Smart Priority Matrix (Bug Fix #2) ────────────────────────────
        /// <summary>
        /// Determines priority from both sentiment AND category:
        /// • Critical — Negative Billing or Account (fraud, hacked, payment failure)
        /// • High     — Negative Technical or Shipping issues
        /// • Medium   — Neutral queries with Shipping/Technical context
        /// • Low      — Positive sentiment or General enquiries
        /// </summary>
        private static string GetSmartPriority(string sentiment, string category)
        {
            return (sentiment, category) switch
            {
                ("Negative", "Billing")   => "Critical",
                ("Negative", "Account")   => "Critical",
                ("Negative", "Technical") => "High",
                ("Negative", "Shipping")  => "High",
                ("Negative", "Returns")   => "High",
                ("Negative", "Promo")     => "Medium",
                ("Negative", "Product")   => "Medium",
                ("Negative", _)           => "High",
                ("Neutral",  "Technical") => "Medium",
                ("Neutral",  "Shipping")  => "Medium",
                ("Neutral",  "Billing")   => "Medium",
                ("Neutral",  "Account")   => "Medium",
                ("Neutral",  "Returns")   => "Medium",
                ("Neutral",  "Promo")     => "Low",
                ("Neutral",  "Product")   => "Low",
                ("Neutral",  _)           => "Low",
                ("Positive", _)           => "Low",
                _                         => "Medium"
            };
        }

        private static bool IsCasualGreeting(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return true;
            var m = msg.Trim().ToLower().TrimEnd('.', '!', '?', ',', ' ');

            string[] greetings = {
                "hi", "hello", "hey", "hello there", "hi there", "hey there", "good morning", "good afternoon", "good evening",
                "howdy", "greetings", "how are you", "how r u", "how are u",
                "thank you", "thanks", "thank u", "bye", "goodbye", "see you", "take care",
                "नमस्ते", "नमस्कार", "प्रणाम", "शुभ प्रभात", "धन्यवाद", "शुक्रिया",
                "hola", "buenos dias", "gracias", "bonjour", "merci"
            };
            return greetings.Any(g => m == g || m.StartsWith(g + " ") || m.EndsWith(" " + g));
        }


        // ── GET api/chat  (chat history for logged-in user) ───────────────────
        [HttpGet]
        public async Task<IActionResult> GetHistory([FromQuery] string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { error = "Email is required." });

            var chats = await _mongoService.GetChatsByEmail(email);
            return Ok(chats);
        }
    }
}
