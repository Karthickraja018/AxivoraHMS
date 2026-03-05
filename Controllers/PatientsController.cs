using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientsController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        /// <summary>
        /// Get all patients with pagination (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PaginationResponse<PatientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<PatientDto>>> GetAllPatients([FromQuery] PaginationParams paginationParams)
        {
            var patients = await _patientService.GetAllPatientsAsync(paginationParams);
            return Ok(patients);
        }

        /// <summary>
        /// Get patient by ID (Admin, Doctor, or own profile)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientDto>> GetPatientById(int id)
        {
            var patient = await _patientService.GetPatientByIdAsync(id);
            
            // Check if user is authorized to view this patient
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (userRole != "Admin" && userRole != "Doctor" && patient.UserId != userId)
            {
                return Forbid();
            }
            
            return Ok(patient);
        }

        /// <summary>
        /// Get patient by Medical Record Number (MRN) (Admin and Doctor only)
        /// </summary>
        [HttpGet("mrn/{mrn}")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientDto>> GetPatientByMRN(string mrn)
        {
            var patient = await _patientService.GetPatientByMRNAsync(mrn);
            return Ok(patient);
        }

        /// <summary>
        /// Search patients by name, MRN, or phone number (Admin and Doctor only)
        /// </summary>
        [HttpGet("search")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(IEnumerable<PatientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<PatientDto>>> SearchPatients([FromQuery] string searchTerm)
        {
            var patients = await _patientService.SearchPatientsAsync(searchTerm);
            return Ok(patients);
        }

        /// <summary>
        /// Get current user's patient profile
        /// </summary>
        [HttpGet("me")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PatientDto>> GetMyProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var patients = await _patientService.GetAllPatientsAsync();
            var patient = patients.FirstOrDefault(p => p.UserId == userId);
            
            if (patient == null)
            {
                return NotFound(new { message = "Patient profile not found. Please complete your profile using POST /api/patients/me." });
            }
            
            return Ok(patient);
        }

        /// <summary>
        /// Create or update current user's patient profile (Authenticated Patient only)
        /// First-time: Creates new patient profile
        /// Subsequent: Updates existing profile if soft-deleted, otherwise returns error
        /// </summary>
        [HttpPost("me")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PatientDto>> CreateOrUpdateMyProfile([FromBody] CompletePatientProfileDto profileDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Extract userId from JWT claims
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if (userId == 0)
            {
                return Unauthorized(new { message = "Invalid token. User ID not found." });
            }

            var patient = await _patientService.CompleteProfileAsync(userId, profileDto);
            
            // Return 201 Created for new profiles, but since we can't easily detect if it was created or restored,
            // we'll use CreatedAtAction for consistency
            return CreatedAtAction(nameof(GetMyProfile), patient);
        }

        /// <summary>
        /// Create patient with user account (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientDto>> CreatePatient([FromBody] CreatePatientDto createPatientDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var patient = await _patientService.CreatePatientAsync(createPatientDto);
            return CreatedAtAction(nameof(GetPatientById), new { id = patient.PatientId }, patient);
        }

        /// <summary>
        /// Update patient information (Admin, Doctor, or own profile)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientDto>> UpdatePatient(int id, [FromBody] UpdatePatientDto updatePatientDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get patient to check ownership
            var existingPatient = await _patientService.GetPatientByIdAsync(id);
            
            // Check if user is authorized to update this patient
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (userRole != "Admin" && existingPatient.UserId != userId)
            {
                return Forbid();
            }

            var patient = await _patientService.UpdatePatientAsync(id, updatePatientDto);
            return Ok(patient);
        }

        /// <summary>
        /// Soft delete patient (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeletePatient(int id)
        {
            await _patientService.DeletePatientAsync(id);
            return NoContent();
        }
    }
}
