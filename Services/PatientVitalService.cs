using AutoMapper;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Models;
using Axivora.Repositories.Interfaces;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class PatientVitalService : IPatientVitalService
    {
        private readonly IPatientVitalRepository _repository;
        private readonly IMapper _mapper;

        public PatientVitalService(IPatientVitalRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper     = mapper;
        }

        public async Task<PaginationResponse<PatientVitalDto>> GetVitalsAsync(int patientId, PaginationParams paginationParams)
        {
            if (!await _repository.PatientExistsAsync(patientId))
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            var totalCount = await _repository.CountByPatientAsync(patientId);
            var vitals     = await _repository.GetPagedByPatientAsync(
                patientId,
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            return new PaginationResponse<PatientVitalDto>(
                _mapper.Map<IEnumerable<PatientVitalDto>>(vitals),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<PatientVitalDto> GetVitalByIdAsync(int patientId, int vitalId)
        {
            if (!await _repository.PatientExistsAsync(patientId))
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            var vital = await _repository.GetByIdAsync(vitalId);

            if (vital is null || vital.PatientId != patientId)
                throw new KeyNotFoundException($"Vital record with ID {vitalId} not found for patient {patientId}.");

            return _mapper.Map<PatientVitalDto>(vital);
        }

        public async Task<PatientVitalDto> CreateVitalAsync(int patientId, CreatePatientVitalDto dto)
        {
            if (!await _repository.PatientExistsAsync(patientId))
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            var vital = _mapper.Map<PatientVital>(dto);
            vital.PatientId   = patientId;
            vital.RecordedAt  = DateTime.UtcNow;

            await _repository.AddAsync(vital);
            await _repository.SaveChangesAsync();

            return _mapper.Map<PatientVitalDto>(vital);
        }

        public async Task<PatientVitalDto> UpdateVitalAsync(int patientId, int vitalId, UpdatePatientVitalDto dto)
        {
            if (!await _repository.PatientExistsAsync(patientId))
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            var vital = await _repository.GetByIdAsync(vitalId);

            if (vital is null || vital.PatientId != patientId)
                throw new KeyNotFoundException($"Vital record with ID {vitalId} not found for patient {patientId}.");

            _mapper.Map(dto, vital);
            await _repository.SaveChangesAsync();

            return _mapper.Map<PatientVitalDto>(vital);
        }

        public async Task DeleteVitalAsync(int patientId, int vitalId)
        {
            if (!await _repository.PatientExistsAsync(patientId))
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            var vital = await _repository.GetByIdAsync(vitalId);

            if (vital is null || vital.PatientId != patientId)
                throw new KeyNotFoundException($"Vital record with ID {vitalId} not found for patient {patientId}.");

            _repository.Remove(vital);
            await _repository.SaveChangesAsync();
        }
    }
}
