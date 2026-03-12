using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class AdminUserRepository : IAdminUserRepository
    {
        private readonly AxivoraDbContext _context;

        public AdminUserRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        private IQueryable<User> BuildQuery(string? email, string? role, bool? isActive)
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => !u.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(email))
                query = query.Where(u => u.Email.Contains(email));

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role!.RoleName == role));

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            return query;
        }

        public async Task<int> CountAsync(string? email, string? role, bool? isActive) =>
            await BuildQuery(email, role, isActive).CountAsync();

        public async Task<IEnumerable<User>> GetPagedAsync(string? email, string? role, bool? isActive, int skip, int take) =>
            await BuildQuery(email, role, isActive)
                .OrderBy(u => u.Email)
                .Skip(skip).Take(take)
                .ToListAsync();

        public async Task<User?> GetByIdAsync(int userId) =>
            await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}
