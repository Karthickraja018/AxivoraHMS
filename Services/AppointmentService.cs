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
    }
}
