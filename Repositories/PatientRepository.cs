using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Axivora.Data;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly AxivoraDbContext _context;
        private IDbContextTransaction? _transaction;

        public PatientRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        private IQueryable<Patient> ActivePatientsQuery() =>
            _context.Patients
                .Include(p => p.Address)
                .Include(p => p.PatientAllergies)
                .Where(p => !p.IsDeleted)
                .AsNoTracking();

        public async Task<IEnumerable<Patient>> GetAllActiveAsync() =>
            await ActivePatientsQuery().ToListAsync();

        public async Task<int> CountActiveAsync() =>
            await ActivePatientsQuery().CountAsync();

        public async Task<IEnumerable<Patient>> GetPagedActiveAsync(int skip, int take) =>
            await ActivePatientsQuery()
                .OrderBy(p => p.FullName)
                .Skip(skip).Take(take)
                .ToListAsync();

        public async Task<Patient?> GetByIdAsync(int patientId) =>
            await ActivePatientsQuery()
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

        public async Task<Patient?> GetByIdForUpdateAsync(int patientId) =>
            await _context.Patients
                .Where(p => p.PatientId == patientId && !p.IsDeleted)
                .FirstOrDefaultAsync();

        public async Task<Patient?> GetByMRNAsync(string mrn) =>
            await ActivePatientsQuery()
                .FirstOrDefaultAsync(p => p.MRN == mrn);

        public async Task<Patient?> GetByUserIdAsync(int userId) =>
            await ActivePatientsQuery()
                .FirstOrDefaultAsync(p => p.UserId == userId);

        public async Task<Patient?> GetByUserIdIncludingDeletedAsync(int userId) =>
            await _context.Patients
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UserId == userId);

        public async Task<IEnumerable<Patient>> SearchAsync(string pattern) =>
            await _context.Patients
                .Include(p => p.Address)
                .Include(p => p.PatientAllergies)
                .Where(p => !p.IsDeleted && (
                    EF.Functions.Like(p.FullName, pattern) ||
                    EF.Functions.Like(p.MRN, pattern) ||
                    (p.PhoneNumber != null && EF.Functions.Like(p.PhoneNumber, pattern))))
                .AsNoTracking()
                .ToListAsync();

        public async Task<bool> EmailExistsAsync(string email) =>
            await _context.Users.AnyAsync(u => u.Email == email);

        public async Task<User?> GetUserByIdAsync(int userId) =>
            await _context.Users.FindAsync(userId);

        public async Task<Address?> GetAddressByIdAsync(int addressId) =>
            await _context.Addresses.FindAsync(addressId);

        public async Task AddUserAsync(User user) =>
            await _context.Users.AddAsync(user);

        public async Task AddAddressAsync(Address address) =>
            await _context.Addresses.AddAsync(address);

        public async Task AddPatientAsync(Patient patient) =>
            await _context.Patients.AddAsync(patient);

        public Task UpdatePatientAsync(Patient patient)
        {
            _context.Patients.Update(patient);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public async Task BeginTransactionAsync() =>
            _transaction = await _context.Database.BeginTransactionAsync();

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }
    }
}
