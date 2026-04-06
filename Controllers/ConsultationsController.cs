using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/consultations")]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public class ConsultationsController : ControllerBase
    {
        private readonly IConsultationService _consultationService;
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly IAppointmentService _appointmentService;
        private readonly IPdfService _pdfService;
        private readonly ILogger<ConsultationsController> _logger;

        public ConsultationsController(
            IConsultationService consultationService,
            IPatientService patientService,
            IDoctorService doctorService,
            IAppointmentService appointmentService,
            IPdfService pdfService,
            ILogger<ConsultationsController> logger)
        {
            _consultationService = consultationService;
            _patientService      = patientService;
            _doctorService       = doctorService;
            _appointmentService  = appointmentService;
            _pdfService          = pdfService;
            _logger              = logger;
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
        /// Get all consultations for the currently authenticated doctor, with pagination.
        /// </summary>
        [HttpGet("doctor/me")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(PaginationResponse<ConsultationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<ConsultationDto>>> GetMyDoctorConsultations([FromQuery] ConsultationDoctorFilterParams filterParams)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var consultations = await _consultationService.GetConsultationsByDoctorUserIdAsync(userId, filterParams);
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

            var role   = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (role == "Patient")
            {
                var patient = await _patientService.GetPatientByUserIdAsync(userId);
                if (consultation.PatientId != patient.PatientId)
                    return Forbid();
            }
            else if (role == "Doctor")
            {
                var doctor = await _doctorService.GetDoctorByUserIdAsync(userId);
                if (doctor is null || consultation.DoctorId != doctor.DoctorId)
                    return Forbid();
            }

            return Ok(consultation);
        }

        /// <summary>
        /// Get consultation by appointment ID. Patients see only their own visit; doctors see their own cases.
        /// </summary>
        [HttpGet("appointment/{appointmentId:int}")]
        [ProducesResponseType(typeof(ConsultationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ConsultationDto>> GetConsultationByAppointmentId(int appointmentId)
        {
            var role   = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Ownership must be checked against the appointment entity:
            // - Patient can only access consultations for their own appointments
            // - Doctor can only access consultations for their own appointments
            if (string.Equals(role, "Patient", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Fetch appointment without ownership enforcement so we can compare explicit IDs.
                    // This makes the check deterministic and avoids false negatives from
                    // mismatched "callerRole" strings inside lower layers.
                    var appt = await _appointmentService.GetAppointmentByIdAsync(appointmentId);

                    if (string.Equals(role, "Patient", StringComparison.OrdinalIgnoreCase))
                    {
                        var patient = await _patientService.GetPatientByUserIdAsync(userId);
                        if (patient is null || appt.PatientId != patient.PatientId)
                        {
                            _logger.LogWarning(
                                "403 visit access denied: appointmentId={AppointmentId}, callerUserId={CallerUserId}, appointmentPatientId={AppointmentPatientId}, callerPatientId={CallerPatientId}",
                                appointmentId, userId, appt.PatientId, patient?.PatientId);
                            return Forbid();
                        }
                    }
                    else if (string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase))
                    {
                        var doctor = await _doctorService.GetDoctorByUserIdAsync(userId);
                        if (doctor is null || appt.DoctorId != doctor.DoctorId)
                        {
                            _logger.LogWarning(
                                "403 visit access denied: appointmentId={AppointmentId}, callerUserId={CallerUserId}, appointmentDoctorId={AppointmentDoctorId}, callerDoctorId={CallerDoctorId}",
                                appointmentId, userId, appt.DoctorId, doctor?.DoctorId);
                            return Forbid();
                        }
                    }
                }
                catch (KeyNotFoundException ex)
                {
                    return NotFound(new { message = ex.Message });
                }
            }

            ConsultationDto consultation;
            try
            {
                consultation = await _consultationService.GetConsultationByAppointmentIdAsync(appointmentId);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }

            return Ok(consultation);
        }

        /// <summary>
        /// Download prescription as PDF (QuestPDF). Patient must own the consultation; doctor must own the case.
        /// </summary>
        [HttpGet("{id:int}/prescription-pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPrescriptionPdf(int id)
        {
            var role   = User.FindFirstValue(ClaimTypes.Role)!;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            byte[] pdf;
            try
            {
                pdf = await _pdfService.BuildPrescriptionPdfAsync(id, userId, role);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            return File(pdf, "application/pdf", $"prescription-{id}.pdf");
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

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role)!;
            var consultation = await _consultationService.CreateConsultationAsync(createConsultationDto, userId, role);
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
        public async Task<ActionResult<ConsultationDto>> UpdateConsultation(int id, [FromBody] UpdateConsultationDto updateConsultationDto)
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
