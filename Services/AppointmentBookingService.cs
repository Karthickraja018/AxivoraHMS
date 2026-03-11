using AutoMapper;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class AppointmentBookingService : IAppointmentBookingService
    {
        private readonly IAppointmentBookingRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<AppointmentBookingService> _logger;

        public AppointmentBookingService(
            IAppointmentBookingRepository repository,
            IMapper mapper,
            ILogger<AppointmentBookingService> logger)
        {
            _repository = repository;
            _mapper     = mapper;
            _logger     = logger;
        }

        /// <inheritdoc/>
        public async Task<AppointmentDto> BookAsync(BookAppointmentDto dto, int callerUserId)
        {
            var patient = await _repository.GetPatientByUserIdAsync(callerUserId);
            if (patient is null)
                throw new KeyNotFoundException(
                    "Patient profile not found. Please complete your profile first.");

            var slot = await _repository.GetSlotByIdAsync(dto.SlotId);
            if (slot is null)
                throw new KeyNotFoundException($"Slot with ID {dto.SlotId} not found.");

            if (slot.Status != SlotStatus.Available)
                throw new InvalidOperationException(
                    $"Slot {dto.SlotId} is not available for booking (current status: {slot.Status}).");

            var status = await _repository.GetDefaultStatusAsync();
            if (status is null)
                throw new InvalidOperationException(
                    "Appointment status 'Scheduled' is not configured in the database.");

            await _repository.BeginTransactionAsync();
            try
            {
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

                await _repository.AddAppointmentAsync(appointment);
                await _repository.SaveChangesAsync();

                // Link the slot to the appointment and mark it Booked
                slot.Status        = SlotStatus.Booked;
                slot.AppointmentId = appointment.AppointmentId;
                await _repository.SaveChangesAsync();

                await _repository.CommitTransactionAsync();

                _logger.LogInformation(
                    "Patient {PatientId} booked slot {SlotId} ? appointment {AppointmentId}.",
                    patient.PatientId, slot.Id, appointment.AppointmentId);

                // Reload with navigation properties for full DTO mapping
                var created = await _repository.GetAppointmentByIdAsync(appointment.AppointmentId);
                return _mapper.Map<AppointmentDto>(created!);
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<AppointmentDto> RescheduleAsync(
            int appointmentId, SlotRescheduleAppointmentDto dto, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetAppointmentByIdAsync(appointmentId);
            if (appointment is null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            // Patients may only reschedule their own appointments
            if (callerRole == "Patient")
            {
                var patient = await _repository.GetPatientByUserIdAsync(callerUserId);
                if (patient is null || appointment.PatientId != patient.PatientId)
                    throw new UnauthorizedAccessException(
                        "You do not have permission to reschedule this appointment.");
            }

            var currentStatus = appointment.Status?.StatusName ?? string.Empty;
            if (currentStatus is "Completed" or "Cancelled")
                throw new InvalidOperationException(
                    "Cannot reschedule a completed or cancelled appointment.");

            var newSlot = await _repository.GetSlotByIdAsync(dto.NewSlotId);
            if (newSlot is null)
                throw new KeyNotFoundException($"Slot with ID {dto.NewSlotId} not found.");

            if (newSlot.Status != SlotStatus.Available)
                throw new InvalidOperationException(
                    $"Slot {dto.NewSlotId} is not available (current status: {newSlot.Status}).");

            await _repository.BeginTransactionAsync();
            try
            {
                // Free the old slot if one exists
                if (appointment.SlotId.HasValue)
                {
                    var oldSlot = await _repository.GetSlotByIdAsync(appointment.SlotId.Value);
                    if (oldSlot is not null)
                    {
                        oldSlot.Status        = SlotStatus.Available;
                        oldSlot.AppointmentId = null;
                    }
                }

                // Claim the new slot
                newSlot.Status        = SlotStatus.Booked;
                newSlot.AppointmentId = appointment.AppointmentId;

                // Update the appointment times to match the new slot
                appointment.SlotId           = newSlot.Id;
                appointment.AppointmentStart = newSlot.SlotStart;
                appointment.AppointmentEnd   = newSlot.SlotEnd;

                await _repository.SaveChangesAsync();
                await _repository.CommitTransactionAsync();

                _logger.LogInformation(
                    "Rescheduled appointment {AppointmentId} to slot {SlotId}.",
                    appointmentId, newSlot.Id);

                var updated = await _repository.GetAppointmentByIdAsync(appointmentId);
                return _mapper.Map<AppointmentDto>(updated!);
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task CancelAsync(int appointmentId, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetAppointmentByIdAsync(appointmentId);
            if (appointment is null)
                throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            // Patients may only cancel their own appointments
            if (callerRole == "Patient")
            {
                var patient = await _repository.GetPatientByUserIdAsync(callerUserId);
                if (patient is null || appointment.PatientId != patient.PatientId)
                    throw new UnauthorizedAccessException(
                        "You do not have permission to cancel this appointment.");
            }

            // Release the slot so it can be booked by someone else
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

            // Update status to Cancelled
            var cancelledStatus = await _repository.GetStatusByNameAsync("Cancelled");
            if (cancelledStatus is not null)
                appointment.StatusId = cancelledStatus.StatusId;

            await _repository.SaveChangesAsync();

            _logger.LogInformation(
                "Cancelled appointment {AppointmentId} by user {UserId}.", appointmentId, callerUserId);
        }
    }
}
