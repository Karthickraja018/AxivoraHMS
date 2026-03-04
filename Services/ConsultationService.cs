using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Axivora.Data;
using Axivora.DTOs;
using Axivora.Models;
using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class ConsultationService : IConsultationService
    {
        private readonly AxivoraDbContext _context;
        private readonly IMapper _mapper;

        public ConsultationService(AxivoraDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ConsultationDto>> GetAllConsultationsAsync()
        {
            var consultations = await _context.Consultations
                .Include(c => c.ICDCode)
                .Include(c => c.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Include(c => c.OrderedTests)
                    .ThenInclude(ot => ot.LabTest)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ConsultationDto>>(consultations);
        }

        public async Task<PaginationResponse<ConsultationDto>> GetAllConsultationsAsync(PaginationParams paginationParams)
        {
            var query = _context.Consultations
                .Include(c => c.ICDCode)
                .Include(c => c.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Include(c => c.OrderedTests)
                    .ThenInclude(ot => ot.LabTest);

            var totalCount = await query.CountAsync();

            var consultations = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
                .Take(paginationParams.PageSize)
                .ToListAsync();

            var consultationDtos = _mapper.Map<IEnumerable<ConsultationDto>>(consultations);

            return new PaginationResponse<ConsultationDto>(
                consultationDtos,
                totalCount,
                paginationParams.PageNumber,
                paginationParams.PageSize);
        }

        public async Task<ConsultationDto> GetConsultationByIdAsync(int consultationId)
        {
            var consultation = await _context.Consultations
                .Include(c => c.ICDCode)
                .Include(c => c.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Include(c => c.OrderedTests)
                    .ThenInclude(ot => ot.LabTest)
                .FirstOrDefaultAsync(c => c.ConsultationId == consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            return _mapper.Map<ConsultationDto>(consultation);
        }

        public async Task<ConsultationDto> GetConsultationByAppointmentIdAsync(int appointmentId)
        {
            var consultation = await _context.Consultations
                .Include(c => c.ICDCode)
                .Include(c => c.Prescriptions)
                    .ThenInclude(p => p.Medicine)
                .Include(c => c.OrderedTests)
                    .ThenInclude(ot => ot.LabTest)
                .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation for appointment {appointmentId} not found.");

            return _mapper.Map<ConsultationDto>(consultation);
        }

        public async Task<ConsultationDto> CreateConsultationAsync(CreateConsultationDto createConsultationDto)
        {
            var existingConsultation = await _context.Consultations
                .AnyAsync(c => c.AppointmentId == createConsultationDto.AppointmentId);

            if (existingConsultation)
                throw new InvalidOperationException("A consultation already exists for this appointment.");

            var consultation = _mapper.Map<Consultation>(createConsultationDto);
            consultation.CreatedAt = DateTime.UtcNow;

            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultation.ConsultationId);
        }

        public async Task<ConsultationDto> UpdateConsultationAsync(int consultationId, CreateConsultationDto updateConsultationDto)
        {
            var consultation = await _context.Consultations.FindAsync(consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            _mapper.Map(updateConsultationDto, consultation);

            await _context.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultationId);
        }

        public async Task<ConsultationDto> AddPrescriptionAsync(int consultationId, CreatePrescriptionDto prescriptionDto)
        {
            var consultation = await _context.Consultations.FindAsync(consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            var prescription = _mapper.Map<Prescription>(prescriptionDto);
            prescription.ConsultationId = consultationId;

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultationId);
        }

        public async Task<ConsultationDto> AddLabTestAsync(int consultationId, CreateOrderedTestDto orderedTestDto)
        {
            var consultation = await _context.Consultations.FindAsync(consultationId);

            if (consultation == null)
                throw new KeyNotFoundException($"Consultation with ID {consultationId} not found.");

            var orderedTest = _mapper.Map<OrderedTest>(orderedTestDto);
            orderedTest.ConsultationId = consultationId;
            orderedTest.Status = "Pending";

            _context.OrderedTests.Add(orderedTest);
            await _context.SaveChangesAsync();

            return await GetConsultationByIdAsync(consultationId);
        }
    }
}
