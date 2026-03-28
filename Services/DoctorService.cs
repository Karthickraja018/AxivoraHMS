using AutoMapper;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Helpers;
using Axivora.Security;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository _repository;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;

        public DoctorService(
            IDoctorRepository repository,
            IMapper mapper,
            IPasswordHasher passwordHasher,
            IEmailService emailService)
        {
            _repository   = repository;
            _mapper       = mapper;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        public async Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync()
        {
            var doctors = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<DoctorDto>>(doctors);
        }

        public async Task<PaginationResponse<DoctorDto>> GetAllDoctorsAsync(PaginationParams paginationParams)
        {
            var totalCount = await _repository.CountAsync();
            var doctors = await _repository.GetPagedAsync(
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            return new PaginationResponse<DoctorDto>(
                _mapper.Map<IEnumerable<DoctorDto>>(doctors),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<DoctorDto> GetDoctorByIdAsync(int doctorId)
        {
            var doctor = await _repository.GetByIdAsync(doctorId);

            if (doctor == null)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            return _mapper.Map<DoctorDto>(doctor);
        }

        public async Task<DoctorDto?> GetDoctorByUserIdAsync(int userId)
        {
            var doctor = await _repository.GetByUserIdAsync(userId);
            return doctor == null ? null : _mapper.Map<DoctorDto>(doctor);
        }

        /// <summary>
        /// Admin-only: User + Doctor role + invitation email. No <see cref="Doctor"/> row until the clinician completes their profile.
        /// </summary>
        public async Task InviteDoctorAsync(InviteDoctorDto dto)
        {
            if (await _repository.EmailExistsAsync(dto.Email))
                throw new InvalidOperationException($"User with email {dto.Email} already exists.");

            var user = new User
            {
                Email           = dto.Email,
                PasswordHash    = _passwordHasher.Hash(dto.Password),
                IsActive        = true,
                IsDeleted       = false,
                IsEmailVerified = true,
                CreatedAt       = DateTime.UtcNow,
                UpdatedAt       = DateTime.UtcNow
            };
            await _repository.AddUserAsync(user);
            await _repository.SaveChangesAsync();

            var doctorRole = await _repository.GetRoleByNameAsync("Doctor");
            if (doctorRole is null)
            {
                doctorRole = new Role { RoleName = "Doctor" };
                await _repository.AddRoleAsync(doctorRole);
                await _repository.SaveChangesAsync();
            }

            await _repository.AddUserRoleAsync(new UserRole { UserId = user.UserId, RoleId = doctorRole.RoleId });
            await _repository.SaveChangesAsync();

            var salutation = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.Email : dto.DisplayName.Trim();
            await _emailService.SendDoctorAccountCreatedAsync(dto.Email, salutation, dto.Password);
        }

        /// <summary>
        /// Doctor completes their own profile (first-time setup after admin invite).
        /// </summary>
        public async Task<DoctorDto> CompleteDoctorProfileAsync(int userId, CompleteDoctorProfileDto dto)
        {
            if (await _repository.GetByUserIdAsync(userId) != null)
                throw new InvalidOperationException("Your clinician profile is already set up.");

            if (await _repository.LicenseNumberExistsAsync(dto.LicenseNumber))
                throw new InvalidOperationException($"Doctor with license number {dto.LicenseNumber} already exists.");

            if (dto.DepartmentIds == null || !dto.DepartmentIds.Any())
                throw new InvalidOperationException("At least one department must be specified.");

            var departmentCount = await _repository.CountMatchingDepartmentsAsync(dto.DepartmentIds);
            if (departmentCount != dto.DepartmentIds.Count)
                throw new InvalidOperationException("One or more specified departments do not exist.");

            await _repository.BeginTransactionAsync();
            try
            {
                int? addressId = null;
                if (dto.Address is not null)
                {
                    var address = _mapper.Map<Address>(dto.Address);
                    address.CreatedAt = DateTime.UtcNow;
                    await _repository.AddAddressAsync(address);
                    await _repository.SaveChangesAsync();
                    addressId = address.AddressId;
                }

                var doctor = new Doctor
                {
                    UserId          = userId,
                    LicenseNumber   = dto.LicenseNumber,
                    FullName        = dto.FullName,
                    Qualification   = dto.Qualification,
                    ExperienceYears = dto.ExperienceYears,
                    AddressId       = addressId,
                    IsActive        = true,
                    IsDeleted       = false,
                    CreatedAt       = DateTime.UtcNow
                };
                await _repository.AddDoctorAsync(doctor);
                await _repository.SaveChangesAsync();

                foreach (var departmentId in dto.DepartmentIds)
                {
                    await _repository.AddDoctorDepartmentAsync(new DoctorDepartment
                    {
                        DoctorId     = doctor.DoctorId,
                        DepartmentId = departmentId
                    });
                }

                await _repository.SaveChangesAsync();
                await _repository.CommitTransactionAsync();

                return await GetDoctorByIdAsync(doctor.DoctorId);
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Admin use only - creates user account and doctor profile in one transaction
        /// </summary>
        public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto createDoctorDto)
        {
            if (await _repository.EmailExistsAsync(createDoctorDto.Email))
                throw new InvalidOperationException($"User with email {createDoctorDto.Email} already exists.");

            if (await _repository.LicenseNumberExistsAsync(createDoctorDto.LicenseNumber))
                throw new InvalidOperationException($"Doctor with license number {createDoctorDto.LicenseNumber} already exists.");

            if (createDoctorDto.DepartmentIds == null || !createDoctorDto.DepartmentIds.Any())
                throw new InvalidOperationException("At least one department must be specified.");

            var departmentCount = await _repository.CountMatchingDepartmentsAsync(createDoctorDto.DepartmentIds);
            if (departmentCount != createDoctorDto.DepartmentIds.Count)
                throw new InvalidOperationException("One or more specified departments do not exist.");

            await _repository.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    Email        = createDoctorDto.Email,
                    PasswordHash = _passwordHasher.Hash(createDoctorDto.Password),
                    IsActive     = true,
                    IsDeleted    = false,
                    CreatedAt    = DateTime.UtcNow,
                    UpdatedAt    = DateTime.UtcNow
                };
                await _repository.AddUserAsync(user);
                await _repository.SaveChangesAsync();

                var doctorRole = await _repository.GetRoleByNameAsync("Doctor");
                if (doctorRole is null)
                {
                    doctorRole = new Role { RoleName = "Doctor" };
                    await _repository.AddRoleAsync(doctorRole);
                    await _repository.SaveChangesAsync();
                }
                await _repository.AddUserRoleAsync(new UserRole { UserId = user.UserId, RoleId = doctorRole.RoleId });
                await _repository.SaveChangesAsync();

                int? addressId = null;
                if (createDoctorDto.Address is not null)
                {
                    var address = _mapper.Map<Address>(createDoctorDto.Address);
                    address.CreatedAt = DateTime.UtcNow;
                    await _repository.AddAddressAsync(address);
                    await _repository.SaveChangesAsync();
                    addressId = address.AddressId;
                }

                var doctor = new Doctor
                {
                    UserId         = user.UserId,
                    LicenseNumber  = createDoctorDto.LicenseNumber,
                    FullName       = createDoctorDto.FullName,
                    Qualification  = createDoctorDto.Qualification,
                    ExperienceYears = createDoctorDto.ExperienceYears,
                    AddressId      = addressId,
                    IsActive       = true,
                    IsDeleted      = false,
                    CreatedAt      = DateTime.UtcNow
                };
                await _repository.AddDoctorAsync(doctor);
                await _repository.SaveChangesAsync();

                foreach (var departmentId in createDoctorDto.DepartmentIds)
                {
                    await _repository.AddDoctorDepartmentAsync(new DoctorDepartment
                    {
                        DoctorId     = doctor.DoctorId,
                        DepartmentId = departmentId
                    });
                }
                await _repository.SaveChangesAsync();

                await _repository.CommitTransactionAsync();

                // Enqueue welcome email with temporary credentials after successful commit
                await _emailService.SendDoctorAccountCreatedAsync(
                    createDoctorDto.Email,
                    createDoctorDto.FullName,
                    createDoctorDto.Password);

                return await GetDoctorByIdAsync(doctor.DoctorId);
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<DoctorDto> UpdateDoctorAsync(int doctorId, UpdateDoctorDto updateDoctorDto)
        {
            var doctor = await _repository.FindAsync(doctorId);

            if (doctor == null || doctor.IsDeleted)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            _mapper.Map(updateDoctorDto, doctor);
            await _repository.SaveChangesAsync();

            return await GetDoctorByIdAsync(doctorId);
        }

        public async Task<bool> DeleteDoctorAsync(int doctorId)
        {
            var doctor = await _repository.FindAsync(doctorId);

            if (doctor == null)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            doctor.IsDeleted = true;
            doctor.IsActive = false;
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DoctorDto>> GetDoctorsByDepartmentAsync(int departmentId)
        {
            var doctors = await _repository.GetByDepartmentAsync(departmentId);
            return _mapper.Map<IEnumerable<DoctorDto>>(doctors);
        }
    }
}
