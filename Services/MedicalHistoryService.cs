using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class MedicalHistoryService : IMedicalHistoryService
    {
        private readonly AxivoraDbContext _context;

        public MedicalHistoryService(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<MedicalHistoryDto> GetMedicalHistoryByPatientIdAsync(int patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.PatientAllergies)
                .Include(p => p.Appointments.Where(a => !a.IsDeleted))
                    .ThenInclude(a => a.Status)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Consultation)
                        .ThenInclude(c => c!.ICDCode)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Consultation)
                        .ThenInclude(c => c!.Prescriptions)
                            .ThenInclude(pr => pr.Medicine)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Consultation)
                        .ThenInclude(c => c!.OrderedTests)
                            .ThenInclude(ot => ot.LabTest)
                .FirstOrDefaultAsync(p => p.PatientId == patientId && !p.IsDeleted);

            if (patient == null)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            return BuildMedicalHistoryDto(patient);
        }

        public async Task<MedicalHistoryDto> GetMyMedicalHistoryAsync(int userId)
        {
            var patient = await _context.Patients
                .Include(p => p.PatientAllergies)
                .Include(p => p.Appointments.Where(a => !a.IsDeleted))
                    .ThenInclude(a => a.Status)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Consultation)
                        .ThenInclude(c => c!.ICDCode)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Consultation)
                        .ThenInclude(c => c!.Prescriptions)
                            .ThenInclude(pr => pr.Medicine)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Consultation)
                        .ThenInclude(c => c!.OrderedTests)
                            .ThenInclude(ot => ot.LabTest)
                .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

            if (patient == null)
                throw new KeyNotFoundException("Patient profile not found. Please complete your profile first.");

            return BuildMedicalHistoryDto(patient);
        }

        private static MedicalHistoryDto BuildMedicalHistoryDto(Models.Patient patient)
        {
            var visits = patient.Appointments
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.AppointmentStart)
                .Select(a => new MedicalVisitDto
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentStart = a.AppointmentStart,
                    AppointmentEnd = a.AppointmentEnd,
                    Reason = a.Reason,
                    Status = a.Status?.StatusName ?? string.Empty,
                    DoctorName = a.Doctor?.FullName ?? string.Empty,
                    Consultation = a.Consultation == null ? null : new MedicalConsultationDto
                    {
                        ConsultationId = a.Consultation.ConsultationId,
                        ChiefComplaint = a.Consultation.ChiefComplaint,
                        Examination = a.Consultation.Examination,
                        DiagnosisNotes = a.Consultation.DiagnosisNotes,
                        TreatmentPlan = a.Consultation.TreatmentPlan,
                        Notes = a.Consultation.Notes,
                        ICDCode = a.Consultation.ICDCode?.Code,
                        CreatedAt = a.Consultation.CreatedAt,
                        Prescriptions = a.Consultation.Prescriptions.Select(p => new PrescriptionDto
                        {
                            PrescriptionId = p.PrescriptionId,
                            MedicineName = p.Medicine?.MedicineName ?? string.Empty,
                            Dosage = p.Dosage,
                            Frequency = p.Frequency,
                            Route = p.Route,
                            DurationDays = p.DurationDays,
                            Instructions = p.Instructions
                        }).ToList(),
                        LabTests = a.Consultation.OrderedTests.Select(ot => new LabResultDto
                        {
                            OrderedTestId = ot.OrderedTestId,
                            ConsultationId = ot.ConsultationId,
                            LabTestId = ot.LabTestId,
                            TestName = ot.LabTest?.TestName ?? string.Empty,
                            Status = ot.Status,
                            Result = ot.Result,
                            ResultDate = ot.ResultDate,
                            PatientId = patient.PatientId,
                            PatientName = patient.FullName
                        }).ToList()
                    }
                }).ToList();

            return new MedicalHistoryDto
            {
                PatientId = patient.PatientId,
                PatientName = patient.FullName,
                MRN = patient.MRN,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                BloodGroup = patient.BloodGroup,
                Allergies = patient.PatientAllergies
                    .Select(a => a.AllergenName)
                    .ToList(),
                Visits = visits
            };
        }
    }
}
