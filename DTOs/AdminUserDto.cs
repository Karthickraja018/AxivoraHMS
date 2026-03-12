namespace Axivora.DTOs
{
    public class AdminUserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string? Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
