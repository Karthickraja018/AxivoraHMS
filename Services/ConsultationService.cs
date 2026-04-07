using AutoMapper;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Helpers;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class ConsultationService : IConsultationService
    {
        private readonly IConsultationRepository _repository;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;

        public ConsultationService(
            IConsultationRepository repository,
            IMapper mapper,
            IEmailService emailService)
        {
            _repository   = repository;
            _mapper       = mapper;
            _emailService = emailService;
        }

        public async Task<IEnumerable<ConsultationDto>> GetAllConsultationsAsync()
        {
            var consultations = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ConsultationDto>>(consultations);
        }

        public async Task<PaginationResponse<ConsultationDto>> GetAllConsultationsAsync(PaginationParams paginationParams)
        {
            var totalCount = await _repository.CountAsync();
            var consultations = await _repository.GetPagedAsync(
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            return new PaginationResponse<ConsultationDto>(
                _mapper.Map<IEnumerable<ConsultationDto>>(consultations),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<ConsultationDto> GetConsultationByIdAsync(int consultationId)
        {
            var consultation = await _repository.GetByIdAsync(consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            return _mapper.Map<ConsultationDto>(consultation);
        }

        public async Task<ConsultationDto> GetConsultationByAppointmentIdAsync(int appointmentId)
        {
            var consultation = await _repository.GetByAppointmentIdAsync(appointmentId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation for appointment {appointmentId} not found.");

            return _mapper.Map<ConsultationDto>(consultation);
        }

        public async Task<PaginationResponse<ConsultationDto>> GetConsultationsByPatientIdAsync(int patientId, PaginationParams paginationParams)
        {
            var totalCount = await _repository.CountByPatientAsync(patientId);
            var consultations = await _repository.GetPagedByPatientAsync(
                patientId,
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            return new PaginationResponse<ConsultationDto>(
                _mapper.Map<IEnumerable<ConsultationDto>>(consultations),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<ConsultationDto> CreateConsultationAsync(CreateConsultationDto createConsultationDto)
        {
            if (await _repository.ExistsForAppointmentAsync(createConsultationDto.AppointmentId))
                throw new InvalidOperationException("A consultation already exists for this appointment.");

            var appointment = await _repository.GetAppointmentWithStatusAsync(createConsultationDto.AppointmentId);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {createConsultationDto.AppointmentId} not found.");

            ValidateAppointmentStatusForConsultation(appointment);

            var consultation = _mapper.Map<Consultation>(createConsultationDto);
            consultation.CreatedAt = DateTime.UtcNow;

            await _repository.AddConsultationAsync(consultation);
            await _repository.SaveChangesAsync();

            // Enqueue completion email if the appointment status is Completed
            await EnqueueCompletionEmailIfApplicableAsync(createConsultationDto.AppointmentId, appointment);

            return await GetConsultationByIdAsync(consultation.ConsultationId);
        }

        public async Task<ConsultationDto> CreateConsultationAsync(CreateConsultationDto createConsultationDto, int callerUserId, string callerRole)
        {
            if (callerRole != "Admin")
            {
                var doctor = await _repository.GetDoctorByUserIdAsync(callerUserId);

                if (doctor == null)
                    throw new KeyNotFoundException("Doctor profile not found.");

                var appointment = await _repository.GetAppointmentWithStatusAsync(createConsultationDto.AppointmentId);

                if (appointment == null)
                    throw new KeyNotFoundException($"Appointment with ID {createConsultationDto.AppointmentId} not found.");

                if (appointment.DoctorId != doctor.DoctorId)
                    throw new UnauthorizedAccessException(
                        "You are not authorized to create a consultation for another doctor's appointment.");

                ValidateAppointmentStatusForConsultation(appointment);

                var result = await CreateConsultationSkippingAppointmentLookupAsync(createConsultationDto);

                // Enqueue completion email if the appointment status is Completed
                await EnqueueCompletionEmailIfApplicableAsync(createConsultationDto.AppointmentId, appointment);

                return result;
            }

            return await CreateConsultationAsync(createConsultationDto);
        }

        private async Task<ConsultationDto> CreateConsultationSkippingAppointmentLookupAsync(CreateConsultationDto createConsultationDto)
        {
            if (await _repository.ExistsForAppointmentAsync(createConsultationDto.AppointmentId))
                throw new InvalidOperationException("A consultation already exists for this appointment.");

            var consultation = _mapper.Map<Consultation>(createConsultationDto);
            consultation.CreatedAt = DateTime.UtcNow;

            await _repository.AddConsultationAsync(consultation);
            await _repository.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultation.ConsultationId);
        }

        // Clinical states where editing/creating consultation details is allowed.
        // PendingDocumentation is included to allow finalizing notes after the session ends.
        private static readonly HashSet<string> _clinicalStatuses = ["inprogress", "pendingdocumentation"];

        private static string NormalizeStatus(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim()
                    .Replace(" ", string.Empty, StringComparison.Ordinal)
                    .Replace("-", string.Empty, StringComparison.Ordinal)
                    .ToLowerInvariant();

        private static void ValidateAppointmentStatusForConsultation(Appointment appointment)
        {
            var statusName = appointment.Status?.StatusName ?? string.Empty;
            var normalized = NormalizeStatus(statusName);
            if (!_clinicalStatuses.Contains(normalized))
                throw new InvalidOperationException(
                    "Consultation can only be created/edited for active appointments (InProgress/PendingDocumentation).");
        }

        public async Task<ConsultationDto> UpdateConsultationAsync(int consultationId, UpdateConsultationDto updateConsultationDto)
        {
            var consultation = await _repository.GetByIdAsync(consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            var appt = await _repository.GetAppointmentWithStatusAsync(consultation.AppointmentId);
            if (appt is null)
                throw new KeyNotFoundException($"Appointment with ID {consultation.AppointmentId} not found.");
            ValidateAppointmentStatusForConsultation(appt);

            var originalAppointmentId = consultation.AppointmentId;
            _mapper.Map(updateConsultationDto, consultation);
            consultation.AppointmentId = originalAppointmentId;

            await _repository.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultationId);
        }

        public async Task<ConsultationDto> AddPrescriptionAsync(int consultationId, CreatePrescriptionDto prescriptionDto)
        {
            var consultation = await _repository.GetByIdAsync(consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            var appt = await _repository.GetAppointmentWithStatusAsync(consultation.AppointmentId);
            if (appt is null)
                throw new KeyNotFoundException($"Appointment with ID {consultation.AppointmentId} not found.");
            ValidateAppointmentStatusForConsultation(appt);

            if (await _repository.IsMedicineAlreadyPrescribedAsync(consultationId, prescriptionDto.MedicineId))
                throw new InvalidOperationException(
                    $"Medicine with ID {prescriptionDto.MedicineId} has already been prescribed in this consultation.");

            var prescription = _mapper.Map<Prescription>(prescriptionDto);
            prescription.ConsultationId = consultationId;

            await _repository.AddPrescriptionAsync(prescription);
            await _repository.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultationId);
        }

        public async Task<ConsultationDto> AddLabTestAsync(int consultationId, CreateOrderedTestDto orderedTestDto)
        {
            var consultation = await _repository.GetByIdAsync(consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            var appt = await _repository.GetAppointmentWithStatusAsync(consultation.AppointmentId);
            if (appt is null)
                throw new KeyNotFoundException($"Appointment with ID {consultation.AppointmentId} not found.");
            ValidateAppointmentStatusForConsultation(appt);

            var orderedTest = _mapper.Map<OrderedTest>(orderedTestDto);
            orderedTest.ConsultationId = consultationId;
            orderedTest.Status = "Pending";

            await _repository.AddOrderedTestAsync(orderedTest);
            await _repository.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultationId);
        }

        public async Task<PaginationResponse<ConsultationDto>> GetConsultationsByDoctorUserIdAsync(int userId, ConsultationDoctorFilterParams filterParams)
        {
            var doctor = await _repository.GetDoctorByUserIdAsync(userId);

            if (doctor == null)
                throw new KeyNotFoundException("Doctor profile not found.");

            var totalCount = await _repository.CountByDoctorFilteredAsync(doctor.DoctorId, filterParams);
            var consultations = await _repository.GetPagedByDoctorFilteredAsync(
                doctor.DoctorId,
                filterParams,
                (filterParams.PageNumber - 1) * filterParams.PageSize,
                filterParams.PageSize);

            return new PaginationResponse<ConsultationDto>(
                _mapper.Map<IEnumerable<ConsultationDto>>(consultations),
                totalCount,
                filterParams.PageNumber,
                filterParams.PageSize);
        }

        /// <summary>
        /// Enqueues an AppointmentCompleted email to the patient when a consultation
        /// is created for an appointment that is in a Completed-equivalent status.
        /// </summary>
        private async Task EnqueueCompletionEmailIfApplicableAsync(int appointmentId, Appointment appointment)
        {
            var statusName = appointment.Status?.StatusName ?? string.Empty;
            if (!string.Equals(statusName, "Completed", StringComparison.OrdinalIgnoreCase))
                return;

            var full = await _repository.GetAppointmentWithPatientAndDoctorAsync(appointmentId);
            if (full?.Patient?.User?.Email is not string patientEmail)
                return;

            await _emailService.SendAppointmentCompletedAsync(
                patientEmail,
                full.Patient.FullName,
                full.Doctor?.FullName ?? "Doctor",
                full.AppointmentStart);
        }
    }
}
