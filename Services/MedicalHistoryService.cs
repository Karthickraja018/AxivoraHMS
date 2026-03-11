using Axivora.DTOs;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class MedicalHistoryService : IMedicalHistoryService
    {
        private readonly IMedicalHistoryRepository _repository;

        public MedicalHistoryService(IMedicalHistoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<MedicalHistoryDto> GetMedicalHistoryByPatientIdAsync(int patientId)
        {
            var patient = await _repository.GetPatientWithFullHistoryByIdAsync(patientId);

            if (patient == null)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            return BuildMedicalHistoryDto(patient);
        }

        public async Task<MedicalHistoryDto> GetMyMedicalHistoryAsync(int userId)
        {
            var patient = await _repository.GetPatientWithFullHistoryByUserIdAsync(userId);

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
                Allergies = patient.PatientAllergies.Select(a => a.AllergenName).ToList(),
                Visits = visits
            };
        }
    }
}
