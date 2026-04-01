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

        public DateTime OrderedAt { get; set; } = DateTime.UtcNow;

        // Optional report attachment (e.g. PDF scan) uploaded by Doctor/Admin
        public string? ReportFilePath { get; set; }
        public string? ReportFileName { get; set; }
        public string? ReportContentType { get; set; }
        public long? ReportSizeBytes { get; set; }

        // Navigation properties
        public Consultation? Consultation { get; set; }
        public LabTest? LabTest { get; set; }
    }
}
