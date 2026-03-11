using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    /// <inheritdoc />
    public class MedicineService : IMedicineService
    {
        private readonly IMedicineRepository _repository;

        public MedicineService(IMedicineRepository repository)
        {
            _repository = repository;
        }

        /// <inheritdoc />
        public async Task<PaginationResponse<MedicineDto>> GetAllAsync(
            string? search, int pageNumber, int pageSize)
        {
            var totalCount = await _repository.CountAsync(search);
            var items = await _repository.GetPagedAsync(search, (pageNumber - 1) * pageSize, pageSize);

            var dtos = items.Select(m => new MedicineDto
            {
                MedicineId   = m.MedicineId,
                MedicineName = m.MedicineName
            }).ToList();

            return new PaginationResponse<MedicineDto>(dtos, totalCount, pageNumber, pageSize);
        }

        /// <inheritdoc />
        public async Task<MedicineDto?> GetByIdAsync(int id)
        {
            var medicine = await _repository.GetByIdAsync(id);

            if (medicine is null)
                return null;

            return new MedicineDto
            {
                MedicineId   = medicine.MedicineId,
                MedicineName = medicine.MedicineName
            };
        }
    }
}
