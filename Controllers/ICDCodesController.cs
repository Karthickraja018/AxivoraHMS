using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/icd-codes")]
    [Authorize(Roles = "Admin,Doctor,Patient")]
    public class ICDCodesController : ControllerBase
    {
        private readonly IICDCodeService _icdCodeService;

        public ICDCodesController(IICDCodeService icdCodeService)
        {
            _icdCodeService = icdCodeService;
        }

        /// <summary>
        /// Get a paginated list of ICD codes, optionally filtered by code or description.
        /// </summary>
        /// <param name="search">Generic search term matched against both Code and Description (OR logic).</param>
        /// <param name="code">Partial match on the ICD Code field.</param>
        /// <param name="description">Partial match on the ICD Description field.</param>
        /// <param name="paginationParams">Page number and page size.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PaginationResponse<ICDCodeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<ICDCodeDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? code,
            [FromQuery] string? description,
            [FromQuery] PaginationParams paginationParams)
        {
            // ?search= is a shorthand that queries both fields simultaneously via OR
            var codeFilter        = code        ?? search;
            var descriptionFilter = description ?? search;

            var result = await _icdCodeService.GetAllAsync(codeFilter, descriptionFilter, paginationParams);
            return Ok(result);
        }
    }
}
