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

        Record AddRecord(Record data, string doctorName);
        Record? GetRecord(int id);
        bool UpdateRecord(int id, Record data);
        bool CloseRecord(int id);

        List<Complaint> GetComplaints(string? filter);
        int GetUnreadCount();
        Complaint? GetAndMarkRead(int id);
        bool RespondToComplaint(int id, Complaint data, string doctorName);
    }

    public class DoctorDashboardStats
    {
        public int TotalPatients { get; set; }
        public int TotalRecords  { get; set; }
        public int ActiveRecords { get; set; }
        public int UnreadComplaints { get; set; }
    }
}
