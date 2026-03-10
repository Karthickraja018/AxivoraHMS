using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;
using Axivora.Helpers;

namespace Axivora.Services
{
    public class ConsultationService : IConsultationService
    {
        private readonly AxivoraDbContext _context;
        private readonly IMapper _mapper;

        public ConsultationService(AxivoraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ConsultationDto>> GetAllConsultationsAsync()
        {
            var consultations = await _context.Consultations
                .Include(c => c.ICDCode)
                .Include(c => c.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(c => c.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Include(c => c.OrderedTests)
                    .ThenInclude(ot => ot.LabTest)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ConsultationDto>>(consultations);
        }

        public async Task<PaginationResponse<ConsultationDto>> GetAllConsultationsAsync(PaginationParams paginationParams)
        {
            var query = _context.Consultations
                .Include(c => c.ICDCode)
                .Include(c => c.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(c => c.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Include(c => c.OrderedTests)
                    .ThenInclude(ot => ot.LabTest);

            var totalCount = await query.CountAsync();

            var consultations = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var consultationDtos = _mapper.Map<IEnumerable<ConsultationDto>>(consultations);

            return new PaginationResponse<ConsultationDto>(
                consultationDtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<ConsultationDto> GetConsultationByIdAsync(int consultationId)
        {
            var consultation = await _context.Consultations
                .Include(c => c.ICDCode)
                .Include(c => c.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(c => c.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Include(c => c.OrderedTests)
                    .ThenInclude(ot => ot.LabTest)
                .FirstOrDefaultAsync(c => c.ConsultationId == consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            return _mapper.Map<ConsultationDto>(consultation);
        }

        public async Task<ConsultationDto> GetConsultationByAppointmentIdAsync(int appointmentId)
        {
            var consultation = await _context.Consultations
                .Include(c => c.ICDCode)
                .Include(c => c.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(c => c.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Include(c => c.OrderedTests)
                    .ThenInclude(ot => ot.LabTest)
                .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation for appointment {appointmentId} not found.");

            return _mapper.Map<ConsultationDto>(consultation);
        }

        /// <summary>
        /// Returns a paginated list of consultations belonging to the specified patient.
        /// </summary>
        /// <param name="patientId">The patient's identifier.</param>
        /// <param name="paginationParams">Pagination settings (page number and page size).</param>
        /// <returns>A <see cref="PaginationResponse{ConsultationDto}"/> for the patient's consultations.</returns>
        public async Task<PaginationResponse<ConsultationDto>> GetConsultationsByPatientIdAsync(int patientId, PaginationParams paginationParams)
        {
            var query = _context.Consultations
                .Include(c => c.ICDCode)
                .Include(c => c.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(c => c.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Include(c => c.OrderedTests)
                    .ThenInclude(ot => ot.LabTest)
                .Where(c => c.Appointment != null && c.Appointment.PatientId == patientId);

            var totalCount = await query.CountAsync();

            var consultations = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var consultationDtos = _mapper.Map<IEnumerable<ConsultationDto>>(consultations);

            return new PaginationResponse<ConsultationDto>(
                consultationDtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<ConsultationDto> CreateConsultationAsync(CreateConsultationDto createConsultationDto)
        {
            var existingConsultation = await _context.Consultations
                .AnyAsync(c => c.AppointmentId == createConsultationDto.AppointmentId);

            if (existingConsultation)
                throw new InvalidOperationException("A consultation already exists for this appointment.");

            var appointment = await _context.Appointments
                .Include(a => a.Status)
                .FirstOrDefaultAsync(a => a.AppointmentId == createConsultationDto.AppointmentId);

            if (appointment == null)
                throw new KeyNotFoundException($"Appointment with ID {createConsultationDto.AppointmentId} not found.");

            ValidateAppointmentStatusForConsultation(appointment);

            var consultation = _mapper.Map<Consultation>(createConsultationDto);
            consultation.CreatedAt = DateTime.UtcNow;

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultation.ConsultationId);
        }

        public async Task<ConsultationDto> CreateConsultationAsync(CreateConsultationDto createConsultationDto, int callerUserId, string callerRole)
        {
            if (callerRole != "Admin")
            {
                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.UserId == callerUserId && !d.IsDeleted);

                if (doctor == null)
                    throw new KeyNotFoundException("Doctor profile not found.");

                var appointment = await _context.Appointments
                    .Include(a => a.Status)
                    .FirstOrDefaultAsync(a => a.AppointmentId == createConsultationDto.AppointmentId);

                if (appointment == null)
                    throw new KeyNotFoundException($"Appointment with ID {createConsultationDto.AppointmentId} not found.");

                if (appointment.DoctorId != doctor.DoctorId)
                    throw new UnauthorizedAccessException(
                        "You are not authorized to create a consultation for another doctor's appointment.");

                ValidateAppointmentStatusForConsultation(appointment);

                // Appointment already validated — skip the duplicate lookup in the unguarded overload
                return await CreateConsultationSkippingAppointmentLookupAsync(createConsultationDto);
            }

            return await CreateConsultationAsync(createConsultationDto);
        }

        private async Task<ConsultationDto> CreateConsultationSkippingAppointmentLookupAsync(CreateConsultationDto createConsultationDto)
        {
            var existingConsultation = await _context.Consultations
                .AnyAsync(c => c.AppointmentId == createConsultationDto.AppointmentId);

            if (existingConsultation)
                throw new InvalidOperationException("A consultation already exists for this appointment.");

            var consultation = _mapper.Map<Consultation>(createConsultationDto);
            consultation.CreatedAt = DateTime.UtcNow;

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultation.ConsultationId);
        }

        private static readonly HashSet<string> _clinicalStatuses =
            ["Checked-In", "In Progress", "Completed"];

        private static void ValidateAppointmentStatusForConsultation(Appointment appointment)
        {
            var statusName = appointment.Status?.StatusName ?? string.Empty;
            if (!_clinicalStatuses.Contains(statusName))
                throw new InvalidOperationException(
                    "Consultation can only be created for active or completed appointments.");
        }

        public async Task<ConsultationDto> UpdateConsultationAsync(int consultationId, UpdateConsultationDto updateConsultationDto)
        {
            var consultation = await _context.Consultations.FindAsync(consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            // AppointmentId is immutable after creation — preserve it across the mapping.
            var originalAppointmentId = consultation.AppointmentId;
            _mapper.Map(updateConsultationDto, consultation);
            consultation.AppointmentId = originalAppointmentId;

            await _context.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultationId);
        }

        public async Task<ConsultationDto> AddPrescriptionAsync(int consultationId, CreatePrescriptionDto prescriptionDto)
        {
            var consultation = await _context.Consultations.FindAsync(consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            var alreadyPrescribed = await _context.Prescriptions
                .AnyAsync(p => p.ConsultationId == consultationId && p.MedicineId == prescriptionDto.MedicineId);

            if (alreadyPrescribed)
                throw new InvalidOperationException(
                    $"Medicine with ID {prescriptionDto.MedicineId} has already been prescribed in this consultation.");

            var prescription = _mapper.Map<Prescription>(prescriptionDto);
            prescription.ConsultationId = consultationId;

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultationId);
        }

        public async Task<ConsultationDto> AddLabTestAsync(int consultationId, CreateOrderedTestDto orderedTestDto)
        {
            var consultation = await _context.Consultations.FindAsync(consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            var orderedTest = _mapper.Map<OrderedTest>(orderedTestDto);
            orderedTest.ConsultationId = consultationId;
            orderedTest.Status = "Pending";

            _context.OrderedTests.Add(orderedTest);
            await _context.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultationId);
        }

        public async Task<PaginationResponse<ConsultationDto>> GetConsultationsByDoctorUserIdAsync(int userId, PaginationParams paginationParams)
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == userId && !d.IsDeleted);

            if (doctor == null)
                throw new KeyNotFoundException("Doctor profile not found.");

            var query = _context.Consultations
                .Include(c => c.ICDCode)
                .Include(c => c.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(c => c.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Include(c => c.OrderedTests)
                    .ThenInclude(ot => ot.LabTest)
                .Where(c => c.Appointment != null && c.Appointment.DoctorId == doctor.DoctorId);

            var totalCount = await query.CountAsync();

            var consultations = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var consultationDtos = _mapper.Map<IEnumerable<ConsultationDto>>(consultations);

            return new PaginationResponse<ConsultationDto>(
                consultationDtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }
    }
}
