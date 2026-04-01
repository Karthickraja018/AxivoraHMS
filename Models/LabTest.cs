using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class LabTest
    {
        public int LabTestId { get; set; }

        [Required(ErrorMessage = "Test name is required")]
        public string TestName { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Unit { get; set; }

        [StringLength(200)]
        public string? ReferenceRange { get; set; }

        [StringLength(50)]
        public string TestType { get; set; } = "Single"; // Single, Multi, Report

        // Navigation properties
        public ICollection<OrderedTest> OrderedTests { get; set; } = new List<OrderedTest>();
    }
}
