using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUsersController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        /// <summary>
        /// Get all users with optional filtering and pagination (Admin only)
        /// </summary>
        /// <param name="email">Filter by partial email match.</param>
        /// <param name="role">Filter by exact role name (e.g. Admin, Doctor, Patient).</param>
        /// <param name="isActive">Filter by active/inactive status.</param>
        /// <param name="paginationParams">Page number and page size.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PaginationResponse<AdminUserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaginationResponse<AdminUserDto>>> GetAll(
            [FromQuery] string? email,
            [FromQuery] string? role,
            [FromQuery] bool? isActive,
            [FromQuery] PaginationParams paginationParams)
        {
            var result = await _adminUserService.GetAllUsersAsync(email, role, isActive, paginationParams);
            return Ok(result);
        }

        /// <summary>
        /// Get a single user by ID (Admin only)
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminUserDto>> GetById(int id)
        {
            var user = await _adminUserService.GetUserByIdAsync(id);
            return Ok(user);
        }

        /// <summary>
        /// Disable a user account (Admin only)
        /// </summary>
        [HttpPatch("{id}/disable")]
        [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminUserDto>> Disable(int id)
        {
            var user = await _adminUserService.DisableUserAsync(id);
            return Ok(user);
        }

        /// <summary>
        /// Enable a user account (Admin only)
        /// </summary>
        [HttpPatch("{id}/enable")]
        [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AdminUserDto>> Enable(int id)
        {
            var user = await _adminUserService.EnableUserAsync(id);
            return Ok(user);
        }
    }
}
