using MediCore.Models;

namespace MediCore.Services
{
    /// <summary>
    /// IDoctorService — contract for all doctor/clinical business logic.
    /// </summary>
    public interface IDoctorService
    {
        // ── Dashboard ──────────────────────────────────────────────────────
        /// <summary>Returns all KPI counts for the doctor dashboard.</summary>
        DoctorDashboardStats GetDashboardStats();

        /// <summary>Returns the N most recently added records (with patient names).</summary>
        List<Record> GetRecentRecords(int take = 5);

        /// <summary>Returns the N most recently updated complaints (with patient names).</summary>
        List<Complaint> GetRecentComplaints(int take = 5);

        /// <summary>
        /// Returns patients that have at least one open complaint,
        /// ordered by open-complaint count descending (highest urgency first).
        /// </summary>
        List<Patient> GetPriorityPatients(int take = 5);

        // ── Patients ───────────────────────────────────────────────────────
        /// <summary>Returns all patients with optional search and filter criteria.</summary>
        List<Patient> GetPatients(string? search, string? gender, string? blood);

        /// <summary>Returns distinct blood groups for the filter dropdown.</summary>
        List<string> GetBloodGroups();

        /// <summary>Returns a patient with full Records and Complaints loaded.</summary>
        Patient? GetPatientDetail(int id);

        // ── Records ────────────────────────────────────────────────────────
        /// <summary>
        /// Creates and saves a new medical record for a patient.
        /// DoctorName and CreatedAt are stamped here, not in the controller.
        /// </summary>
        Record AddRecord(int patientId, string diagnosis, string treatment,
                         string? notes, DateTime visitDate, string status, string doctorName);

        /// <summary>Returns a single record with its patient loaded.</summary>
        Record? GetRecord(int id);

        /// <summary>Updates an existing record's clinical fields.</summary>
        bool UpdateRecord(int id, string diagnosis, string treatment,
                          string? notes, DateTime visitDate, string status);

        /// <summary>
        /// Business rule: "closing" a record means Status = "Closed".
        /// It is a soft archive — the record stays visible but is flagged inactive.
        /// </summary>
        bool CloseRecord(int id);

        // ── Complaints ─────────────────────────────────────────────────────
        /// <summary>Returns complaints with optional filter and patient name loaded.</summary>
        List<Complaint> GetComplaints(string? filter);

        /// <summary>Returns total unread complaint count for sidebar badge.</summary>
        int GetUnreadCount();

        /// <summary>
        /// Retrieves a complaint and marks it as read.
        /// Business rule: opening a complaint automatically clears the unread flag.
        /// </summary>
        Complaint? GetAndMarkRead(int id);

        /// <summary>
        /// Saves doctor's response to a complaint and updates its status.
        /// RespondedBy and RespondedAt are stamped here, not in the controller.
        /// </summary>
        bool RespondToComplaint(int id, string response, string status, string doctorName);
    }

    /// <summary>Flat DTO for doctor dashboard KPI cards.</summary>
    public class DoctorDashboardStats
    {
        public int TotalPatients    { get; set; }
        public int TotalRecords     { get; set; }
        public int ActiveRecords    { get; set; }
        public int UnreadComplaints { get; set; }
    }
}
