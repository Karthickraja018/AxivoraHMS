using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface IPatientRepository
    {
        Task<IEnumerable<Patient>> GetAllActiveAsync();
        Task<int> CountActiveAsync();
        Task<IEnumerable<Patient>> GetPagedActiveAsync(int skip, int take);
        Task<Patient?> GetByIdAsync(int patientId);
        Task<Patient?> GetByIdForUpdateAsync(int patientId);
        Task<Patient?> GetByMRNAsync(string mrn);
        Task<Patient?> GetByUserIdAsync(int userId);
        Task<Patient?> GetByUserIdIncludingDeletedAsync(int userId);
        Task<IEnumerable<Patient>> SearchAsync(string pattern);
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetUserByIdAsync(int userId);
        Task<Address?> GetAddressByIdAsync(int addressId);
        Task AddUserAsync(User user);
        Task AddAddressAsync(Address address);
        Task AddPatientAsync(Patient patient);
        Task UpdatePatientAsync(Patient patient);
        Task SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
