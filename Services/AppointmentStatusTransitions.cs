namespace Axivora.Services
{
    /// <summary>
    /// Defines and enforces the appointment status state machine.
    ///
    /// Terminal states (Completed, Cancelled, NoShow) cannot be left once entered.
    /// Transitions that are not listed in the matrix for a given role are rejected
    /// with <see cref="InvalidOperationException"/>.
    /// </summary>
    internal static class AppointmentStatusTransitions
    {
        // Terminal states
        private static readonly HashSet<string> TerminalStatuses =
        [
            "Completed",
            "Cancelled",
            "NoShow"
        ];

        // Allowed transitions per role
        // Key  : (fromStatus, toStatus)
        // Value: minimum role required ("Patient" < "Doctor" < "Admin")
        //        stored as a set of roles that ARE allowed to make this move.
        private static readonly Dictionary<(string From, string To), HashSet<string>> AllowedTransitions = new()
        {
            // Scheduled -> InProgress: Doctor / Admin only
            [("Scheduled", "InProgress")] = ["Doctor", "Admin"],

            // InProgress -> PendingDocumentation: Doctor / Admin only
            [("InProgress", "PendingDocumentation")] = ["Doctor", "Admin"],

            // PendingDocumentation -> Completed: Doctor / Admin only
            [("PendingDocumentation", "Completed")] = ["Doctor", "Admin"],

            // Scheduled -> Cancelled: all roles (ownership enforced separately)
            [("Scheduled", "Cancelled")]  = ["Patient", "Doctor", "Admin"],

            // Scheduled -> NoShow: Doctor / Admin only (primarily for background job / ops)
            [("Scheduled", "NoShow")]     = ["Doctor", "Admin"],
        };

        /// <summary>
        /// Validates that the transition from <paramref name="fromStatus"/> to
        /// <paramref name="toStatus"/> is permitted for the given <paramref name="callerRole"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the transition is not defined or the caller's role is not
        /// permitted to make it.
        /// </exception>
        public static void Validate(string fromStatus, string toStatus, string callerRole)
        {
            if (fromStatus == toStatus)
                return;

            if (TerminalStatuses.Contains(fromStatus))
                throw new InvalidOperationException(
                    $"Cannot transition from '{fromStatus}': it is a terminal status.");

            if (!AllowedTransitions.TryGetValue((fromStatus, toStatus), out var allowedRoles))
                throw new InvalidOperationException(
                    $"Transition from '{fromStatus}' to '{toStatus}' is not permitted.");

            if (!allowedRoles.Contains(callerRole))
                throw new InvalidOperationException(
                    $"Role '{callerRole}' is not allowed to transition an appointment from '{fromStatus}' to '{toStatus}'.");
        }
    }
}
