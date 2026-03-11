using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public LabTestsController(ILabTestService labTestService)
        {
            _labTestService = labTestService;
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
        /// <param name="search">Optional partial name filter (e.g. <c>blood</c> matches <c>Blood Glucose - Fasting</c>).</param>
        /// <param name="pageNumber">1-based page number. Defaults to <c>1</c>.</param>
        /// <param name="pageSize">Records per page (1–100). Defaults to <c>20</c>.</param>
        /// <response code="200">Paginated lab test catalogue returned successfully.</response>
        /// <response code="401">JWT token is missing or invalid.</response>
        [HttpGet("catalogue")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        [ProducesResponseType(typeof(PaginationResponse<LabTestCatalogueDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PaginationResponse<LabTestCatalogueDto>>> GetCatalogue(
            [FromQuery] string? search,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize   = 20)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize   = Math.Clamp(pageSize, 1, 100);

            var result = await _labTestService.GetCatalogueAsync(search, pageNumber, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Returns a single lab test catalogue entry by its identifier.
        /// </summary>
        /// <param name="id">The <c>LabTestId</c> to retrieve.</param>
        /// <response code="200">Lab test found and returned.</response>
        /// <response code="401">JWT token is missing or invalid.</response>
        /// <response code="404">No lab test exists with the given identifier.</response>
        [HttpGet("catalogue/{id:int}")]
        [Authorize(Roles = "Admin,Doctor,Patient")]
        [ProducesResponseType(typeof(LabTestCatalogueDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LabTestCatalogueDto>> GetCatalogueItem(int id)
        {
            var item = await _labTestService.GetCatalogueItemAsync(id);
            if (item is null)
                return NotFound();

            return Ok(item);
        }
    }
}
