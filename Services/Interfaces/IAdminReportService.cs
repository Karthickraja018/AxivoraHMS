using Axivora.DTOs.Reports;
using Axivora.Helpers;

namespace Axivora.Services.Interfaces
{
    /// <summary>
    /// Provides admin-level reporting queries against the pre-built database views.
    /// </summary>
    public interface IAdminReportService
    {
        /// <summary>
        /// Returns a paginated list of appointments matching the supplied filters,
        /// sourced from <c>vw_AppointmentReport</c>.
        /// </summary>
        /// <param name="filter">Date range, status, doctor and pagination parameters.</param>
        Task<PaginationResponse<AppointmentReportDto>> GetAppointmentReportAsync(ReportFilterDto filter);

        /// <summary>
        /// Returns per-doctor workload totals for the optional date window,
        /// sourced from <c>vw_DoctorWorkloadReport</c>.
        /// </summary>
        /// <param name="from">Inclusive start of the date range (UTC). Null means no lower bound.</param>
        /// <param name="to">Inclusive end of the date range (UTC). Null means no upper bound.</param>
        Task<IEnumerable<DoctorWorkloadDto>> GetDoctorWorkloadReportAsync(DateTime? from, DateTime? to);
    }
}
