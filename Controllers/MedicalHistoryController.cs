using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Axivora.DTOs;
using Axivora.Services.Interfaces;

namespace Axivora.Controllers
{
    [ApiController]
    [Route("api/patients")]
    [Authorize]
    public class MedicalHistoryController : ControllerBase
    {
        private readonly IMedicalHistoryService _medicalHistoryService;

        public MedicalHistoryController(IMedicalHistoryService medicalHistoryService)
        {
            _medicalHistoryService = medicalHistoryService;
        }

        /// <summary>
        /// Get complete medical history for a specific patient (Admin, Doctor)
        /// </summary>
        [HttpGet("{patientId}/medical-history")]
        [Authorize(Roles = "Admin,Doctor")]
        [ProducesResponseType(typeof(MedicalHistoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<MedicalHistoryDto>> GetMedicalHistory(int patientId)
        {
            var history = await _medicalHistoryService.GetMedicalHistoryByPatientIdAsync(patientId);
            return Ok(history);
        }

        /// <summary>
        /// Get medical history for the currently authenticated patient
        /// </summary>
        [HttpGet("me/medical-history")]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(MedicalHistoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<MedicalHistoryDto>> GetMyMedicalHistory()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var history = await _medicalHistoryService.GetMyMedicalHistoryAsync(userId);
            return Ok(history);
        }
    }
}
