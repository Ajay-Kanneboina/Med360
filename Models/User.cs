using System.ComponentModel.DataAnnotations;

namespace MediCore.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required] 
        public string FullName { get; set; } = string.Empty;

        [Required] 
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required] 
        public string Role { get; set; } = "Patient";

        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
