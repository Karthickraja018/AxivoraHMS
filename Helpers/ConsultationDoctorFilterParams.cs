namespace Axivora.Helpers
{
    /// <summary>
    /// Query parameters for doctor consultation history with pagination.
    /// </summary>
    public class ConsultationDoctorFilterParams : PaginationParams
    {
        /// <summary>Free-text search across patient name and clinical notes.</summary>
        public string? Search { get; set; }

        /// <summary>Inclusive lower bound for consultation created date-time (UTC).</summary>
        public DateTime? From { get; set; }

        /// <summary>Inclusive upper bound for consultation created date-time (UTC).</summary>
        public DateTime? To { get; set; }

        /// <summary>
        /// Optional focus bucket: all, needsDocumentation, hasLabs, hasPrescriptions, hasIcd.
        /// </summary>
        public string? Focus { get; set; }
    }
}
