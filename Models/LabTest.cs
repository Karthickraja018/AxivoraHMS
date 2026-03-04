using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class LabTest
    {
        public int LabTestId { get; set; }

        [Required(ErrorMessage = "Test name is required")]
        public string TestName { get; set; } = null!;

        // Navigation properties
        public ICollection<OrderedTest> OrderedTests { get; set; } = new List<OrderedTest>();
    }
}
