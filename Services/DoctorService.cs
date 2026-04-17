using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.Models;

namespace MediCore.Services
{
    /// <summary>
    /// DoctorService — concrete implementation of IDoctorService.
    /// Contains ALL business logic for clinical operations.
    /// The DoctorController delegates every data decision to this class.
    /// </summary>
    public class DoctorService : IDoctorService
    {
        private readonly AppDbContext _db;

        /// <summary>AppDbContext injected by ASP.NET Core DI container.</summary>
        public DoctorService(AppDbContext db) => _db = db;

        // ── Dashboard ──────────────────────────────────────────────────────

        /// <summary>Aggregates all KPI counts needed for the doctor dashboard card row.</summary>
        public DoctorDashboardStats GetDashboardStats() => new DoctorDashboardStats
        {
            TotalPatients    = _db.Patients.Count(),
            TotalRecords     = _db.Records.Count(),
            // Business rule: "active" records are those still under treatment
            ActiveRecords    = _db.Records.Count(r => r.Status == "Active"),
            UnreadComplaints = _db.Complaints.Count(c => !c.IsRead)
        };

        /// <summary>
        /// Returns the N most recently created records with patient names.
        /// Used in the dashboard "Recently Added Records" feed.
        /// </summary>
        public List<Record> GetRecentRecords(int take = 5) =>
            _db.Records
               .Include(r => r.Patient)
               .OrderByDescending(r => r.CreatedAt)
               .Take(take)
               .ToList();

        /// <summary>
        /// Returns the N most recently updated complaints with patient names.
        /// Used in the dashboard complaints table.
        /// </summary>
        public List<Complaint> GetRecentComplaints(int take = 5) =>
            _db.Complaints
               .Include(c => c.Patient)
               .OrderByDescending(c => c.UpdatedAt)
               .Take(take)
               .ToList();

        /// <summary>
        /// Business rule: "priority patient" = has at least one Active complaint.
        /// Sorted by open-complaint count descending so the busiest cases appear first.
        /// This triage logic lives in the service, not the controller.
        /// </summary>
        public List<Patient> GetPriorityPatients(int take = 5) =>
            _db.Patients
               .Include(p => p.Complaints)
               .Where(p => p.Complaints.Any(c => c.Status == "Active"))
               .OrderByDescending(p => p.Complaints.Count(c => c.Status == "Active"))
               .Take(take)
               .ToList();

