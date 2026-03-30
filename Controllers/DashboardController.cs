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

        public DashboardController(IPatientDashboardService patientDashboardService)
        {
            _patientDashboardService = patientDashboardService;
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
    }
}
