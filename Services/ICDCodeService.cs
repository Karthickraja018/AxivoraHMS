using AutoMapper;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Repositories.Interfaces;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class ICDCodeService : IICDCodeService
    {
        private readonly IICDCodeRepository _repository;
        private readonly IMapper _mapper;

        public ICDCodeService(IICDCodeRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper     = mapper;
        }

        public async Task<PaginationResponse<ICDCodeDto>> GetAllAsync(
            string? code,
            string? description,
            PaginationParams paginationParams)
        {
            var totalCount = await _repository.CountAsync(code, description);
            var items      = await _repository.GetPagedAsync(
                code,
                description,
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            return new PaginationResponse<ICDCodeDto>(
                _mapper.Map<IEnumerable<ICDCodeDto>>(items),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }
    }
}
