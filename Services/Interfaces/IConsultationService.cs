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
        Task<PaginationResponse<ConsultationDto>> GetConsultationsByPatientIdAsync(int patientId, PaginationParams paginationParams);
        Task<ConsultationDto> CreateConsultationAsync(CreateConsultationDto createConsultationDto);
        Task<ConsultationDto> CreateConsultationAsync(CreateConsultationDto createConsultationDto, int callerUserId, string callerRole);
        Task<ConsultationDto> UpdateConsultationAsync(int consultationId, UpdateConsultationDto updateConsultationDto);
        Task<ConsultationDto> AddPrescriptionAsync(int consultationId, CreatePrescriptionDto prescriptionDto);
        Task<ConsultationDto> AddLabTestAsync(int consultationId, CreateOrderedTestDto orderedTestDto);
        Task<PaginationResponse<ConsultationDto>> GetConsultationsByDoctorUserIdAsync(int userId, ConsultationDoctorFilterParams filterParams);
    }
}
