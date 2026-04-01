using Axivora.Services.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Axivora.Services.BackgroundServices
{
    /// <summary>
    /// Periodically marks overdue Scheduled appointments as NoShow and frees their slots.
    /// This prevents permanent slot blockage when a patient never shows up.
    /// </summary>
    public class AppointmentNoShowService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentNoShowService> _logger;

        private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(10);

        public AppointmentNoShowService(
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentNoShowService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Service} started – will check for NoShow every {Interval}.",
                nameof(AppointmentNoShowService), RunInterval);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var apptService = scope.ServiceProvider.GetRequiredService<IAppointmentService>();

                    var count = await apptService.AutoMarkNoShowsAsync(DateTime.UtcNow, stoppingToken);
                    if (count > 0)
                        _logger.LogInformation("Auto-marked {Count} appointment(s) as NoShow.", count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while auto-marking NoShow appointments.");
                }

                try
                {
                    await Task.Delay(RunInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("{Service} stopped.", nameof(AppointmentNoShowService));
        }
    }
}
