using Axivora.DTOs;
using Axivora.Helpers;

namespace Axivora.Services.Interfaces
{
    /// <summary>
    /// Provides read-only access to the medicines catalogue.
    /// </summary>
    public interface IMedicineService
    {
        /// <summary>
        /// Returns a paginated list of medicines, optionally filtered by name.
        /// </summary>
        /// <param name="search">Optional case-insensitive partial match on <c>MedicineName</c>.</param>
        /// <param name="pageNumber">1-based page index.</param>
        /// <param name="pageSize">Number of records per page (max 100).</param>
        Task<PaginationResponse<MedicineDto>> GetAllAsync(string? search, int pageNumber, int pageSize);

        /// <summary>
        /// Returns a single medicine by its identifier, or <see langword="null"/> if not found.
        /// </summary>
        /// <param name="id">The <c>MedicineId</c> to look up.</param>
        Task<MedicineDto?> GetByIdAsync(int id);
    }
}
