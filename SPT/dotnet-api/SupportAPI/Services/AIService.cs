using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportAPI.Services
{
    // ──────────────────────────────────────────────────────────────────────────
    // AIService
    // 1) GetReply()  → 30+ keyword-based FAQ answers (instant, no API call)
    // 2) GetGeminiReply() → Calls Google Gemini API for intelligent fallback
    // ──────────────────────────────────────────────────────────────────────────
    public class AIService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _groqClient;
        private readonly HttpClient _geminiClient;

        public AIService(IConfiguration config, IHttpClientFactory factory)
        {
            _config = config;
            _groqClient = factory.CreateClient("GroqClient");
            _geminiClient = factory.CreateClient("GeminiClient");
        }

        // ── 1. Keyword-based FAQ Matcher ─────────────────────────────────────
        public string? GetReply(string message)
        {
            var m = message.ToLower().Trim();

            // ── Greetings ────────────────────────────────────────────────────
            if (Contains(m, "hi", "hello", "hey", "good morning", "good afternoon",
                             "good evening", "howdy", "greetings"))
                return "👋 Hello! Welcome to AI Smart Customer Support. How can I help you today?";

            // ── How are you ──────────────────────────────────────────────────
            if (Contains(m, "how are you", "how r u", "how are u"))
                return "I'm doing great, thanks for asking! 😊 How can I assist you with your orders, payments, or returns today?";

            // ── Billing — UPI / Bank Debited but Order Failed ─────────────────
            if (Contains(m, "debited", "deducted", "paise kat gaye", "cut gaye", "failed but deducted", "charged but not placed", "money deducted"))
                return "💳 **Diagnosis: Payment Gateway Reconciliation Pending**\n\n" +
                       "• **What Happened**: Your bank debited the amount, but our payment gateway timed out before receiving the transaction webhook.\n" +
                       "• **Auto-Reconciliation SLA**: Under NPCI/RBI guidelines, if the transaction settles, your order auto-confirms within **2 hours**.\n" +
                       "• **Auto-Reversal SLA**: If unsettled, the funds will auto-reverse to your source bank/UPI account in **24 to 48 business hours**.\n" +
                       "• **Action Required**: Please share your 12-digit **UPI Reference / Bank UTR Number** so we can track and safeguard your payment immediately.";

            // ── Billing — Payment Failed (Generic) ────────────────────────────
            if (Contains(m, "payment failed", "payment not processed", "payment error",
                             "charge failed", "transaction failed", "payment declined"))
                return "💳 **Diagnosis: Transaction Declined by Bank / Gateway**\n\n" +
                       "• **Troubleshooting Checklist**:\n" +
                       "  1. Verify your daily card/UPI transaction limit is not exceeded\n" +
                       "  2. Check if international/online transactions are enabled in your bank app\n" +
                       "  3. Retry using an alternative payment method (UPI, NetBanking, or Credit Card)\n\n" +
                       "• **Note**: No amount was captured for this attempt. Would you like a payment retry link?";

            // ── Billing — Refund ─────────────────────────────────────────────
            if (Contains(m, "refund", "money back", "get my money", "reimbursement",
                             "refund not received", "refund status"))
                return "💰 **Diagnosis: Refund Processing Timeline & Status**\n\n" +
                       "• **Refund SLAs by Payment Method**:\n" +
                       "  • **UPI / Wallet**: 2 to 24 business hours\n" +
                       "  • **NetBanking / Debit Card**: 3 to 5 business days\n" +
                       "  • **Credit Card**: 5 to 7 business days\n" +
                       "  • **Cash on Delivery (COD)**: Instant transfer once you provide your Bank Account / UPI ID\n\n" +
                       "• **Next Steps**: Share your **Order ID** (e.g., `OD-492019`) to view the real-time ARN/RRN banking reference number.";

            // ── Billing — Invoice / Receipt ──────────────────────────────────
            if (Contains(m, "invoice", "receipt", "bill", "billing statement", "tax invoice", "gst"))
                return "🧾 **Diagnosis: Tax & GST Invoice Generation**\n\n" +
                       "• You can download the official GST invoice directly from:\n" +
                       "  **Dashboard → My Orders → Select Order → Download Invoice (PDF)**\n\n" +
                       "• For business GST input credit, ensure your GSTIN was entered during checkout.\n" +
                       "Need help downloading? Share your registered email or Order ID!";

            // ── Shipping — Marked Delivered but Not Received (DNR) ────────────
            if (Contains(m, "marked delivered", "shows delivered", "says delivered", "not received", "didn't receive", "never received", "fake delivery"))
                return "📦 **Diagnosis: Delivered but Not Received (DNR) Investigation**\n\n" +
                       "• **Immediate Checklist**:\n" +
                       "  1. Please check with your security guard, building reception, or household members\n" +
                       "  2. Verify whether a delivery OTP was shared on your phone\n" +
                       "  3. Courier partners sometimes log deliveries in batch at end-of-day; packages frequently arrive by next morning\n\n" +
                       "• **Resolution SLA**: If untraceable after **24 hours**, our logistics security team initiates an automatic refund or priority replacement within **48 hours**.\n" +
                       "• Please share your **Order ID** so we can pull the GPS delivery pin.";

            // ── Shipping — Order Tracking / Live Status ───────────────────────
            if (Contains(m, "shipping", "delivery", "package", "parcel", "order status",
                             "track order", "track my order", "track", "where is my order", "where is my package", "order tracking", "when will it arrive", "tracking"))
                return "🚚 **Diagnosis: Live Logistics & Milestone Status**\n\n" +
                       "• **Delivery Timelines**:\n" +
                       "  • **Express Delivery**: 1 to 2 business days\n" +
                       "  • **Standard Delivery**: 3 to 5 business days\n" +
                       "• **Live Milestone**: You can track transit hubs live under **Dashboard → My Orders**.\n\n" +
                       "• Please share your **Order ID** (e.g. `OD-782103`) to check live courier vehicle dispatch status.";

            // ── Returns — Damaged / Defective on Arrival (DOA) ─────────────────
            if (Contains(m, "damaged", "broken", "defective", "tuta hua", "wrong item", "empty box", "not working", "cracked"))
                return "↩️ **Diagnosis: Damaged / Defective on Delivery (DOA)**\n\n" +
                       "• **Coverage**: Fully protected under our **7-Day Hassle-Free Replacement Policy**.\n" +
                       "• **Required Steps**:\n" +
                       "  1. Keep the outer box packaging and manufacturer serial number intact\n" +
                       "  2. We will schedule a doorstep courier pickup with reverse inspection\n\n" +
                       "• **Replacement SLA**: Your replacement item will be dispatched within **24 hours** of pickup confirmation.\n" +
                       "• Please provide your **Order ID** so we can schedule the doorstep exchange.";

            // ── Returns — Return & Exchange Policy ───────────────────────────
            if (Contains(m, "return", "send back", "exchange", "replacement", "wrong size", "size exchange"))
                return "🔄 **Diagnosis: Return & Doorstep Exchange Protocol**\n\n" +
                       "• **Eligibility**: Eligible within the 7-day to 30-day return window in original condition.\n" +
                       "• **Doorstep Pickup SLA**: Courier arrives within **24 to 48 business hours** of request (9 AM - 7 PM).\n" +
                       "• **Refund Trigger**: Instant refund initiated as soon as the courier scans the return barcode.\n\n" +
                       "• Initiate now: **Orders → Select Item → Return/Exchange**, or share your Order ID here!";

            // ── Promo & Coupons ───────────────────────────────────────────────
            if (Contains(m, "promo code", "coupon", "discount", "voucher", "cashback", "supercoins", "offer not applied"))
                return "🏷️ **Diagnosis: Promotional & Discount Code Inquiry**\n\n" +
                       "• **Common Checks**:\n" +
                       "  1. Minimum cart value must be met (excluding shipping fees)\n" +
                       "  2. Bank instant discount offers (e.g., 10% off) apply exclusively to eligible card types\n" +
                       "  3. Coupons cannot be stacked with certain flash sale prices\n\n" +
                       "• **Cashback SLA**: Auto-credited to your wallet/account within **24 hours** of delivery.\n" +
                       "• Share your coupon code and order total if you need assistance applying it!";

            // ── Account — Login Issues ────────────────────────────────────────
            if (Contains(m, "can't login", "cannot login", "login failed", "login problem",
                             "sign in issue", "unable to login", "login error", "cant sign in"))
                return "🔐 **Diagnosis: Account Authentication Issue**\n\n" +
                       "• **Quick Fixes**:\n" +
                       "  1. Check for typos in your registered email / phone number\n" +
                       "  2. Clear browser cache or try an incognito browsing window\n" +
                       "  3. Use the 'Forgot Password' link to generate a secure 1-time reset link (valid 30 mins)\n\n" +
                       "• If your account was suspended or locked, type 'escalate' and our security desk will assist you.";

            // ── Account — Password Reset ─────────────────────────────────────
            if (Contains(m, "forgot password", "forgot my password", "forgot",
                             "reset password", "change password",
                             "password reset", "password issue", "lost password"))
                return "🔑 **Diagnosis: Password Recovery & 2FA Reset**\n\n" +
                       "1. Click 'Forgot Password' on the login screen\n" +
                       "2. Enter your registered email address\n" +
                       "3. Check your inbox and spam folder for the reset email (expires in 30 minutes)\n" +
                       "4. Choose a strong password with at least 8 characters\n\n" +
                       "Need a direct reset link? Let us know your registered email.";

            // ── Account — Two-Factor Auth ─────────────────────────────────────
            if (Contains(m, "two factor", "2fa", "otp", "verification code",
                             "authentication code", "otp not received"))
                return "🛡️ **Diagnosis: OTP / 2-Factor Authentication Failure**\n\n" +
                       "• **SMS Troubleshooting**:\n" +
                       "  • Check if telecom DND (Do Not Disturb) is delaying transactional messages\n" +
                       "  • Wait 60 seconds before clicking 'Resend OTP'\n" +
                       "  • Ensure your SIM has active cellular network reception\n\n" +
                       "• We can also send the OTP to your registered email address upon request.";

            // ── Privacy / Data ────────────────────────────────────────────────
            if (Contains(m, "privacy", "data", "gdpr", "personal information",
                             "my data", "data breach", "security"))
                return "🔒 **Diagnosis: Data Privacy & Security Protocol**\n\n" +
                       "• All sensitive customer and payment data is encrypted with AES-256 and TLS 1.3 standards.\n" +
                       "• Manage your privacy preferences under Settings → Privacy & Security.\n" +
                       "• For security audits or suspected unauthorized logins, our team freezes active sessions immediately.";

            // ── Thank you / Positive ─────────────────────────────────────────
            if (Contains(m, "thank you", "thanks", "thank u",
                             "appreciate", "very helpful", "great help", "awesome help"))
                return "😊 You're very welcome! I'm happy I could help.\n" +
                       "Is there anything else I can assist you with today?";

            // ── Goodbye ──────────────────────────────────────────────────────
            if (Contains(m, "bye", "goodbye", "good bye", "see you", "later",
                             "take care", "that's all", "thats all", "nothing else"))
                return "👋 Thank you for contacting AI Support! Have a wonderful day!\n" +
                       "Feel free to return anytime if you need help. 😊";

            // ── Escalate to Human ────────────────────────────────────────────
            if (Contains(m, "human", "agent", "person", "real person", "speak to",
                             "talk to", "escalate", "supervisor", "manager"))
                return "👥 I'm connecting you with a human support agent now.\n" +
                       "• Average wait time: 2–5 minutes\n" +
                       "• Available: Mon–Sat, 9 AM – 6 PM IST\n\n" +
                       "I'll also create a support ticket so your issue is documented.";

            // No FAQ match — return null → Gemini handles it
            return null;
        }

        // ── Helper: multi-keyword Contains with whole-word protection for short words ──
        private static bool Contains(string message, params string[] keywords)
        {
            return keywords.Any(k =>
            {
                if (k.Length <= 3)
                    return System.Text.RegularExpressions.Regex.IsMatch(message, $@"\b{System.Text.RegularExpressions.Regex.Escape(k)}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return message.Contains(k, StringComparison.OrdinalIgnoreCase);
            });
        }


        // ── 2. Multi-Provider AI Fallback (Groq Primary + Gemini Secondary) ─
        public async Task<string> GetGroqReply(
            string userMessage, 
            string category, 
            string sentiment, 
            List<(string role, string content)>? conversationHistory = null)
        {
            return await GetAIReply(userMessage, category, sentiment, conversationHistory);
        }

        public async Task<string> GetAIReply(
            string userMessage, 
            string category, 
            string sentiment, 
            List<(string role, string content)>? conversationHistory = null)
        {
            // Build a customer-support focused system prompt with native multilingual capability
            var systemPrompt =
                "You are an expert, empathetic AI customer support assistant for an e-commerce platform. " +
                "Automatically detect the user's language (e.g., Hindi, Marathi, English, Spanish, German, French) and reply fluently in that exact same language. " +
                "Keep replies concise (max 3-4 sentences or a short bullet list). " +
                "Be professional, friendly, and solution-focused. " +
                "When a user provides an Order ID, reference number, or transaction detail following an inquiry (such as cancellation, refund, return, or order tracking): " +
                "specifically acknowledge that Order ID, confirm that the action (e.g., cancellation or status check) is initiated, provide clear timeline expectations (e.g., confirmation email within 2-4 hours, refund within 3-5 business days), and reassure the user. " +
                $"The user's issue is classified as: Category={category}, Sentiment={sentiment}. " +
                "If the issue is complex or negative sentiment, offer to create a support ticket.";

            // ── Step 2a: Try Primary Provider — Groq API (Ultra-Fast LPU) ────
            var groqReply = await TryGroqModels(userMessage, systemPrompt, conversationHistory);
            if (!string.IsNullOrWhiteSpace(groqReply))
            {
                return groqReply;
            }

            // ── Step 2b: Try Secondary Provider — Google Gemini API ──────────
            Console.WriteLine("[AIService] Groq unavailable. Failing over to Google Gemini API...");
            var geminiReply = await TryGeminiModels(userMessage, systemPrompt, conversationHistory);
            if (!string.IsNullOrWhiteSpace(geminiReply))
            {
                return geminiReply;
            }

            // ── Step 2c: Intelligent Contextual Diagnostic Fallback ──────────
            Console.WriteLine("[AIService] External LLMs unavailable. Using domain-expert resolution fallback.");
            return GetContextualDiagnosticFallback(userMessage, category, sentiment);
        }

        private static string GetContextualDiagnosticFallback(string message, string category, string sentiment)
        {
            var isNegative = sentiment.Equals("Negative", StringComparison.OrdinalIgnoreCase);

            return category switch
            {
                "Shipping" => isNegative
                    ? "📦 **Diagnosis: Shipment Delay / Delivery Escalation**\n\n" +
                      "• **Action Taken**: We have marked your delivery for priority escalation with our logistics carrier partner.\n" +
                      "• **Checklist**:\n" +
                      "  1. Verify if an OTP or SMS was received from the delivery executive.\n" +
                      "  2. Check with neighbors or reception if the parcel was dropped safely.\n" +
                      "• **SLA**: If your package is not delivered within 24 hours, an instant replacement or 100% refund is initiated automatically. Please share your **Order ID**."
                    : "🚚 **Diagnosis: Shipment Tracking & Logistics Status**\n\n" +
                      "• Your order is moving through our fulfillment logistics network.\n" +
                      "• Standard delivery takes 2-4 business days. You can track live milestone updates under **My Orders**.\n" +
                      "• Please share your **Order ID** if you need the courier AWB tracking number.",

                "Billing" => isNegative
                    ? "💳 **Diagnosis: Payment Failure / Deducted Funds Reversal**\n\n" +
                      "• **Reversal Guarantee**: If your bank debited the amount but the order was not placed, the money is safe in the banking settlement gateway.\n" +
                      "• **SLA**: Funds auto-reverse to your source bank or UPI account within **24 to 48 business hours**.\n" +
                      "• **Next Step**: Please provide your 12-digit **UPI Reference Number (UTR)** or transaction ID so we can verify the status immediately."
                    : "🧾 **Diagnosis: Billing & Invoice Assistance**\n\n" +
                      "• You can view invoices and transaction receipts directly under **Dashboard → My Orders**.\n" +
                      "• For GST business invoices or payment method updates, please provide your registered Order ID.",

                "Returns" =>
                    "↩️ **Diagnosis: Return & Replacement Policy**\n\n" +
                    "• **7-Day Hassle-Free Window**: Eligible products can be returned or replaced directly from your dashboard.\n" +
                    "• **Doorstep Pickup**: Our courier will pick up the item from your delivery address within 24-48 hours.\n" +
                    "• **Refund Timeline**: Refunds are credited within 2-4 hours of successful doorstep inspection.",

                "Account" =>
                    "🔒 **Diagnosis: Account Security & Access**\n\n" +
                    "• If you are experiencing login or OTP issues, please verify your registered email and mobile number.\n" +
                    "• You can reset your password using the **Forgot Password** link on the login page.\n" +
                    "• For account unlocking or security reviews, our support team can verify your identity via email.",

                "Technical" =>
                    "⚙️ **Diagnosis: App & System Troubleshooting**\n\n" +
                    "• Please try clearing your browser cache or restarting the app.\n" +
                    "• Ensure your internet connection is stable and disable active VPNs during checkout.\n" +
                    "• If the problem persists, our technical engineering team can inspect your session logs.",

                _ =>
                    "💬 **Diagnosis: Customer Support Assistance**\n\n" +
                    "• Thank you for reaching out. We have analyzed your query and our customer support team is ready to assist you.\n" +
                    "• Please provide your **Order ID** or additional details so we can resolve this promptly for you!"
            };
        }

        private async Task<string?> TryGroqModels(
            string userMessage, 
            string systemPrompt, 
            List<(string role, string content)>? conversationHistory = null)
        {
            try
            {
                var apiKey = _config["Groq:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GROQ_API_KEY_HERE")
                    return null;

                // Priority list of active models on Groq (verified active and fast)
                var models = new[]
                {
                    "qwen/qwen3.8-27b",
                    "openai/gpt-oss-120b",
                    "openai/gpt-oss-20b",
                    "groq/compound-mini"
                };

                var msgList = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                if (conversationHistory != null && conversationHistory.Count > 0)
                {
                    foreach (var turn in conversationHistory.TakeLast(6))
                    {
                        var r = turn.role.Equals("bot", StringComparison.OrdinalIgnoreCase) || turn.role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                            ? "assistant"
                            : "user";
                        msgList.Add(new { role = r, content = turn.content });
                    }
                }

                msgList.Add(new { role = "user", content = userMessage });

                foreach (var model in models)
                {
                    try
                    {
                        var payload = new
                        {
                            model,
                            messages    = msgList.ToArray(),
                            temperature = 0.4,
                            max_tokens  = 300,
                            top_p       = 0.9,
                        };

                        var json = JsonSerializer.Serialize(payload);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        _groqClient.DefaultRequestHeaders.Remove("Authorization");
                        _groqClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                        var response = await _groqClient.PostAsync(
                            "https://api.groq.com/openai/v1/chat/completions", content);

                        if (response.IsSuccessStatusCode)
                        {
                            var resultJson = await response.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(resultJson);

                            var text = doc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                Console.WriteLine($"[Groq] Success with model: {model}");
                                return text.Trim();
                            }
                        }
                        else
                        {
                            var err = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"[Groq] Model {model} returned status {(int)response.StatusCode}: {err[..Math.Min(120, err.Length)]}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Groq] Model {model} exception: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Groq] Unexpected error: {ex.Message}");
            }
            return null;
        }

        private async Task<string?> TryGeminiModels(
            string userMessage, 
            string systemPrompt, 
            List<(string role, string content)>? conversationHistory = null)
        {
            try
            {
                var apiKey = _config["Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY_HERE")
                    return null;

                var models = new[]
                {
                    "models/gemini-flash-latest",
                    "models/gemini-flash-lite-latest",
                    "models/gemini-2.5-flash-lite",
                    "models/gemini-3.5-flash",
                    "models/gemini-3.5-flash-lite",
                    "models/gemini-3.7-flash",
                    "models/gemini-3.6-flash"
                };

                var historyBuilder = new StringBuilder();
                if (conversationHistory != null && conversationHistory.Count > 0)
                {
                    historyBuilder.AppendLine("Previous conversation context:");
                    foreach (var turn in conversationHistory.TakeLast(6))
                    {
                        var speaker = turn.role.Equals("bot", StringComparison.OrdinalIgnoreCase) || turn.role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                            ? "Assistant"
                            : "Customer";
                        historyBuilder.AppendLine($"{speaker}: {turn.content}");
                    }
                    historyBuilder.AppendLine();
                }

                var fullPrompt = $"{systemPrompt}\n\n{historyBuilder}Customer Inquiry: {userMessage}\nAssistant:";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = fullPrompt }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);

                foreach (var model in models)
                {
                    try
                    {
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        var url = $"https://generativelanguage.googleapis.com/v1beta/{model}:generateContent?key={apiKey}";

                        var response = await _geminiClient.PostAsync(url, content);

                        if (response.IsSuccessStatusCode)
                        {
                            var resultJson = await response.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(resultJson);

                            var candidates = doc.RootElement.GetProperty("candidates");
                            if (candidates.GetArrayLength() > 0)
                            {
                                var parts = candidates[0].GetProperty("content").GetProperty("parts");
                                if (parts.GetArrayLength() > 0)
                                {
                                    var text = parts[0].GetProperty("text").GetString();
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        Console.WriteLine($"[Gemini] Fallback success with model: {model}");
                                        return text.Trim();
                                    }
                                }
                            }
                        }
                        else
                        {
                            var err = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"[Gemini] Model {model} returned status {(int)response.StatusCode}: {err[..Math.Min(120, err.Length)]}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Gemini] Model {model} exception: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gemini] Unexpected error: {ex.Message}");
            }
            return null;
        }

        // ── Legacy methods kept for backward compatibility ────────────────────
        public string GetCategory(string message)
        {
            message = message.ToLower();
            if (message.Contains("payment") || message.Contains("refund")) return "Billing";
            if (message.Contains("error")   || message.Contains("crash"))  return "Technical";
            if (message.Contains("login")   || message.Contains("account"))return "Account";
            return "General";
        }

        public string GetSentiment(string message)
        {
            message = message.ToLower();
            if (message.Contains("angry") || message.Contains("worst")) return "Negative";
            if (message.Contains("good")  || message.Contains("thanks"))return "Positive";
            return "Neutral";
        }

        public string GetPriority(string sentiment)
            => sentiment == "Negative" ? "High" : "Medium";
    }
}