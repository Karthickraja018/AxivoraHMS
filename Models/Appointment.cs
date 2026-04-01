using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }

        public int PatientId { get; set; }

        public int DoctorId { get; set; }

        public int StatusId { get; set; }

        /// <summary>
        /// References the pre-generated slot this appointment occupies.
        /// Null for appointments created via the legacy flow.
        /// </summary>
        public int? SlotId { get; set; }

        [Required(ErrorMessage = "Appointment start time is required")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentStart { get; set; }

        [Required(ErrorMessage = "Appointment end time is required")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentEnd { get; set; }

        public string? Reason { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Set to true by <see cref="Services.BackgroundServices.AppointmentReminderService"/> once
        /// the 24-hour reminder email has been enqueued, preventing duplicate reminders.
        /// </summary>
        public bool ReminderSent { get; set; }

        /// <summary>
        /// Set to true by <see cref="Services.BackgroundServices.AppointmentReminderService"/> once
        /// the 2-hour reminder email has been enqueued.
        /// </summary>
        public bool Reminder2HoursSent { get; set; }

        /// <summary>Optimistic concurrency token – prevents conflicting concurrent updates.</summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        // Navigation properties
        public Patient? Patient { get; set; }
        public Doctor? Doctor { get; set; }
        public AppointmentStatus? Status { get; set; }
        public Consultation? Consultation { get; set; }
        public AppointmentSlot? Slot { get; set; }
    }
}
