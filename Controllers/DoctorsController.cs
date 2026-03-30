using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Axivora.DTOs;
using Axivora.Services.Interfaces;
using Axivora.Helpers;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/doctors")]
    [Authorize]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorService;

        public DoctorsController(IDoctorService doctorService)
        {
            _doctorService = doctorService;
        }

        /// <summary>
        /// Get all doctors with pagination
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginationResponse<DoctorDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginationResponse<DoctorDto>>> GetAllDoctors([FromQuery] DoctorQueryParams queryParams)
        {
            var doctors = await _doctorService.GetAllDoctorsAsync(queryParams);
            return Ok(doctors);
        }

        /// <summary>
        /// Current doctor's profile (JWT). 404 if the clinician has not completed profile setup yet.
        /// Must be registered before <c>{id}</c> so <c>me</c> is not parsed as an integer route value.
        /// </summary>
        [HttpGet("me")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorDto>> GetMyDoctorProfile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var doctor = await _doctorService.GetDoctorByUserIdAsync(userId);
            if (doctor == null)
                return NotFound(new { message = "Clinician profile not found. Complete your profile to continue." });
            return Ok(doctor);
        }

        /// <summary>
        /// Get doctors by department
        /// </summary>
        [HttpGet("department/{departmentId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<DoctorDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DoctorDto>>> GetDoctorsByDepartment(int departmentId)
        {
            var doctors = await _doctorService.GetDoctorsByDepartmentAsync(departmentId);
            return Ok(doctors);
        }

        /// <summary>
        /// Get doctor by ID
        /// </summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorDto>> GetDoctorById(int id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            return Ok(doctor);
        }

        /// <summary>
        /// Admin-only: invite a doctor (login only). They complete license, departments, and address after sign-in.
        /// </summary>
        [HttpPost("invite")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> InviteDoctor([FromBody] InviteDoctorDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _doctorService.InviteDoctorAsync(dto);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Doctor-only: create clinician profile for the signed-in user (after admin invite).
        /// </summary>
        [HttpPost("complete-profile")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DoctorDto>> CompleteDoctorProfile([FromBody] CompleteDoctorProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var doctor = await _doctorService.CompleteDoctorProfileAsync(userId, dto);
                return CreatedAtAction(nameof(GetDoctorById), new { id = doctor.DoctorId }, doctor);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Doctor-only: update the signed-in clinician profile (self-service).
        /// </summary>
        [HttpPut("me")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorDto>> UpdateMyDoctorProfile([FromBody] UpdateMyDoctorProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var doctor = await _doctorService.UpdateMyDoctorProfileAsync(userId, dto);
                return Ok(doctor);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Create new doctor (Admin only) — legacy: user + full profile in one step
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DoctorDto>> CreateDoctor([FromBody] CreateDoctorDto createDoctorDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var doctor = await _doctorService.CreateDoctorAsync(createDoctorDto);
            return CreatedAtAction(nameof(GetDoctorById), new { id = doctor.DoctorId }, doctor);
        }

        /// <summary>
        /// Update doctor (Admin only)
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DoctorDto>> UpdateDoctor(int id, [FromBody] UpdateDoctorDto updateDoctorDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var doctor = await _doctorService.UpdateDoctorAsync(id, updateDoctorDto);
            return Ok(doctor);
        }

        /// <summary>
        /// Delete doctor (Admin only)
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteDoctor(int id)
        {
            await _doctorService.DeleteDoctorAsync(id);
            return NoContent();
        }
    }
}
