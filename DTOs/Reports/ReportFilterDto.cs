using System.ComponentModel.DataAnnotations;

namespace Axivora.DTOs.Reports
{
    /// <summary>Query filters shared across admin report endpoints.</summary>
    public class ReportFilterDto
    {
        /// <summary>Include only appointments on or after this date-time (UTC).</summary>
        public DateTime? From { get; set; }

        /// <summary>Include only appointments on or before this date-time (UTC).</summary>
        public DateTime? To { get; set; }

        /// <summary>Filter by appointment status name (e.g. <c>Scheduled</c>, <c>Completed</c>, <c>Cancelled</c>).</summary>
        public string? Status { get; set; }

        /// <summary>Filter by a specific doctor's identifier.</summary>
        public int? DoctorId { get; set; }

        /// <summary>1-based page number. Defaults to <c>1</c>.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be at least 1.")]
        public int PageNumber { get; set; } = 1;

        /// <summary>Number of records per page (1–100). Defaults to <c>20</c>.</summary>
        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 20;
    }
}
