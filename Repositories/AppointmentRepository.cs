using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly AxivoraDbContext _context;
        private IDbContextTransaction? _transaction;

        public AppointmentRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Read-only base query — AsNoTracking improves performance for all list/count
        /// queries that never modify the returned entities.
        /// </summary>
        private IQueryable<Appointment> BaseQuery() =>
            _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Status)
                .Where(a => !a.IsDeleted);

        public async Task<IEnumerable<Appointment>> GetAllAsync() =>
            await BaseQuery().ToListAsync();

        public async Task<int> CountAsync() =>
            await BaseQuery().CountAsync();

        public async Task<IEnumerable<Appointment>> GetPagedAsync(int skip, int take) =>
            await BaseQuery()
                .OrderByDescending(a => a.AppointmentStart)
                .Skip(skip).Take(take)
                .ToListAsync();

        /// <summary>
        /// Fetches a tracked appointment so the service layer can mutate and save it.
        /// Tracking is intentionally kept here — do NOT add AsNoTracking.
        /// </summary>
        public async Task<Appointment?> GetByIdAsync(int appointmentId) =>
            await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Status)
                .Where(a => !a.IsDeleted)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        public async Task<IEnumerable<Appointment>> GetByPatientIdAsync(int patientId) =>
            await BaseQuery()
                .Where(a => a.PatientId == patientId)
                .ToListAsync();

        public async Task<IEnumerable<Appointment>> GetByDoctorIdAsync(int doctorId) =>
            await BaseQuery()
                .Where(a => a.DoctorId == doctorId)
                .ToListAsync();

        public async Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate) =>
            await BaseQuery()
                .Where(a => a.AppointmentStart >= startDate && a.AppointmentStart <= endDate)
                .ToListAsync();

        public async Task<bool> DoctorExistsAsync(int doctorId) =>
            await _context.Doctors
                .AsNoTracking()
                .IgnoreQueryFilters()
                .AnyAsync(d => d.DoctorId == doctorId && !d.IsDeleted);

        public async Task<bool> PatientExistsAsync(int patientId) =>
            await _context.Patients
                .AsNoTracking()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.PatientId == patientId && !p.IsDeleted);

        public async Task<bool> StatusExistsAsync(int statusId) =>
            await _context.AppointmentStatuses
                .AsNoTracking()
                .AnyAsync(s => s.StatusId == statusId);

        public async Task<AppointmentStatus?> GetStatusByIdAsync(int statusId) =>
            await _context.AppointmentStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StatusId == statusId);

        public async Task<AppointmentStatus?> GetStatusByNameAsync(string statusName) =>
            await _context.AppointmentStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StatusName == statusName);

        public async Task<Patient?> GetPatientByUserIdAsync(int userId) =>
            await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

        public async Task<Doctor?> GetDoctorByUserIdAsync(int userId) =>
            await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId && !d.IsDeleted);

        public async Task<int> CountByPatientAsync(int patientId, string? status)
        {
            var query = BaseQuery().Where(a => a.PatientId == patientId);
            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                query = query.Where(a => a.Status != null && a.Status.StatusName == status);
            return await query.CountAsync();
        }

        public async Task<IEnumerable<Appointment>> GetPagedByPatientAsync(int patientId, string? status, int skip, int take)
        {
            var query = BaseQuery().Where(a => a.PatientId == patientId);
            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                query = query.Where(a => a.Status != null && a.Status.StatusName == status);
            return await query
                .OrderByDescending(a => a.AppointmentStart)
                .Skip(skip).Take(take)
                .ToListAsync();
        }

        public async Task<int> CountByDoctorAsync(int doctorId, DateTime? date)
        {
            var query = BaseQuery().Where(a => a.DoctorId == doctorId);
            if (date.HasValue)
            {
                var dayStart = date.Value.Date;
                var dayEnd   = dayStart.AddDays(1);
                query = query.Where(a => a.AppointmentStart >= dayStart && a.AppointmentStart < dayEnd);
            }
            return await query.CountAsync();
        }

        public async Task<IEnumerable<Appointment>> GetPagedByDoctorAsync(int doctorId, DateTime? date, int skip, int take)
        {
            var query = BaseQuery().Where(a => a.DoctorId == doctorId);
            if (date.HasValue)
            {
                var dayStart = date.Value.Date;
                var dayEnd   = dayStart.AddDays(1);
                query = query.Where(a => a.AppointmentStart >= dayStart && a.AppointmentStart < dayEnd);
            }
            return await query
                .OrderBy(a => a.AppointmentStart)
                .Skip(skip).Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Fetches a tracked slot so the service layer can update its status and save it.
        /// Tracking is intentionally kept here — do NOT add AsNoTracking.
        /// </summary>
        public async Task<AppointmentSlot?> GetSlotByIdAsync(int slotId) =>
            await _context.AppointmentSlots
                .Include(s => s.AvailabilityDay)
                .FirstOrDefaultAsync(s => s.Id == slotId);

        public async Task<Patient?> GetPatientWithUserAsync(int patientId) =>
            await _context.Patients
                .AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientId == patientId && !p.IsDeleted);

        public async Task<string?> GetDoctorFullNameAsync(int doctorId) =>
            await _context.Doctors
                .AsNoTracking()
                .Where(d => d.DoctorId == doctorId && !d.IsDeleted)
                .Select(d => d.FullName)
                .FirstOrDefaultAsync();

        public async Task AddAsync(Appointment appointment) =>
            await _context.Appointments.AddAsync(appointment);

        public async Task AddAuditLogAsync(AuditLog auditLog) =>
            await _context.AuditLogs.AddAsync(auditLog);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public async Task BeginTransactionAsync() =>
            _transaction = await _context.Database.BeginTransactionAsync();

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }
    }
}
