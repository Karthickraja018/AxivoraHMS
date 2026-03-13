using AutoMapper;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Helpers;
using Axivora.Security;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _repository;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            IPatientRepository repository,
            IMapper mapper,
            IPasswordHasher passwordHasher,
            ILogger<PatientService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<IEnumerable<PatientDto>> GetAllPatientsAsync()
        {
            var patients = await _repository.GetAllActiveAsync();
            return _mapper.Map<IEnumerable<PatientDto>>(patients);
        }

        public async Task<PaginationResponse<PatientDto>> GetAllPatientsAsync(PaginationParams paginationParams)
        {
            var totalCount = await _repository.CountActiveAsync();
            var patients = await _repository.GetPagedActiveAsync(
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            return new PaginationResponse<PatientDto>(
                _mapper.Map<IEnumerable<PatientDto>>(patients),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<PatientDto> GetPatientByIdAsync(int patientId)
        {
            var patient = await _repository.GetByIdAsync(patientId);

            if (patient is null)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            return _mapper.Map<PatientDto>(patient);
        }

        public async Task<PatientDto> GetPatientByMRNAsync(string mrn)
        {
            var patient = await _repository.GetByMRNAsync(mrn);

            if (patient is null)
                throw new KeyNotFoundException($"Patient with MRN {mrn} not found.");

            return _mapper.Map<PatientDto>(patient);
        }

        public async Task<PatientDto> GetPatientByUserIdAsync(int userId)
        {
            var patient = await _repository.GetByUserIdAsync(userId);

            if (patient is null)
                throw new KeyNotFoundException($"Patient profile not found for user ID {userId}.");

            return _mapper.Map<PatientDto>(patient);
        }

        /// <summary>
        /// Used by authenticated patients to create or restore their profile after registration.
        /// </summary>
        public async Task<PatientDto> CompleteProfileAsync(int userId, CompletePatientProfileDto profileDto)
        {
            var user = await _repository.GetUserByIdAsync(userId);
            if (user is null || user.IsDeleted || !user.IsActive)
                throw new UnauthorizedAccessException("Invalid user.");

            var existingPatient = await _repository.GetByUserIdIncludingDeletedAsync(userId);

            if (existingPatient is not null)
            {
                if (!existingPatient.IsDeleted)
                    throw new InvalidOperationException(
                        "Patient profile already exists and is active. Use the update endpoint to modify your profile.");

                _logger.LogInformation("Restoring soft-deleted patient profile for user {UserId}.", userId);
                return await RestorePatientAsync(existingPatient, profileDto);
            }

            _logger.LogInformation("Creating new patient profile for user {UserId}.", userId);
            return await CreateNewPatientAsync(userId, profileDto);
        }

        /// <summary>
        /// Admin use only – creates user account and patient profile in one transaction.
        /// </summary>
        public async Task<PatientDto> CreatePatientAsync(CreatePatientDto createPatientDto)
        {
            if (await _repository.EmailExistsAsync(createPatientDto.Email))
                throw new InvalidOperationException($"User with email {createPatientDto.Email} already exists.");

            await _repository.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    Email = createPatientDto.Email,
                    PasswordHash = _passwordHasher.Hash(createPatientDto.Password),
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _repository.AddUserAsync(user);
                await _repository.SaveChangesAsync();

                var address = await CreateAddressAsync(createPatientDto.Address);

                var patient = new Patient
                {
                    UserId = user.UserId,
                    AddressId = address.AddressId,
                    MRN = Guid.NewGuid().ToString(),
                    FullName = createPatientDto.FullName,
                    DateOfBirth = createPatientDto.DateOfBirth,
                    Gender = createPatientDto.Gender,
                    PhoneNumber = createPatientDto.PhoneNumber,
                    BloodGroup = createPatientDto.BloodGroup,
                    EmergencyContact = createPatientDto.EmergencyContact,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _repository.AddPatientAsync(patient);
                await _repository.SaveChangesAsync();

                patient.MRN = GenerateMRN(patient.PatientId);
                await _repository.SaveChangesAsync();

                await _repository.CommitTransactionAsync();

                _logger.LogInformation(
                    "Admin created patient {PatientId} (MRN: {MRN}) for user {UserId}.",
                    patient.PatientId, patient.MRN, user.UserId);

                return await GetPatientByIdAsync(patient.PatientId);
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to create patient for email {Email}.", createPatientDto.Email);
                throw;
            }
        }

        public async Task<PatientDto> UpdatePatientAsync(int patientId, UpdatePatientDto updatePatientDto)
        {
            var patient = await _repository.GetByIdAsync(patientId);

            if (patient is null)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            _mapper.Map(updatePatientDto, patient);

            if (updatePatientDto.Address is not null)
            {
                var createAddressDto = _mapper.Map<CreateAddressDto>(updatePatientDto.Address);
                await UpsertAddressAsync(patient, createAddressDto);
            }

            await _repository.SaveChangesAsync();

            _logger.LogInformation("Updated patient {PatientId}.", patientId);
            return await GetPatientByIdAsync(patientId);
        }

        public async Task<bool> DeletePatientAsync(int patientId)
        {
            var patient = await _repository.GetByIdForUpdateAsync(patientId);

            if (patient is null)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            patient.IsDeleted = true;
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Soft-deleted patient {PatientId}.", patientId);
            return true;
        }

        public async Task<IEnumerable<PatientDto>> SearchPatientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return [];

            var pattern = $"%{searchTerm.Trim()}%";
            var patients = await _repository.SearchAsync(pattern);
            return _mapper.Map<IEnumerable<PatientDto>>(patients);
        }

        // Private helpers

        private async Task<PatientDto> RestorePatientAsync(Patient patient, CompletePatientProfileDto profileDto)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                await UpsertAddressAsync(patient, profileDto.Address);

                patient.FullName = profileDto.FullName;
                patient.DateOfBirth = profileDto.DateOfBirth;
                patient.Gender = profileDto.Gender;
                patient.PhoneNumber = profileDto.PhoneNumber;
                patient.BloodGroup = profileDto.BloodGroup;
                patient.EmergencyContact = profileDto.EmergencyContact;
                patient.IsDeleted = false;
                patient.CreatedAt = DateTime.UtcNow;

                await _repository.UpdatePatientAsync(patient);
                await _repository.SaveChangesAsync();
                await _repository.CommitTransactionAsync();

                return await GetPatientByIdAsync(patient.PatientId);
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to restore patient profile for patient {PatientId}.", patient.PatientId);
                throw;
            }
        }

        private async Task<PatientDto> CreateNewPatientAsync(int userId, CompletePatientProfileDto profileDto)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var address = await CreateAddressAsync(profileDto.Address);

                var patient = new Patient
                {
                    UserId = userId,
                    AddressId = address.AddressId,
                    MRN = Guid.NewGuid().ToString(),
                    FullName = profileDto.FullName,
                    DateOfBirth = profileDto.DateOfBirth,
                    Gender = profileDto.Gender,
                    PhoneNumber = profileDto.PhoneNumber,
                    BloodGroup = profileDto.BloodGroup,
                    EmergencyContact = profileDto.EmergencyContact,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };
                await _repository.AddPatientAsync(patient);
                await _repository.SaveChangesAsync();

                patient.MRN = GenerateMRN(patient.PatientId);
                await _repository.SaveChangesAsync();

                await _repository.CommitTransactionAsync();
                return await GetPatientByIdAsync(patient.PatientId);
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to create new patient profile for user {UserId}.", userId);
                throw;
            }
        }

        private async Task<Address> CreateAddressAsync(CreateAddressDto dto)
        {
            var address = _mapper.Map<Address>(dto);
            address.CreatedAt = DateTime.UtcNow;
            await _repository.AddAddressAsync(address);
            await _repository.SaveChangesAsync();
            return address;
        }

        private async Task UpsertAddressAsync(Patient patient, CreateAddressDto dto)
        {
            if (patient.AddressId > 0)
            {
                var existing = await _repository.GetAddressByIdAsync(patient.AddressId);
                if (existing is not null)
                {
                    _mapper.Map(dto, existing);
                    return;
                }
            }

            var address = await CreateAddressAsync(dto);
            patient.AddressId = address.AddressId;
        }

        private static string GenerateMRN(int patientId) =>
            $"MRN{DateTime.UtcNow:yyyyMMdd}{patientId:D6}";
    }
}
