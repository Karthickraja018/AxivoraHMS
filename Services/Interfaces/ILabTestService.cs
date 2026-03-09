using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface ILabTestService
    {
        Task<LabResultDto> UploadResultAsync(int orderedTestId, LabResultUpdateDto dto);
        Task<IEnumerable<LabResultDto>> GetResultsByPatientAsync(int patientId);
        Task<IEnumerable<LabResultDto>> GetResultsByConsultationAsync(int consultationId);
    }
}
