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
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public class ConsultationsController : ControllerBase
    {
        private readonly IConsultationService _consultationService;
        private readonly IPatientService _patientService;

        public ConsultationsController(IConsultationService consultationService, IPatientService patientService)
        {
            _consultationService = consultationService;
            _patientService = patientService;
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
        /// Get consultation by ID. Patients may only retrieve consultations linked to their own appointments.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ConsultationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultationDto>> GetConsultationById(int id)
        {
            var consultation = await _consultationService.GetConsultationByIdAsync(id);

            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role == "Patient")
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var patient = await _patientService.GetPatientByUserIdAsync(userId);
                if (consultation.PatientId != patient.PatientId)
                    return Forbid();
            }

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
        /// Get all consultations for the currently authenticated patient, with pagination.
        /// </summary>
        /// <param name="paginationParams">Pagination settings (pageNumber, pageSize).</param>
        /// <returns>A paginated list of <see cref="ConsultationDto"/> belonging to the requesting patient.</returns>
        [HttpGet("me")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(PaginationResponse<ConsultationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<ConsultationDto>>> GetMyConsultations([FromQuery] PaginationParams paginationParams)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var patient = await _patientService.GetPatientByUserIdAsync(userId);
            var consultations = await _consultationService.GetConsultationsByPatientIdAsync(patient.PatientId, paginationParams);
            return Ok(consultations);
        }

        /// <summary>
        /// Create new consultation
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")]
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
        [Authorize(Roles = "Doctor,Admin")]
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
        [Authorize(Roles = "Doctor,Admin")]
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
        [Authorize(Roles = "Doctor,Admin")]
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
