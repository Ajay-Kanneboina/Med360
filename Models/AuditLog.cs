namespace MediCore.Models
{
    // Real database entity — maps to AuditLogs table in SQL Server.
    // Unlike AuditEntry (which was a temporary in-memory object),
    // this gets a permanent row every time something important happens.
    public class AuditLog
    {
        public int      Id        { get; set; }
        public int?     UserId    { get; set; }           // FK to Users table
        public string   Actor     { get; set; } = "";   // who did it (display name)
        public string   Action    { get; set; } = "";   // what they did
        public string   Target    { get; set; } = "";   // what was affected
        public string   Category  { get; set; } = "";   // "User" / "Record" / "Complaint"
        public DateTime Timestamp { get; set; }
    }
}
