
using MediCore.Models;

namespace MediCore.Services
{
    public interface IAdminService
    {
        AdminDashboardStats GetDashboardStats();
        List<Patient> GetRecentPatients(int take = 5);
        List<Complaint> GetRecentComplaints(int take = 5);
        List<User> GetPendingUsers();
        List<User> GetAllStaff(string? role, string? search);
        User? ApproveUser(int id);
        string? RejectUser(int id);
        User? ToggleUserActive(int id);
        string? DeleteUser(int id);
        List<Patient> GetAllPatients(string? search, string? gender);
        Patient? GetPatientDetail(int id);
        bool DeletePatient(int id);
        List<Record> GetAllRecords(string? status, string? search);
        List<Complaint> GetAllComplaints(string? status, string? search);
        int GetUnreadComplaintCount();
        List<AuditEntry> GetAuditEntries(int take = 80);
        AdminAnalyticsStats GetAnalyticsStats();
    }

    public class AdminDashboardStats
    {
        public int TotalPatients { get; set; }
        public int TotalRecords { get; set; }
        public int TotalComplaints { get; set; }
        public int ActiveRecords { get; set; }
        public int ResolvedComplaints { get; set; }
        public int PendingApprovals { get; set; }
        public int NewComplaints { get; set; }
        public int DoctorCount { get; set; }
        public int NurseCount { get; set; }
        public int ReceptionistCount  { get; set; }
    }

    public class AdminAnalyticsStats
    {
        public int TotalPatients { get; set; }
        public int TotalRecords { get; set; }
        public int TotalComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int ActiveRecords { get; set; }
        public int TotalStaff { get; set; }
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public int OtherGender { get; set; }
        public int ActiveComplaints { get; set; }
        public int ReviewedComplaints { get; set; }
        public int ClosedComplaints { get; set; }
        public int DoctorCount { get; set; }
        public int NurseCount { get; set; }
        public int ReceptionistCount { get; set; }
        public List<BloodGroupStat> BloodGroups { get; set; } = new();
    }

    public class BloodGroupStat
    {
        public string Group { get; set; } = "";
        public int Count { get; set; }
    }
}
