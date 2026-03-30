namespace Axivora.Helpers
{
    /// <summary>
    /// Query parameters for GET /api/doctors (pagination + optional filters).
    /// </summary>
    public class DoctorQueryParams : PaginationParams
    {
        /// <summary>Case-sensitive substring match on full name (SQL Contains).</summary>
        public string? Name { get; set; }

        /// <summary>Filter doctors associated with this department/specialty.</summary>
        public int? DepartmentId { get; set; }

        /// <summary>When true, only doctors with at least one future available slot.</summary>
        public bool? HasAvailableSlots { get; set; }

        /// <summary>When true/false, filter by active flag; when omitted, include all non-deleted doctors.</summary>
        public bool? IsActive { get; set; }
    }
}
