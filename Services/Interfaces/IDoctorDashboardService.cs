using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IDoctorDashboardService
    {
        Task<DoctorDashboardDto> GetDoctorDashboardAsync(int doctorUserId);
    }
}

