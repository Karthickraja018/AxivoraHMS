using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Axivora.DTOs.Reports;
using Axivora.Helpers;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    /// <summary>
    /// Admin-only reporting endpoints backed by pre-built SQL Server views.
    /// All routes require the <c>Admin</c> role.
    /// </summary>
    [ApiController]
    [Route("api/admin/reports")]
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : ControllerBase
    {
        private readonly IAdminReportService _reportService;

        public AdminReportsController(IAdminReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Returns a paginated list of appointments for admin reporting purposes.
        /// </summary>
        /// <remarks>
        /// Sourced from <c>vw_AppointmentReport</c>. Supports optional filtering by date range,
        /// appointment status, and doctor. Results are ordered by appointment start date descending.
        ///
        /// **Filter parameters:**
        /// - `from` / `to` — UTC date-time bounds applied to <c>AppointmentStart</c>.
        /// - `status` — exact match on status name (e.g. <c>Scheduled</c>, <c>Completed</c>, <c>Cancelled</c>).
        /// - `doctorId` — restricts results to a single doctor.
        /// - `pageNumber` / `pageSize` — 1-based pagination (max page size 100, default 20).
        /// </remarks>
        /// <param name="filter">Combined filter and pagination parameters.</param>
        /// <response code="200">Paginated appointment report returned successfully.</response>
        /// <response code="400">One or more filter parameters failed validation.</response>
        /// <response code="401">JWT token is missing or invalid.</response>
        /// <response code="403">Caller does not have the Admin role.</response>
        [HttpGet("appointments")]
        [ProducesResponseType(typeof(PaginationResponse<AppointmentReportDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<AppointmentReportDto>>> GetAppointmentReport(
            [FromQuery] ReportFilterDto filter)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _reportService.GetAppointmentReportAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Returns a workload summary for every active doctor.
        /// </summary>
        /// <remarks>
        /// Sourced from <c>vw_DoctorWorkloadReport</c>. When <paramref name="from"/> or
        /// <paramref name="to"/> are provided, only doctors who have at least one appointment
        /// within that window are included. Results are ordered alphabetically by doctor name.
        ///
        /// Each row includes:
        /// - Total, completed, and cancelled appointment counts.
        /// - Total consultation records linked to this doctor's appointments.
        /// </remarks>
        /// <param name="from">Inclusive start of the date range (UTC). Omit for no lower bound.</param>
        /// <param name="to">Inclusive end of the date range (UTC). Omit for no upper bound.</param>
        /// <response code="200">Doctor workload report returned successfully.</response>
        /// <response code="401">JWT token is missing or invalid.</response>
        /// <response code="403">Caller does not have the Admin role.</response>
        [HttpGet("doctors")]
        [ProducesResponseType(typeof(IEnumerable<DoctorWorkloadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<DoctorWorkloadDto>>> GetDoctorWorkloadReport(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var result = await _reportService.GetDoctorWorkloadReportAsync(from, to);
            return Ok(result);
        }
    }
}
