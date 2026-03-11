using Axivora.DTOs;
using Axivora.Helpers;
using Axivora.Services.Interfaces;
using Axivora.Repositories.Interfaces;

namespace Axivora.Services
{
    public class LabTestService : ILabTestService
    {
        private readonly ILabTestRepository _repository;

        public LabTestService(ILabTestRepository repository)
        {
            _repository = repository;
        }

        public async Task<LabResultDto> UploadResultAsync(int orderedTestId, LabResultUpdateDto dto)
        {
            var orderedTest = await _repository.GetOrderedTestByIdAsync(orderedTestId);

            if (orderedTest == null)
                throw new KeyNotFoundException($"Ordered test with ID {orderedTestId} not found.");

            if (orderedTest.Status == "Completed")
                throw new InvalidOperationException("This lab test result has already been uploaded. Use PUT to update it.");

            orderedTest.Result     = dto.Result;
            orderedTest.Status     = "Completed";
            orderedTest.ResultDate = DateTime.UtcNow;

            await _repository.SaveChangesAsync();

            return MapToLabResultDto(orderedTest);
        }

        public async Task<IEnumerable<LabResultDto>> GetResultsByPatientAsync(int patientId)
        {
            if (!await _repository.PatientExistsAsync(patientId))
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            var orderedTests = await _repository.GetByPatientIdAsync(patientId);
            return orderedTests.Select(MapToLabResultDto);
        }

        public async Task<IEnumerable<LabResultDto>> GetResultsByConsultationAsync(int consultationId)
        {
            if (!await _repository.ConsultationExistsAsync(consultationId))
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            var orderedTests = await _repository.GetByConsultationIdAsync(consultationId);
            return orderedTests.Select(MapToLabResultDto);
        }

        public async Task<PaginationResponse<LabTestCatalogueDto>> GetCatalogueAsync(string? search, int pageNumber, int pageSize)
        {
            var totalCount = await _repository.CountCatalogueAsync(search);
            var items = await _repository.GetCataloguePagedAsync(search, (pageNumber - 1) * pageSize, pageSize);

            var dtos = items.Select(lt => new LabTestCatalogueDto
            {
                LabTestId = lt.LabTestId,
                TestName  = lt.TestName
            }).ToList();

            return new PaginationResponse<LabTestCatalogueDto>(dtos, totalCount, pageNumber, pageSize);
        }

        public async Task<LabTestCatalogueDto?> GetCatalogueItemAsync(int id)
        {
            var labTest = await _repository.GetCatalogueItemAsync(id);

            if (labTest is null)
                return null;

            return new LabTestCatalogueDto
            {
                LabTestId = labTest.LabTestId,
                TestName  = labTest.TestName
            };
        }

        private static LabResultDto MapToLabResultDto(Models.OrderedTest ot) => new()
        {
            OrderedTestId  = ot.OrderedTestId,
            ConsultationId = ot.ConsultationId,
            LabTestId      = ot.LabTestId,
            TestName       = ot.LabTest?.TestName ?? string.Empty,
            Status         = ot.Status,
            Result         = ot.Result,
            ResultDate     = ot.ResultDate,
            PatientId      = ot.Consultation?.Appointment?.PatientId ?? 0,
            PatientName    = ot.Consultation?.Appointment?.Patient?.FullName ?? string.Empty
        };
    }
}
