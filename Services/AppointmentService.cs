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
        private static readonly TimeSpan SlotBookingCutoff = TimeSpan.FromSeconds(0);
        private static readonly string[] LegacyScheduledLikeStatuses =
        [
            "Scheduled",
            "Confirmed",
            "Checked-In",
            "Rescheduled"
        ];

        private static string NormalizeStatus(string s) =>
            string.IsNullOrWhiteSpace(s)
                ? string.Empty
                : s.Trim()
                    .Replace(" ", string.Empty, StringComparison.Ordinal)
                    .Replace("-", string.Empty, StringComparison.Ordinal);

        private readonly IAppointmentRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<AppointmentService> _logger;
        private readonly IEmailService _emailService;
        private readonly IConsultationRepository _consultationRepository;
        private readonly IAppointmentTransitionValidator _transitionValidator;

        public AppointmentService(
            IAppointmentRepository repository,
            IMapper mapper,
            ILogger<AppointmentService> logger,
            IEmailService emailService,
            IConsultationRepository consultationRepository,
            IAppointmentTransitionValidator transitionValidator)
        {
            _repository   = repository;
            _mapper       = mapper;
            _logger       = logger;
            _emailService = emailService;
            _consultationRepository = consultationRepository;
            _transitionValidator = transitionValidator;
        }

        // Read

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

        public async Task<PaginationResponse<AppointmentDto>> GetMyAppointmentsAsync(
            int userId, PaginationParams paginationParams, PatientAppointmentsFilter? filter)
        {
            var patient = await _repository.GetPatientByUserIdAsync(userId)
                ?? throw new KeyNotFoundException("Patient profile not found. Please complete your profile first.");

            filter ??= new PatientAppointmentsFilter();

            var totalCount   = await _repository.CountByPatientAsync(patient.PatientId, filter);
            var appointments = await _repository.GetPagedByPatientAsync(
                patient.PatientId, filter,
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            return new PaginationResponse<AppointmentDto>(
                _mapper.Map<IEnumerable<AppointmentDto>>(appointments),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<PaginationResponse<AppointmentDto>> GetDoctorAppointmentsAsync(int userId, PaginationParams paginationParams)
        {
            var doctor = await _repository.GetDoctorByUserIdAsync(userId)
                ?? throw new KeyNotFoundException("Doctor profile not found.");
 
            var totalCount   = await _repository.CountByDoctorAsync(doctor.DoctorId, paginationParams.StartDate, paginationParams.EndDate);
            var appointments = await _repository.GetPagedByDoctorAsync(
                doctor.DoctorId, paginationParams.StartDate, paginationParams.EndDate,
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);
 
            return new PaginationResponse<AppointmentDto>(
                _mapper.Map<IEnumerable<AppointmentDto>>(appointments),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        // Update

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

            // FIX: Prevent editing if Completed
            if (appointment.Status?.StatusName == "Completed")
                throw new InvalidOperationException("Cannot update a completed appointment.");

            await EnforceOwnershipAsync(appointment, callerUserId, callerRole);

            if (updateAppointmentDto.StatusId.HasValue)
            {
                var targetStatus = await _repository.GetStatusByIdAsync(updateAppointmentDto.StatusId.Value)
                    ?? throw new KeyNotFoundException($"Appointment status with ID {updateAppointmentDto.StatusId.Value} not found.");

                var currentStatusName = appointment.Status?.StatusName ?? string.Empty;
                _transitionValidator.ValidateTransition(currentStatusName, targetStatus.StatusName, callerRole);
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

        public async Task<AppointmentDto> UpdateAppointmentStatusAsync(
            int appointmentId, string statusName, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            await EnforceOwnershipAsync(appointment, callerUserId, callerRole);

            var targetStatus = await _repository.GetStatusByNameAsync(statusName)
                ?? throw new KeyNotFoundException($"Appointment status '{statusName}' not found.");

            var currentStatusName = appointment.Status?.StatusName ?? string.Empty;
            _transitionValidator.ValidateTransition(currentStatusName, statusName, callerRole);

            if (string.Equals(currentStatusName, statusName, StringComparison.OrdinalIgnoreCase))
                return await GetAppointmentByIdAsync(appointmentId);

            if (statusName == "Cancelled")
            {
                await ApplyCancelledWithSlotReleaseAsync(appointment);
                _logger.LogInformation(
                    "Appointment {AppointmentId} cancelled (status-only, record retained).", appointmentId);
                return await GetAppointmentByIdAsync(appointmentId);
            }

            if (statusName == "NoShow")
            {
                await ApplyNoShowWithSlotReleaseAsync(appointment);
                _logger.LogInformation(
                    "Appointment {AppointmentId} marked NoShow (status-only, slot released).", appointmentId);
                return await GetAppointmentByIdAsync(appointmentId);
            }

            appointment.StatusId = targetStatus.StatusId;
            await _repository.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointmentId);
        }

        private async Task ApplyCancelledWithSlotReleaseAsync(Appointment appointment)
        {
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

                var cancelledStatus = await _repository.GetStatusByNameAsync("Cancelled")
                    ?? throw new InvalidOperationException("Appointment status 'Cancelled' is not configured.");
                appointment.StatusId = cancelledStatus.StatusId;

                await _repository.SaveChangesAsync();
                await _repository.CommitTransactionAsync();
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task ApplyNoShowWithSlotReleaseAsync(Appointment appointment)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                if (appointment.SlotId.HasValue)
                {
                    var slot = await _repository.GetSlotByIdAsync(appointment.SlotId.Value);
                    if (slot is not null)
                    {
                        slot.Status = SlotStatus.Available;
                        slot.AppointmentId = null;
                    }
                }

                var noShowStatus = await _repository.GetStatusByNameAsync("NoShow")
                    ?? throw new InvalidOperationException("Appointment status 'NoShow' is not configured.");
                appointment.StatusId = noShowStatus.StatusId;

                await _repository.SaveChangesAsync();
                await _repository.CommitTransactionAsync();
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        // Confirmation step removed: booking is direct and starts in Scheduled.

        // Cancel (legacy)

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

        // Slot-based Booking

        /// <summary>
        /// Books a slot for the caller patient.
        ///
        /// FIX 1 ? Race condition prevention: the slot is fetched and its availability
        /// validated INSIDE the transaction so no concurrent request can book the same slot
        /// between the check and the update.
        ///
        /// FIX 2 ? Optimistic concurrency: if two transactions reach SaveChanges at the same
        /// time, the RowVersion token on AppointmentSlot causes one to throw
        /// DbUpdateConcurrencyException, which is converted to InvalidOperationException.
        /// </summary>
        public async Task<AppointmentDto> BookAsync(CreateAppointmentDto dto, int callerUserId)
        {
            var patient = await _repository.GetPatientByUserIdAsync(callerUserId)
                ?? throw new KeyNotFoundException("Patient profile not found. Please complete your profile first.");

            var status = await _repository.GetStatusByNameAsync("Scheduled")
                ?? throw new InvalidOperationException("Appointment status 'Scheduled' is not configured.");

            await _repository.BeginTransactionAsync();
            try
            {
                var slot = await _repository.GetSlotByIdAsync(dto.SlotId)
                    ?? throw new KeyNotFoundException($"Slot with ID {dto.SlotId} not found.");

                // Prevent past-slot bookings (server authoritative)
                var now = DateTime.UtcNow;
                if (slot.SlotStart <= now + SlotBookingCutoff)
                    throw new InvalidOperationException("Cannot book a past time slot.");

                if (slot.Status != SlotStatus.Available)
                    throw new InvalidOperationException(
                        $"Slot {dto.SlotId} is not available for booking (current status: {slot.Status}).");

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

                try
                {
                    await _repository.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new InvalidOperationException(
                        "Slot was already booked by another user. Please choose a different slot.");
                }

                slot.AppointmentId = appointment.AppointmentId;
                await _repository.SaveChangesAsync();

                await _repository.CommitTransactionAsync();

                _logger.LogInformation(
                    "Patient {PatientId} booked slot {SlotId} -> appointment {AppointmentId}.",
                    patient.PatientId, slot.Id, appointment.AppointmentId);

                // Enqueue confirmation email after successful booking
                var patientWithUser = await _repository.GetPatientWithUserAsync(patient.PatientId);
                var doctorName      = await _repository.GetDoctorFullNameAsync(slot.DoctorId);
                if (patientWithUser?.User?.Email is string patientEmail)
                {
                    await _emailService.SendAppointmentRequestReceivedAsync(
                        patientEmail,
                        patientWithUser.FullName,
                        doctorName ?? "Doctor",
                        appointment.AppointmentStart);
                }

                return await GetAppointmentByIdAsync(appointment.AppointmentId);
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        // New production booking flow endpoints

        public async Task<AppointmentDto> CancelAsync(int appointmentId, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            await EnforceOwnershipAsync(appointment, callerUserId, callerRole);

            var currentRaw = appointment.Status?.StatusName ?? string.Empty;
            var current = NormalizeStatus(currentRaw);
            
            // FIX: Strictly allow cancel ONLY if Scheduled
            if (!currentRaw.Equals("Scheduled", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Cancellation is only allowed for Scheduled appointments. Current status: '{currentRaw}'.");

            var cancelled = await _repository.GetStatusByNameAsync("Cancelled")
                ?? throw new InvalidOperationException("Appointment status 'Cancelled' is not configured.");

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

                appointment.StatusId = cancelled.StatusId;
                await _repository.SaveChangesAsync();
                await _repository.CommitTransactionAsync();
                return await GetAppointmentByIdAsync(appointmentId);
            }
            catch
            {
                await _repository.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<AppointmentDto> StartAsync(int appointmentId, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            await EnforceOwnershipAsync(appointment, callerUserId, callerRole);
            if (callerRole is not ("Doctor" or "Admin"))
                throw new UnauthorizedAccessException("Only Doctor or Admin can start a consultation.");

            var currentRaw = appointment.Status?.StatusName ?? string.Empty;
            var current = NormalizeStatus(currentRaw);
            
            if (current.Equals("InProgress", StringComparison.OrdinalIgnoreCase))
                return await GetAppointmentByIdAsync(appointmentId); // idempotent start

            // Allow doctors/admin to recover missed sessions from NoShow.
            if (current.Equals("NoShow", StringComparison.OrdinalIgnoreCase))
            {
                var inProgressRecovery = await _repository.GetStatusByNameAsync("InProgress")
                    ?? throw new InvalidOperationException("Appointment status 'InProgress' is not configured.");

                appointment.StatusId = inProgressRecovery.StatusId;
                await _repository.SaveChangesAsync();
                return await GetAppointmentByIdAsync(appointmentId);
            }

            if (current.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                current.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Cannot start consultation from status '{currentRaw}'.");
            }

            // Normal validation via state machine for non-terminal states.
            _transitionValidator.ValidateTransition(currentRaw, "InProgress", callerRole);

            var inProgress = await _repository.GetStatusByNameAsync("InProgress")
                ?? throw new InvalidOperationException("Appointment status 'InProgress' is not configured.");
 
            appointment.StatusId = inProgress.StatusId;
            await _repository.SaveChangesAsync();
            return await GetAppointmentByIdAsync(appointmentId);
        }

        public async Task<AppointmentDto> EndAsync(int appointmentId, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            await EnforceOwnershipAsync(appointment, callerUserId, callerRole);
            if (callerRole is not ("Doctor" or "Admin"))
                throw new UnauthorizedAccessException("Only Doctor or Admin can end a consultation session.");

            var currentRaw = appointment.Status?.StatusName ?? string.Empty;
            
            // Check status (idempotent if already pending)
            if (currentRaw == "PendingDocumentation")
                return await GetAppointmentByIdAsync(appointmentId);

            _transitionValidator.ValidateTransition(currentRaw, "PendingDocumentation", callerRole);

            var pending = await _repository.GetStatusByNameAsync("PendingDocumentation")
                ?? throw new InvalidOperationException("Appointment status 'PendingDocumentation' is not configured.");

            appointment.StatusId = pending.StatusId;
            await _repository.SaveChangesAsync();
            return await GetAppointmentByIdAsync(appointmentId);
        }

        public async Task<AppointmentDto> CompleteAsync(int appointmentId, int callerUserId, string callerRole)
        {
            var appointment = await _repository.GetByIdAsync(appointmentId)
                ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found.");

            await EnforceOwnershipAsync(appointment, callerUserId, callerRole);
            if (callerRole is not ("Doctor" or "Admin"))
                throw new UnauthorizedAccessException("Only Doctor or Admin can complete a consultation.");

            var currentRaw = appointment.Status?.StatusName ?? string.Empty;
            
            if (currentRaw == "Completed")
                return await GetAppointmentByIdAsync(appointmentId);

            // DATA VALIDATION (Required for Complete)
            var consult = await _consultationRepository.GetByAppointmentIdAsync(appointmentId);
            if (consult == null)
                throw new InvalidOperationException("Consultation record missing. Cannot finalize.");

            // Check if both prescriptions and tests are empty/null
            var prescriptionCount = consult.Prescriptions?.Count ?? 0;
            var testCount = consult.OrderedTests?.Count ?? 0;

            if (string.IsNullOrWhiteSpace(consult.ChiefComplaint) ||
                string.IsNullOrWhiteSpace(consult.DiagnosisNotes) ||
                (prescriptionCount == 0 && testCount == 0))
            {
                throw new InvalidOperationException("Documentation incomplete. Core logic requires: Chief Complaint, Diagnosis, and either a Prescription or Lab Order.");
            }

            _transitionValidator.ValidateTransition(currentRaw, "Completed", callerRole);

            var completed = await _repository.GetStatusByNameAsync("Completed")
                ?? throw new InvalidOperationException("Appointment status 'Completed' is not configured.");
 
            appointment.StatusId = completed.StatusId;
            await _repository.SaveChangesAsync();
            return await GetAppointmentByIdAsync(appointmentId);
        }

        /// <summary>
        /// Auto marks overdue Scheduled appointments as NoShow and releases the slot.
        /// Intended for background jobs.
        /// </summary>
        public async Task<int> AutoMarkNoShowsAsync(DateTime utcNow, CancellationToken ct)
        {
            var noShow = await _repository.GetStatusByNameAsync("NoShow")
                ?? throw new InvalidOperationException("Appointment status 'NoShow' is not configured.");
            var scheduled = await _repository.GetStatusByNameAsync("Scheduled")
                ?? throw new InvalidOperationException("Appointment status 'Scheduled' is not configured.");

            await _repository.BeginTransactionAsync();
            try
            {
                var overdue = await _repository.GetOverdueScheduledAppointmentsAsync(
                    scheduled.StatusId, utcNow, ct);

                if (overdue.Count == 0)
                {
                    await _repository.CommitTransactionAsync();
                    return 0;
                }

                foreach (var appt in overdue)
                {
                    appt.StatusId = noShow.StatusId;
                    if (appt.Slot is not null)
                    {
                        appt.Slot.Status        = SlotStatus.Available;
                        appt.Slot.AppointmentId = null;
                    }
                }

                await _repository.SaveChangesAsync();
                await _repository.CommitTransactionAsync();
                return overdue.Count;
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
            // FIX: Strictly allow reschedule ONLY if Scheduled
            if (!currentStatus.Equals("Scheduled", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Rescheduling is only allowed for Scheduled appointments. Current status: '{currentStatus}'.");

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

        // Private helpers

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
