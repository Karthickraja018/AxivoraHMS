using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor,Admin")]
    public class ConsultationsController : ControllerBase
    {
        private readonly IConsultationService _consultationService;

        public ConsultationsController(IConsultationService consultationService)
        {
            _consultationService = consultationService;
        }

        /// <summary>
        /// Get all consultations with pagination (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PaginationResponse<ConsultationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<ConsultationDto>>> GetAllConsultations([FromQuery] PaginationParams paginationParams)
        {
            var consultations = await _consultationService.GetAllConsultationsAsync(paginationParams);
            return Ok(consultations);
        }

        /// <summary>
        /// Get consultation by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ConsultationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultationDto>> GetConsultationById(int id)
        {
            var consultation = await _consultationService.GetConsultationByIdAsync(id);
            return Ok(consultation);
        }

        /// <summary>
        /// Get consultation by appointment ID
        /// </summary>
        [HttpGet("appointment/{appointmentId}")]
        [ProducesResponseType(typeof(ConsultationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultationDto>> GetConsultationByAppointment(int appointmentId)
        {
            var consultation = await _consultationService.GetConsultationByAppointmentIdAsync(appointmentId);
            return Ok(consultation);
        }

        /// <summary>
        /// Create new consultation
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ConsultationDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultationDto>> CreateConsultation([FromBody] CreateConsultationDto createConsultationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var consultation = await _consultationService.CreateConsultationAsync(createConsultationDto);
            return CreatedAtAction(nameof(GetConsultationById), new { id = consultation.ConsultationId }, consultation);
        }

        /// <summary>
        /// Update consultation
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ConsultationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultationDto>> UpdateConsultation(int id, [FromBody] CreateConsultationDto updateConsultationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var consultation = await _consultationService.UpdateConsultationAsync(id, updateConsultationDto);
            return Ok(consultation);
        }

        /// <summary>
        /// Add prescription to consultation
        /// </summary>
        [HttpPost("{id}/prescriptions")]
        [ProducesResponseType(typeof(ConsultationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultationDto>> AddPrescription(int id, [FromBody] CreatePrescriptionDto prescriptionDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var consultation = await _consultationService.AddPrescriptionAsync(id, prescriptionDto);
            return Ok(consultation);
        }

        /// <summary>
        /// Add lab test to consultation
        /// </summary>
        [HttpPost("{id}/lab-tests")]
        [ProducesResponseType(typeof(ConsultationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultationDto>> AddLabTest(int id, [FromBody] CreateOrderedTestDto orderedTestDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var consultation = await _consultationService.AddLabTestAsync(id, orderedTestDto);
            return Ok(consultation);
        }
    }
}
