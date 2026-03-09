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
    }

    public class LabResultUpdateDto
    {
        [Required(ErrorMessage = "Result is required")]
        [StringLength(2000)]
        public string Result { get; set; } = null!;
    }
}
