using MediCore.Models;

namespace MediCore.Services
{
    public interface IDoctorService
    {
        DoctorDashboardStats GetDashboardStats();
        List<Record> GetRecentRecords(int take = 5);
        List<Complaint> GetRecentComplaints(int take = 5);
        List<Patient> GetPriorityPatients(int take = 5);

        List<Patient> GetPatients(string? search, string? gender, string? blood);
        List<string> GetBloodGroups();
        Patient? GetPatientDetail(int id);

        Record AddRecord(int patientId, string diagnosis, string treatment,
                         string? notes, DateTime visitDate, string status, string doctorName);
        Record? GetRecord(int id);
        bool UpdateRecord(int id, string diagnosis, string treatment,
                          string? notes, DateTime visitDate, string status);
        bool CloseRecord(int id);

        List<Complaint> GetComplaints(string? filter);
        int GetUnreadCount();
        Complaint? GetAndMarkRead(int id);
        bool RespondToComplaint(int id, string response, string status, string doctorName);
    }

    public class DoctorDashboardStats
    {
        public int TotalPatients    { get; set; }
        public int TotalRecords     { get; set; }
        public int ActiveRecords    { get; set; }
        public int UnreadComplaints { get; set; }
    }
}
