namespace SupportAPI.Services
{
    public class AIService
    {
        public string GetCategory(string message)
        {
            message = message.ToLower();

            if (message.Contains("payment") || message.Contains("refund"))
                return "Billing";

            if (message.Contains("error") || message.Contains("crash"))
                return "Technical";

            if (message.Contains("login") || message.Contains("account"))
                return "Account";

            return "General";
        }

        public string GetSentiment(string message)
        {
            message = message.ToLower();

            if (message.Contains("angry") || message.Contains("worst"))
                return "Negative";

            if (message.Contains("good") || message.Contains("thanks"))
                return "Positive";

            return "Neutral";
        }

        public string GetReply(string message)
        {
            message = message.ToLower();

            if (message.Contains("hi") || message.Contains("hello"))
                return "Hello! How can I help you today?";

            if (message.Contains("payment"))
                return "Please check billing section.";

            return null;
        }
    }
}