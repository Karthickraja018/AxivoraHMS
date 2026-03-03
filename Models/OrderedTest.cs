using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class OrderedTest
    {
        [Key]
        public int OrderedTestId { get; set; }

        [Required]
        public int ConsultationId { get; set; }

        [Required]
        public int LabTestId { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public string? Result { get; set; }

        public DateTime? ResultDate { get; set; }

        // Navigation properties
        public Consultation? Consultation { get; set; }
        public LabTest? LabTest { get; set; }
    }
}
