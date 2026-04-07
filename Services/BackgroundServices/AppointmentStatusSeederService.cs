using Axivora.Data;
using Axivora.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Axivora.Services.BackgroundServices
{
    /// <summary>
    /// Ensures required appointment statuses exist in the DB.
    /// Avoids runtime failures when a fresh database is created without seed data.
    /// </summary>
    public class AppointmentStatusSeederService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentStatusSeederService> _logger;

        private static readonly string[] RequiredStatuses =
        [
            "Scheduled",
            "InProgress",
            "PendingDocumentation",
            "Completed",
            "Cancelled",
            "NoShow"
        ];

        public AppointmentStatusSeederService(
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentStatusSeederService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // One-time on startup
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AxivoraDbContext>();

                // In prod, migrations should already be applied. In dev, Program.cs migrates automatically.
                // We also normalize legacy status names and map removed statuses to the simplified flow.

                await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

                var existingRows = await db.AppointmentStatuses
                    .ToListAsync(stoppingToken);

                // Ensure required statuses exist (case-insensitive)
                foreach (var name in RequiredStatuses)
                {
                    if (!existingRows.Any(s => s.StatusName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        var row = new AppointmentStatus { StatusName = name };
                        db.AppointmentStatuses.Add(row);
                        existingRows.Add(row);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);

                AppointmentStatus GetRequired(string name) =>
                    existingRows.First(s => s.StatusName.Equals(name, StringComparison.OrdinalIgnoreCase));

                var scheduledId  = GetRequired("Scheduled").StatusId;
                var inProgressId = GetRequired("InProgress").StatusId;
                var pendingDocId = GetRequired("PendingDocumentation").StatusId;
                var noShowId     = GetRequired("NoShow").StatusId;

                // Legacy -> new mapping
                var remap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Confirmed"]   = scheduledId,
                    ["Checked-In"]  = scheduledId,
                    ["Rescheduled"] = scheduledId,
                    ["In Progress"] = inProgressId,
                    ["Pending Documentation"] = pendingDocId,
                    ["No-Show"]     = noShowId
                };

                // Update appointments that still reference removed/legacy statuses
                foreach (var kvp in remap)
                {
                    var legacy = existingRows.FirstOrDefault(s => s.StatusName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                    if (legacy is null) continue;

                    var affected = await db.Appointments
                        .Where(a => !a.IsDeleted && a.StatusId == legacy.StatusId)
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.StatusId, kvp.Value), stoppingToken);

                    if (affected > 0)
                        _logger.LogInformation("Mapped {Count} appointment(s) from '{Legacy}' -> statusId {NewId}.", affected, kvp.Key, kvp.Value);
                }

                // Rename legacy rows in-place where safe (prevents future clients filtering by old names)
                void RenameIfPresent(string from, string to)
                {
                    var fromRow = existingRows.FirstOrDefault(s => s.StatusName.Equals(from, StringComparison.OrdinalIgnoreCase));
                    if (fromRow is null) return;

                    var toRow = existingRows.FirstOrDefault(s => s.StatusName.Equals(to, StringComparison.OrdinalIgnoreCase));
                    if (toRow is not null)
                    {
                        // Both exist: appointments were already remapped above, so we can delete the legacy row.
                        db.AppointmentStatuses.Remove(fromRow);
                        existingRows.Remove(fromRow);
                        return;
                    }

                    fromRow.StatusName = to;
                }
                RenameIfPresent("In Progress", "InProgress");
                RenameIfPresent("Pending Documentation", "PendingDocumentation");
                RenameIfPresent("No-Show", "NoShow");

                await db.SaveChangesAsync(stoppingToken);

                await tx.CommitAsync(stoppingToken);

                _logger.LogInformation("Appointment status seeding/normalization completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed appointment statuses.");
            }
        }
    }
}
