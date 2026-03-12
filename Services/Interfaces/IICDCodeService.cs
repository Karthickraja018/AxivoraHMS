using Axivora.DTOs;
using Axivora.Helpers;

namespace Axivora.Services.Interfaces
{
    public interface IICDCodeService
    {
        Task<PaginationResponse<ICDCodeDto>> GetAllAsync(string? code, string? description, PaginationParams paginationParams);
    }
}
