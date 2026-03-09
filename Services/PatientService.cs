using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Helpers;
using Axivora.Security;

namespace Axivora.Services
{
    public class PatientService : IPatientService
    {
        private readonly AxivoraDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            AxivoraDbContext context,
            IMapper mapper,
            IPasswordHasher passwordHasher,
            ILogger<PatientService> logger)
        {
            _context = context;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        // ?? Reusable base query (read-only, with standard includes) ???????????
        private IQueryable<Patient> ActivePatients() =>
            _context.Patients
                .Include(p => p.Address)
                .Include(p => p.PatientAllergies)
                .Where(p => !p.IsDeleted)
                .AsNoTracking();

        // ?????????????????????????????????????????????????????????????????????

        public async Task<IEnumerable<PatientDto>> GetAllPatientsAsync()
        {
            var patients = await ActivePatients().ToListAsync();
            return _mapper.Map<IEnumerable<PatientDto>>(patients);
        }

        public async Task<PaginationResponse<PatientDto>> GetAllPatientsAsync(PaginationParams paginationParams)
        {
            var query = ActivePatients();
            var totalCount = await query.CountAsync();

            var patients = await query
                .OrderBy(p => p.FullName)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            return new PaginationResponse<PatientDto>(
                _mapper.Map<IEnumerable<PatientDto>>(patients),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<PatientDto> GetPatientByIdAsync(int patientId)
        {
            var patient = await ActivePatients()
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (patient is null)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            return _mapper.Map<PatientDto>(patient);
        }

        public async Task<PatientDto> GetPatientByMRNAsync(string mrn)
        {
            var patient = await ActivePatients()
                .FirstOrDefaultAsync(p => p.MRN == mrn);

            if (patient is null)
                throw new KeyNotFoundException($"Patient with MRN {mrn} not found.");

            return _mapper.Map<PatientDto>(patient);
        }

        public async Task<PatientDto> GetPatientByUserIdAsync(int userId)
        {
            var patient = await ActivePatients()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient is null)
                throw new KeyNotFoundException($"Patient profile not found for user ID {userId}.");

            return _mapper.Map<PatientDto>(patient);
        }

        /// <summary>
        /// Used by authenticated patients to create or restore their profile after registration.
        /// </summary>
        public async Task<PatientDto> CompleteProfileAsync(int userId, CompletePatientProfileDto profileDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user is null || user.IsDeleted || !user.IsActive)
                throw new UnauthorizedAccessException("Invalid user.");

            // Check ALL records including soft-deleted ones.
            var existingPatient = await _context.Patients
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UserId == userId);

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
            if (await _context.Users.AnyAsync(u => u.Email == createPatientDto.Email))
                throw new InvalidOperationException(
                    $"User with email {createPatientDto.Email} already exists.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
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
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

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
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                patient.MRN = GenerateMRN(patient.PatientId);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Admin created patient {PatientId} (MRN: {MRN}) for user {UserId}.",
                    patient.PatientId, patient.MRN, user.UserId);

                return await GetPatientByIdAsync(patient.PatientId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create patient for email {Email}.", createPatientDto.Email);
                throw;
            }
        }

        public async Task<PatientDto> UpdatePatientAsync(int patientId, UpdatePatientDto updatePatientDto)
        {
            var patient = await _context.Patients.FindAsync(patientId);

            if (patient is null || patient.IsDeleted)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            _mapper.Map(updatePatientDto, patient);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated patient {PatientId}.", patientId);
            return await GetPatientByIdAsync(patientId);
        }

        public async Task<bool> DeletePatientAsync(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);

            if (patient is null)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            patient.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Soft-deleted patient {PatientId}.", patientId);
            return true;
        }

        public async Task<IEnumerable<PatientDto>> SearchPatientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return [];

            var pattern = $"%{searchTerm.Trim()}%";

            var patients = await _context.Patients
                .Include(p => p.Address)
                .Include(p => p.PatientAllergies)
                .Where(p => !p.IsDeleted && (
                    EF.Functions.Like(p.FullName, pattern) ||
                    EF.Functions.Like(p.MRN, pattern) ||
                    (p.PhoneNumber != null && EF.Functions.Like(p.PhoneNumber, pattern))))
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<IEnumerable<PatientDto>>(patients);
        }

        // ?? Private helpers ???????????????????????????????????????????????????

        private async Task<PatientDto> RestorePatientAsync(Patient patient, CompletePatientProfileDto profileDto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
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

                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetPatientByIdAsync(patient.PatientId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to restore patient profile for patient {PatientId}.", patient.PatientId);
                throw;
            }
        }

        private async Task<PatientDto> CreateNewPatientAsync(int userId, CompletePatientProfileDto profileDto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
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
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                patient.MRN = GenerateMRN(patient.PatientId);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return await GetPatientByIdAsync(patient.PatientId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create new patient profile for user {UserId}.", userId);
                throw;
            }
        }

        /// <summary>Creates a new Address row and returns it.</summary>
        private async Task<Address> CreateAddressAsync(CreateAddressDto dto)
        {
            var address = _mapper.Map<Address>(dto);
            address.CreatedAt = DateTime.UtcNow;
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
            return address;
        }

        /// <summary>
        /// Updates an existing address if found, otherwise creates a new one and
        /// links it to the patient.
        /// </summary>
        private async Task UpsertAddressAsync(Patient patient, CreateAddressDto dto)
        {
            if (patient.AddressId > 0)
            {
                var existing = await _context.Addresses.FindAsync(patient.AddressId);
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
