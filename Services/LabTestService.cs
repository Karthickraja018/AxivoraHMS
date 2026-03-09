using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class LabTestService : ILabTestService
    {
        private readonly AxivoraDbContext _context;
        private readonly IMapper _mapper;

        public LabTestService(AxivoraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<LabResultDto> UploadResultAsync(int orderedTestId, LabResultUpdateDto dto)
        {
            var orderedTest = await _context.OrderedTests
                .Include(ot => ot.LabTest)
                .Include(ot => ot.Consultation)
                    .ThenInclude(c => c!.Appointment)
                        .ThenInclude(a => a!.Patient)
                .FirstOrDefaultAsync(ot => ot.OrderedTestId == orderedTestId);

            if (orderedTest == null)
                throw new KeyNotFoundException($"Ordered test with ID {orderedTestId} not found.");

            if (orderedTest.Status == "Completed")
                throw new InvalidOperationException("This lab test result has already been uploaded. Use PUT to update it.");

            orderedTest.Result = dto.Result;
            orderedTest.Status = "Completed";
            orderedTest.ResultDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToLabResultDto(orderedTest);
        }

        public async Task<IEnumerable<LabResultDto>> GetResultsByPatientAsync(int patientId)
        {
            var patientExists = await _context.Patients.AnyAsync(p => p.PatientId == patientId && !p.IsDeleted);
            if (!patientExists)
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");

            var orderedTests = await _context.OrderedTests
                .Include(ot => ot.LabTest)
                .Include(ot => ot.Consultation)
                    .ThenInclude(c => c!.Appointment)
                        .ThenInclude(a => a!.Patient)
                .Where(ot => ot.Consultation!.Appointment!.PatientId == patientId)
                .OrderByDescending(ot => ot.ResultDate ?? DateTime.MinValue)
                .ToListAsync();

            return orderedTests.Select(MapToLabResultDto);
        }

        public async Task<IEnumerable<LabResultDto>> GetResultsByConsultationAsync(int consultationId)
        {
            var consultationExists = await _context.Consultations.AnyAsync(c => c.ConsultationId == consultationId);
            if (!consultationExists)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            var orderedTests = await _context.OrderedTests
                .Include(ot => ot.LabTest)
                .Include(ot => ot.Consultation)
                    .ThenInclude(c => c!.Appointment)
                        .ThenInclude(a => a!.Patient)
                .Where(ot => ot.ConsultationId == consultationId)
                .OrderBy(ot => ot.OrderedTestId)
                .ToListAsync();

            return orderedTests.Select(MapToLabResultDto);
        }

        private static LabResultDto MapToLabResultDto(Models.OrderedTest ot) => new()
        {
            OrderedTestId = ot.OrderedTestId,
            ConsultationId = ot.ConsultationId,
            LabTestId = ot.LabTestId,
            TestName = ot.LabTest?.TestName ?? string.Empty,
            Status = ot.Status,
            Result = ot.Result,
            ResultDate = ot.ResultDate,
            PatientId = ot.Consultation?.Appointment?.PatientId ?? 0,
            PatientName = ot.Consultation?.Appointment?.Patient?.FullName ?? string.Empty
        };
    }
}
