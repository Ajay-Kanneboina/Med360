using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.Models;

namespace MediCore.Services
{
    public class PatientService : IPatientService
    {
        private readonly AppDbContext _db;

        public PatientService(AppDbContext db) => _db = db;

        public Patient? GetPatientForDashboard(int patientId) =>
            _db.Patients
               .Include(p => p.Complaints)
               .Include(p => p.Records)
               .FirstOrDefault(p => p.Id == patientId);

        public PatientDashboardStats GetDashboardStats(int patientId) => new PatientDashboardStats
        {
            OpenCount     = _db.Complaints.Count(c => c.PatientId == patientId && c.Status == "Active"),
            ReviewedCount = _db.Complaints.Count(c => c.PatientId == patientId && c.Status == "Reviewed"),
            RecordCount   = _db.Records.Count(r => r.PatientId == patientId)
        };

        public List<Record> GetMyRecords(int patientId, string? status, string? search)
        {
            var q = _db.Records.Where(r => r.PatientId == patientId).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(r => r.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(r =>
                    r.Diagnosis.ToLower().Contains(s) ||
                    r.Treatment.ToLower().Contains(s) ||
                    (r.Notes != null && r.Notes.ToLower().Contains(s)));
            }

            return q.OrderByDescending(r => r.VisitDate).ToList();
        }

        public PatientRecordStats GetRecordStats(int patientId) => new PatientRecordStats
        {
            Total  = _db.Records.Count(r => r.PatientId == patientId),
            Active = _db.Records.Count(r => r.PatientId == patientId && r.Status == "Active"),
            Closed = _db.Records.Count(r => r.PatientId == patientId && r.Status == "Closed")
        };

        public Record? GetMyRecord(int recordId, int patientId)
        {
            var record = _db.Records.Find(recordId);
            return (record == null || record.PatientId != patientId) ? null : record;
        }

        public List<Complaint> GetMyComplaints(int patientId) =>
            _db.Complaints
               .Where(c => c.PatientId == patientId)
               .OrderByDescending(c => c.UpdatedAt)
               .ToList();

        public Complaint SubmitComplaint(int patientId, string description, string? additionalNotes)
        {
            var complaint = new Complaint
            {
                PatientId       = patientId,
                Description     = description,
                AdditionalNotes = additionalNotes,
                Status          = "Active",
                IsRead          = false,
                SubmittedAt     = DateTime.Now,
                UpdatedAt       = DateTime.Now
            };
            _db.Complaints.Add(complaint);
            _db.SaveChanges();
            return complaint;
        }

        public Complaint? GetMyComplaint(int complaintId, int patientId)
        {
            var complaint = _db.Complaints.Find(complaintId);
            return (complaint == null || complaint.PatientId != patientId) ? null : complaint;
        }

        public bool UpdateComplaint(int complaintId, int patientId,
                                    string description, string? additionalNotes)
        {
            var complaint = _db.Complaints.Find(complaintId);
            if (complaint == null || complaint.PatientId != patientId) return false;

            complaint.Description     = description;
            complaint.AdditionalNotes = additionalNotes;
            complaint.UpdatedAt       = DateTime.Now;
            complaint.IsRead          = false;
            _db.SaveChanges();
            return true;
        }

        public Patient? GetMyProfile(int patientId) =>
            _db.Patients.Find(patientId);

        public bool UpdateProfile(int patientId,
                                  string?   phone,
                                  string?   email,
                                  string?   address,
                                  string?   emergencyContact,
                                  DateTime? dateOfBirth,
                                  string?   gender,
                                  string?   bloodGroup,
                                  string?   medicalHistory)
        {
            var patient = _db.Patients.Find(patientId);
            if (patient == null) return false;

            patient.Phone            = phone;
            patient.Email            = email;
            patient.Address          = address;
            patient.EmergencyContact = emergencyContact;

            if (dateOfBirth.HasValue) patient.DateOfBirth = dateOfBirth.Value;
            patient.Gender         = string.IsNullOrWhiteSpace(gender)         ? null : gender;
            patient.BloodGroup     = string.IsNullOrWhiteSpace(bloodGroup)     ? null : bloodGroup;
            patient.MedicalHistory = string.IsNullOrWhiteSpace(medicalHistory) ? null : medicalHistory;

            _db.SaveChanges();
            return true;
        }
    }
}
