namespace Axivora.Helpers
{
    /// <summary>
    /// Filters for GET /api/appointments/me (patient's own appointments).
    /// </summary>
    public class PatientAppointmentsFilter
    {
        /// <summary>Case-insensitive match on doctor full name or visit reason.</summary>
        public string? Search { get; set; }

        public int? DoctorId { get; set; }

        /// <summary>Exact status name (e.g. Scheduled, InProgress).</summary>
        public string? Status { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
