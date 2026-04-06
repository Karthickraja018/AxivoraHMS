using Axivora.Services.Interfaces;

namespace Axivora.Services
{
    public class AppointmentTransitionValidator : IAppointmentTransitionValidator
    {
        private static readonly HashSet<string> TerminalStatuses =
        [
            "completed",
            "cancelled",
            "noshow"
        ];

        private readonly IEnumerable<IAppointmentTransitionStrategy> _strategies;

        public AppointmentTransitionValidator(IEnumerable<IAppointmentTransitionStrategy> strategies)
        {
            _strategies = strategies;
        }

        public void ValidateTransition(string fromStatus, string toStatus, string callerRole)
        {
            var from = Normalize(fromStatus);
            var to = Normalize(toStatus);

            if (from == to)
                return;

            if (TerminalStatuses.Contains(from))
                throw new InvalidOperationException($"Cannot transition from '{fromStatus}': it is a terminal status.");

            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(to));
            if (strategy is null)
                throw new InvalidOperationException($"Transition from '{fromStatus}' to '{toStatus}' is not permitted.");

            strategy.Validate(fromStatus, toStatus, callerRole);
        }

        internal static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim()
                    .Replace(" ", string.Empty, StringComparison.Ordinal)
                    .Replace("-", string.Empty, StringComparison.Ordinal)
                    .ToLowerInvariant();
    }
}
