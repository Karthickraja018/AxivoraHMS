using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly AxivoraDbContext _context;

        public DepartmentRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Department>> GetAllActiveAsync() =>
            await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

        public async Task<int> CountActiveAsync() =>
            await _context.Departments.CountAsync();

        public async Task<IEnumerable<Department>> GetPagedAsync(int skip, int take) =>
            await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .Skip(skip).Take(take)
                .ToListAsync();

        public async Task<Department?> GetByIdAsync(int departmentId) =>
            await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == departmentId);

        public async Task<bool> NameExistsAsync(string name, int? excludeId = null) =>
            await _context.Departments.AnyAsync(d =>
                d.DepartmentName == name &&
                (excludeId == null || d.DepartmentId != excludeId));

        public async Task AddAsync(Department department) =>
            await _context.Departments.AddAsync(department);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
