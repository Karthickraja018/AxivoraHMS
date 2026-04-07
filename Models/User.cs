using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Axivora.Models
{
    public class User
    {
        public int UserId { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        /// <summary>True once the user has verified their email address via OTP.</summary>
        public bool IsEmailVerified { get; set; }

        /// <summary>When true, user must update temporary password before accessing protected workflows.</summary>
        public bool MustChangePassword { get; set; }

        /// <summary>Hashed OTP stored temporarily until the user verifies their email.</summary>
        public string? EmailVerificationOtp { get; set; }

        /// <summary>UTC expiry time of the current OTP. Null when no OTP is pending.</summary>
        public DateTime? OtpExpiresAt { get; set; }

        /// <summary>Hashed one-time password reset token.</summary>
        public string? PasswordResetTokenHash { get; set; }

        /// <summary>UTC expiry time for the password reset token.</summary>
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        /// <summary>UTC timestamp when password reset was requested.</summary>
        public DateTime? PasswordResetRequestedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public Patient? Patient { get; set; }
        public Doctor? Doctor { get; set; }
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
