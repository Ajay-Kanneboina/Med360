using System.ComponentModel.DataAnnotations;

namespace MediCore.Models
{
    public class Complaint
    {
        [Key]
        public int Id { get; set; }
        public int PatientId { get; set; }

        [Required] 
        public string Description { get; set; } = string.Empty;
        public string? AdditionalNotes { get; set; }
        public string Status { get; set; } = "Active";
        public string? DoctorResponse { get; set; }
        public string? RespondedBy { get; set; }
        public DateTime? RespondedAt { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public Patient? Patient { get; set; }
    }
}
