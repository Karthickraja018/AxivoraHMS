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

        private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ReminderWindow = TimeSpan.FromMinutes(15);

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

                var now = DateTime.UtcNow;
                var window24Start = now.AddHours(24) - ReminderWindow;
                var window24End   = now.AddHours(24) + ReminderWindow;
                var window2Start  = now.AddHours(2)  - ReminderWindow;
                var window2End    = now.AddHours(2)  + ReminderWindow;

                // 24h reminders
                var appts24 = await db.Appointments
                    .Include(a => a.Patient).ThenInclude(p => p!.User)
                    .Include(a => a.Doctor)
                    .Include(a => a.Status)
                    .Where(a =>
                        !a.IsDeleted &&
                        !a.ReminderSent &&
                        a.Status != null && a.Status.StatusName == "Scheduled" &&
                        a.AppointmentStart >= window24Start &&
                        a.AppointmentStart <= window24End)
                    .ToListAsync(stoppingToken);

                // 2h reminders
                var appts2 = await db.Appointments
                    .Include(a => a.Patient).ThenInclude(p => p!.User)
                    .Include(a => a.Doctor)
                    .Include(a => a.Status)
                    .Where(a =>
                        !a.IsDeleted &&
                        !a.Reminder2HoursSent &&
                        a.Status != null && a.Status.StatusName == "Scheduled" &&
                        a.AppointmentStart >= window2Start &&
                        a.AppointmentStart <= window2End)
                    .ToListAsync(stoppingToken);

                if (appts24.Count == 0 && appts2.Count == 0)
                {
                    _logger.LogDebug("No pending appointment reminders found.");
                    return;
                }

                _logger.LogInformation("Sending appointment reminders: {Count24} (24h), {Count2} (2h).",
                    appts24.Count, appts2.Count);

                foreach (var appointment in appts24)
                {
                    try
                    {
                        var patientEmail = appointment.Patient?.User?.Email;
                        if (string.IsNullOrWhiteSpace(patientEmail)) continue;

                        await emailService.SendAppointmentReminderAsync(
                            patientEmail,
                            appointment.Patient?.FullName ?? "Patient",
                            appointment.Doctor?.FullName ?? "Doctor",
                            appointment.AppointmentStart);

                        appointment.ReminderSent = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to enqueue 24h reminder for appointment {Id}.", appointment.AppointmentId);
                    }
                }

                foreach (var appointment in appts2)
                {
                    try
                    {
                        var patientEmail = appointment.Patient?.User?.Email;
                        if (string.IsNullOrWhiteSpace(patientEmail)) continue;

                        await emailService.SendAppointmentReminder2HoursAsync(
                            patientEmail,
                            appointment.Patient?.FullName ?? "Patient",
                            appointment.Doctor?.FullName ?? "Doctor",
                            appointment.AppointmentStart);

                        appointment.Reminder2HoursSent = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to enqueue 2h reminder for appointment {Id}.", appointment.AppointmentId);
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
