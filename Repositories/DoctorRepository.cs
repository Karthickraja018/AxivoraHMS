using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Axivora.Data;
using Axivora.Helpers;
using Axivora.Models;
using Axivora.Repositories.Interfaces;

namespace Axivora.Repositories
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly AxivoraDbContext _context;
        private IDbContextTransaction? _transaction;

        public DoctorRepository(AxivoraDbContext context)
        {
            _context = context;
        }

        private IQueryable<Doctor> BaseQuery() =>
            _context.Doctors
                .Include(d => d.Address)
                .Include(d => d.DoctorDepartments)
                    .ThenInclude(dd => dd.Department)
                .Where(d => !d.IsDeleted);

        public async Task<IEnumerable<Doctor>> GetAllAsync() =>
            await BaseQuery().ToListAsync();

        public async Task<int> CountAsync() =>
            await BaseQuery().CountAsync();

        public async Task<IEnumerable<Doctor>> GetPagedAsync(int skip, int take) =>
            await BaseQuery()
                .OrderBy(d => d.FullName)
                .Skip(skip).Take(take)
                .ToListAsync();

        private IQueryable<Doctor> FilteredDoctorQuery(DoctorQueryParams p)
        {
            var q = _context.Doctors
                .AsNoTracking()
                .Include(d => d.Address)
                .Include(d => d.DoctorDepartments)
                    .ThenInclude(dd => dd.Department)
                .Where(d => !d.IsDeleted);

            if (p.IsActive.HasValue)
                q = q.Where(d => d.IsActive == p.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(p.Name))
            {
                var term = p.Name.Trim();
                q = q.Where(d => d.FullName.Contains(term));
            }

            if (p.DepartmentId.HasValue)
            {
                var deptId = p.DepartmentId.Value;
                q = q.Where(d => d.DoctorDepartments.Any(dd => dd.DepartmentId == deptId));
            }

            if (p.HasAvailableSlots == true)
            {
                var now = DateTime.UtcNow;
                q = q.Where(d => d.AppointmentSlots.Any(s =>
                    s.Status == SlotStatus.Available && s.SlotStart >= now));
            }

            return q;
        }

        public async Task<int> CountFilteredAsync(DoctorQueryParams queryParams) =>
            await FilteredDoctorQuery(queryParams).CountAsync();

        public async Task<IEnumerable<Doctor>> GetFilteredPagedAsync(int skip, int take, DoctorQueryParams queryParams) =>
            await FilteredDoctorQuery(queryParams)
                .OrderBy(d => d.FullName)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

        public async Task<Doctor?> GetByIdAsync(int doctorId) =>
            await BaseQuery()
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

        public async Task<Doctor?> GetByUserIdAsync(int userId) =>
            await BaseQuery()
                .FirstOrDefaultAsync(d => d.UserId == userId);

        public async Task<IEnumerable<Doctor>> GetByDepartmentAsync(int departmentId) =>
            await BaseQuery()
                .Where(d => d.IsActive && d.DoctorDepartments.Any(dd => dd.DepartmentId == departmentId))
                .ToListAsync();

        public async Task<bool> EmailExistsAsync(string email) =>
            await _context.Users.AnyAsync(u => u.Email == email);

        public async Task<bool> LicenseNumberExistsAsync(string licenseNumber) =>
            await _context.Doctors.IgnoreQueryFilters()
                .AnyAsync(d => d.LicenseNumber == licenseNumber);

        public async Task<int> CountMatchingDepartmentsAsync(IEnumerable<int> departmentIds) =>
            await _context.Departments
                .Where(d => departmentIds.Contains(d.DepartmentId))
                .CountAsync();

        public async Task<Role?> GetRoleByNameAsync(string roleName) =>
            await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);

        public async Task AddUserAsync(User user) =>
            await _context.Users.AddAsync(user);

        public async Task AddRoleAsync(Role role) =>
            await _context.Roles.AddAsync(role);

        public async Task AddUserRoleAsync(UserRole userRole) =>
            await _context.UserRoles.AddAsync(userRole);

        public async Task AddAddressAsync(Address address) =>
            await _context.Addresses.AddAsync(address);

        public async Task AddDoctorAsync(Doctor doctor) =>
            await _context.Doctors.AddAsync(doctor);

        public async Task AddDoctorDepartmentAsync(DoctorDepartment doctorDepartment) =>
            await _context.DoctorDepartments.AddAsync(doctorDepartment);

        public async Task<Doctor?> FindAsync(int doctorId) =>
            await _context.Doctors.FindAsync(doctorId);

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
