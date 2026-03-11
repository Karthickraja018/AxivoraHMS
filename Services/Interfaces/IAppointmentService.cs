using Axivora.DTOs;
using Axivora.Helpers;

namespace Axivora.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync();
        Task<PaginationResponse<AppointmentDto>> GetAllAppointmentsAsync(PaginationParams paginationParams);
        Task<AppointmentDto> GetAppointmentByIdAsync(int appointmentId);
        Task<AppointmentDto> GetAppointmentByIdAsync(int appointmentId, int callerUserId, string callerRole);
        Task<IEnumerable<AppointmentDto>> GetAppointmentsByPatientIdAsync(int patientId);
        Task<IEnumerable<AppointmentDto>> GetAppointmentsByDoctorIdAsync(int doctorId);
        Task<AppointmentDto> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto updateAppointmentDto);
        Task<AppointmentDto> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto updateAppointmentDto, int callerUserId, string callerRole);
        Task<bool> CancelAppointmentAsync(int appointmentId);
        Task<bool> CancelAppointmentAsync(int appointmentId, int callerUserId, string callerRole);
        Task<IEnumerable<AppointmentDto>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<PaginationResponse<AppointmentDto>> GetMyAppointmentsAsync(int userId, PaginationParams paginationParams, string? status);
        Task<PaginationResponse<AppointmentDto>> GetDoctorAppointmentsAsync(int userId, PaginationParams paginationParams, DateTime? date);
        Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, string statusName);
        Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, string statusName, string callerRole);
    }
}
