using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class AppointmentBookingRepository : IAppointmentBookingRepository
    {
        private readonly AxivoraDbContext _context;
        private IDbContextTransaction? _transaction;

        public AppointmentBookingRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<AppointmentSlot?> GetSlotByIdAsync(int slotId) =>
            await _context.AppointmentSlots
                .Include(s => s.AvailabilityDay)
                .FirstOrDefaultAsync(s => s.Id == slotId);

        public async Task<Appointment?> GetAppointmentByIdAsync(int appointmentId) =>
            await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Status)
                .Include(a => a.Slot)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && !a.IsDeleted);

        public async Task<Patient?> GetPatientByUserIdAsync(int userId) =>
            await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

        /// <summary>Returns the "Scheduled" status, which is the default for new bookings.</summary>
        public async Task<AppointmentStatus?> GetDefaultStatusAsync() =>
            await _context.AppointmentStatuses
                .FirstOrDefaultAsync(s => s.StatusName == "Scheduled");

        public async Task<AppointmentStatus?> GetStatusByNameAsync(string name) =>
            await _context.AppointmentStatuses
                .FirstOrDefaultAsync(s => s.StatusName == name);

        public async Task AddAppointmentAsync(Appointment appointment) =>
            await _context.Appointments.AddAsync(appointment);

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
