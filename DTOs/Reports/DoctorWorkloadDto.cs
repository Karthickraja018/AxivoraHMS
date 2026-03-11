namespace Axivora.DTOs.Reports
{
    /// <summary>Doctor workload summary row from the workload report view.</summary>
    public class DoctorWorkloadDto
    {
        /// <summary>Unique doctor identifier.</summary>
        public int DoctorId { get; set; }

        /// <summary>Full name of the doctor.</summary>
        public string DoctorName { get; set; } = null!;

        /// <summary>Academic / professional qualification of the doctor.</summary>
        public string? Qualification { get; set; }

        /// <summary>Department the doctor belongs to.</summary>
        public string? DepartmentName { get; set; }

        /// <summary>Total number of distinct appointments (excluding soft-deleted).</summary>
        public int TotalAppointments { get; set; }

        /// <summary>Number of appointments with status <c>Completed</c>.</summary>
        public int CompletedAppointments { get; set; }

        /// <summary>Number of appointments with status <c>Cancelled</c>.</summary>
        public int CancelledAppointments { get; set; }

        /// <summary>Total number of consultation records linked to this doctor's appointments.</summary>
        public int TotalConsultations { get; set; }
    }
}
