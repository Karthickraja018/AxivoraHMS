using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Authorize]
    public class SlotController : ControllerBase
    {
        private readonly ISlotService _slotService;

        public SlotController(ISlotService slotService)
        {
            _slotService = slotService;
        }

        /// <summary>
        /// Get a single slot's full detail by ID.
        /// </summary>
        [HttpGet("api/slots/{slotId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SlotDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SlotDetailDto>> GetSlotById(int slotId)
        {
            var slot = await _slotService.GetSlotDetailAsync(slotId);
            return Ok(slot);
        }

        /// <summary>
        /// Update the status of a slot. Admin only.
        /// Allowed values: Available, Booked, Blocked, Cancelled.
        /// </summary>
        [HttpPatch("api/slots/{slotId:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(SlotDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<SlotDetailDto>> UpdateSlotStatus(
            int slotId, [FromBody] UpdateSlotStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _slotService.UpdateSlotStatusAsync(slotId, dto);
            return Ok(result);
        }

        /// <summary>
        /// Get available slots for a doctor on a specific date.
        /// </summary>
        [HttpGet("api/doctors/{doctorId:int}/slots")]
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
