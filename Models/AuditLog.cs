namespace MediCore.Models
{
    public class AuditLog
    {
        public int      Id        { get; set; }
        public int?     UserId    { get; set; }
        public string   Actor     { get; set; } = "";
        public string   Action    { get; set; } = "";
        public string   Target    { get; set; } = "";
        public string   Category  { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}
