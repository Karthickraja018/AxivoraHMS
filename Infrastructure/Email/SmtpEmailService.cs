using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Axivora.Models;

namespace Axivora.Infrastructure.Email
{
    /// <summary>
    /// Low-level SMTP sender. Provides a single <see cref="SendAsync"/> method used
    /// exclusively by <see cref="Services.BackgroundServices.EmailBackgroundService"/>
    /// to deliver pre-composed email messages from the queue.
    /// </summary>
    public class SmtpEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
        {
            _settings = settings.Value;
            _logger   = logger;
        }

        /// <summary>Opens an SMTP connection and delivers the message.</summary>
        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl   = true
            };

            using var message = new MailMessage
            {
                From       = new MailAddress(_settings.FromEmail, "AxivoraHMS"),
                Subject    = subject,
                Body       = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email delivered to {To} | Subject: {Subject}", to, subject);
        }
    }
}
