using Axivora.DTOs;
using Axivora.Helpers;

namespace Axivora.Services.Interfaces
{
    public interface ILabTestService
    {
        Task<LabResultDto> UploadResultAsync(int orderedTestId, LabResultUpdateDto dto);
        Task<IEnumerable<LabResultDto>> GetResultsByPatientAsync(int patientId);
        Task<IEnumerable<LabResultDto>> GetResultsByConsultationAsync(int consultationId);

        /// <summary>
        /// Returns a paginated list of lab tests from the catalogue, optionally filtered by name.
        /// </summary>
        /// <param name="search">Optional case-insensitive partial match on <c>TestName</c>.</param>
        /// <param name="pageNumber">1-based page index.</param>
        /// <param name="pageSize">Number of records per page (max 100).</param>
        Task<PaginationResponse<LabTestCatalogueDto>> GetCatalogueAsync(string? search, int pageNumber, int pageSize);

        /// <summary>
        /// Returns a single lab test catalogue entry by its identifier, or <see langword="null"/> if not found.
        /// </summary>
        /// <param name="id">The <c>LabTestId</c> to look up.</param>
        Task<LabTestCatalogueDto?> GetCatalogueItemAsync(int id);
    }
}
