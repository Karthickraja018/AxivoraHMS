using Axivora.Services.Interfaces;

namespace Axivora.BackgroundServices
{
    /// <summary>
    /// Runs daily at midnight UTC to generate DoctorAvailabilityDay records
    /// and their corresponding AppointmentSlots for the next 30 days,
    /// based on all active DoctorAvailabilityTemplate entries.
    /// </summary>
    public class AvailabilityGenerationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AvailabilityGenerationBackgroundService> _logger;

        // How many days ahead to generate
        private const int DaysAhead = 30;

        // Run at this time each day (UTC)
        private static readonly TimeSpan RunAt = TimeSpan.Zero;

        public AvailabilityGenerationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<AvailabilityGenerationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "{Service} started. Will generate availability days daily at {Time} UTC.",
                nameof(AvailabilityGenerationBackgroundService), RunAt);

            // Run once immediately on startup so the system is populated without waiting
            await RunGenerationAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = CalculateDelayUntilNextRun();
                _logger.LogDebug(
                    "Next availability generation run in {Delay}.", delay);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                await RunGenerationAsync(stoppingToken);
            }

            _logger.LogInformation(
                "{Service} stopped.", nameof(AvailabilityGenerationBackgroundService));
        }

        private async Task RunGenerationAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            try
            {
                // IDoctorAvailabilityService is scoped, so we must create a scope here
                using var scope   = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<IDoctorAvailabilityService>();

                _logger.LogInformation(
                    "Running availability generation for the next {Days} days.", DaysAhead);

                await service.GenerateAvailabilityDaysAsync(DaysAhead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during availability day generation.");
            }
        }

        /// <summary>
        /// Calculates how long to wait until midnight UTC of the next day.
        /// </summary>
        private static TimeSpan CalculateDelayUntilNextRun()
        {
            var now         = DateTime.UtcNow;
            var nextRun     = now.Date.AddDays(1).Add(RunAt);
            return nextRun - now;
        }
    }
}
