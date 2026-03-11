using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IDoctorRepository
    {
        Task<IEnumerable<Doctor>> GetAllAsync();
        Task<int> CountAsync();
        Task<IEnumerable<Doctor>> GetPagedAsync(int skip, int take);
        Task<Doctor?> GetByIdAsync(int doctorId);
        Task<IEnumerable<Doctor>> GetByDepartmentAsync(int departmentId);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> LicenseNumberExistsAsync(string licenseNumber);
        Task<int> CountMatchingDepartmentsAsync(IEnumerable<int> departmentIds);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task AddUserAsync(User user);
        Task AddRoleAsync(Role role);
        Task AddUserRoleAsync(UserRole userRole);
        Task AddAddressAsync(Address address);
        Task AddDoctorAsync(Doctor doctor);
        Task AddDoctorDepartmentAsync(DoctorDepartment doctorDepartment);
        Task SaveChangesAsync();
        Task<Doctor?> FindAsync(int doctorId);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
