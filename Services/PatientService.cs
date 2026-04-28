using MediCore.Data;
using MediCore.Models;

namespace MediCore.Services
{
    public class PatientService : IPatientService
    {
        private readonly AppDbContext _db;

        public PatientService(AppDbContext db) => _db = db;

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
            Total = _db.Records.Count(r => r.PatientId == patientId),
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

        public Complaint SubmitComplaint(int patientId, Complaint data)
        {
            var complaint = new Complaint
            {
                PatientId = patientId,
                Description = data.Description,
                AdditionalNotes = data.AdditionalNotes,
                Status = "Active",
                IsRead = false,
                SubmittedAt = DateTime.Now,
                UpdatedAt  = DateTime.Now
            };
            _db.Complaints.Add(complaint);
            _db.SaveChanges();
            return complaint;
        }

        public bool UpdateComplaint(int complaintId, int patientId, Complaint data)
        {
            var complaint = _db.Complaints.Find(complaintId);
            if (complaint == null || complaint.PatientId != patientId) return false;

            complaint.Description = data.Description;
            complaint.AdditionalNotes = data.AdditionalNotes;
            complaint.UpdatedAt = DateTime.Now;
            complaint.IsRead = false;
            _db.SaveChanges();
            return true;
        }

        public Patient? GetMyProfile(int patientId) =>
            _db.Patients.Find(patientId);

        public bool UpdateProfile(int patientId, Patient data)
        {
            var patient = _db.Patients.Find(patientId);
            if (patient == null) return false;

            patient.Phone  = data.Phone;
            patient.Email  = data.Email;
            patient.Address = data.Address;
            patient.EmergencyContact = data.EmergencyContact;

            if (data.DateOfBirth != default) patient.DateOfBirth = data.DateOfBirth;
            patient.Gender  = string.IsNullOrWhiteSpace(data.Gender) ? null : data.Gender;
            patient.BloodGroup = string.IsNullOrWhiteSpace(data.BloodGroup) ? null : data.BloodGroup;
            patient.MedicalHistory = string.IsNullOrWhiteSpace(data.MedicalHistory) ? null : data.MedicalHistory;

            _db.SaveChanges();
            return true;
        }
    }
}
