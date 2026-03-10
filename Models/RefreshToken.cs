using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(512)]
        public string Token { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsRevoked { get; set; }

        public DateTime? RevokedAt { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
    }
}
