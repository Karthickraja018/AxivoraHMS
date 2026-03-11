using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Axivora.DTOs;
using Axivora.Repositories.Interfaces;

namespace Axivora.Security
{
    /// <summary>
    /// Handles the <see cref="OwnershipRequirement"/> for <see cref="AppointmentDto"/> resources.
    ///
    /// Authorization rules:
    ///   Admin  — always allowed.
    ///   Doctor — allowed only when the appointment's DoctorId matches the caller's doctor profile.
    ///   Patient — allowed only when the appointment's PatientId matches the caller's patient profile.
    ///
    /// Register via:
    ///   services.AddScoped&lt;IAuthorizationHandler, OwnershipAuthorizationHandler&gt;()
    /// </summary>
    public class OwnershipAuthorizationHandler
        : AuthorizationHandler<OwnershipRequirement, AppointmentDto>
    {
        private readonly IAppointmentRepository _repository;

        public OwnershipAuthorizationHandler(IAppointmentRepository repository)
        {
            _repository = repository;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OwnershipRequirement requirement,
            AppointmentDto resource)
        {
            var role = context.User.FindFirstValue(ClaimTypes.Role);

            // Admins bypass ownership checks
            if (role == "Admin")
            {
                context.Succeed(requirement);
                return;
            }

            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return;

            if (role == "Doctor")
            {
                var doctor = await _repository.GetDoctorByUserIdAsync(userId);
                if (doctor is not null && resource.DoctorId == doctor.DoctorId)
                    context.Succeed(requirement);
                return;
            }

            // Default — Patient ownership check
            var patient = await _repository.GetPatientByUserIdAsync(userId);
            if (patient is not null && resource.PatientId == patient.PatientId)
                context.Succeed(requirement);
        }
    }
}
