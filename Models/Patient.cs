using System.ComponentModel.DataAnnotations;

namespace MediCore.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }
        public int? UserId { get; set; }

        [Required] 
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; } = DateTime.Today;
        public string? Gender { get; set; }
        public string? BloodGroup { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContact { get; set; }
        public string? MedicalHistory { get; set; }
        public DateTime RegisteredOn { get; set; } = DateTime.Now;
        public List<Complaint> Complaints { get; set; } = new List<Complaint>();
        public List<Record> Records { get; set; } = new List<Record>();
    }
}
