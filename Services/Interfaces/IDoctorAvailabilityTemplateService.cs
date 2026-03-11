using Axivora.DTOs;

namespace Axivora.Services.Interfaces
{
    public interface IDoctorAvailabilityTemplateService
    {
        Task<AvailabilityTemplateDto> CreateTemplateAsync(int doctorId, CreateAvailabilityTemplateDto dto);
        Task<IEnumerable<AvailabilityTemplateDto>> GetTemplatesByDoctorAsync(int doctorId);
        Task<AvailabilityTemplateDto> UpdateTemplateAsync(int templateId, UpdateAvailabilityTemplateDto dto);
        Task DeleteTemplateAsync(int templateId);
    }
}
