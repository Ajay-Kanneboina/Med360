using MediCore.Models;

namespace MediCore.Services
{
    public interface IPatientService
    {
        List<Record> GetMyRecords(int patientId, string? status, string? search);
        PatientRecordStats GetRecordStats(int patientId);
        Record? GetMyRecord(int recordId, int patientId);

        List<Complaint> GetMyComplaints(int patientId);
        Complaint SubmitComplaint(int patientId, Complaint data);
        bool UpdateComplaint(int complaintId, int patientId, Complaint data);

        Patient? GetMyProfile(int patientId);
        bool UpdateProfile(int patientId, Patient data);
    }

    public class PatientRecordStats
    {
        public int Total  { get; set; }
        public int Active { get; set; }
        public int Closed { get; set; }
    }
}
