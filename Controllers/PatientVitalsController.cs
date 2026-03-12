using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/patients/{patientId}/vitals")]
    [Authorize]
    public class PatientVitalsController : ControllerBase
    {
        private readonly IPatientVitalService _vitalService;
        private readonly IPatientService _patientService;

        public PatientVitalsController(IPatientVitalService vitalService, IPatientService patientService)
        {
            _vitalService   = vitalService;
            _patientService = patientService;
        }

        /// <summary>
        /// Get paginated vitals for a patient.
        /// Doctors and Admins can view any patient; Patients can only view their own.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginationResponse<PatientVitalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PaginationResponse<PatientVitalDto>>> GetVitals(
            int patientId,
            [FromQuery] PaginationParams paginationParams)
        {
            if (!await AuthorizePatientAccessAsync(patientId))
                return Forbid();

            var result = await _vitalService.GetVitalsAsync(patientId, paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Get a single vital record.
        /// Doctors and Admins can view any patient; Patients can only view their own.
        /// </summary>
        [HttpGet("{vitalId}")]
        [ProducesResponseType(typeof(PatientVitalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PatientVitalDto>> GetVital(int patientId, int vitalId)
        {
            if (!await AuthorizePatientAccessAsync(patientId))
                return Forbid();

            var vital = await _vitalService.GetVitalByIdAsync(patientId, vitalId);
            return Ok(vital);
        }

        /// <summary>
        /// Record a new vital entry for a patient (Doctor, Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(typeof(PatientVitalDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientVitalDto>> CreateVital(int patientId, [FromBody] CreatePatientVitalDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var vital = await _vitalService.CreateVitalAsync(patientId, dto);
            return CreatedAtAction(nameof(GetVital), new { patientId, vitalId = vital.VitalId }, vital);
        }

        /// <summary>
        /// Update a vital record (Doctor, Admin only)
        /// </summary>
        [HttpPut("{vitalId}")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(typeof(PatientVitalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientVitalDto>> UpdateVital(int patientId, int vitalId, [FromBody] UpdatePatientVitalDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var vital = await _vitalService.UpdateVitalAsync(patientId, vitalId, dto);
            return Ok(vital);
        }

        /// <summary>
        /// Delete a vital record (Doctor, Admin only)
        /// </summary>
        [HttpDelete("{vitalId}")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteVital(int patientId, int vitalId)
        {
            await _vitalService.DeleteVitalAsync(patientId, vitalId);
            return NoContent();
        }

        // Patients can only access their own vitals; Doctors and Admins can access any patient's vitals.
        private async Task<bool> AuthorizePatientAccessAsync(int patientId)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (userRole is "Admin" or "Doctor")
                return true;

            var userId  = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var patient = await _patientService.GetPatientByIdAsync(patientId);

            return patient.UserId == userId;
        }
    }
}
