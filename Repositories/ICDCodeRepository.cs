using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class ICDCodeRepository : IICDCodeRepository
    {
        private readonly AxivoraDbContext _context;

        public ICDCodeRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        private IQueryable<ICDCode> BuildQuery(string? code, string? description)
        {
            var query = _context.ICDCodes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(description))
            {
                // OR search — matches either field
                query = query.Where(icd =>
                    icd.Code.Contains(code) ||
                    icd.Description.Contains(description));
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(code))
                    query = query.Where(icd => icd.Code.Contains(code));

                if (!string.IsNullOrWhiteSpace(description))
                    query = query.Where(icd => icd.Description.Contains(description));
            }

            return query;
        }

        public async Task<int> CountAsync(string? code, string? description) =>
            await BuildQuery(code, description).CountAsync();

        public async Task<IEnumerable<ICDCode>> GetPagedAsync(string? code, string? description, int skip, int take) =>
            await BuildQuery(code, description)
                .OrderBy(icd => icd.Code)
                .Skip(skip).Take(take)
                .ToListAsync();
    }
}
