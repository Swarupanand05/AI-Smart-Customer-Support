using System.Net;
using System.Net.Mail;

namespace SupportAPI.Services
{
    // ──────────────────────────────────────────────────────────────────────────
    // EmailService — Gmail SMTP notification service
    // Sends branded HTML emails for ticket events and account lifecycle.
    // ──────────────────────────────────────────────────────────────────────────
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Sends an HTML email via Gmail SMTP. Returns true if sent successfully, false otherwise.
        /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var host        = _config["Smtp:Host"] ?? "smtp.gmail.com";
                var port        = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
                var enableSsl   = bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) ? ssl : true;
                var senderEmail = _config["Smtp:SenderEmail"] ?? "supportaicustomer@gmail.com";
                var senderName  = _config["Smtp:SenderName"] ?? "AI Smart Customer Support";
                var appPassword = _config["Smtp:AppPassword"];

                if (string.IsNullOrWhiteSpace(appPassword) ||
                    appPassword.Contains("YOUR_") ||
                    appPassword.Length < 8)
                {
                    _logger.LogWarning("[EmailService] Gmail 16-character App Password not set in appsettings.json. Skipping email to {Email}", toEmail);
                    return false;
                }

                // Strip spaces in 16-character App Password if entered with spaces
                var cleanPassword = appPassword.Replace(" ", "").Trim();

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(senderEmail, cleanPassword),
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000 // 15 seconds
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                _logger.LogInformation("[EmailService] Email successfully delivered to {Email} with subject: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Failed to deliver email to {Email}: {Message}", toEmail, ex.Message);
                return false;
            }
        }

        // ── Template 1: Ticket Creation Confirmation ───────────────────────────
        public async Task SendTicketCreatedEmailAsync(
            string toEmail,
            string ticketId,
            string subject,
            string category,
            string priority)
        {
            var priorityColor = priority switch
            {
                "Critical" => "#ef4444",
                "High"     => "#f97316",
                "Medium"   => "#eab308",
                _          => "#10b981"
            };

            var html = $@"
            <!DOCTYPE html>
            <html>
            <head><meta charset='utf-8'></head>
            <body style='margin:0; padding:24px; font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,sans-serif; background-color:#0f172a; color:#e2e8f0;'>
              <div style='max-width:580px; margin:0 auto; background-color:#1e293b; border:1px solid #334155; border-radius:16px; overflow:hidden; box-shadow:0 10px 25px rgba(0,0,0,0.4);'>
                <div style='background:linear-gradient(135deg, #6366f1, #4f46e5); padding:28px 24px; text-align:center;'>
                  <h1 style='margin:0; font-size:22px; color:#ffffff; font-weight:700;'>🤖 AI Smart Customer Support</h1>
                  <p style='margin:6px 0 0 0; color:#c7d2fe; font-size:14px;'>Support Ticket Confirmation</p>
                </div>
                <div style='padding:28px 24px;'>
                  <p style='font-size:15px; color:#cbd5e1; margin-top:0;'>Hello,</p>
                  <p style='font-size:14px; color:#94a3b8; line-height:1.6;'>
                    Your support request has been logged into our system. Our AI resolution engine and support team have been notified.
                  </p>
                  
                  <div style='background-color:#0f172a; border:1px solid #334155; border-radius:12px; padding:18px; margin:20px 0;'>
                    <table style='width:100%; border-collapse:collapse; font-size:14px;'>
                      <tr>
                        <td style='padding:6px 0; color:#64748b;'>Ticket ID:</td>
                        <td style='padding:6px 0; font-weight:700; color:#38bdf8;'>#{ticketId}</td>
                      </tr>
                      <tr>
                        <td style='padding:6px 0; color:#64748b;'>Subject:</td>
                        <td style='padding:6px 0; font-weight:600; color:#f1f5f9;'>{subject}</td>
                      </tr>
                      <tr>
                        <td style='padding:6px 0; color:#64748b;'>Category:</td>
                        <td style='padding:6px 0; color:#e2e8f0;'>{category}</td>
                      </tr>
                      <tr>
                        <td style='padding:6px 0; color:#64748b;'>Priority:</td>
                        <td style='padding:6px 0;'>
                          <span style='background-color:{priorityColor}20; color:{priorityColor}; font-weight:700; padding:3px 10px; border-radius:999px; font-size:12px;'>{priority}</span>
                        </td>
                      </tr>
                    </table>
                  </div>

                  <p style='font-size:13px; color:#94a3b8; line-height:1.6;'>
                    You can check live ticket progress, add follow-ups, or chat with our AI anytime in your <strong>Customer Portal</strong>.
                  </p>
                </div>
                <div style='background-color:#0f172a; border-top:1px solid #334155; padding:16px 24px; text-align:center; font-size:12px; color:#64748b;'>
                  This is an automated notification from AI Smart Customer Support.<br/>
                  Sender: supportaicustomer@gmail.com
                </div>
              </div>
            </body>
            </html>";

            await SendEmailAsync(toEmail, $"[Ticket #{ticketId}] Request Received: {subject}", html);
        }

        // ── Template 2: Admin Status Update / Resolution ──────────────────────
        public async Task SendTicketStatusUpdateEmailAsync(
            string toEmail,
            string ticketId,
            string status,
            string? adminReply,
            string? priority = null)
        {
            var isResolved = status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) ||
                             status.Equals("Closed", StringComparison.OrdinalIgnoreCase);

            var statusColor = isResolved ? "#10b981" : "#38bdf8";

            var html = $@"
            <!DOCTYPE html>
            <html>
            <head><meta charset='utf-8'></head>
            <body style='margin:0; padding:24px; font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,sans-serif; background-color:#0f172a; color:#e2e8f0;'>
              <div style='max-width:580px; margin:0 auto; background-color:#1e293b; border:1px solid #334155; border-radius:16px; overflow:hidden; box-shadow:0 10px 25px rgba(0,0,0,0.4);'>
                <div style='background:linear-gradient(135deg, {(isResolved ? "#059669, #10b981" : "#0284c7, #38bdf8")}); padding:28px 24px; text-align:center;'>
                  <h1 style='margin:0; font-size:22px; color:#ffffff; font-weight:700;'>Ticket Update: {status}</h1>
                  <p style='margin:6px 0 0 0; color:#e0f2fe; font-size:14px;'>Ticket ID #{ticketId}</p>
                </div>
                <div style='padding:28px 24px;'>
                  <p style='font-size:15px; color:#cbd5e1; margin-top:0;'>Hello,</p>
                  <p style='font-size:14px; color:#94a3b8; line-height:1.6;'>
                    Your support ticket has been updated to: 
                    <span style='background-color:{statusColor}25; color:{statusColor}; font-weight:700; padding:3px 10px; border-radius:999px; font-size:12px;'>{status}</span>
                  </p>

                  {(string.IsNullOrWhiteSpace(adminReply) ? "" : $@"
                  <div style='background-color:#0f172a; border-left:4px solid {statusColor}; border-radius:0 8px 8px 0; padding:16px; margin:20px 0;'>
                    <p style='margin:0 0 6px 0; font-size:12px; font-weight:700; color:#94a3b8; text-transform:uppercase;'>Support Agent Reply:</p>
                    <p style='margin:0; font-size:14px; color:#f1f5f9; line-height:1.6;'>{adminReply}</p>
                  </div>")}

                  <p style='font-size:13px; color:#94a3b8; line-height:1.6;'>
                    If your issue has not been fully resolved, you can reply directly through your customer dashboard.
                  </p>
                </div>
                <div style='background-color:#0f172a; border-top:1px solid #334155; padding:16px 24px; text-align:center; font-size:12px; color:#64748b;'>
                  &copy; 2026 AI Smart Customer Support &bull; supportaicustomer@gmail.com
                </div>
              </div>
            </body>
            </html>";

            await SendEmailAsync(toEmail, $"[Update #{ticketId}] Status Changed: {status}", html);
        }

        // ── Template 3: Welcome Email ─────────────────────────────────────────
        public async Task SendWelcomeEmailAsync(string toEmail, string fullName)
        {
            var html = $@"
            <!DOCTYPE html>
            <html>
            <head><meta charset='utf-8'></head>
            <body style='margin:0; padding:24px; font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,sans-serif; background-color:#0f172a; color:#e2e8f0;'>
              <div style='max-width:580px; margin:0 auto; background-color:#1e293b; border:1px solid #334155; border-radius:16px; overflow:hidden;'>
                <div style='background:linear-gradient(135deg, #6366f1, #4f46e5); padding:28px 24px; text-align:center;'>
                  <h1 style='margin:0; font-size:22px; color:#ffffff;'>Welcome to AI Smart Support!</h1>
                </div>
                <div style='padding:28px 24px;'>
                  <p style='font-size:15px; color:#cbd5e1;'>Hello {fullName},</p>
                  <p style='font-size:14px; color:#94a3b8; line-height:1.6;'>
                    Thank you for signing up. Your account is active and you can now experience instant AI customer support, real-time ticket escalation, and multi-turn conversational assistance.
                  </p>
                </div>
                <div style='background-color:#0f172a; border-top:1px solid #334155; padding:16px 24px; text-align:center; font-size:12px; color:#64748b;'>
                  AI Smart Customer Support &bull; supportaicustomer@gmail.com
                </div>
              </div>
            </body>
            </html>";

            await SendEmailAsync(toEmail, "Welcome to AI Smart Customer Support", html);
        }
    }
}
