using Axivora.Infrastructure.Email;
using Axivora.Services.Interfaces;

namespace Axivora.Services.BackgroundServices
{
    /// <summary>
    /// Long-running hosted service that drains the <see cref="IEmailQueue"/> and
    /// delivers each message via <see cref="SmtpEmailService"/>.
    ///
    /// Design notes:
    /// - IEmailQueue is singleton; SmtpEmailService is transient – both are injected
    ///   directly into this singleton BackgroundService.
    /// - The loop polls every second. If the queue is empty it simply waits,
    ///   keeping CPU usage negligible.
    /// - Failures are logged but never propagate so the service keeps running.
    /// </summary>
    public class EmailBackgroundService : BackgroundService
    {
        private readonly IEmailQueue _emailQueue;
        private readonly SmtpEmailService _smtpService;
        private readonly ILogger<EmailBackgroundService> _logger;

        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);

        public EmailBackgroundService(
            IEmailQueue emailQueue,
            SmtpEmailService smtpService,
            ILogger<EmailBackgroundService> logger)
        {
            _emailQueue  = emailQueue;
            _smtpService = smtpService;
            _logger      = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Service} started – listening for queued emails.", nameof(EmailBackgroundService));

            while (!stoppingToken.IsCancellationRequested)
            {
                // Drain all messages currently in the queue before sleeping
                var message = _emailQueue.Dequeue();
                while (message is not null)
                {
                    try
                    {
                        await _smtpService.SendAsync(message.To, message.Subject, message.Body);
                    }
                    catch (Exception ex)
                    {
                        // Log and continue – do NOT re-throw so the background service stays alive
                        _logger.LogError(ex,
                            "Failed to send email to {To} | Subject: {Subject}",
                            message.To, message.Subject);
                    }

                    message = _emailQueue.Dequeue();
                }

                try
                {
                    await Task.Delay(PollingInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("{Service} stopped.", nameof(EmailBackgroundService));
        }
    }
}