        // ── Patients ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns patients with Records and Complaints loaded.
        /// Business rule: search is case-insensitive across name, email, and phone.
        /// </summary>
        public List<Patient> GetPatients(string? search, string? gender, string? blood)
        {
            var q = _db.Patients
                       .Include(p => p.Records)
                       .Include(p => p.Complaints)
                       .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(p =>
                    p.FullName.ToLower().Contains(s) ||
                    (p.Email  != null && p.Email.ToLower().Contains(s)) ||
                    (p.Phone  != null && p.Phone.Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(gender))
                q = q.Where(p => p.Gender == gender);

            if (!string.IsNullOrWhiteSpace(blood))
                q = q.Where(p => p.BloodGroup == blood);

            return q.OrderBy(p => p.FullName).ToList();
        }

        /// <summary>Returns distinct non-null blood groups for the filter dropdown.</summary>
        public List<string> GetBloodGroups() =>
            _db.Patients
               .Where(p => p.BloodGroup != null)
               .Select(p => p.BloodGroup!)
               .Distinct()
               .OrderBy(b => b)
               .ToList();

        /// <summary>Loads a patient with full Records and Complaints for the detail page.</summary>
        public Patient? GetPatientDetail(int id) =>
            _db.Patients
               .Include(p => p.Records)
               .Include(p => p.Complaints)
               .FirstOrDefault(p => p.Id == id);

        // ── Records ────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new medical record.
        /// Business rules enforced here:
        ///   • DoctorName is stamped from the session (passed in from controller)
        ///   • CreatedAt is always set to DateTime.Now by the service
        ///   • Default status is "Active" if not specified
        /// </summary>
        public Record AddRecord(int patientId, string diagnosis, string treatment,
                                string? notes, DateTime visitDate, string status, string doctorName)
        {
            var record = new Record
            {
                PatientId  = patientId,
                Diagnosis  = diagnosis,
                Treatment  = treatment,
                Notes      = notes,
                VisitDate  = visitDate,
                Status     = string.IsNullOrWhiteSpace(status) ? "Active" : status,
                DoctorName = doctorName,           // stamped from session, not user input
                CreatedAt  = DateTime.Now          // always set by service, never trusted from form
            };

            _db.Records.Add(record);
            _db.SaveChanges();
            return record;
        }

        /// <summary>Returns a single record with its Patient navigation property loaded.</summary>
        public Record? GetRecord(int id) =>
            _db.Records.Include(r => r.Patient).FirstOrDefault(r => r.Id == id);

        /// <summary>
        /// Updates clinical fields on an existing record.
        /// Business rule: PatientId and DoctorName are never changed on edit —
        /// only the clinical content (diagnosis, treatment, notes, date, status).
        /// </summary>
        public bool UpdateRecord(int id, string diagnosis, string treatment,
                                 string? notes, DateTime visitDate, string status)
        {
            var record = _db.Records.Find(id);
            if (record == null) return false;

            record.Diagnosis = diagnosis;
            record.Treatment = treatment;
            record.Notes     = notes;
            record.VisitDate = visitDate;
            record.Status    = status;
            _db.SaveChanges();
            return true;
        }

        /// <summary>
        /// Business rule: closing a record sets Status = "Closed".
        /// This is a soft archive — data is preserved for audit and compliance.
        /// Returns false if the record does not exist.
        /// </summary>
        public bool CloseRecord(int id)
        {
            var record = _db.Records.Find(id);
            if (record == null) return false;

            record.Status = "Closed";
            _db.SaveChanges();
            return true;
        }

        // ── Complaints ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns complaints with patient names loaded.
        /// Business rule: "unread" is handled as a special filter
        /// (maps to IsRead = false), separate from the Status field.
        /// </summary>
        public List<Complaint> GetComplaints(string? filter)
        {
            var q = _db.Complaints.Include(c => c.Patient).AsQueryable();

            if (filter == "unread")   q = q.Where(c => !c.IsRead);
            if (filter == "active")   q = q.Where(c => c.Status == "Active");
            if (filter == "reviewed") q = q.Where(c => c.Status == "Reviewed");

            return q.OrderByDescending(c => c.UpdatedAt).ToList();
        }

        /// <summary>Returns total unread complaints count for the sidebar badge.</summary>
        public int GetUnreadCount() => _db.Complaints.Count(c => !c.IsRead);

        /// <summary>
        /// Loads a complaint with patient data and marks it as read.
        /// Business rule: simply opening a complaint clears the unread flag —
        /// no separate "mark as read" action is needed.
        /// </summary>
        public Complaint? GetAndMarkRead(int id)
        {
            var complaint = _db.Complaints
                               .Include(c => c.Patient)
                               .FirstOrDefault(c => c.Id == id);

            if (complaint == null) return null;

            // Business rule: viewed = read
            complaint.IsRead = true;
            _db.SaveChanges();
            return complaint;
        }

        /// <summary>
        /// Saves a doctor's response to a complaint.
        /// Business rules:
        ///   • RespondedBy is stamped from session (doctorName param), not the form
        ///   • RespondedAt is always DateTime.Now — the service controls timestamps
        ///   • UpdatedAt is refreshed so the complaint rises to top of the list
        /// </summary>
        public bool RespondToComplaint(int id, string response, string status, string doctorName)
        {
            var complaint = _db.Complaints.Find(id);
            if (complaint == null) return false;

            complaint.DoctorResponse = response;
            complaint.RespondedBy    = doctorName;     // stamped from session, not user input
            complaint.RespondedAt    = DateTime.Now;   // always set by service
            complaint.Status         = status;
            complaint.UpdatedAt      = DateTime.Now;
            _db.SaveChanges();
            return true;
        }
    }
}
