using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    /// <inheritdoc />
    public class MedicineService : IMedicineService
    {
        private readonly AxivoraDbContext _context;

        public MedicineService(AxivoraDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<PaginationResponse<MedicineDto>> GetAllAsync(
            string? search, int pageNumber, int pageSize)
        {
            var query = _context.Medicines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.MedicineName.Contains(search));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(m => m.MedicineName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MedicineDto
                {
                    MedicineId   = m.MedicineId,
                    MedicineName = m.MedicineName
                })
                .ToListAsync();

            return new PaginationResponse<MedicineDto>(items, totalCount, pageNumber, pageSize);
        }

        /// <inheritdoc />
        public async Task<MedicineDto?> GetByIdAsync(int id)
        {
            var medicine = await _context.Medicines
                .FirstOrDefaultAsync(m => m.MedicineId == id);

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
