using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    /// <summary>
    /// Stores a patient's post-session feedback for a completed consultation.
    /// One feedback per consultation (enforced by UNIQUE constraint on ConsultationId).
    /// Rating follows a 1–5 scale where 1 = Very Poor and 5 = Excellent.
    /// </summary>
    public class SessionFeedback
    {
        public int FeedbackId { get; set; }

        /// <summary>
        /// The consultation this feedback belongs to (1-to-1).
        /// </summary>
        [Required]
        public int ConsultationId { get; set; }

        /// <summary>
        /// The patient who submitted the feedback.
        /// Stored explicitly so the owership check does not require joining
        /// through Consultation ? Appointment ? Patient.
        /// </summary>
        [Required]
        public int PatientId { get; set; }

        /// <summary>
        /// Numeric satisfaction rating: 1 (Very Poor) to 5 (Excellent).
        /// </summary>
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        /// <summary>
        /// Optional free-text comment from the patient.
        /// </summary>
        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Set to true when the patient edits their original feedback.
        /// </summary>
        public bool IsEdited { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Consultation? Consultation { get; set; }
        public Patient? Patient { get; set; }
    }
}
