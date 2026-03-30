using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IPatientDashboardService
    {
        Task<PatientDashboardDto> GetPatientDashboardAsync(int patientUserId);
    }
}
