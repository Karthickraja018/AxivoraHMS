using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IConsultationRepository
    {
        Task<IEnumerable<Consultation>> GetAllAsync();
        Task<int> CountAsync();
        Task<IEnumerable<Consultation>> GetPagedAsync(int skip, int take);
        Task<Consultation?> GetByIdAsync(int consultationId);
        Task<Consultation?> GetByAppointmentIdAsync(int appointmentId);
        Task<bool> ExistsForAppointmentAsync(int appointmentId);
        Task<int> CountByPatientAsync(int patientId);
        Task<IEnumerable<Consultation>> GetPagedByPatientAsync(int patientId, int skip, int take);
        Task<Doctor?> GetDoctorByUserIdAsync(int userId);
        Task<Appointment?> GetAppointmentWithStatusAsync(int appointmentId);
        Task<Appointment?> GetAppointmentWithPatientAndDoctorAsync(int appointmentId);
        Task<int> CountByDoctorAsync(int doctorId);
        Task<IEnumerable<Consultation>> GetPagedByDoctorAsync(int doctorId, int skip, int take);
        Task AddConsultationAsync(Consultation consultation);
        Task<bool> IsMedicineAlreadyPrescribedAsync(int consultationId, int medicineId);
        Task AddPrescriptionAsync(Prescription prescription);
        Task AddOrderedTestAsync(OrderedTest orderedTest);
        Task SaveChangesAsync();
    }
}
