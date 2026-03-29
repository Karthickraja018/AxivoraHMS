using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Services.Interfaces;

namespace Axivora.Services.BackgroundServices
{
    /// <summary>
    /// Runs every hour and enqueues a reminder email for every appointment that:
    ///   1. Is scheduled roughly 24 hours from now (within a ±30-minute window).
    ///   2. Has not already had a reminder sent (<see cref="Models.Appointment.ReminderSent"/> == false).
    ///
    /// The reminder window is kept intentionally wide (±30 min) so that the job can
    /// still catch appointments even if a run is slightly delayed.
    /// </summary>
    public class AppointmentReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderService> _logger;

        private static readonly TimeSpan RunInterval   = TimeSpan.FromHours(1);
        private static readonly TimeSpan ReminderLeadTime = TimeSpan.FromHours(24);
        private static readonly TimeSpan ReminderWindow   = TimeSpan.FromMinutes(30);

        public AppointmentReminderService(
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Service} started – will check for reminders every {Interval}.",
                nameof(AppointmentReminderService), RunInterval);

            // Give the app and DB some breathing room on startup
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await SendPendingRemindersAsync(stoppingToken);

                try
                {
                    await Task.Delay(RunInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("{Service} stopped.", nameof(AppointmentReminderService));
        }

        private async Task SendPendingRemindersAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            try
            {
                // Scoped services (DbContext, IEmailService) must be resolved inside a scope
                using var scope        = _scopeFactory.CreateScope();
                var db                 = scope.ServiceProvider.GetRequiredService<AxivoraDbContext>();
                var emailService       = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var windowStart = DateTime.UtcNow.Add(ReminderLeadTime - ReminderWindow);
                var windowEnd   = DateTime.UtcNow.Add(ReminderLeadTime + ReminderWindow);

                // Find appointments in the reminder window that haven't been reminded yet
                var appointments = await db.Appointments
                    .Include(a => a.Patient)
                        .ThenInclude(p => p!.User)
                    .Include(a => a.Doctor)
                    .Where(a =>
                        !a.IsDeleted &&
                        !a.ReminderSent &&
                        a.AppointmentStart >= windowStart &&
                        a.AppointmentStart <= windowEnd)
                    .ToListAsync(stoppingToken);

                if (appointments.Count == 0)
                {
                    _logger.LogDebug("No pending appointment reminders found.");
                    return;
                }

                _logger.LogInformation("Sending {Count} appointment reminder(s).", appointments.Count);

                foreach (var appointment in appointments)
                {
                    try
                    {
                        var patientEmail = appointment.Patient?.User?.Email;
                        var patientName  = appointment.Patient?.FullName ?? "Patient";
                        var doctorName   = appointment.Doctor?.FullName  ?? "Doctor";

                        if (string.IsNullOrWhiteSpace(patientEmail))
                        {
                            _logger.LogWarning(
                                "Appointment {Id}: patient email not found, skipping reminder.",
                                appointment.AppointmentId);
                            continue;
                        }

                        await emailService.SendAppointmentReminderAsync(
                            patientEmail, patientName, doctorName, appointment.AppointmentStart);

                        // Mark as sent so we don't send it again next hour
                        appointment.ReminderSent = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to enqueue reminder for appointment {Id}.",
                            appointment.AppointmentId);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Service}.{Method}.",
                    nameof(AppointmentReminderService), nameof(SendPendingRemindersAsync));
            }
        }
    }
}
