namespace Axivora.DTOs
{
    public class MedicalHistoryDto
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public string MRN { get; set; } = null!;
        public DateOnly DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? BloodGroup { get; set; }
        public List<string> Allergies { get; set; } = new();
        public List<MedicalVisitDto> Visits { get; set; } = new();
    }

    public class MedicalVisitDto
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentStart { get; set; }
        public DateTime AppointmentEnd { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = null!;
        public string DoctorName { get; set; } = null!;
        public MedicalConsultationDto? Consultation { get; set; }
    }

    public class MedicalConsultationDto
    {
        public int ConsultationId { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? Examination { get; set; }
        public string? DiagnosisNotes { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? Notes { get; set; }
        public string? ICDCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PrescriptionDto> Prescriptions { get; set; } = new();
        public List<LabResultDto> LabTests { get; set; } = new();
    }
}
