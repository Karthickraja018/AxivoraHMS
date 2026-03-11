using System.ComponentModel.DataAnnotations;

namespace Axivora.DTOs
{
    public class SessionFeedbackDto
    {
        public int FeedbackId { get; set; }
        public int ConsultationId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = null!;

        /// <summary>1 = Very Poor, 2 = Poor, 3 = Average, 4 = Good, 5 = Excellent</summary>
        public int Rating { get; set; }
        public string RatingLabel { get; set; } = null!;
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateFeedbackDto
    {
        [Required]
        public int ConsultationId { get; set; }

        /// <summary>1 (Very Poor) to 5 (Excellent).</summary>
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 (Very Poor) and 5 (Excellent).")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string? Comment { get; set; }
    }

    public class UpdateFeedbackDto
    {
        /// <summary>1 (Very Poor) to 5 (Excellent).</summary>
        [Range(1, 5, ErrorMessage = "Rating must be between 1 (Very Poor) and 5 (Excellent).")]
        public int? Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string? Comment { get; set; }
    }
}
