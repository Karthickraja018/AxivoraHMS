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
    public class DoctorService : IDoctorService
    {
        private readonly AxivoraDbContext _context;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;

        public DoctorService(AxivoraDbContext context, IMapper mapper, IPasswordHasher passwordHasher)
        {
            _context = context;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
        }

        public async Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync()
        {
            var doctors = await _context.Doctors
                .Include(d => d.Address)
                .Include(d => d.DoctorDepartments)
                    .ThenInclude(dd => dd.Department)
                .Where(d => !d.IsDeleted)
                .ToListAsync();

            return _mapper.Map<IEnumerable<DoctorDto>>(doctors);
        }

        public async Task<PaginationResponse<DoctorDto>> GetAllDoctorsAsync(PaginationParams paginationParams)
        {
            var query = _context.Doctors
                .Include(d => d.Address)
                .Include(d => d.DoctorDepartments)
                    .ThenInclude(dd => dd.Department)
                .Where(d => !d.IsDeleted);

            var totalCount = await query.CountAsync();

            var doctors = await query
                .OrderBy(d => d.FullName)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var doctorDtos = _mapper.Map<IEnumerable<DoctorDto>>(doctors);

            return new PaginationResponse<DoctorDto>(
                doctorDtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<DoctorDto> GetDoctorByIdAsync(int doctorId)
        {
            var doctor = await _context.Doctors
                .Include(d => d.Address)
                .Include(d => d.DoctorDepartments)
                    .ThenInclude(dd => dd.Department)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.IsDeleted);

            if (doctor == null)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            return _mapper.Map<DoctorDto>(doctor);
        }

        /// <summary>
        /// Admin use only - creates user account and doctor profile in one transaction
        /// </summary>
        public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto createDoctorDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == createDoctorDto.Email))
                throw new InvalidOperationException($"User with email {createDoctorDto.Email} already exists.");

            if (await _context.Doctors.IgnoreQueryFilters().AnyAsync(d => d.LicenseNumber == createDoctorDto.LicenseNumber))
                throw new InvalidOperationException($"Doctor with license number {createDoctorDto.LicenseNumber} already exists.");

            if (createDoctorDto.DepartmentIds == null || !createDoctorDto.DepartmentIds.Any())
                throw new InvalidOperationException("At least one department must be specified.");

            var departmentCount = await _context.Departments
                .Where(d => createDoctorDto.DepartmentIds.Contains(d.DepartmentId))
                .CountAsync();

            if (departmentCount != createDoctorDto.DepartmentIds.Count)
                throw new InvalidOperationException("One or more specified departments do not exist.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    Email = createDoctorDto.Email,
                    PasswordHash = _passwordHasher.Hash(createDoctorDto.Password),
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var doctorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Doctor");
                if (doctorRole is null)
                {
                    doctorRole = new Role { RoleName = "Doctor" };
                    _context.Roles.Add(doctorRole);
                    await _context.SaveChangesAsync();
                }
                _context.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = doctorRole.RoleId });
                await _context.SaveChangesAsync();

                int? addressId = null;
                if (createDoctorDto.Address is not null)
                {
                    var address = _mapper.Map<Address>(createDoctorDto.Address);
                    address.CreatedAt = DateTime.UtcNow;
                    _context.Addresses.Add(address);
                    await _context.SaveChangesAsync();
                    addressId = address.AddressId;
                }

                var doctor = new Doctor
                {
                    UserId = user.UserId,
                    LicenseNumber = createDoctorDto.LicenseNumber,
                    FullName = createDoctorDto.FullName,
                    Qualification = createDoctorDto.Qualification,
                    ExperienceYears = createDoctorDto.ExperienceYears,
                    AddressId = addressId,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                foreach (var departmentId in createDoctorDto.DepartmentIds)
                {
                    _context.DoctorDepartments.Add(new DoctorDepartment
                    {
                        DoctorId = doctor.DoctorId,
                        DepartmentId = departmentId
                    });
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return await GetDoctorByIdAsync(doctor.DoctorId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<DoctorDto> UpdateDoctorAsync(int doctorId, UpdateDoctorDto updateDoctorDto)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);

            if (doctor == null || doctor.IsDeleted)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            _mapper.Map(updateDoctorDto, doctor);

            await _context.SaveChangesAsync();

            return await GetDoctorByIdAsync(doctorId);
        }

        public async Task<bool> DeleteDoctorAsync(int doctorId)
        {
            var doctor = await _context.Doctors.FindAsync(doctorId);

            if (doctor == null)
                throw new KeyNotFoundException($"Doctor with ID {doctorId} not found.");

            doctor.IsDeleted = true;
            doctor.IsActive = false;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DoctorDto>> GetDoctorsByDepartmentAsync(int departmentId)
        {
            var doctors = await _context.Doctors
                .Include(d => d.Address)
                .Include(d => d.DoctorDepartments)
                    .ThenInclude(dd => dd.Department)
                .Where(d => !d.IsDeleted && d.IsActive &&
                    d.DoctorDepartments.Any(dd => dd.DepartmentId == departmentId))
                .ToListAsync();

            return _mapper.Map<IEnumerable<DoctorDto>>(doctors);
        }
    }
}
