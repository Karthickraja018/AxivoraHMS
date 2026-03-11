namespace Axivora.DTOs.Reports
{
    /// <summary>A single row from the appointment report view.</summary>
    public class AppointmentReportDto
    {
        /// <summary>Unique appointment identifier.</summary>
        public int AppointmentId { get; set; }

        /// <summary>Date and time the appointment is scheduled to start.</summary>
        public DateTime AppointmentStart { get; set; }

        /// <summary>Date and time the appointment is scheduled to end.</summary>
        public DateTime AppointmentEnd { get; set; }

        /// <summary>Full name of the patient.</summary>
        public string PatientName { get; set; } = null!;

        /// <summary>Patient contact phone number.</summary>
        public string? PatientPhone { get; set; }

        /// <summary>Medical Record Number of the patient.</summary>
        public string MRN { get; set; } = null!;

        /// <summary>Full name of the attending doctor.</summary>
        public string DoctorName { get; set; } = null!;

        /// <summary>Department the doctor belongs to (may be null if unassigned).</summary>
        public string? DepartmentName { get; set; }

        /// <summary>Current status of the appointment (e.g. Scheduled, Completed, Cancelled).</summary>
        public string StatusName { get; set; } = null!;

        /// <summary>Reason provided by the patient for the appointment.</summary>
        public string? Reason { get; set; }

        /// <summary>Indicates whether a consultation record exists for this appointment.</summary>
        public bool HasConsultation { get; set; }
    }
}
