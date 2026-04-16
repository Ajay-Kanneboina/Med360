using System.ComponentModel.DataAnnotations;

namespace MediCore.Models
{
    public class Record
    {
        public int      Id         { get; set; }
        public int      PatientId  { get; set; }
        [Required] public string Diagnosis  { get; set; } = string.Empty;
        [Required] public string Treatment  { get; set; } = string.Empty;
        public string?  Notes      { get; set; }
        public DateTime VisitDate  { get; set; } = DateTime.Today;
        public string   Status     { get; set; } = "Active";
        public string?  DoctorName { get; set; }
        public DateTime CreatedAt  { get; set; } = DateTime.Now;
        public Patient? Patient    { get; set; }
    }
}
