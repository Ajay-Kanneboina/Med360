using System.ComponentModel.DataAnnotations;

namespace MediCore.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required] 
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required] 
        public DateTime AppointmentDate { get; set; }

        [Required]
        public string TimeSlot { get; set; } = string.Empty;
        public string Status { get; set; } = "Scheduled";
        public string? Notes { get; set; }
        public string? CancelReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public Patient? Patient { get; set; }
        public User? Doctor { get; set; }
    }
}
