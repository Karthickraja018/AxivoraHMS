using Microsoft.AspNetCore.Mvc;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ConsultationsController : ControllerBase
    {
        private readonly IConsultationService _consultationService;

        public ConsultationsController(IConsultationService consultationService)
        {
            _consultationService = consultationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConsultationDto>>> GetAllConsultations()
        {
            var consultations = await _consultationService.GetAllConsultationsAsync();
            return Ok(consultations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ConsultationDto>> GetConsultationById(int id)
        {
            var consultation = await _consultationService.GetConsultationByIdAsync(id);
            return Ok(consultation);
        }

        [HttpGet("appointment/{appointmentId}")]
        public async Task<ActionResult<ConsultationDto>> GetConsultationByAppointment(int appointmentId)
        {
            var consultation = await _consultationService.GetConsultationByAppointmentIdAsync(appointmentId);
            return Ok(consultation);
        }

        [HttpPost]
        public async Task<ActionResult<ConsultationDto>> CreateConsultation([FromBody] CreateConsultationDto createConsultationDto)
        {
            var consultation = await _consultationService.CreateConsultationAsync(createConsultationDto);
            return CreatedAtAction(nameof(GetConsultationById), new { id = consultation.ConsultationId }, consultation);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ConsultationDto>> UpdateConsultation(int id, [FromBody] CreateConsultationDto updateConsultationDto)
        {
            var consultation = await _consultationService.UpdateConsultationAsync(id, updateConsultationDto);
            return Ok(consultation);
        }

        [HttpPost("{id}/prescriptions")]
        public async Task<ActionResult<ConsultationDto>> AddPrescription(int id, [FromBody] CreatePrescriptionDto prescriptionDto)
        {
            var consultation = await _consultationService.AddPrescriptionAsync(id, prescriptionDto);
            return Ok(consultation);
        }

        [HttpPost("{id}/lab-tests")]
        public async Task<ActionResult<ConsultationDto>> AddLabTest(int id, [FromBody] CreateOrderedTestDto orderedTestDto)
        {
            var consultation = await _consultationService.AddLabTestAsync(id, orderedTestDto);
            return Ok(consultation);
        }
    }
}
