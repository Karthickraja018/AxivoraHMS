using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// <summary>
        /// Submit feedback for a completed consultation (Patient only).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(SessionFeedbackDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<SessionFeedbackDto>> CreateFeedback([FromBody] CreateFeedbackDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var callerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var feedback = await _feedbackService.CreateFeedbackAsync(dto, callerUserId);
            return CreatedAtAction(
                nameof(GetFeedbackByConsultation),
                new { consultationId = feedback.ConsultationId },
                feedback);
        }

        /// <summary>
        /// Edit rating and/or comment of existing feedback (Patient who submitted it).
        /// </summary>
        [HttpPut("{feedbackId}")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(SessionFeedbackDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SessionFeedbackDto>> UpdateFeedback(
            int feedbackId, [FromBody] UpdateFeedbackDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var callerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var feedback = await _feedbackService.UpdateFeedbackAsync(feedbackId, dto, callerUserId);
            return Ok(feedback);
        }

        /// <summary>
        /// Get feedback for a consultation.
        /// Patient: own consultations only. Doctor: their consultations only. Admin: unrestricted.
        /// </summary>
        [HttpGet("consultation/{consultationId}")]
        [Authorize(Roles = "Patient,Doctor,Admin")]
        [ProducesResponseType(typeof(SessionFeedbackDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SessionFeedbackDto>> GetFeedbackByConsultation(int consultationId)
        {
            var callerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var callerRole   = User.FindFirstValue(ClaimTypes.Role)!;
            var feedback = await _feedbackService.GetFeedbackByConsultationAsync(
                consultationId, callerUserId, callerRole);
            return Ok(feedback);
        }

        /// <summary>
        /// Get all feedback for a doctor's consultations (Admin or that Doctor).
        /// </summary>
        [HttpGet("doctor/{doctorId}")]
        [Authorize(Roles = "Doctor,Admin")]
        [ProducesResponseType(typeof(IEnumerable<SessionFeedbackDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<SessionFeedbackDto>>> GetFeedbackByDoctor(int doctorId)
        {
            var callerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var callerRole   = User.FindFirstValue(ClaimTypes.Role)!;
            var feedbacks = await _feedbackService.GetFeedbackByDoctorAsync(
                doctorId, callerUserId, callerRole);
            return Ok(feedbacks);
        }

        /// <summary>
        /// Get all feedback submitted by a patient (Admin or that Patient).
        /// </summary>
        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Patient,Admin")]
        [ProducesResponseType(typeof(IEnumerable<SessionFeedbackDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<SessionFeedbackDto>>> GetFeedbackByPatient(int patientId)
        {
            var callerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var callerRole   = User.FindFirstValue(ClaimTypes.Role)!;
            var feedbacks = await _feedbackService.GetFeedbackByPatientAsync(
                patientId, callerUserId, callerRole);
            return Ok(feedbacks);
        }

        /// <summary>
        /// Delete feedback (submitting Patient or Admin).
        /// </summary>
        [HttpDelete("{feedbackId}")]
        [Authorize(Roles = "Patient,Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteFeedback(int feedbackId)
        {
            var callerUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var callerRole   = User.FindFirstValue(ClaimTypes.Role)!;
            await _feedbackService.DeleteFeedbackAsync(feedbackId, callerUserId, callerRole);
            return NoContent();
        }
    }
}
