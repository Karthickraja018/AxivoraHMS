namespace Axivora.Models
{
    public class DoctorWorkloadReportView
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = null!;
        public string? Qualification { get; set; }
        public string? DepartmentName { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int TotalConsultations { get; set; }
    }
}
