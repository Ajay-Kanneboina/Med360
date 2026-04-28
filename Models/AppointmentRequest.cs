using System.ComponentModel.DataAnnotations;

namespace MediCore.Models
{
    public class AppointmentRequest
    {
        [Key]
        public int Id { get; set; }

        [Required] 
        public int PatientId { get; set; }

        [Required] 
        public string Message { get; set; } = string.Empty;
        public string? PreferredDate { get; set; }
        public string? PreferredTime { get; set; }
        public bool IsHandled { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public Patient? Patient { get; set; }
    }
}
