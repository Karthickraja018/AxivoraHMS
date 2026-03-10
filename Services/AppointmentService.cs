using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Helpers;

namespace Axivora.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly AxivoraDbContext _context;
        private readonly IMapper _mapper;

        public AppointmentService(AxivoraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Status)
                .Where(a => !a.IsDeleted)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<PaginationResponse<AppointmentDto>> GetAllAppointmentsAsync(PaginationParams paginationParams)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Status)
                .Where(a => !a.IsDeleted);

            var totalCount = await query.CountAsync();

            var appointments = await query
                .OrderByDescending(a => a.AppointmentStart)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var appointmentDtos = _mapper.Map<IEnumerable<AppointmentDto>>(appointments);

            return new PaginationResponse<AppointmentDto>(
                appointmentDtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<AppointmentDto> GetAppointmentByIdAsync(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Status)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && !a.IsDeleted);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByPatientIdAsync(int patientId)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Status)
                .Where(a => a.PatientId == patientId && !a.IsDeleted)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByDoctorIdAsync(int doctorId)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Status)
                .Where(a => a.DoctorId == doctorId && !a.IsDeleted)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto)
        {
            var doctorExists = await _context.Doctors
                .IgnoreQueryFilters()
                .AnyAsync(d => d.DoctorId == createAppointmentDto.DoctorId && !d.IsDeleted);

            if (!doctorExists)
                throw new KeyNotFoundException($"Doctor with ID {createAppointmentDto.DoctorId} not found.");

            var patientExists = await _context.Patients
                .IgnoreQueryFilters()
                .AnyAsync(p => p.PatientId == createAppointmentDto.PatientId && !p.IsDeleted);

            if (!patientExists)
                throw new KeyNotFoundException($"Patient with ID {createAppointmentDto.PatientId} not found.");

            var statusExists = await _context.AppointmentStatuses
                .AnyAsync(s => s.StatusId == createAppointmentDto.StatusId);

            if (!statusExists)
                throw new KeyNotFoundException($"Appointment status with ID {createAppointmentDto.StatusId} not found.");

            var existingAppointment = await _context.Appointments
                .AnyAsync(a => a.DoctorId == createAppointmentDto.DoctorId &&
                    !a.IsDeleted &&
                    ((createAppointmentDto.AppointmentStart >= a.AppointmentStart &&
                      createAppointmentDto.AppointmentStart < a.AppointmentEnd) ||
                     (createAppointmentDto.AppointmentEnd > a.AppointmentStart &&
                      createAppointmentDto.AppointmentEnd <= a.AppointmentEnd)));

            if (existingAppointment)
                throw new InvalidOperationException("Doctor already has an appointment during this time slot.");

            var appointment = _mapper.Map<Appointment>(createAppointmentDto);
            appointment.CreatedAt = DateTime.UtcNow;
            appointment.IsDeleted = false;

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointment.AppointmentId);
        }

        public async Task<AppointmentDto> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto updateAppointmentDto)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);

            if (appointment == null || appointment.IsDeleted)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            _mapper.Map(updateAppointmentDto, appointment);

            await _context.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointmentId);
        }

        public async Task<bool> CancelAppointmentAsync(int appointmentId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            appointment.IsDeleted = true;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Status)
                .Where(a => !a.IsDeleted && 
                    a.AppointmentStart >= startDate && 
                    a.AppointmentStart <= endDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<PaginationResponse<AppointmentDto>> GetMyAppointmentsAsync(int userId, PaginationParams paginationParams, string? status)
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

            if (patient == null)
                throw new KeyNotFoundException("Patient profile not found. Please complete your profile first.");

            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Status)
                .Where(a => a.PatientId == patient.PatientId && !a.IsDeleted);

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                query = query.Where(a => a.Status != null && a.Status.StatusName == status);

            var totalCount = await query.CountAsync();

            var appointments = await query
                .OrderByDescending(a => a.AppointmentStart)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var appointmentDtos = _mapper.Map<IEnumerable<AppointmentDto>>(appointments);

            return new PaginationResponse<AppointmentDto>(
                appointmentDtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<PaginationResponse<AppointmentDto>> GetDoctorAppointmentsAsync(int userId, PaginationParams paginationParams, DateTime? date)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == userId && !d.IsDeleted);

            if (doctor == null)
                throw new KeyNotFoundException("Doctor profile not found.");

            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Status)
                .Where(a => a.DoctorId == doctor.DoctorId && !a.IsDeleted);

            if (date.HasValue)
            {
                var dayStart = date.Value.Date;
                var dayEnd = dayStart.AddDays(1);
                query = query.Where(a => a.AppointmentStart >= dayStart && a.AppointmentStart < dayEnd);
            }

            var totalCount = await query.CountAsync();

            var appointments = await query
                .OrderBy(a => a.AppointmentStart)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var appointmentDtos = _mapper.Map<IEnumerable<AppointmentDto>>(appointments);

            return new PaginationResponse<AppointmentDto>(
                appointmentDtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, string statusName)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && !a.IsDeleted);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            var status = await _context.AppointmentStatuses
                .FirstOrDefaultAsync(s => s.StatusName == statusName);

            if (status == null)
                throw new KeyNotFoundException($"Appointment status '{statusName}' not found.");

            appointment.StatusId = status.StatusId;
            await _context.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointmentId);
        }
    }
}
