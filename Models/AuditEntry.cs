namespace MediCore.Models
{
    /// <summary>
    /// AuditEntry — lightweight DTO used by the audit log view.
    /// Represents a single auditable system event assembled from
    /// Users, Records, or Complaints tables by AdminService.
    /// </summary>
    public class AuditEntry
    {
        /// <summary>Who performed the action (doctor name, patient name, or system).</summary>
        public string Actor { get; set; } = "";

        /// <summary>Human-readable description of the event (e.g. "Medical Record Added").</summary>
        public string Action { get; set; } = "";

        /// <summary>The entity affected (patient name, email, or truncated description).</summary>
        public string Target { get; set; } = "";

        /// <summary>Event category: "User", "Record", or "Complaint".</summary>
        public string Category { get; set; } = "";

        /// <summary>When the event occurred — used for sorting the feed.</summary>
        public DateTime Timestamp { get; set; }
    }
}
