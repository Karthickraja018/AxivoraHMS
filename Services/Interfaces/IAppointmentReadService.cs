using Axivora.DTOs;
using Axivora.Helpers;

namespace Axivora.Services.Interfaces
{
    public interface IAppointmentReadService
    {
        Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync();
        Task<PaginationResponse<AppointmentDto>> GetAllAppointmentsAsync(PaginationParams paginationParams);
        Task<AppointmentDto> GetAppointmentByIdAsync(int appointmentId);
        Task<AppointmentDto> GetAppointmentByIdAsync(int appointmentId, int callerUserId, string callerRole);
        Task<IEnumerable<AppointmentDto>> GetAppointmentsByPatientIdAsync(int patientId);
        Task<IEnumerable<AppointmentDto>> GetAppointmentsByDoctorIdAsync(int doctorId);
        Task<IEnumerable<AppointmentDto>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<PaginationResponse<AppointmentDto>> GetMyAppointmentsAsync(
            int userId, PaginationParams paginationParams, PatientAppointmentsFilter? filter);
        Task<PaginationResponse<AppointmentDto>> GetDoctorAppointmentsAsync(int userId, PaginationParams paginationParams);
    }
}
