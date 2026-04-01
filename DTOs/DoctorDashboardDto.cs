namespace Axivora.DTOs
{
    public class DoctorDashboardDto
    {
        public DoctorDto Profile { get; set; } = null!;
        public DoctorDashboardStatsDto Stats { get; set; } = null!;
        public AppointmentDto? NextAppointment { get; set; }
        public List<AppointmentDto> TodayAppointments { get; set; } = new();
        public List<DoctorDashboardPendingConsultationDto> PendingConsultations { get; set; } = new();
        public List<LabResultDto> PendingLabResults { get; set; } = new();
    }

    public class DoctorDashboardStatsDto
    {
        public int TodayPatientsCount { get; set; }
        public DateTime? NextAppointmentTime { get; set; }
        public int PendingConsultationsCount { get; set; }
        public int CancelledTodayCount { get; set; }
    }

    public class DoctorDashboardPendingConsultationDto
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime AppointmentStart { get; set; }
    }
}

