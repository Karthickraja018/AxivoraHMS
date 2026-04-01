using System.ComponentModel.DataAnnotations;

namespace Axivora.DTOs
{
    public class LabResultDto
    {
        public int OrderedTestId { get; set; }
        public int ConsultationId { get; set; }
        public int LabTestId { get; set; }
        public string TestName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Result { get; set; }
        public DateTime? ResultDate { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public DateTime OrderedAt { get; set; }
        public string TestType { get; set; } = null!;
        public string? Unit { get; set; }
        public string? ReferenceRange { get; set; }
        public bool HasReportFile { get; set; }
        public string? ReportFileName { get; set; }
    }

    public class LabResultUpdateDto
    {
        [Required(ErrorMessage = "Result is required")]
        [StringLength(10000)]
        public string Result { get; set; } = null!;
    }

    /// <summary>Read-only lab test catalogue entry.</summary>
    public class LabTestCatalogueDto
    {
        /// <summary>Unique lab test identifier.</summary>
        public int LabTestId { get; set; }

        /// <summary>Full test name (e.g. Complete Blood Count (CBC)).</summary>
        public string TestName { get; set; } = null!;

        public string? Description { get; set; }

        public string TestType { get; set; } = null!;
        public string? Unit { get; set; }
        public string? ReferenceRange { get; set; }
    }

    /// <summary>Patient-facing view of their own lab results.</summary>
    public class PatientLabResultDto
    {
        public int OrderedTestId { get; set; }
        public string LabTestName { get; set; } = null!;
        public string? Result { get; set; }
        public DateTime OrderedDate { get; set; }
        public DateTime? ResultDate { get; set; }
        public string DoctorName { get; set; } = null!;
        public bool HasReportFile { get; set; }
        public string? ReportFileName { get; set; }
    }
}
