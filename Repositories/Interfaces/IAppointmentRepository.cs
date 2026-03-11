using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<IEnumerable<Appointment>> GetAllAsync();
        Task<int> CountAsync();
        Task<IEnumerable<Appointment>> GetPagedAsync(int skip, int take);
        Task<Appointment?> GetByIdAsync(int appointmentId);
        Task<IEnumerable<Appointment>> GetByPatientIdAsync(int patientId);
        Task<IEnumerable<Appointment>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> DoctorExistsAsync(int doctorId);
        Task<bool> PatientExistsAsync(int patientId);
        Task<bool> StatusExistsAsync(int statusId);
        Task<AppointmentStatus?> GetStatusByIdAsync(int statusId);
        Task<AppointmentStatus?> GetStatusByNameAsync(string statusName);
        Task<Patient?> GetPatientByUserIdAsync(int userId);
        Task<Doctor?> GetDoctorByUserIdAsync(int userId);
        Task<int> CountByPatientAsync(int patientId, string? status);
        Task<IEnumerable<Appointment>> GetPagedByPatientAsync(int patientId, string? status, int skip, int take);
        Task<int> CountByDoctorAsync(int doctorId, DateTime? date);
        Task<IEnumerable<Appointment>> GetPagedByDoctorAsync(int doctorId, DateTime? date, int skip, int take);
        Task<AppointmentSlot?> GetSlotByIdAsync(int slotId);
        Task AddAsync(Appointment appointment);
        Task AddAuditLogAsync(AuditLog auditLog);
        Task SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
