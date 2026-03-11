using AutoMapper;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class DoctorAvailabilityTemplateService : IDoctorAvailabilityTemplateService
    {
        private readonly IAvailabilityTemplateRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<DoctorAvailabilityTemplateService> _logger;

        public DoctorAvailabilityTemplateService(
            IAvailabilityTemplateRepository repository,
            IMapper mapper,
            ILogger<DoctorAvailabilityTemplateService> logger)
        {
            _repository = repository;
            _mapper     = mapper;
            _logger     = logger;
        }

        public async Task<AvailabilityTemplateDto> CreateTemplateAsync(
            int doctorId, CreateAvailabilityTemplateDto dto)
        {
            var doctor = await _repository.GetDoctorByIdAsync(doctorId);
            if (doctor is null)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            // EndTime > StartTime is also enforced by IValidatableObject on the DTO,
            // but we repeat it here to guard against programmatic misuse.
            if (dto.EndTime <= dto.StartTime)
                throw new ArgumentException("EndTime must be after StartTime.");

            var template = _mapper.Map<DoctorAvailabilityTemplate>(dto);
            template.DoctorId  = doctorId;
            template.CreatedAt = DateTime.UtcNow;

            await _repository.AddAsync(template);
            await _repository.SaveChangesAsync();

            _logger.LogInformation(
                "Created availability template {TemplateId} for doctor {DoctorId} on {Day}.",
                template.Id, doctorId, (DayOfWeek)dto.DayOfWeek);

            // Reload with navigation to satisfy DoctorName mapping
            var created = await _repository.GetByIdWithDoctorAsync(template.Id);
            return _mapper.Map<AvailabilityTemplateDto>(created!);
        }

        public async Task<IEnumerable<AvailabilityTemplateDto>> GetTemplatesByDoctorAsync(int doctorId)
        {
            var doctor = await _repository.GetDoctorByIdAsync(doctorId);
            if (doctor is null)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            var templates = await _repository.GetByDoctorIdAsync(doctorId);
            return _mapper.Map<IEnumerable<AvailabilityTemplateDto>>(templates);
        }

        public async Task<AvailabilityTemplateDto> UpdateTemplateAsync(
            int templateId, UpdateAvailabilityTemplateDto dto)
        {
            var template = await _repository.GetByIdWithDoctorAsync(templateId);
            if (template is null)
                throw new KeyNotFoundException($"Availability template with ID {templateId} not found.");

            if (dto.IsActive.HasValue)
                template.IsActive = dto.IsActive.Value;

            if (dto.EffectiveToDate.HasValue)
            {
                if (dto.EffectiveToDate.Value <= template.EffectiveFromDate)
                    throw new ArgumentException("EffectiveToDate must be after EffectiveFromDate.");
                template.EffectiveToDate = dto.EffectiveToDate.Value;
            }

            await _repository.SaveChangesAsync();

            _logger.LogInformation("Updated availability template {TemplateId}.", templateId);
            return _mapper.Map<AvailabilityTemplateDto>(template);
        }

        public async Task DeleteTemplateAsync(int templateId)
        {
            var template = await _repository.GetByIdAsync(templateId);
            if (template is null)
                throw new KeyNotFoundException($"Availability template with ID {templateId} not found.");

            // Soft-delete by deactivating rather than removing to preserve historical day records
            template.IsActive = false;
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Deactivated availability template {TemplateId}.", templateId);
        }
    }
}
