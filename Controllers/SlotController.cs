using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/slots")]
    [Authorize]
    public class SlotController : ControllerBase
    {
        private readonly ISlotService _slotService;

        public SlotController(ISlotService slotService)
        {
            _slotService = slotService;
        }

        /// <summary>
        /// Get all Available slots for a specific doctor on a given date.
        /// </summary>
        /// <param name="doctorId">The doctor whose slots to retrieve.</param>
        /// <param name="date">Date in YYYY-MM-DD format.</param>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SlotDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<SlotDto>>> GetAvailableSlots(
            [FromQuery] int doctorId,
            [FromQuery] DateOnly date)
        {
            if (doctorId <= 0)
                return BadRequest(new { message = "A valid doctorId is required." });

            var slots = await _slotService.GetAvailableSlotsAsync(doctorId, date);
            return Ok(slots);
        }

        /// <summary>
        /// Get available slots for a specific doctor on a given date.
        /// Convenience route that mirrors GET /api/doctors/{doctorId}/slots?date=...
        /// </summary>
        [HttpGet("/api/doctors/{doctorId}/slots")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SlotDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<SlotDto>>> GetAvailableSlotsByDoctor(
            int doctorId,
            [FromQuery] DateOnly date)
        {
            var slots = await _slotService.GetAvailableSlotsAsync(doctorId, date);
            return Ok(slots);
        }
    }
}
