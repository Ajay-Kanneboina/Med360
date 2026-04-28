using System.ComponentModel.DataAnnotations;

namespace MediCore.Models
{
    public class DoctorAvailability
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required] 
        public int DayOfWeek { get; set; }

        [Required] 
        public string StartTime { get; set; } = "09:00 AM";

        [Required]
        public string EndTime { get; set; } = "05:00 PM";

        public int  MaxSlots    { get; set; } = 10;
        public bool IsAvailable { get; set; } = true;
        public User? Doctor { get; set; }
    }
}
