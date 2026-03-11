using Axivora.Models;

namespace Axivora.Repositories.Interfaces
{
    public interface ILabTestRepository
    {
        Task<OrderedTest?> GetOrderedTestByIdAsync(int orderedTestId);
        Task<bool> PatientExistsAsync(int patientId);
        Task<bool> ConsultationExistsAsync(int consultationId);
        Task<IEnumerable<OrderedTest>> GetByPatientIdAsync(int patientId);
        Task<IEnumerable<OrderedTest>> GetByConsultationIdAsync(int consultationId);
        Task<int> CountCatalogueAsync(string? search);
        Task<IEnumerable<LabTest>> GetCataloguePagedAsync(string? search, int skip, int take);
        Task<LabTest?> GetCatalogueItemAsync(int id);
        Task SaveChangesAsync();
    }
}
