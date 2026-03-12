using AutoMapper;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Models;
using Axivora.Repositories.Interfaces;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _repository;
        private readonly IMapper _mapper;

        public DepartmentService(IDepartmentRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper     = mapper;
        }

        public async Task<PaginationResponse<DepartmentDto>> GetAllAsync(PaginationParams paginationParams)
        {
            var totalCount = await _repository.CountActiveAsync();
            var departments = await _repository.GetPagedAsync(
                (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                paginationParams.PageSize);

            return new PaginationResponse<DepartmentDto>(
                _mapper.Map<IEnumerable<DepartmentDto>>(departments),
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<DepartmentDto> GetByIdAsync(int departmentId)
        {
            var department = await _repository.GetByIdAsync(departmentId);

            if (department is null)
                throw new KeyNotFoundException($"Department with ID {departmentId} not found.");

            return _mapper.Map<DepartmentDto>(department);
        }

        public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
        {
            if (await _repository.NameExistsAsync(dto.DepartmentName))
                throw new InvalidOperationException($"A department with the name '{dto.DepartmentName}' already exists.");

            var department = _mapper.Map<Department>(dto);
            await _repository.AddAsync(department);
            await _repository.SaveChangesAsync();

            return _mapper.Map<DepartmentDto>(department);
        }

        public async Task<DepartmentDto> UpdateAsync(int departmentId, UpdateDepartmentDto dto)
        {
            var department = await _repository.GetByIdAsync(departmentId);

            if (department is null)
                throw new KeyNotFoundException($"Department with ID {departmentId} not found.");

            if (await _repository.NameExistsAsync(dto.DepartmentName, excludeId: departmentId))
                throw new InvalidOperationException($"A department with the name '{dto.DepartmentName}' already exists.");

            _mapper.Map(dto, department);
            await _repository.SaveChangesAsync();

            return _mapper.Map<DepartmentDto>(department);
        }

        public async Task DeleteAsync(int departmentId)
        {
            var department = await _repository.GetByIdAsync(departmentId);

            if (department is null)
                throw new KeyNotFoundException($"Department with ID {departmentId} not found.");

            department.IsActive = false;
            await _repository.SaveChangesAsync();
        }
    }
}
