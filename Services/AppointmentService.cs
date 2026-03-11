using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            IAppointmentRepository repository,
            IMapper mapper,
            ILogger<AppointmentService> logger)
        {
            _repository = repository;
            _mapper     = mapper;
            _logger     = logger;
        }

        // ?? Read ??????????????????????????????????????????????????????????????

        public async Task<IEnumerable<AppointmentDto>> GetAllAppointmentsAsync()
        {
            var appointments = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<PaginationResponse<AppointmentDto>> GetAllAppointmentsAsync(PaginationParams paginationParams)
        {
            var totalCount   = await _repository.CountAsync();
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
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");
            return _mapper.Map<AppointmentDto>(appointment);
        }

        public async Task<AppointmentDto> GetAppointmentByIdAsync(int appointmentId, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");
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

        public async Task<IEnumerable<AppointmentDto>> GetAppointmentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var appointments = await _repository.GetByDateRangeAsync(startDate, endDate);
            return _mapper.Map<IEnumerable<AppointmentDto>>(appointments);
        }

        public async Task<PaginationResponse<AppointmentDto>> GetMyAppointmentsAsync(int userId, PaginationParams paginationParams, string? status)
        {
            var patient = await _repository.GetPatientByUserIdAsync(userId)
                ?? throw new KeyNotFoundException("Patient profile not found. Please complete your profile first.");

            var totalCount   = await _repository.CountByPatientAsync(patient.PatientId, status);
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
            var doctor = await _repository.GetDoctorByUserIdAsync(userId)
                ?? throw new KeyNotFoundException("Doctor profile not found.");

            var totalCount   = await _repository.CountByDoctorAsync(doctor.DoctorId, date);
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

        // ?? Update ????????????????????????????????????????????????????????????

        public async Task<AppointmentDto> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto updateAppointmentDto)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            _mapper.Map(updateAppointmentDto, appointment);
            await _repository.SaveChangesAsync();
            return await GetAppointmentByIdAsync(appointmentId);
        }

        public async Task<AppointmentDto> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentDto updateAppointmentDto, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            await EnforceOwnershipAsync(appointment, callerUserId, callerRole);

            if (updateAppointmentDto.StatusId.HasValue)
            {
                var targetStatus = await _repository.GetStatusByIdAsync(updateAppointmentDto.StatusId.Value)
                    ?? throw new KeyNotFoundException($"Appointment status with ID {updateAppointmentDto.StatusId.Value} not found.");

                var currentStatusName = appointment.Status?.StatusName ?? string.Empty;
                AppointmentStatusTransitions.Validate(currentStatusName, targetStatus.StatusName, callerRole);
            }

            _mapper.Map(updateAppointmentDto, appointment);
            await _repository.SaveChangesAsync();
            return await GetAppointmentByIdAsync(appointmentId);
        }

        public async Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, string statusName)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            var status = await _repository.GetStatusByNameAsync(statusName)
                ?? throw new KeyNotFoundException($"Appointment status '{statusName}' not found.");

            appointment.StatusId = status.StatusId;
            await _repository.SaveChangesAsync();
            return await GetAppointmentByIdAsync(appointmentId);
        }

        public async Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, string statusName, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            var targetStatus = await _repository.GetStatusByNameAsync(statusName)
                ?? throw new KeyNotFoundException($"Appointment status '{statusName}' not found.");

            var currentStatusName = appointment.Status?.StatusName ?? string.Empty;
            AppointmentStatusTransitions.Validate(currentStatusName, statusName, callerRole);

            appointment.StatusId = targetStatus.StatusId;
            await _repository.SaveChangesAsync();
            return await GetAppointmentByIdAsync(appointmentId);
        }

        // ?? Cancel (legacy) ???????????????????????????????????????????????????

        public async Task<bool> CancelAppointmentAsync(int appointmentId)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            appointment.IsDeleted = true;
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAppointmentAsync(int appointmentId, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            await EnforceOwnershipAsync(appointment, callerUserId, callerRole);
            appointment.IsDeleted = true;
            await _repository.SaveChangesAsync();
            return true;
        }

        // ?? Slot-based Booking ????????????????????????????????????????????????

        /// <summary>
        /// Books a slot for the caller patient.
        ///
        /// FIX 1 — Race condition prevention: the slot is fetched and its availability
        /// validated INSIDE the transaction so no concurrent request can book the same slot
        /// between the check and the update.
        ///
        /// FIX 2 — Optimistic concurrency: if two transactions reach SaveChanges at the same
        /// time, the RowVersion token on AppointmentSlot causes one to throw
        /// DbUpdateConcurrencyException, which is converted to InvalidOperationException.
        /// </summary>
        public async Task<AppointmentDto> BookAsync(CreateAppointmentDto dto, int callerUserId)
        {
            var patient = await _repository.GetPatientByUserIdAsync(callerUserId)
                ?? throw new KeyNotFoundException("Patient profile not found. Please complete your profile first.");

            // Resolve the "Scheduled" status before opening the transaction to minimise its duration
            var status = await _repository.GetStatusByNameAsync("Scheduled")
                ?? throw new InvalidOperationException("Appointment status 'Scheduled' is not configured.");

            await _repository.BeginTransactionAsync();
            try
            {
                // FIX 1: Fetch and validate the slot INSIDE the transaction
                var slot = await _repository.GetSlotByIdAsync(dto.SlotId)
                    ?? throw new KeyNotFoundException($"Slot with ID {dto.SlotId} not found.");

                if (slot.Status != SlotStatus.Available)
                    throw new InvalidOperationException(
                        $"Slot {dto.SlotId} is not available for booking (current status: {slot.Status}).");

                // Mark the slot booked atomically before creating the appointment
                slot.Status = SlotStatus.Booked;

                var appointment = new Appointment
                {
                    PatientId        = patient.PatientId,
                    DoctorId         = slot.DoctorId,
                    SlotId           = slot.Id,
                    StatusId         = status.StatusId,
                    AppointmentStart = slot.SlotStart,
                    AppointmentEnd   = slot.SlotEnd,
                    Reason           = dto.Reason,
                    IsDeleted        = false,
                    CreatedAt        = DateTime.UtcNow
                };

                await _repository.AddAsync(appointment);

                // FIX 2: SaveChanges here will throw DbUpdateConcurrencyException if another
                // transaction already modified the slot's RowVersion since we read it
                try
                {
                    await _repository.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new InvalidOperationException(
                        "Slot was already booked by another user. Please choose a different slot.");
                }

                // Link the appointment back to the slot now that the appointment ID is known
                slot.AppointmentId = appointment.AppointmentId;
                await _repository.SaveChangesAsync();

                await _repository.CommitTransactionAsync();

                _logger.LogInformation(
                    "Patient {PatientId} booked slot {SlotId} ? appointment {AppointmentId}.",
                    patient.PatientId, slot.Id, appointment.AppointmentId);

                return await GetAppointmentByIdAsync(appointment.AppointmentId);
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Reschedules an appointment to a new slot.
        ///
        /// FIX 4: Both the old slot release and the new slot booking are wrapped in a
        /// transaction so the two entities are always updated atomically.
        /// FIX 2: DbUpdateConcurrencyException on the new slot is surfaced as
        /// InvalidOperationException so callers receive a meaningful HTTP 409.
        /// </summary>
        public async Task<AppointmentDto> RescheduleAsync(
            int appointmentId, RescheduleAppointmentDto dto, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            if (callerRole == "Patient")
            {
                var patient = await _repository.GetPatientByUserIdAsync(callerUserId);
                if (patient is null || appointment.PatientId != patient.PatientId)
                    throw new UnauthorizedAccessException(
                        "You do not have permission to reschedule this appointment.");
            }

            var currentStatus = appointment.Status?.StatusName ?? string.Empty;
            if (currentStatus is "Completed" or "Cancelled")
                throw new InvalidOperationException("Cannot reschedule a completed or cancelled appointment.");

            var newSlot = await _repository.GetSlotByIdAsync(dto.NewSlotId)
                ?? throw new KeyNotFoundException($"Slot with ID {dto.NewSlotId} not found.");

            if (newSlot.Status != SlotStatus.Available)
                throw new InvalidOperationException(
                    $"Slot {dto.NewSlotId} is not available (current status: {newSlot.Status}).");

            // FIX 4: Wrap old-slot release + new-slot booking in a single transaction
            await _repository.BeginTransactionAsync();
            try
            {
                if (appointment.SlotId.HasValue)
                {
                    var oldSlot = await _repository.GetSlotByIdAsync(appointment.SlotId.Value);
                    if (oldSlot is not null)
                    {
                        oldSlot.Status        = SlotStatus.Available;
                        oldSlot.AppointmentId = null;
                    }
                }

                newSlot.Status        = SlotStatus.Booked;
                newSlot.AppointmentId = appointment.AppointmentId;

                appointment.SlotId           = newSlot.Id;
                appointment.AppointmentStart = newSlot.SlotStart;
                appointment.AppointmentEnd   = newSlot.SlotEnd;

                // FIX 2: Catch RowVersion conflicts on the new slot
                try
                {
                    await _repository.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new InvalidOperationException(
                        "Slot was already booked by another user. Please choose a different slot.");
                }

                await _repository.CommitTransactionAsync();

                _logger.LogInformation(
                    "Rescheduled appointment {AppointmentId} to slot {SlotId}.",
                    appointmentId, newSlot.Id);

                return await GetAppointmentByIdAsync(appointmentId);
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Cancels (soft-deletes) an appointment and releases its slot.
        ///
        /// FIX 4: Slot release, soft-delete, and status update are wrapped in a single
        /// transaction so all three entities are updated atomically.
        /// </summary>
        public async Task DeleteAsync(int appointmentId, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            if (callerRole == "Patient")
            {
                var patient = await _repository.GetPatientByUserIdAsync(callerUserId);
                if (patient is null || appointment.PatientId != patient.PatientId)
                    throw new UnauthorizedAccessException(
                        "You do not have permission to cancel this appointment.");
            }
            else if (callerRole == "Doctor")
            {
                var doctor = await _repository.GetDoctorByUserIdAsync(callerUserId);
                if (doctor is null || appointment.DoctorId != doctor.DoctorId)
                    throw new UnauthorizedAccessException(
                        "You do not have permission to cancel this appointment.");
            }

            // FIX 4: Wrap slot release + soft-delete + status update in a single transaction
            await _repository.BeginTransactionAsync();
            try
            {
                if (appointment.SlotId.HasValue)
                {
                    var slot = await _repository.GetSlotByIdAsync(appointment.SlotId.Value);
                    if (slot is not null)
                    {
                        slot.Status        = SlotStatus.Available;
                        slot.AppointmentId = null;
                    }
                }

                appointment.IsDeleted = true;

                var cancelledStatus = await _repository.GetStatusByNameAsync("Cancelled");
                if (cancelledStatus is not null)
                    appointment.StatusId = cancelledStatus.StatusId;

                await _repository.SaveChangesAsync();
                await _repository.CommitTransactionAsync();
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }

            _logger.LogInformation(
                "Cancelled appointment {AppointmentId} by user {UserId}.", appointmentId, callerUserId);
        }

        // ?? Private helpers ???????????????????????????????????????????????????

        private async Task EnforceOwnershipAsync(Appointment appointment, int callerUserId, string callerRole)
        {
            if (callerRole == "Admin")
                return;

            if (callerRole == "Doctor")
            {
                var doctor = await _repository.GetDoctorByUserIdAsync(callerUserId);
                if (doctor is null || appointment.DoctorId != doctor.DoctorId)
                    throw new UnauthorizedAccessException("You do not have permission to access this appointment.");
                return;
            }

            var patient = await _repository.GetPatientByUserIdAsync(callerUserId);
            if (patient is null || appointment.PatientId != patient.PatientId)
                throw new UnauthorizedAccessException("You do not have permission to access this appointment.");
        }
    }
}
