using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IPatientService _patientService;

        public AppointmentsController(IAppointmentService appointmentService, IPatientService patientService)
        {
            _appointmentService = appointmentService;
            _patientService = patientService;
        }

        /// <summary>
        /// Get all appointments with pagination (Admin and Doctor only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(PaginationResponse<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<AppointmentDto>>> GetAllAppointments([FromQuery] PaginationParams paginationParams)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role == "Doctor")
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var appointments = await _appointmentService.GetDoctorAppointmentsAsync(userId, paginationParams, null);
                return Ok(appointments);
            }

            var allAppointments = await _appointmentService.GetAllAppointmentsAsync(paginationParams);
            return Ok(allAppointments);
        }

        /// <summary>
        /// Get appointment by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AppointmentDto>> GetAppointmentById(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id, userId, role);
            return Ok(appointment);
        }

        /// <summary>
        /// Get appointments for the currently authenticated doctor, with optional date filter and pagination.
        /// </summary>
        [HttpGet("doctor/me")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(PaginationResponse<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<AppointmentDto>>> GetMyDoctorAppointments(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] DateTime? date = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var appointments = await _appointmentService.GetDoctorAppointmentsAsync(userId, paginationParams, date);
            return Ok(appointments);
        }

        /// <summary>
        /// Create new appointment
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentDto createAppointmentDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var appointment = await _appointmentService.CreateAppointmentAsync(createAppointmentDto, userId, role);
            return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.AppointmentId }, appointment);
        }

        /// <summary>
        /// Update appointment status (Doctor and Admin only)
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> UpdateAppointmentStatus(int id, [FromBody] UpdateAppointmentStatusDto statusDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var appointment = await _appointmentService.UpdateAppointmentStatusAsync(id, statusDto.Status, role);
            return Ok(appointment);
        }

        /// <summary>
        /// Cancel appointment
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> CancelAppointment(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            await _appointmentService.CancelAppointmentAsync(id, userId, role);
            return NoContent();
        }

        /// <summary>
        /// Reschedule an appointment to a new time window.
        /// </summary>
        /// <remarks>
        /// Only appointments with status <c>Scheduled</c> or <c>Confirmed</c> can be rescheduled.
        /// Patients may only reschedule their own appointments.
        /// Doctors and Admins may reschedule any appointment they have access to.
        ///
        /// **409 Conflict** is returned when the doctor already has another non-deleted appointment
        /// whose time window overlaps with the requested <c>AppointmentStart</c>–<c>AppointmentEnd</c> range.
        /// </remarks>
        /// <param name="id">The appointment to reschedule.</param>
        /// <param name="dto">The new start and end date-times (UTC).</param>
        /// <response code="200">Appointment rescheduled successfully.</response>
        /// <response code="400">Validation error — end must be after start.</response>
        /// <response code="401">JWT token is missing or invalid.</response>
        /// <response code="403">Caller is not the owner of this appointment.</response>
        /// <response code="404">No appointment with the given ID exists (or it is soft-deleted).</response>
        /// <response code="409">
        /// The doctor already has an appointment that overlaps the requested time window.
        /// Choose a different time slot.
        /// </response>
        [HttpPatch("{id}/reschedule")]
        [Authorize]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppointmentDto>> RescheduleAppointment(
            int id, [FromBody] RescheduleAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role   = User.FindFirstValue(ClaimTypes.Role)!;

            try
            {
                var result = await _appointmentService.RescheduleAsync(id, dto, userId, role);

                if (result is null)
                    return NotFound();

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("already taken") || ex.Message.Contains("overlaps"))
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get appointments for the currently authenticated patient, with optional status filter and pagination.
        /// </summary>
        [HttpGet("me")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(PaginationResponse<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<AppointmentDto>>> GetMyAppointments(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] string? status = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var appointments = await _appointmentService.GetMyAppointmentsAsync(userId, paginationParams, status);
            return Ok(appointments);
        }
    }
}
