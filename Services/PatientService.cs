using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.Models;

namespace MediCore.Services
{
    /// <summary>
    /// PatientService — concrete implementation of IPatientService.
    /// All business logic for the patient portal lives here.
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly AppDbContext _db;

        /// <summary>AppDbContext injected by ASP.NET Core DI container.</summary>
        public PatientService(AppDbContext db) => _db = db;

        // ── Dashboard ──────────────────────────────────────────────────────

        /// <summary>Loads patient with Complaints and Records for the dashboard.</summary>
        public Patient? GetPatientForDashboard(int patientId) =>
            _db.Patients
               .Include(p => p.Complaints)
               .Include(p => p.Records)
               .FirstOrDefault(p => p.Id == patientId);

        /// <summary>
        /// Computes dashboard KPI tile values.
        /// Business rules: Open = Active, Reviewed = Reviewed status.
        /// </summary>
        public PatientDashboardStats GetDashboardStats(int patientId) => new PatientDashboardStats
        {
            OpenCount     = _db.Complaints.Count(c => c.PatientId == patientId && c.Status == "Active"),
            ReviewedCount = _db.Complaints.Count(c => c.PatientId == patientId && c.Status == "Reviewed"),
            RecordCount   = _db.Records.Count(r => r.PatientId == patientId)
        };

        // ── Records ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns this patient's records with optional filters.
        /// Business rule: keyword search spans Diagnosis, Treatment, and Notes.
        /// </summary>
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

        /// <summary>Computes Total / Active / Closed record counts for the summary pills.</summary>
        public PatientRecordStats GetRecordStats(int patientId) => new PatientRecordStats
        {
            Total  = _db.Records.Count(r => r.PatientId == patientId),
            Active = _db.Records.Count(r => r.PatientId == patientId && r.Status == "Active"),
            Closed = _db.Records.Count(r => r.PatientId == patientId && r.Status == "Closed")
        };

        /// <summary>
        /// Returns a record only if it belongs to this patient.
        /// Business rule (security): patients must never view other patients' records.
        /// </summary>
        public Record? GetMyRecord(int recordId, int patientId)
        {
            var record = _db.Records.Find(recordId);
            return (record == null || record.PatientId != patientId) ? null : record;
        }

        // ── Complaints ─────────────────────────────────────────────────────

        /// <summary>Returns all complaints for this patient, newest first.</summary>
        public List<Complaint> GetMyComplaints(int patientId) =>
            _db.Complaints
               .Where(c => c.PatientId == patientId)
               .OrderByDescending(c => c.UpdatedAt)
               .ToList();

        /// <summary>
        /// Creates a new complaint.
        /// Business rules: IsRead = false, Status = Active, timestamps by service.
        /// </summary>
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

        /// <summary>
        /// Returns a complaint only if it belongs to this patient (ownership guard).
        /// </summary>
        public Complaint? GetMyComplaint(int complaintId, int patientId)
        {
            var complaint = _db.Complaints.Find(complaintId);
            return (complaint == null || complaint.PatientId != patientId) ? null : complaint;
        }

        /// <summary>
        /// Updates complaint text and resets IsRead = false to re-notify the doctor.
        /// </summary>
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

        // ── Profile ────────────────────────────────────────────────────────

        /// <summary>Returns the patient's own profile data (read-only clinical fields).</summary>
        public Patient? GetMyProfile(int patientId) =>
            _db.Patients.Find(patientId);

        /// <summary>
        /// Updates the patient's own profile — both contact and clinical fields.
        /// FullName is intentionally excluded (identity field managed by reception).
        ///
        /// Caveat: patient-edited clinical fields (DOB, gender, blood group,
        /// medical history) should be treated as self-reported. Clinicians
        /// should confirm them at the next visit.
        ///
        /// Null / empty parameters for optional fields clear the field. DOB
        /// is optional (nullable) — null leaves the existing value unchanged
        /// so the form can omit it if desired.
        /// Returns false if patient not found.
        /// </summary>
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

            // Contact fields
            patient.Phone            = phone;
            patient.Email            = email;
            patient.Address          = address;
            patient.EmergencyContact = emergencyContact;

            // Clinical fields — now patient-editable
            if (dateOfBirth.HasValue) patient.DateOfBirth = dateOfBirth.Value;
            patient.Gender         = string.IsNullOrWhiteSpace(gender)         ? null : gender;
            patient.BloodGroup     = string.IsNullOrWhiteSpace(bloodGroup)     ? null : bloodGroup;
            patient.MedicalHistory = string.IsNullOrWhiteSpace(medicalHistory) ? null : medicalHistory;

            _db.SaveChanges();
            return true;
        }
    }
}
