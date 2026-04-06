namespace Axivora.DTOs
{
    public class ConsultationDto
    {
        public int ConsultationId { get; set; }
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string ChiefComplaint { get; set; }
        public string Examination { get; set; }
        public string DiagnosisNotes { get; set; }
        public string TreatmentPlan { get; set; }
        public string ICDCode { get; set; }
        public int? ICDId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string DoctorName { get; set; }
        public List<PrescriptionDto> Prescriptions { get; set; }
        public List<OrderedTestDto> OrderedTests { get; set; }
    }

    public class CreateConsultationDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int AppointmentId { get; set; }

        // These are intentionally optional on creation to allow "start consultation" flows
        // (e.g. add medications) before the clinician finalizes all notes.
        public string? ChiefComplaint { get; set; }
        public string? Examination { get; set; }
        public string? DiagnosisNotes { get; set; }
        public string? TreatmentPlan { get; set; }
        public int? ICDId { get; set; }
    }

    public class UpdateConsultationDto
    {
        public string? ChiefComplaint { get; set; }
        public string? Examination { get; set; }
        public string? DiagnosisNotes { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? InternalNotes { get; set; }
        public int? ICDId { get; set; }
    }

    public class PrescriptionDto
    {
        public int PrescriptionId { get; set; }
        public string MedicineName { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public string Route { get; set; }
        public int? DurationDays { get; set; }
        public string Instructions { get; set; }
    }

    public class CreatePrescriptionDto
    {
        public int MedicineId { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public string Route { get; set; }
        public int? DurationDays { get; set; }
        public string Instructions { get; set; }
    }

    public class OrderedTestDto
    {
        public int OrderedTestId { get; set; }
        public string TestName { get; set; }
        public string Status { get; set; }
        public string Result { get; set; }
        public DateTime? ResultDate { get; set; }
        public string TestType { get; set; } = "Single";
        public bool HasReportFile { get; set; }
        public string? ReportFileName { get; set; }
    }

    public class CreateOrderedTestDto
    {
        public int LabTestId { get; set; }
    }
}
