using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/doctors")]
    [Authorize]
    public class DoctorAvailabilityController : ControllerBase
    {
        private readonly IDoctorAvailabilityTemplateService _templateService;
        private readonly IDoctorAvailabilityService _availabilityService;
        private readonly ISlotService _slotService;

        public DoctorAvailabilityController(
            IDoctorAvailabilityTemplateService templateService,
            IDoctorAvailabilityService availabilityService,
            ISlotService slotService)
        {
            _templateService     = templateService;
            _availabilityService = availabilityService;
            _slotService         = slotService;
        }

        // ?? Templates ?????????????????????????????????????????????????????????

        /// <summary>Create a recurring weekly availability template for a doctor.</summary>
        [HttpPost("{doctorId:int}/availability-template")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(AvailabilityTemplateDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AvailabilityTemplateDto>> CreateTemplate(
            int doctorId, [FromBody] CreateAvailabilityTemplateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _templateService.CreateTemplateAsync(doctorId, dto);
            return CreatedAtAction(nameof(GetTemplates), new { doctorId }, result);
        }

        /// <summary>Get all availability templates for a doctor.</summary>
        [HttpGet("{doctorId:int}/availability-template")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<AvailabilityTemplateDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<AvailabilityTemplateDto>>> GetTemplates(int doctorId)
        {
            var templates = await _templateService.GetTemplatesByDoctorAsync(doctorId);
            return Ok(templates);
        }

        /// <summary>Update (deactivate or set expiry on) an availability template.</summary>
        [HttpPatch("availability-template/{templateId:int}")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(AvailabilityTemplateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AvailabilityTemplateDto>> UpdateTemplate(
            int templateId, [FromBody] UpdateAvailabilityTemplateDto dto)
        {
            var result = await _templateService.UpdateTemplateAsync(templateId, dto);
            return Ok(result);
        }

        /// <summary>Deactivate an availability template (soft delete).</summary>
        [HttpDelete("availability-template/{templateId:int}")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteTemplate(int templateId)
        {
            await _templateService.DeleteTemplateAsync(templateId);
            return NoContent();
        }

        // ?? Availability Days ?????????????????????????????????????????????????

        /// <summary>Get all availability day records for a doctor.</summary>
        [HttpGet("{doctorId:int}/availability-days")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<AvailabilityDayDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AvailabilityDayDto>>> GetAvailabilityDays(int doctorId)
        {
            var days = await _availabilityService.GetAvailabilityDaysAsync(doctorId);
            return Ok(days);
        }

        /// <summary>
        /// Update the status of an availability day (Open / Closed / Leave / Holiday).
        /// Setting Leave or Holiday automatically blocks all Available slots.
        /// Setting Open restores Blocked slots to Available.
        /// </summary>
        [HttpPatch("availability-day/{dayId:int}")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(AvailabilityDayDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AvailabilityDayDto>> UpdateDayStatus(
            int dayId, [FromBody] UpdateAvailabilityDayStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _availabilityService.UpdateDayStatusAsync(dayId, dto);
            return Ok(result);
        }

        // ?? Calendar ??????????????????????????????????????????????????????????

        /// <summary>
        /// Get an aggregated daily calendar for a doctor over a date range.
        /// Shows total, available, and booked slot counts per day.
        /// </summary>
        [HttpGet("{doctorId:int}/calendar")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<DoctorCalendarDayDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<DoctorCalendarDayDto>>> GetDoctorCalendar(
            int doctorId,
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to)
        {
            if (to < from)
                return BadRequest(new { message = "The 'to' date must be on or after 'from'." });

            var calendar = await _slotService.GetDoctorCalendarAsync(doctorId, from, to);
            return Ok(calendar);
        }

        // ?? Patient Availability Preview ??????????????????????????????????????

        /// <summary>
        /// Returns per-day available slot counts for a doctor — designed for patient booking UIs.
        /// </summary>
        [HttpGet("{doctorId:int}/availability")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<PatientAvailabilityPreviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<PatientAvailabilityPreviewDto>>> GetAvailabilityPreview(
            int doctorId,
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to)
        {
            if (to < from)
                return BadRequest(new { message = "The 'to' date must be on or after 'from'." });

            var preview = await _slotService.GetAvailabilityPreviewAsync(doctorId, from, to);
            return Ok(preview);
        }

        // ?? Doctor Leave ??????????????????????????????????????????????????????

        /// <summary>
        /// Apply leave for a doctor over a date range.
        /// Marks availability days as Leave and blocks all Available slots.
        /// </summary>
        [HttpPost("{doctorId:int}/leave")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> ApplyLeave(
            int doctorId, [FromBody] DoctorLeaveDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _slotService.ApplyLeaveAsync(doctorId, dto);
            return NoContent();
        }
    }
}
