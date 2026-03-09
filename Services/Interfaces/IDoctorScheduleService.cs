using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IDoctorScheduleService
    {
        Task<DoctorScheduleDto> CreateScheduleAsync(int doctorId, CreateScheduleDto dto);
        Task<IEnumerable<DoctorScheduleDto>> GetSchedulesByDoctorAsync(int doctorId);
        Task<DoctorScheduleDto> UpdateScheduleAsync(int scheduleId, UpdateScheduleDto dto);
        Task DeleteScheduleAsync(int scheduleId);
    }
}
