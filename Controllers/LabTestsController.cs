using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/lab-tests")]
    [Authorize]
    public class LabTestsController : ControllerBase
    {
        private readonly ILabTestService _labTestService;

        public LabTestsController(ILabTestService labTestService)
        {
            _labTestService = labTestService;
        }

        /// <summary>
        /// Upload or update a lab test result (Admin, LabTechnician only)
        /// </summary>
        [HttpPut("{orderedTestId}/result")]
        [Authorize(Roles = "Admin,LabTechnician")]
        [ProducesResponseType(typeof(LabResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<LabResultDto>> UploadResult(int orderedTestId, [FromBody] LabResultUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _labTestService.UploadResultAsync(orderedTestId, dto);
            return Ok(result);
        }

        /// <summary>
        /// Get all lab test results for a patient (Admin, Doctor, LabTechnician)
        /// </summary>
        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Admin,Doctor,LabTechnician")]
        [ProducesResponseType(typeof(IEnumerable<LabResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<LabResultDto>>> GetResultsByPatient(int patientId)
        {
            var results = await _labTestService.GetResultsByPatientAsync(patientId);
            return Ok(results);
        }

        /// <summary>
        /// Get all lab tests ordered during a consultation (Admin, Doctor, LabTechnician)
        /// </summary>
        [HttpGet("consultation/{consultationId}")]
        [Authorize(Roles = "Admin,Doctor,LabTechnician")]
        [ProducesResponseType(typeof(IEnumerable<LabResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<LabResultDto>>> GetResultsByConsultation(int consultationId)
        {
            var results = await _labTestService.GetResultsByConsultationAsync(consultationId);
            return Ok(results);
        }
    }
}
