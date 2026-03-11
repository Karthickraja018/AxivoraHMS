namespace Axivora.Models
{
    public class AppointmentReportView
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentStart { get; set; }
        public DateTime AppointmentEnd { get; set; }
        public string? Reason { get; set; }
        public string StatusName { get; set; } = null!;
        public string PatientName { get; set; } = null!;
        public string? PatientPhone { get; set; }
        public string MRN { get; set; } = null!;
        public string DoctorName { get; set; } = null!;
        public string? DepartmentName { get; set; }
        public bool HasConsultation { get; set; }
    }
}
