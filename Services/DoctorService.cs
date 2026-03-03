using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly AxivoraDbContext _context;
        private readonly IMapper _mapper;

        public DoctorService(AxivoraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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

        public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto createDoctorDto)
        {
            var doctor = _mapper.Map<Doctor>(createDoctorDto);
            doctor.CreatedAt = DateTime.UtcNow;
            doctor.IsDeleted = false;
            doctor.IsActive = true;

            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();

            if (createDoctorDto.DepartmentIds != null && createDoctorDto.DepartmentIds.Any())
            {
                foreach (var departmentId in createDoctorDto.DepartmentIds)
                {
                    _context.DoctorDepartments.Add(new DoctorDepartment
                    {
                        DoctorId = doctor.DoctorId,
                        DepartmentId = departmentId
                    });
                }
                await _context.SaveChangesAsync();
            }

            return await GetDoctorByIdAsync(doctor.DoctorId);
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
