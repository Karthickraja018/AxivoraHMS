using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Axivora.Models
{
    public class Role
    {
        [Key]
        
        public int RoleId { get; set; }
        [Required]
        [StringLength(100)]
        public string RoleName { get; set; } = null!;

        // Navigation: one role can be assigned to many users via UserRoles
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
