using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class LabTest
    {
        [Key]
        public int LabTestId { get; set; }

        [Required]
        [StringLength(150)]
        public string TestName { get; set; } = null!;

        // Navigation properties
        public ICollection<OrderedTest> OrderedTests { get; set; } = new List<OrderedTest>();
    }
}
