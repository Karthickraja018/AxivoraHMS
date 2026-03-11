using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    /// <summary>
    /// Provides read-only access to the medicines catalogue.
    /// Accessible to all authenticated users (Doctor, Admin, Patient).
    /// </summary>
    [ApiController]
    [Route("api/medicines")]
    [Authorize]
    public class MedicinesController : ControllerBase
    {
        private readonly IMedicineService _medicineService;

        public MedicinesController(IMedicineService medicineService)
        {
            _medicineService = medicineService;
        }

        /// <summary>
        /// Returns a paginated list of medicines, optionally filtered by name.
        /// </summary>
        /// <remarks>
        /// Search is a case-insensitive partial match on the medicine name.
        /// Results are sorted alphabetically. Useful for prescription auto-complete forms.
        /// </remarks>
        /// <param name="search">Optional partial name filter (e.g. <c>para</c> matches <c>Paracetamol 500mg</c>).</param>
        /// <param name="pageNumber">1-based page number. Defaults to <c>1</c>.</param>
        /// <param name="pageSize">Records per page (1–100). Defaults to <c>20</c>.</param>
        /// <response code="200">Paginated medicine list returned successfully.</response>
        /// <response code="401">JWT token is missing or invalid.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginationResponse<MedicineDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PaginationResponse<MedicineDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize   = 20)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize   = Math.Clamp(pageSize, 1, 100);

            var result = await _medicineService.GetAllAsync(search, pageNumber, pageSize);
            return Ok(result);
        }
    }
}
