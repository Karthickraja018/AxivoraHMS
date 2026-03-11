using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    /// <summary>
    /// Handles slot-based appointment booking, rescheduling, and cancellation.
    /// This controller works with the date-based slot model.
    /// For legacy appointment management see <see cref="AppointmentsController"/>.
    /// </summary>
    [ApiController]
    [Route("api/appointments")]
    [Authorize]
    public class AppointmentBookingController : ControllerBase
    {
        private readonly IAppointmentBookingService _bookingService;

        public AppointmentBookingController(IAppointmentBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        /// <summary>
        /// Book an available slot. Patients only.
        /// Creates an appointment and atomically marks the slot as Booked.
        /// </summary>
        /// <remarks>
        /// The requesting user must have a completed patient profile.
        /// The slot must have status = Available.
        /// </remarks>
        [HttpPost("book")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> BookAppointment(
            [FromBody] BookAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var result = await _bookingService.BookAsync(dto, userId);
                return CreatedAtAction(nameof(BookAppointment), new { id = result.AppointmentId }, result);
            }
            catch (InvalidOperationException ex)
            {
                // Slot already booked / blocked
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Reschedule an existing appointment to a different available slot.
        /// </summary>
        /// <remarks>
        /// - Validates the new slot is Available.
        /// - Frees the previously held slot.
        /// - Atomically claims the new slot.
        /// - Patients may only reschedule their own appointments.
        /// </remarks>
        [HttpPatch("{id}/reschedule")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> RescheduleAppointment(
            int id, [FromBody] SlotRescheduleAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role   = User.FindFirstValue(ClaimTypes.Role)!;

            try
            {
                var result = await _bookingService.RescheduleAsync(id, dto, userId, role);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cancel an appointment and release its slot back to Available.
        /// </summary>
        [HttpDelete("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> CancelAppointment(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role   = User.FindFirstValue(ClaimTypes.Role)!;

            try
            {
                await _bookingService.CancelAsync(id, userId, role);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        }
    }
}
