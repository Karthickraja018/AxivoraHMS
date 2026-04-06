namespace Axivora.DTOs
{
    public class PatientDashboardDto
    {
        public PatientDashboardProfileDto Profile { get; set; } = null!;
        public PatientDashboardNextAppointmentDto? NextAppointment { get; set; }
        public PatientDashboardStatsDto Stats { get; set; } = null!;
        public List<PatientDashboardRecentAppointmentDto> RecentAppointments { get; set; } = new();
        public List<PatientDashboardRecentConsultationDto> RecentConsultations { get; set; } = new();
        public List<PatientDashboardPrescriptionDto> Prescriptions { get; set; } = new();
        public List<PatientDashboardLabResultDto> LabResults { get; set; } = new();
        public List<PatientDashboardActivityDto> RecentActivity { get; set; } = new();
        public List<PatientDashboardVitalsDto> VitalsHistory { get; set; } = new();
    }

    public class PatientDashboardProfileDto
    {
        public string Name { get; set; } = null!;
        public string Age { get; set; } = null!;
        public string Gender { get; set; } = null!;
    }

    public class PatientDashboardNextAppointmentDto
    {
        public int AppointmentId { get; set; }
        public string DoctorName { get; set; } = null!;
        public string Specialization { get; set; } = null!;
        public string Date { get; set; } = null!;
        public string Time { get; set; } = null!;
        public string Status { get; set; } = null!;
    }

    public class PatientDashboardStatsDto
    {
        public int TotalVisits { get; set; }
        public int ActivePrescriptions { get; set; }
        public int PendingTests { get; set; }
        public string? LastVisitDate { get; set; }
    }

    public class PatientDashboardRecentAppointmentDto
    {
        public int AppointmentId { get; set; }
        public string DoctorName { get; set; } = null!;
        public string Date { get; set; } = null!;
        public string Time { get; set; } = null!;
        public string Status { get; set; } = null!;
    }

    public class PatientDashboardRecentConsultationDto
    {
        public int ConsultationId { get; set; }
        public string DoctorName { get; set; } = null!;
        public string Diagnosis { get; set; } = null!;
        public string Date { get; set; } = null!;
        public bool HasFeedback { get; set; }
    }

    public class PatientDashboardPrescriptionDto
    {
        public int PrescriptionId { get; set; }
        public string MedicineName { get; set; } = null!;
        public string Dosage { get; set; } = null!;
    }

    public class PatientDashboardLabResultDto
    {
        public int OrderedTestId { get; set; }
        public string TestName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string TestType { get; set; } = "Single";
        public bool HasReportFile { get; set; }
        public string DoctorName { get; set; } = null!;
        public string? ReportFileName { get; set; }
    }

    public class PatientDashboardActivityDto
    {
        public string Id { get; set; } = null!;
        public string Kind { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Subtitle { get; set; } = null!;
        public DateTime At { get; set; }
    }

    public class PatientDashboardVitalsDto
    {
        public string Date { get; set; } = null!;
        public string Bp { get; set; } = null!;
        public int HeartRate { get; set; }
        public decimal Temperature { get; set; }
        public decimal Weight { get; set; }
    }
}
