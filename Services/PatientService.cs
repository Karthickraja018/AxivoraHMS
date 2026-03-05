using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class PatientService : IPatientService
    {
        private readonly AxivoraDbContext _context;
        private readonly IMapper _mapper;

        public PatientService(AxivoraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PatientDto>> GetAllPatientsAsync()
        {
            var patients = await _context.Patients
                .Include(p => p.Address)
                .Include(p => p.PatientAllergies)
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            return _mapper.Map<IEnumerable<PatientDto>>(patients);
        }

        public async Task<PaginationResponse<PatientDto>> GetAllPatientsAsync(PaginationParams paginationParams)
        {
            var query = _context.Patients
                .Include(p => p.Address)
                .Include(p => p.PatientAllergies)
                .Where(p => !p.IsDeleted);

            var totalCount = await query.CountAsync();

            var patients = await query
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var patientDtos = _mapper.Map<IEnumerable<PatientDto>>(patients);

            return new PaginationResponse<PatientDto>(
                patientDtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<PatientDto> GetPatientByIdAsync(int patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.Address)
                .Include(p => p.PatientAllergies)
                .FirstOrDefaultAsync(p => p.PatientId == patientId && !p.IsDeleted);

            if (patient == null)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            return _mapper.Map<PatientDto>(patient);
        }

        public async Task<PatientDto> GetPatientByMRNAsync(string mrn)
        {
            var patient = await _context.Patients
                .Include(p => p.Address)
                .Include(p => p.PatientAllergies)
                .FirstOrDefaultAsync(p => p.MRN == mrn && !p.IsDeleted);

            if (patient == null)
                throw new KeyNotFoundException($"Patient with MRN {mrn} not found.");

            return _mapper.Map<PatientDto>(patient);
        }

        public async Task<PatientDto> GetPatientByUserIdAsync(int userId)
        {
            var patient = await _context.Patients
                .Include(p => p.Address)
                .Include(p => p.PatientAllergies)
                .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

            if (patient == null)
                throw new KeyNotFoundException($"Patient profile not found for user ID {userId}.");

            return _mapper.Map<PatientDto>(patient);
        }

        /// <summary>
        /// Used by authenticated users to complete their patient profile after registration
        /// </summary>
        public async Task<PatientDto> CompleteProfileAsync(int userId, CompletePatientProfileDto profileDto)
        {
            // Check if user exists and is active
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsDeleted || !user.IsActive)
                throw new UnauthorizedAccessException("Invalid user.");

            // Check if profile already exists (including soft-deleted ones)
            var existingPatient = await _context.Patients
                .IgnoreQueryFilters() // Important: Check ALL patients including soft-deleted
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existingPatient != null)
            {
                // If patient exists and is soft-deleted, restore and update it
                if (existingPatient.IsDeleted)
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    
                    try
                    {
                        // Update or create address
                        Address address;
                        if (existingPatient.AddressId > 0)
                        {
                            address = await _context.Addresses.FindAsync(existingPatient.AddressId);
                            if (address != null)
                            {
                                _mapper.Map(profileDto.Address, address);
                            }
                            else
                            {
                                address = _mapper.Map<Address>(profileDto.Address);
                                address.CreatedAt = DateTime.UtcNow;
                                _context.Addresses.Add(address);
                                await _context.SaveChangesAsync();
                                existingPatient.AddressId = address.AddressId;
                            }
                        }
                        else
                        {
                            address = _mapper.Map<Address>(profileDto.Address);
                            address.CreatedAt = DateTime.UtcNow;
                            _context.Addresses.Add(address);
                            await _context.SaveChangesAsync();
                            existingPatient.AddressId = address.AddressId;
                        }

                        // Restore and update patient
                        existingPatient.FullName = profileDto.FullName;
                        existingPatient.DateOfBirth = profileDto.DateOfBirth;
                        existingPatient.Gender = profileDto.Gender;
                        existingPatient.PhoneNumber = profileDto.PhoneNumber;
                        existingPatient.BloodGroup = profileDto.BloodGroup;
                        existingPatient.EmergencyContact = profileDto.EmergencyContact;
                        existingPatient.IsDeleted = false;
                        existingPatient.CreatedAt = DateTime.UtcNow;
                        
                        _context.Patients.Update(existingPatient);
                        await _context.SaveChangesAsync();
                        
                        await transaction.CommitAsync();
                        
                        return await GetPatientByIdAsync(existingPatient.PatientId);
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                else
                {
                    // Patient already exists and is active - cannot complete profile again
                    throw new InvalidOperationException("Patient profile already exists and is active. Use the update endpoint to modify your profile.");
                }
            }

            // No existing patient found - create new one
            using var newTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create Address
                var newAddress = _mapper.Map<Address>(profileDto.Address);
                newAddress.CreatedAt = DateTime.UtcNow;

                _context.Addresses.Add(newAddress);
                await _context.SaveChangesAsync();

                // Create Patient with temporary unique MRN to avoid unique constraint collision
                var patient = new Patient
                {
                    UserId = userId,
                    AddressId = newAddress.AddressId,
                    MRN = Guid.NewGuid().ToString(), // temporary unique value
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

                // Now that PatientId is generated, create the final MRN based on the persisted id
                patient.MRN = GenerateMRNFromId(patient.PatientId);
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                await newTransaction.CommitAsync();

                return await GetPatientByIdAsync(patient.PatientId);
            }
            catch
            {
                await newTransaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Admin use only - creates user account and patient profile in one transaction
        /// </summary>
        public async Task<PatientDto> CreatePatientAsync(CreatePatientDto createPatientDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == createPatientDto.Email))
                    throw new InvalidOperationException($"User with email {createPatientDto.Email} already exists.");

                // 2. Create User account
                var user = new User
                {
                    Email = createPatientDto.Email,
                    PasswordHash = createPatientDto.Password, // TODO: Hash password with BCrypt
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 3. Create Address
                var address = _mapper.Map<Address>(createPatientDto.Address);
                address.CreatedAt = DateTime.UtcNow;
                
                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();

                // 4. Create Patient with temporary MRN
                var patient = new Patient
                {
                    UserId = user.UserId,
                    AddressId = address.AddressId,
                    MRN = Guid.NewGuid().ToString(), // temporary unique value
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

                // Set final MRN using the generated PatientId
                patient.MRN = GenerateMRNFromId(patient.PatientId);
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return await GetPatientByIdAsync(patient.PatientId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PatientDto> UpdatePatientAsync(int patientId, UpdatePatientDto updatePatientDto)
        {
            var patient = await _context.Patients.FindAsync(patientId);

            if (patient == null || patient.IsDeleted)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            _mapper.Map(updatePatientDto, patient);

            await _context.SaveChangesAsync();

            return await GetPatientByIdAsync(patientId);
        }

        public async Task<bool> DeletePatientAsync(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);

            if (patient == null)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            // Soft delete
            patient.IsDeleted = true;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<PatientDto>> SearchPatientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<PatientDto>();

            var normalizedSearchTerm = searchTerm.Trim().ToLower();

            var patients = await _context.Patients
                .Include(p => p.Address)
                .Include(p => p.PatientAllergies)
                .Where(p => !p.IsDeleted && 
                    (p.FullName.ToLower().Contains(normalizedSearchTerm) || 
                     p.MRN.ToLower().Contains(normalizedSearchTerm) ||
                     (p.PhoneNumber != null && p.PhoneNumber.Contains(searchTerm))))
                .ToListAsync();

            return _mapper.Map<IEnumerable<PatientDto>>(patients);
        }

        private string GenerateMRNFromId(int patientId)
        {
            return $"MRN{DateTime.UtcNow:yyyyMMdd}{patientId:D6}";
        }

        private async Task<string> GenerateMRNAsync()
        {
            var lastPatient = await _context.Patients
                .OrderByDescending(p => p.PatientId)
                .FirstOrDefaultAsync();

            var nextId = (lastPatient?.PatientId ?? 0) + 1;
            return $"MRN{DateTime.UtcNow:yyyyMMdd}{nextId:D6}";
        }
    }
}
