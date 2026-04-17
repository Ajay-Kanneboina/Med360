
using MediCore.Models;

namespace MediCore.Services
{
    /// <summary>
    /// IAdminService — contract for all admin business logic.
    /// The controller depends on this interface, not the concrete class,
    /// which makes it easy to swap implementations or mock in unit tests.
    /// </summary>
    public interface IAdminService
    {
        // ── Dashboard ──────────────────────────────────────────────────────
        /// <summary>Returns all KPI counts needed for the admin dashboard.</summary>
        AdminDashboardStats GetDashboardStats();

        /// <summary>Returns the 5 most recently registered patients.</summary>
        List<Patient> GetRecentPatients(int take = 5);

        /// <summary>Returns the 5 most recently updated complaints.</summary>
        List<Complaint> GetRecentComplaints(int take = 5);

        // ── User Management ────────────────────────────────────────────────
        /// <summary>
        /// Returns staff accounts (Doctor, Nurse, Receptionist) that are
        /// not yet active — i.e. awaiting admin approval.
        /// </summary>
        List<User> GetPendingUsers();

        /// <summary>
        /// Returns all non-patient users, with optional role and name/email filters.
        /// </summary>
        List<User> GetAllStaff(string? role, string? search);

        /// <summary>Activates a pending user account. Returns the approved user.</summary>
        User? ApproveUser(int id);

        /// <summary>Removes a user registration entirely. Returns the deleted user's name.</summary>
        string? RejectUser(int id);

        /// <summary>Flips IsActive on a staff account. Returns updated user.</summary>
        User? ToggleUserActive(int id);

        /// <summary>Permanently deletes a staff user. Returns deleted user's name.</summary>
        string? DeleteUser(int id);

        // ── Patient Management ─────────────────────────────────────────────
        /// <summary>Returns all patients with optional name/email/phone and gender filters.</summary>
        List<Patient> GetAllPatients(string? search, string? gender);

        /// <summary>Returns a single patient with their Records and Complaints loaded.</summary>
        Patient? GetPatientDetail(int id);

        /// <summary>Deletes a patient and all their cascaded records/complaints.</summary>
        bool DeletePatient(int id);

        // ── Records ────────────────────────────────────────────────────────
        /// <summary>Returns all medical records with optional status and keyword filters.</summary>
        List<Record> GetAllRecords(string? status, string? search);

        // ── Complaints ─────────────────────────────────────────────────────
        /// <summary>Returns complaints with optional status/unread and keyword filters.</summary>
        List<Complaint> GetAllComplaints(string? status, string? search);

        /// <summary>Returns total count of unread complaints.</summary>
        int GetUnreadComplaintCount();

        // ── Audit Log ──────────────────────────────────────────────────────
        /// <summary>
        /// Builds a unified, time-sorted audit feed from Users, Records,
        /// and Complaints tables. Business rule: what counts as an auditable
        /// event is defined here, not in the controller.
        /// </summary>
        List<AuditEntry> GetAuditEntries(int take = 80);

        // ── Analytics ─────────────────────────────────────────────────────
        /// <summary>Returns all analytics data needed for the analytics view.</summary>
        AdminAnalyticsStats GetAnalyticsStats();
    }

    // ── DTOs returned by the service ──────────────────────────────────────

    /// <summary>Flat DTO carrying all KPI counts for the dashboard view.</summary>
    public class AdminDashboardStats
    {
        public int TotalPatients      { get; set; }
        public int TotalUsers         { get; set; }
        public int TotalRecords       { get; set; }
        public int TotalComplaints    { get; set; }
        public int ActiveRecords      { get; set; }
        public int ResolvedComplaints { get; set; }
        public int PendingApprovals   { get; set; }
        public int NewComplaints      { get; set; }
        public int DoctorCount        { get; set; }
        public int NurseCount         { get; set; }
        public int ReceptionistCount  { get; set; }
    }

    /// <summary>Flat DTO carrying all analytics breakdown counts.</summary>
    public class AdminAnalyticsStats
    {
        public int TotalPatients      { get; set; }
        public int TotalRecords       { get; set; }
        public int TotalComplaints    { get; set; }
        public int ResolvedComplaints { get; set; }
        public int ActiveRecords      { get; set; }
        public int TotalStaff         { get; set; }
        public int MaleCount          { get; set; }
        public int FemaleCount        { get; set; }
        public int OtherGender        { get; set; }
        public int ActiveComplaints   { get; set; }
        public int ReviewedComplaints { get; set; }
        public int ClosedComplaints   { get; set; }
        public int DoctorCount        { get; set; }
        public int NurseCount         { get; set; }
        public int ReceptionistCount  { get; set; }
        public List<BloodGroupStat> BloodGroups { get; set; } = new();
    }

    /// <summary>Blood group name + patient count pair used in analytics.</summary>
    public class BloodGroupStat
    {
        public string Group { get; set; } = "";
        public int    Count { get; set; }
    }
}
