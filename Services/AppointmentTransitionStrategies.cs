using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    internal abstract class AppointmentTransitionStrategyBase : IAppointmentTransitionStrategy
    {
        private readonly string _from;
        private readonly string _to;
        private readonly HashSet<string> _allowedRoles;

        protected AppointmentTransitionStrategyBase(string from, string to, params string[] allowedRoles)
        {
            _from = AppointmentTransitionValidator.Normalize(from);
            _to = AppointmentTransitionValidator.Normalize(to);
            _allowedRoles = allowedRoles
                .Select(r => r.Trim())
                .Where(r => r.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public bool CanHandle(string toStatus) => AppointmentTransitionValidator.Normalize(toStatus) == _to;

        public void Validate(string fromStatus, string toStatus, string callerRole)
        {
            var from = AppointmentTransitionValidator.Normalize(fromStatus);
            var to = AppointmentTransitionValidator.Normalize(toStatus);

            if (to != _to || from != _from)
                throw new InvalidOperationException($"Transition from '{fromStatus}' to '{toStatus}' is not permitted.");

            if (!_allowedRoles.Contains(callerRole))
                throw new InvalidOperationException(
                    $"Role '{callerRole}' is not allowed to transition an appointment from '{fromStatus}' to '{toStatus}'.");
        }
    }

    internal sealed class StartConsultationTransitionStrategy : AppointmentTransitionStrategyBase
    {
        public StartConsultationTransitionStrategy() : base("Scheduled", "InProgress", "Doctor", "Admin")
        {
        }
    }

    internal sealed class EndConsultationTransitionStrategy : AppointmentTransitionStrategyBase
    {
        public EndConsultationTransitionStrategy() : base("InProgress", "PendingDocumentation", "Doctor", "Admin")
        {
        }
    }

    internal sealed class CompleteConsultationTransitionStrategy : AppointmentTransitionStrategyBase
    {
        public CompleteConsultationTransitionStrategy() : base("PendingDocumentation", "Completed", "Doctor", "Admin")
        {
        }
    }

    internal sealed class CancelAppointmentTransitionStrategy : AppointmentTransitionStrategyBase
    {
        public CancelAppointmentTransitionStrategy() : base("Scheduled", "Cancelled", "Patient", "Doctor", "Admin")
        {
        }
    }

    internal sealed class NoShowTransitionStrategy : AppointmentTransitionStrategyBase
    {
        public NoShowTransitionStrategy() : base("Scheduled", "NoShow", "Doctor", "Admin")
        {
        }
    }
}
