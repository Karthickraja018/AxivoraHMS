using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentReadService _appointmentReadService;
        private readonly IAppointmentBookingService _appointmentBookingService;
        private readonly IAppointmentLifecycleService _appointmentLifecycleService;
        private readonly IIdempotencyService _idempotencyService;

        public AppointmentsController(
            IAppointmentReadService appointmentReadService,
            IAppointmentBookingService appointmentBookingService,
            IAppointmentLifecycleService appointmentLifecycleService,
            IIdempotencyService idempotencyService)
        {
            _appointmentReadService = appointmentReadService;
            _appointmentBookingService = appointmentBookingService;
            _appointmentLifecycleService = appointmentLifecycleService;
            _idempotencyService = idempotencyService;
        }

        /// <summary>
        /// Book an available slot. Patients only.
        /// Creates an appointment and atomically marks the slot as Booked.
        ///
        /// FIX 11 � Idempotency: supply an optional <c>Idempotency-Key</c> header to make
        /// this operation safe to retry. If the key has been seen before the stored response
        /// is returned without creating a duplicate appointment.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> BookAppointment(
            [FromBody] CreateAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // FIX 11: Check for a client-supplied idempotency key
            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var stored = await _idempotencyService.GetStoredResponseAsync(idempotencyKey);
                if (stored is not null)
                {
                    // Return the previously stored response � no duplicate booking created
                    var cached = JsonSerializer.Deserialize<AppointmentDto>(stored,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return Ok(cached);
                }
            }

            try
            {
                var result = await _appointmentBookingService.BookAsync(dto, userId);

                // Persist the result so subsequent retries with the same key are short-circuited
                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    var requestBody = JsonSerializer.Serialize(dto,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    await _idempotencyService.StoreResponseAsync(idempotencyKey, requestBody, result);
                }

                return CreatedAtAction(nameof(GetAppointmentById), new { id = result.AppointmentId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get all appointments with pagination. Admin sees all; Doctors see their own.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(PaginationResponse<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<AppointmentDto>>> GetAllAppointments(
            [FromQuery] PaginationParams paginationParams)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (role == "Doctor")
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var doctorAppointments = await _appointmentReadService.GetDoctorAppointmentsAsync(userId, paginationParams);
                return Ok(doctorAppointments);
            }

            var allAppointments = await _appointmentReadService.GetAllAppointmentsAsync(paginationParams);
            return Ok(allAppointments);
        }

        /// <summary>
        /// Get appointment by ID. Ownership is enforced for Patient and Doctor roles.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AppointmentDto>> GetAppointmentById(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role   = User.FindFirstValue(ClaimTypes.Role)!;
            var appointment = await _appointmentReadService.GetAppointmentByIdAsync(id, userId, role);
            return Ok(appointment);
        }

        /// <summary>
        /// Get appointments for the currently authenticated patient with optional filters.
        /// </summary>
        [HttpGet("me")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(PaginationResponse<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<AppointmentDto>>> GetMyAppointments(
            [FromQuery] PaginationParams paginationParams,
            [FromQuery] int? doctorId = null,
            [FromQuery] string? status = null)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var filter = new PatientAppointmentsFilter
            {
                Search   = paginationParams.SearchTerm,
                DoctorId = doctorId,
                Status   = status,
                FromDate = paginationParams.StartDate,
                ToDate   = paginationParams.EndDate
            };
            var appointments = await _appointmentReadService.GetMyAppointmentsAsync(userId, paginationParams, filter);
            return Ok(appointments);
        }

        /// <summary>
        /// Get appointments for the currently authenticated doctor with optional date filter.
        /// </summary>
        [HttpGet("doctor/me")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(PaginationResponse<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<AppointmentDto>>> GetMyDoctorAppointments(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var paginationParams = new PaginationParams
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                StartDate = startDate,
                EndDate = endDate
            };

            Console.WriteLine($"[DEBUG] GetMyDoctorAppointments: Start={startDate}, End={endDate}, Search={searchTerm}");
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var appointments = await _appointmentReadService.GetDoctorAppointmentsAsync(userId, paginationParams);
            return Ok(appointments);
        }

        /// <summary>
        /// Reschedule an existing appointment to a different available slot.
        /// </summary>
        [HttpPatch("{id:int}/reschedule")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> RescheduleAppointment(
            int id, [FromBody] RescheduleAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role   = User.FindFirstValue(ClaimTypes.Role)!;

            try
            {
                var result = await _appointmentBookingService.RescheduleAsync(id, dto, userId, role);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cancel an appointment. Patients can cancel their own Scheduled appointment.
        /// Cannot cancel InProgress or Completed.
        /// </summary>
        [HttpPatch("{id:int}/cancel")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> CancelAppointment(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role   = User.FindFirstValue(ClaimTypes.Role)!;
            try
            {
                var result = await _appointmentLifecycleService.CancelAsync(id, userId, role);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Start consultation (Scheduled -> InProgress). Doctor/Admin only.
        /// </summary>
        [HttpPatch("{id:int}/start")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppointmentDto>> StartConsultation(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role   = User.FindFirstValue(ClaimTypes.Role)!;
            try
            {
                var result = await _appointmentLifecycleService.StartAsync(id, userId, role);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// End consultation (InProgress -> PendingDocumentation). Doctor/Admin only.
        /// </summary>
        [HttpPatch("{id:int}/end")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppointmentDto>> EndConsultation(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role   = User.FindFirstValue(ClaimTypes.Role)!;
            try
            {
                var result = await _appointmentLifecycleService.EndAsync(id, userId, role);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Complete consultation (InProgress -> Completed). Doctor/Admin only.
        /// </summary>
        [HttpPatch("{id:int}/complete")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AppointmentDto>> CompleteConsultation(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role   = User.FindFirstValue(ClaimTypes.Role)!;
            try
            {
                var result = await _appointmentLifecycleService.CompleteAsync(id, userId, role);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Update appointment status. Patients may cancel (? Cancelled); doctors/admins drive the clinical lifecycle.
        /// </summary>
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentDto>> UpdateAppointmentStatus(
            int id, [FromBody] UpdateAppointmentStatusDto statusDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role   = User.FindFirstValue(ClaimTypes.Role)!;
            try
            {
                var appointment = await _appointmentLifecycleService.UpdateAppointmentStatusAsync(id, statusDto.Status, userId, role);
                return Ok(appointment);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

    }
}
