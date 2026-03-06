using Axivora.Helpers;
using Axivora.DTOs;
using Axivora.Models;

namespace Axivora.Services.Interfaces
{
    public interface IConsultationService
    {
        Task<IEnumerable<ConsultationDto>> GetAllConsultationsAsync();
        Task<PaginationResponse<ConsultationDto>> GetAllConsultationsAsync(PaginationParams paginationParams);
        Task<ConsultationDto> GetConsultationByIdAsync(int consultationId);
        Task<ConsultationDto> GetConsultationByAppointmentIdAsync(int appointmentId);
        Task<ConsultationDto> CreateConsultationAsync(CreateConsultationDto createConsultationDto);
        Task<ConsultationDto> UpdateConsultationAsync(int consultationId, CreateConsultationDto updateConsultationDto);
        Task<ConsultationDto> AddPrescriptionAsync(int consultationId, CreatePrescriptionDto prescriptionDto);
        Task<ConsultationDto> AddLabTestAsync(int consultationId, CreateOrderedTestDto orderedTestDto);
    }
}
