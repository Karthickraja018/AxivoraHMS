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

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        /// <summary>
        /// Get all appointments with pagination (Admin and Doctor only).
        /// Doctors automatically see only their own appointments.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(PaginationResponse<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<AppointmentDto>>> GetAllAppointments(
            [FromQuery] PaginationParams paginationParams)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role == "Doctor")
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var doctorAppointments = await _appointmentService.GetDoctorAppointmentsAsync(userId, paginationParams, null);
                return Ok(doctorAppointments);
            }

            var allAppointments = await _appointmentService.GetAllAppointmentsAsync(paginationParams);
            return Ok(allAppointments);
        }

        /// <summary>
        /// Get appointment by ID. Ownership is enforced for Patient and Doctor roles.
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
        /// Get appointments for the currently authenticated doctor with optional date filter.
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
        /// Update appointment status (Doctor and Admin only).
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> UpdateAppointmentStatus(
            int id, [FromBody] UpdateAppointmentStatusDto statusDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var appointment = await _appointmentService.UpdateAppointmentStatusAsync(id, statusDto.Status, role);
            return Ok(appointment);
        }

        /// <summary>
        /// Get appointments for the currently authenticated patient with optional status filter.
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
