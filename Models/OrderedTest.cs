using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class OrderedTest
    {
        public int OrderedTestId { get; set; }

        public int ConsultationId { get; set; }

        public int LabTestId { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = null!;

        public string? Result { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ResultDate { get; set; }

        // Navigation properties
        public Consultation? Consultation { get; set; }
        public LabTest? LabTest { get; set; }
    }
}
