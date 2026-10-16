using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportAPI.Services
{
    // ──────────────────────────────────────────────────────────────────────────
    // MLService — calls the Python Flask ML API on port 5001
    // Returns category, sentiment and confidence scores
    // ──────────────────────────────────────────────────────────────────────────
    public class MLService
    {
        private readonly HttpClient _httpClient;
        private const string ML_ENDPOINT = "http://127.0.0.1:5001/predict";

        public MLService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        // ── Predict Category + Sentiment + Priority (with context-aware fallback) ───
        public async Task<(string category, string sentiment, string priority, double categoryConf, double sentimentConf)>
            Predict(string message, string? contextCategory = null, string? contextPriority = null)
        {
            try
            {
                var body = JsonSerializer.Serialize(new { message });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ML_ENDPOINT, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[MLService] Non-success status: {response.StatusCode} — executing controlled fallback.");
                    return ExecuteControlledFallback(message, contextCategory, contextPriority);
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MLResponse>(json);

                return (
                    result?.Category      ?? "General",
                    result?.Sentiment     ?? "Neutral",
                    result?.Priority      ?? "Medium",
                    result?.CategoryConf  ?? 0.5,
                    result?.SentimentConf ?? 0.5
                );
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[MLService] Request timed out — ML API may be starting or under load. Using controlled fallback.");
                return ExecuteControlledFallback(message, contextCategory, contextPriority);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MLService] Exception: {ex.Message} — executing controlled fallback.");
                return ExecuteControlledFallback(message, contextCategory, contextPriority);
            }
        }

        // ── Controlled Rule-Based Fallback Strategy ───────────────────────────
        private static (string category, string sentiment, string priority, double categoryConf, double sentimentConf)
            ExecuteControlledFallback(string message, string? contextCategory, string? contextPriority)
        {
            // 1. If active session established a domain category, preserve it
            if (!string.IsNullOrWhiteSpace(contextCategory) && !contextCategory.Equals("General", StringComparison.OrdinalIgnoreCase))
            {
                var prio = !string.IsNullOrWhiteSpace(contextPriority) ? contextPriority : "Medium";
                return (contextCategory, "Neutral", prio, 0.70, 0.65);
            }

            var m = message.ToLowerInvariant();

            // 2. Detect negative sentiment indicators
            bool isNegative = m.Contains("failed") || m.Contains("error") || m.Contains("broken") ||
                              m.Contains("worst") || m.Contains("terrible") || m.Contains("not working") ||
                              m.Contains("deducted") || m.Contains("hacked") || m.Contains("stuck") ||
                              m.Contains("charged twice") || m.Contains("urgent");

            string sentiment = isNegative ? "Negative" : "Neutral";
            double sentConf = isNegative ? 0.75 : 0.60;

            // 3. Keyword-based category heuristic
            if (m.Contains("payment") || m.Contains("refund") || m.Contains("charge") ||
                m.Contains("bill") || m.Contains("invoice") || m.Contains("upi") || m.Contains("deducted"))
            {
                return ("Billing", sentiment, isNegative ? "Critical" : "Medium", 0.70, sentConf);
            }

            if (m.Contains("return") || m.Contains("exchange") || m.Contains("replacement") || m.Contains("cancel"))
            {
                return ("Returns", sentiment, isNegative ? "High" : "Medium", 0.70, sentConf);
            }

            if (m.Contains("order") || m.Contains("deliver") || m.Contains("track") ||
                m.Contains("courier") || m.Contains("parcel") || m.Contains("shipment"))
            {
                return ("Shipping", sentiment, isNegative ? "High" : "Medium", 0.70, sentConf);
            }

            if (m.Contains("login") || m.Contains("password") || m.Contains("otp") ||
                m.Contains("account") || m.Contains("profile") || m.Contains("2fa"))
            {
                return ("Account", sentiment, isNegative ? "Critical" : "Medium", 0.70, sentConf);
            }

            if (m.Contains("crash") || m.Contains("bug") || m.Contains("server") ||
                m.Contains("api") || m.Contains("slow") || m.Contains("not loading"))
            {
                return ("Technical", sentiment, isNegative ? "High" : "Medium", 0.70, sentConf);
            }

            if (m.Contains("promo") || m.Contains("coupon") || m.Contains("discount") || m.Contains("voucher"))
            {
                return ("Promo", sentiment, "Low", 0.65, sentConf);
            }

            if (m.Contains("spec") || m.Contains("warranty") || m.Contains("compatible") || m.Contains("feature"))
            {
                return ("Product", sentiment, "Low", 0.65, sentConf);
            }

            // Unrecognized query -> General (conservative confidence)
            return ("General", "Neutral", "Low", 0.50, 0.50);
        }
    }

    // ── Response DTO ──────────────────────────────────────────────────────────
    public class MLResponse
    {
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("sentiment")]
        public string? Sentiment { get; set; }

        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        [JsonPropertyName("category_confidence")]
        public double CategoryConf { get; set; }

        [JsonPropertyName("sentiment_confidence")]
        public double SentimentConf { get; set; }
    }
}
