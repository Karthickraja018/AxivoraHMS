using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/doctors")]
    [Authorize]
    public class DoctorScheduleController : ControllerBase
    {
        private readonly IDoctorScheduleService _scheduleService;

        public DoctorScheduleController(IDoctorScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        /// <summary>
        /// Create a new availability schedule for a doctor (Admin, Doctor)
        /// </summary>
        [HttpPost("{doctorId}/schedule")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(DoctorScheduleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DoctorScheduleDto>> CreateSchedule(int doctorId, [FromBody] CreateScheduleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var schedule = await _scheduleService.CreateScheduleAsync(doctorId, dto);
            return CreatedAtAction(nameof(GetSchedule), new { doctorId }, schedule);
        }

        /// <summary>
        /// Get weekly schedule for a doctor
        /// </summary>
        [HttpGet("{doctorId}/schedule")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<DoctorScheduleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<DoctorScheduleDto>>> GetSchedule(int doctorId)
        {
            var schedules = await _scheduleService.GetSchedulesByDoctorAsync(doctorId);
            return Ok(schedules);
        }

        /// <summary>
        /// Update a doctor schedule (Admin, Doctor)
        /// </summary>
        [HttpPut("schedule/{scheduleId}")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(DoctorScheduleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DoctorScheduleDto>> UpdateSchedule(int scheduleId, [FromBody] UpdateScheduleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var schedule = await _scheduleService.UpdateScheduleAsync(scheduleId, dto);
            return Ok(schedule);
        }

        /// <summary>
        /// Delete a doctor schedule (Admin, Doctor)
        /// </summary>
        [HttpDelete("schedule/{scheduleId}")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteSchedule(int scheduleId)
        {
            await _scheduleService.DeleteScheduleAsync(scheduleId);
            return NoContent();
        }
    }
}
