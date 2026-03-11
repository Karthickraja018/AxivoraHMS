using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class MedicineRepository : IMedicineRepository
    {
        private readonly AxivoraDbContext _context;

        public MedicineRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<int> CountAsync(string? search)
        {
            var query = _context.Medicines.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.MedicineName.Contains(search));
            return await query.CountAsync();
        }

        public async Task<IEnumerable<Medicine>> GetPagedAsync(string? search, int skip, int take)
        {
            var query = _context.Medicines.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.MedicineName.Contains(search));
            return await query
                .OrderBy(m => m.MedicineName)
                .Skip(skip).Take(take)
                .ToListAsync();
        }

        public async Task<Medicine?> GetByIdAsync(int id) =>
            await _context.Medicines.FirstOrDefaultAsync(m => m.MedicineId == id);
    }
}
