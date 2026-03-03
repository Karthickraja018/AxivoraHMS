using System;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditId { get; set; }

        [Required]
        public int UserId { get; set; }

        [StringLength(100)]
        public string? Action { get; set; }

        [StringLength(100)]
        public string? EntityName { get; set; }

        public int? EntityId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
    }
}
