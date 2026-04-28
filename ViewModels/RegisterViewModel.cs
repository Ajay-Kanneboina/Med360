using System.ComponentModel.DataAnnotations;

namespace MediCore.ViewModels
{
    public class RegisterViewModel
    {
        [Required] 
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress] 
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)] 
        public string Password { get; set; } = string.Empty;

        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required] 
        public string Role { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
}
