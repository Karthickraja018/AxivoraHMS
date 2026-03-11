using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    /// <summary>
    /// Represents a single bookable time slot within a DoctorAvailabilityDay.
    /// Slots are stored once (not regenerated dynamically).
    /// </summary>
    public class AppointmentSlot
    {
        public int Id { get; set; }

        public int DoctorId { get; set; }

        public int AvailabilityDayId { get; set; }

        public DateTime SlotStart { get; set; }

        public DateTime SlotEnd { get; set; }

        /// <summary>Available / Booked / Blocked / Cancelled</summary>
        public string Status { get; set; } = SlotStatus.Available;

        /// <summary>Populated once a patient books this slot.</summary>
        public int? AppointmentId { get; set; }

        /// <summary>Optimistic concurrency token — prevents simultaneous double-bookings.</summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        // Navigation properties
        public Doctor? Doctor { get; set; }
        public DoctorAvailabilityDay? AvailabilityDay { get; set; }
        public Appointment? Appointment { get; set; }
    }

    public static class SlotStatus
    {
        public const string Available = "Available";
        public const string Booked    = "Booked";
        public const string Blocked   = "Blocked";
        public const string Cancelled = "Cancelled";
    }
}
