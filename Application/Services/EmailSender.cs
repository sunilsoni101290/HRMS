using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Application.Services
{
    // Reads SMTP settings from configuration ("Smtp" section - see
    // API/appsettings.json). If Smtp:Host is empty/missing this is a no-op
    // by design: this sandboxed environment has no real mail server, and a
    // workflow (Leave Apply/Approve/Reject/...) must never fail, retry-loop,
    // or throw just because outbound email isn't configured. The in-app
    // Notification (Application.Services.Communication.NotificationService)
    // is the channel that actually has to work; this is a best-effort extra.
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string toEmail, string subject, string body, bool isBodyHtml = false)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return false;

            var host = _configuration["Smtp:Host"];

            if (string.IsNullOrWhiteSpace(host))
            {
                // Not configured - deliberately skip rather than attempt and
                // fail loudly. This is the expected state in this sandbox.
                _logger.LogInformation(
                    "Smtp:Host is not configured - skipping email to {ToEmail} (\"{Subject}\").",
                    toEmail, subject);

                return false;
            }

            try
            {
                var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
                var enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var parsedSsl) ? parsedSsl : true;
                var username = _configuration["Smtp:Username"];
                var password = _configuration["Smtp:Password"];
                var fromAddress = _configuration["Smtp:FromAddress"];
                var fromName = _configuration["Smtp:FromName"];

                if (string.IsNullOrWhiteSpace(fromAddress))
                    fromAddress = string.IsNullOrWhiteSpace(username) ? "no-reply@localhost" : username;

                using var message = new MailMessage
                {
                    From = new MailAddress(fromAddress, string.IsNullOrWhiteSpace(fromName) ? "HRMS" : fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isBodyHtml
                };

                message.To.Add(toEmail);

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl
                };

                if (!string.IsNullOrWhiteSpace(username))
                    client.Credentials = new NetworkCredential(username, password);

                await client.SendMailAsync(message);

                return true;
            }
            catch (Exception ex)
            {
                // Never let an email failure bubble up - log and move on.
                _logger.LogWarning(ex, "Failed to send email to {ToEmail} (\"{Subject}\").", toEmail, subject);

                return false;
            }
        }
    }
}
