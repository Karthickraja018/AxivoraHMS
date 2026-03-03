namespace Axivora.DTOs
{
    public class AppointmentDto
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public DateTime AppointmentStart { get; set; }
        public DateTime AppointmentEnd { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
    }

    public class CreateAppointmentDto
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime AppointmentStart { get; set; }
        public DateTime AppointmentEnd { get; set; }
        public string Reason { get; set; }
        public int StatusId { get; set; }
    }

    public class UpdateAppointmentDto
    {
        public DateTime? AppointmentStart { get; set; }
        public DateTime? AppointmentEnd { get; set; }
        public string Reason { get; set; }
        public int? StatusId { get; set; }
    }
}
