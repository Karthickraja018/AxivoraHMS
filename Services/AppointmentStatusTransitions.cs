namespace Axivora.Services
{
    /// <summary>
    /// Defines and enforces the appointment status state machine.
    ///
    /// Terminal states (Completed, Cancelled, No-Show) cannot be left once entered.
    /// Transitions that are not listed in the matrix for a given role are rejected
    /// with <see cref="InvalidOperationException"/>.
    /// </summary>
    internal static class AppointmentStatusTransitions
    {
        // ?? Terminal states ???????????????????????????????????????????????????
        private static readonly HashSet<string> TerminalStatuses =
        [
            "Completed",
            "Cancelled",
            "No-Show"
        ];

        // ?? Allowed transitions per role ?????????????????????????????????????
        // Key  : (fromStatus, toStatus)
        // Value: minimum role required ("Patient" < "Doctor" < "Admin")
        //        stored as a set of roles that ARE allowed to make this move.
        private static readonly Dictionary<(string From, string To), HashSet<string>> AllowedTransitions = new()
        {
            // Scheduled ? Confirmed: Doctor / Admin only
            [("Scheduled", "Confirmed")]    = ["Doctor", "Admin"],

            // Confirmed ? Checked-In: any authenticated role
            [("Confirmed", "Checked-In")]   = ["Patient", "Doctor", "Admin"],

            // Checked-In ? In Progress: Doctor / Admin only
            [("Checked-In", "In Progress")] = ["Doctor", "Admin"],

            // In Progress ? Completed: Doctor / Admin only
            [("In Progress", "Completed")]  = ["Doctor", "Admin"],

            // Any non-terminal ? Cancelled: all roles (ownership enforced separately)
            [("Scheduled",   "Cancelled")]  = ["Patient", "Doctor", "Admin"],
            [("Confirmed",   "Cancelled")]  = ["Patient", "Doctor", "Admin"],
            [("Checked-In",  "Cancelled")]  = ["Patient", "Doctor", "Admin"],
            [("In Progress", "Cancelled")]  = ["Patient", "Doctor", "Admin"],
            [("Rescheduled", "Cancelled")]  = ["Patient", "Doctor", "Admin"],

            // Any non-terminal ? No-Show: Doctor / Admin only
            [("Scheduled",   "No-Show")]    = ["Doctor", "Admin"],
            [("Confirmed",   "No-Show")]    = ["Doctor", "Admin"],
            [("Checked-In",  "No-Show")]    = ["Doctor", "Admin"],

            // Any non-terminal ? Rescheduled: Doctor / Admin only
            [("Scheduled",   "Rescheduled")]  = ["Doctor", "Admin"],
            [("Confirmed",   "Rescheduled")]  = ["Doctor", "Admin"],
            [("Checked-In",  "Rescheduled")]  = ["Doctor", "Admin"],
            [("In Progress", "Rescheduled")]  = ["Doctor", "Admin"],
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
