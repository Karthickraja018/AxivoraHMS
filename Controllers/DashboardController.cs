using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IPatientDashboardService _patientDashboardService;
        private readonly IDoctorDashboardService _doctorDashboardService;

        public DashboardController(
            IPatientDashboardService patientDashboardService,
            IDoctorDashboardService doctorDashboardService)
        {
            _patientDashboardService = patientDashboardService;
            _doctorDashboardService = doctorDashboardService;
        }

        /// <summary>
        /// Aggregated patient portal dashboard (profile, next visit, stats, recent clinical data).
        /// </summary>
        [HttpGet("patient")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(PatientDashboardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PatientDashboardDto>> GetPatientDashboard()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var dto = await _patientDashboardService.GetPatientDashboardAsync(userId);
            return Ok(dto);
        }

        /// <summary>
        /// Aggregated doctor dashboard (today's appointments, next patient, pending consults, pending lab results).
        /// </summary>
        [HttpGet("doctor")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(DoctorDashboardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorDashboardDto>> GetDoctorDashboard()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var dto = await _doctorDashboardService.GetDoctorDashboardAsync(userId);
            return Ok(dto);
        }
    }
}
