using AutoMapper;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Helpers;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IMapper _mapper;

        public AppointmentService(IAppointmentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync()
        {
            var appointments = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<PaginationResponse<AppointmentDto>> GetAllAppointmentsAsync(PaginationParams paginationParams)
        {
            var totalCount = await _repository.CountAsync();
            var appointments = await _repository.GetPagedAsync(
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            return new PaginationResponse<AppointmentDto>(
                _mapper.Map<IEnumerable<AppointmentDto>>(appointments),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<AppointmentDto> GetAppointmentByIdAsync(int appointmentId)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<AppointmentDto> GetAppointmentByIdAsync(int appointmentId, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            await EnforceOwnershipAsync(appointment, callerUserId, callerRole);

            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByPatientIdAsync(int patientId)
        {
            var appointments = await _repository.GetByPatientIdAsync(patientId);
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByDoctorIdAsync(int doctorId)
        {
            var appointments = await _repository.GetByDoctorIdAsync(doctorId);
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto)
        {
            if (!await _repository.DoctorExistsAsync(createAppointmentDto.DoctorId))
                throw new KeyNotFoundException($"Doctor with ID {createAppointmentDto.DoctorId} not found.");

            if (!await _repository.PatientExistsAsync(createAppointmentDto.PatientId))
                throw new KeyNotFoundException($"Patient with ID {createAppointmentDto.PatientId} not found.");

            if (!await _repository.StatusExistsAsync(createAppointmentDto.StatusId))
                throw new KeyNotFoundException($"Appointment status with ID {createAppointmentDto.StatusId} not found.");

            if (await _repository.HasConflictAsync(createAppointmentDto.DoctorId, createAppointmentDto.AppointmentStart, createAppointmentDto.AppointmentEnd))
                throw new InvalidOperationException("Doctor already has an appointment during this time slot.");

            if (!await _repository.IsWithinDoctorScheduleAsync(createAppointmentDto.DoctorId, createAppointmentDto.AppointmentStart, createAppointmentDto.AppointmentEnd))
                throw new InvalidOperationException("The requested time slot does not fall within the doctor's scheduled working hours.");

            var appointment = _mapper.Map<Appointment>(createAppointmentDto);
            appointment.CreatedAt = DateTime.UtcNow;
            appointment.IsDeleted = false;

            await _repository.AddAsync(appointment);
            await _repository.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointment.AppointmentId);
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto createAppointmentDto, int callerUserId, string callerRole)
        {
            if (callerRole == "Patient")
            {
                var callerPatient = await _repository.GetPatientByUserIdAsync(callerUserId);

                if (callerPatient == null)
                    throw new KeyNotFoundException("Patient profile not found. Please complete your profile first.");

                createAppointmentDto.PatientId = callerPatient.PatientId;
            }

            var result = await CreateAppointmentAsync(createAppointmentDto);

            if (callerRole is "Doctor" or "Admin")
            {
                await _repository.AddAuditLogAsync(new AuditLog
                {
                    UserId = callerUserId,
                    Action = "CreateAppointment",
                    EntityName = "Appointment",
                    EntityId = result.AppointmentId,
                    NewValue = $"PatientId={result.PatientId}, DoctorId={result.DoctorId}, Start={result.AppointmentStart:O}"
                });
                await _repository.SaveChangesAsync();
            }

            return result;
        }

        public async Task<AppointmentDto> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto updateAppointmentDto)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            _mapper.Map(updateAppointmentDto, appointment);
            await _repository.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointmentId);
        }

        public async Task<AppointmentDto> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto updateAppointmentDto, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            await EnforceOwnershipAsync(appointment, callerUserId, callerRole);

            if (updateAppointmentDto.StatusId.HasValue)
            {
                var targetStatus = await _repository.GetStatusByIdAsync(updateAppointmentDto.StatusId.Value);

                if (targetStatus == null)
                    throw new KeyNotFoundException($"Appointment status with ID {updateAppointmentDto.StatusId.Value} not found.");

                var currentStatusName = appointment.Status?.StatusName ?? string.Empty;
                AppointmentStatusTransitions.Validate(currentStatusName, targetStatus.StatusName, callerRole);
            }

            _mapper.Map(updateAppointmentDto, appointment);
            await _repository.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointmentId);
        }

        public async Task<bool> CancelAppointmentAsync(int appointmentId)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            appointment.IsDeleted = true;
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAppointmentAsync(int appointmentId, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            await EnforceOwnershipAsync(appointment, callerUserId, callerRole);

            appointment.IsDeleted = true;
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var appointments = await _repository.GetByDateRangeAsync(startDate, endDate);
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<PaginationResponse<AppointmentDto>> GetMyAppointmentsAsync(int userId, PaginationParams paginationParams, string? status)
        {
            var patient = await _repository.GetPatientByUserIdAsync(userId);

            if (patient == null)
                throw new KeyNotFoundException("Patient profile not found. Please complete your profile first.");

            var totalCount = await _repository.CountByPatientAsync(patient.PatientId, status);
            var appointments = await _repository.GetPagedByPatientAsync(
                patient.PatientId, status,
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            return new PaginationResponse<AppointmentDto>(
                _mapper.Map<IEnumerable<AppointmentDto>>(appointments),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<PaginationResponse<AppointmentDto>> GetDoctorAppointmentsAsync(int userId, PaginationParams paginationParams, DateTime? date)
        {
            var doctor = await _repository.GetDoctorByUserIdAsync(userId);

            if (doctor == null)
                throw new KeyNotFoundException("Doctor profile not found.");

            var totalCount = await _repository.CountByDoctorAsync(doctor.DoctorId, date);
            var appointments = await _repository.GetPagedByDoctorAsync(
                doctor.DoctorId, date,
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            return new PaginationResponse<AppointmentDto>(
                _mapper.Map<IEnumerable<AppointmentDto>>(appointments),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, string statusName)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            var status = await _repository.GetStatusByNameAsync(statusName);

            if (status == null)
                throw new KeyNotFoundException($"Appointment status '{statusName}' not found.");

            appointment.StatusId = status.StatusId;
            await _repository.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointmentId);
        }

        public async Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, string statusName, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            var targetStatus = await _repository.GetStatusByNameAsync(statusName);

            if (targetStatus == null)
                throw new KeyNotFoundException($"Appointment status '{statusName}' not found.");

            var currentStatusName = appointment.Status?.StatusName ?? string.Empty;
            AppointmentStatusTransitions.Validate(currentStatusName, statusName, callerRole);

            appointment.StatusId = targetStatus.StatusId;
            await _repository.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointmentId);
        }

        public async Task<AppointmentDto?> RescheduleAsync(int id, RescheduleAppointmentDto dto, int currentUserId, string role)
        {
            var appointment = await _repository.GetByIdAsync(id);

            if (appointment is null)
                return null;

            if (role == "Patient")
            {
                var patient = await _repository.GetPatientByUserIdAsync(currentUserId);

                if (patient is null || appointment.PatientId != patient.PatientId)
                    throw new UnauthorizedAccessException("You do not have permission to reschedule this appointment.");
            }

            var statusName = appointment.Status?.StatusName ?? string.Empty;
            if (statusName is "Completed" or "Cancelled")
                throw new InvalidOperationException("Cannot reschedule a completed or cancelled appointment.");

            if (await _repository.HasConflictAsync(appointment.DoctorId, dto.AppointmentStart, dto.AppointmentEnd, id))
                throw new InvalidOperationException("The requested time slot is already taken by another appointment for this doctor.");

            appointment.AppointmentStart = dto.AppointmentStart;
            appointment.AppointmentEnd = dto.AppointmentEnd;

            await _repository.SaveChangesAsync();

            return await GetAppointmentByIdAsync(id);
        }

        private async Task EnforceOwnershipAsync(Appointment appointment, int callerUserId, string callerRole)
        {
            if (callerRole == "Admin")
                return;

            if (callerRole == "Doctor")
            {
                var doctor = await _repository.GetDoctorByUserIdAsync(callerUserId);

                if (doctor == null || appointment.DoctorId != doctor.DoctorId)
                    throw new UnauthorizedAccessException("You do not have permission to access this appointment.");

                return;
            }

            var patient = await _repository.GetPatientByUserIdAsync(callerUserId);

            if (patient == null || appointment.PatientId != patient.PatientId)
                throw new UnauthorizedAccessException("You do not have permission to access this appointment.");
        }
    }
}
