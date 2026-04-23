using MediCore.Models;

namespace MediCore.Services
{
    public interface IPatientService
    {
        List<Record> GetMyRecords(int patientId, string? status, string? search);
        PatientRecordStats GetRecordStats(int patientId);
        Record? GetMyRecord(int recordId, int patientId);

        List<Complaint> GetMyComplaints(int patientId);
        Complaint SubmitComplaint(int patientId, string description, string? additionalNotes);
        bool UpdateComplaint(int complaintId, int patientId,
                             string description, string? additionalNotes);

        Patient? GetMyProfile(int patientId);
        bool UpdateProfile(int patientId,
                           string?   phone,
                           string?   email,
                           string?   address,
                           string?   emergencyContact,
                           DateTime? dateOfBirth,
                           string?   gender,
                           string?   bloodGroup,
                           string?   medicalHistory);
    }

    public class PatientRecordStats
    {
        public int Total  { get; set; }
        public int Active { get; set; }
        public int Closed { get; set; }
    }
}
