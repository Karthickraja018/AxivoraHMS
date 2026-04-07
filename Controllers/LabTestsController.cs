using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Axivora.DTOs;
using Axivora.Services.Interfaces;
using Axivora.Helpers;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/lab-tests")]
    [Authorize]
    public class LabTestsController : ControllerBase
    {
        private readonly ILabTestService _labTestService;
        private readonly IPdfService _pdfService;

        public LabTestsController(ILabTestService labTestService, IPdfService pdfService)
        {
            _labTestService = labTestService;
            _pdfService     = pdfService;
        }

        /// <summary>
        /// Get all lab test results (Admin only).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PaginationResponse<LabResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<LabResultDto>>> GetAllResults([FromQuery] PaginationParams paginationParams)
        {
            var results = await _labTestService.GetAllResultsAsync(paginationParams);
            return Ok(results);
        }

        /// <summary>
        /// Download a single ordered test report as PDF. <paramref name="id"/> is the ordered-test id.
        /// </summary>
        [HttpGet("{id:int}/report-pdf")]
        [Authorize(Roles = "Patient,Doctor,Admin")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetLabReportPdf(int id)
        {
            var role   = User.FindFirstValue(ClaimTypes.Role)!;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var pdf = await _pdfService.BuildLabReportPdfAsync(id, userId, role);
                return File(pdf, "application/pdf", $"lab-report-{id}.pdf");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Upload or update a lab test result (Admin, Doctor only)
        /// </summary>
        [HttpPut("{orderedTestId}/result")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(LabResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<LabResultDto>> UploadResult(int orderedTestId, [FromBody] LabResultUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _labTestService.UploadResultAsync(orderedTestId, dto);
            return Ok(result);
        }

        /// <summary>
        /// Upload a lab report attachment file (PDF/image) for an ordered test (Admin, Doctor only).
        /// </summary>
        [HttpPost("{orderedTestId:int}/report-file")]
        [Authorize(Roles = "Admin,Doctor")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(LabResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<LabResultDto>> UploadReportFile(
            int orderedTestId,
            [FromForm] UploadLabReportFileDto request,
            CancellationToken ct)
        {
            if (request.File is null || request.File.Length == 0)
                return BadRequest(new { message = "File is required." });

            var role   = User.FindFirstValue(ClaimTypes.Role)!;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var dto = await _labTestService.UploadReportFileAsync(orderedTestId, request.File, request.Summary, userId, role, ct);
                return Ok(dto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Download an uploaded lab report attachment file for an ordered test.
        /// Patients can download only their own reports; Doctors their own; Admin all.
        /// </summary>
        [HttpGet("{orderedTestId:int}/report-file")]
        [Authorize(Roles = "Patient,Doctor,Admin")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DownloadReportFile(int orderedTestId, CancellationToken ct)
        {
            var role   = User.FindFirstValue(ClaimTypes.Role)!;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var (stream, contentType, fileName) =
                    await _labTestService.DownloadReportFileAsync(orderedTestId, userId, role, ct);
                return File(stream, contentType, fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Unified lab report download for an ordered test.
        /// For Single/Multi test types, returns generated PDF.
        /// For Report test type, returns uploaded report file.
        /// </summary>
        [HttpGet("{orderedTestId:int}/download")]
        [Authorize(Roles = "Patient,Doctor,Admin")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DownloadReportAuto(int orderedTestId, CancellationToken ct)
        {
            var role   = User.FindFirstValue(ClaimTypes.Role)!;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var (stream, contentType, fileName) =
                    await _labTestService.DownloadPatientReportAsync(orderedTestId, userId, role, ct);
                return File(stream, contentType, fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Get all lab test results for a patient (Admin, Doctor)
        /// </summary>
        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(IEnumerable<LabResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<LabResultDto>>> GetResultsByPatient(int patientId)
        {
            var results = await _labTestService.GetResultsByPatientAsync(patientId);
            return Ok(results);
        }

        /// <summary>
        /// Get all lab tests ordered during a consultation (Admin, Doctor)
        /// </summary>
        [HttpGet("consultation/{consultationId}")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(IEnumerable<LabResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<LabResultDto>>> GetResultsByConsultation(int consultationId)
        {
            var results = await _labTestService.GetResultsByConsultationAsync(consultationId);
            return Ok(results);
        }

        /// <summary>
        /// Returns a paginated list of available lab tests, optionally filtered by name.
        /// </summary>
        /// <remarks>
        /// Queries the <c>LabTests</c> catalogue table (not ordered test results).
        /// Search is a case-insensitive partial match. Results are sorted alphabetically.
        /// Useful for order-test selection forms.
        /// </remarks>
        /// <param name="paginationParams">Pagination and search parameters.</param>
        [HttpGet("catalogue")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(PaginationResponse<LabTestCatalogueDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<LabTestCatalogueDto>>> GetCatalogue([FromQuery] PaginationParams paginationParams)
        {
            var results = await _labTestService.GetCatalogueAsync(paginationParams);
            return Ok(results);
        }

        /// <summary>
        /// Get all lab results for the currently authenticated patient.
        /// </summary>
        [HttpGet("me")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(IEnumerable<PatientLabResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<PatientLabResultDto>>> GetMyResults()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var results = await _labTestService.GetMyLabResultsAsync(userId);
            return Ok(results);
        }
    }
}
