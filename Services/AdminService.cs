using Microsoft.EntityFrameworkCore;

using MediCore.Data;
using MediCore.Models;

namespace MediCore.Services
{
    /// <summary>
    /// AdminService — concrete implementation of IAdminService.
    /// ALL business logic for admin operations lives here.
    /// The AdminController becomes a thin HTTP adapter that simply
    /// calls this service and passes results to views.
    /// </summary>
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _db;

        /// <summary>AppDbContext is injected by ASP.NET Core DI container.</summary>
        public AdminService(AppDbContext db) => _db = db;

        // ── Dashboard ──────────────────────────────────────────────────────

        /// <summary>
        /// Aggregates all KPI counts in a single service call.
        /// Business rule: PendingApprovals only covers Doctor, Nurse, Receptionist —
        /// not Admin or Patient accounts.
        /// </summary>
        public AdminDashboardStats GetDashboardStats() => new AdminDashboardStats
        {
            TotalPatients      = _db.Patients.Count(),
            TotalUsers         = _db.Users.Count(),
            TotalRecords       = _db.Records.Count(),
            TotalComplaints    = _db.Complaints.Count(),
            ActiveRecords      = _db.Records.Count(r => r.Status == "Active"),
            ResolvedComplaints = _db.Complaints.Count(c => c.Status == "Reviewed"),
            // Business rule: only staff roles need approval, not patients/admins
            PendingApprovals   = _db.Users.Count(u => !u.IsActive &&
                                    (u.Role == "Doctor" || u.Role == "Nurse" || u.Role == "Receptionist")),
            NewComplaints      = _db.Complaints.Count(c => !c.IsRead),
            DoctorCount        = _db.Users.Count(u => u.Role == "Doctor"       && u.IsActive),
            NurseCount         = _db.Users.Count(u => u.Role == "Nurse"        && u.IsActive),
            ReceptionistCount  = _db.Users.Count(u => u.Role == "Receptionist" && u.IsActive)
        };

        /// <summary>Returns the N most recently registered patients for dashboard feed.</summary>
        public List<Patient> GetRecentPatients(int take = 5) =>
            _db.Patients
               .OrderByDescending(p => p.RegisteredOn)
               .Take(take)
               .ToList();

        /// <summary>Returns the N most recently updated complaints for dashboard feed.</summary>
        public List<Complaint> GetRecentComplaints(int take = 5) =>
            _db.Complaints
               .Include(c => c.Patient)
               .OrderByDescending(c => c.UpdatedAt)
               .Take(take)
               .ToList();

        // ── User Management ────────────────────────────────────────────────

        /// <summary>
        /// Business rule: "pending" means IsActive = false AND role is a
        /// clinical/front-desk role. Admins and Patients are never in this queue.
        /// </summary>
        public List<User> GetPendingUsers() =>
            _db.Users
               .Where(u => !u.IsActive &&
                           (u.Role == "Doctor" || u.Role == "Nurse" || u.Role == "Receptionist"))
               .OrderBy(u => u.CreatedAt)
               .ToList();

        /// <summary>
        /// Returns all staff (non-Patient) accounts with optional filters.
        /// Search is case-insensitive across FullName and Email.
        /// </summary>
        public List<User> GetAllStaff(string? role, string? search)
        {
            var q = _db.Users.Where(u => u.Role != "Patient").AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                q = q.Where(u => u.Role == role);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            return q.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToList();
        }

        /// <summary>
        /// Business rule: approving a user simply flips IsActive = true.
        /// Returns the user so the controller can compose the success message.
        /// Returns null if user not found.
        /// </summary>
        public User? ApproveUser(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return null;

            user.IsActive = true;
            _db.SaveChanges();
            return user;
        }

        /// <summary>
        /// Business rule: rejection removes the record entirely — no soft-delete.
        /// Returns the user's name for the success message, or null if not found.
        /// </summary>
        public string? RejectUser(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return null;

            _db.Users.Remove(user);
            _db.SaveChanges();
            return user.FullName;
        }

        /// <summary>
        /// Toggles IsActive. Business rule: deactivating a user prevents login
        /// without deleting their data or audit trail.
        /// </summary>
        public User? ToggleUserActive(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return null;

            user.IsActive = !user.IsActive;
            _db.SaveChanges();
            return user;
        }

        /// <summary>Permanently deletes a staff user. Returns name for success message.</summary>
        public string? DeleteUser(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return null;

            _db.Users.Remove(user);
            _db.SaveChanges();
            return user.FullName;
        }

        // ── Patient Management ─────────────────────────────────────────────

        /// <summary>
        /// Returns patients with Records and Complaints loaded.
        /// Search spans FullName, Email, and Phone.
        /// Business rule: results ordered newest-registered first.
        /// </summary>
        public List<Patient> GetAllPatients(string? search, string? gender)
        {
            var q = _db.Patients
                       .Include(p => p.Records)
                       .Include(p => p.Complaints)
                       .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(p =>
                    p.FullName.Contains(search) ||
                    (p.Email  != null && p.Email.Contains(search)) ||
                    (p.Phone  != null && p.Phone.Contains(search)));

            if (!string.IsNullOrWhiteSpace(gender))
                q = q.Where(p => p.Gender == gender);

            return q.OrderByDescending(p => p.RegisteredOn).ToList();
        }

        /// <summary>Loads a patient with full Records and Complaints for the detail page.</summary>
        public Patient? GetPatientDetail(int id) =>
            _db.Patients
               .Include(p => p.Records)
               .Include(p => p.Complaints)
               .FirstOrDefault(p => p.Id == id);

        /// <summary>
        /// Deletes a patient. Cascading delete in AppDbContext removes their
        /// Records and Complaints automatically via EF Core cascade rules.
        /// </summary>
        public bool DeletePatient(int id)
        {
            var patient = _db.Patients.Find(id);
            if (patient == null) return false;

            _db.Patients.Remove(patient);
            _db.SaveChanges();
            return true;
        }

        // ── Records ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns records with patient name loaded.
        /// Business rule: keyword search spans Diagnosis, Treatment, and patient name.
        /// </summary>
        public List<Record> GetAllRecords(string? status, string? search)
        {
            var q = _db.Records.Include(r => r.Patient).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(r => r.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(r =>
                    r.Diagnosis.Contains(search) ||
                    r.Treatment.Contains(search) ||
                    (r.Patient != null && r.Patient.FullName.Contains(search)));

            return q.OrderByDescending(r => r.VisitDate).ToList();
        }

        // ── Complaints ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns complaints with patient name loaded.
        /// Business rule: "unread" is a special filter value — maps to IsRead = false
        /// rather than a Status value.
        /// </summary>
        public List<Complaint> GetAllComplaints(string? status, string? search)
        {
            var q = _db.Complaints.Include(c => c.Patient).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                q = status == "unread"
                    ? q.Where(c => !c.IsRead)
                    : q.Where(c => c.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(c =>
                    c.Description.Contains(search) ||
                    (c.Patient != null && c.Patient.FullName.Contains(search)));

            return q.OrderByDescending(c => c.UpdatedAt).ToList();
        }

        /// <summary>Returns the count of complaints the doctor hasn't opened yet.</summary>
        public int GetUnreadComplaintCount() =>
            _db.Complaints.Count(c => !c.IsRead);

        // ── Audit Log ──────────────────────────────────────────────────────

        /// <summary>
        /// Business rule: an auditable event is one of —
        ///   • User account created or still pending
        ///   • Medical record added by a doctor
        ///   • Complaint submitted by a patient
        ///   • Complaint responded to by staff
        /// Events from all three tables are merged, sorted by timestamp,
        /// and capped at `take` entries for page performance.
        /// </summary>
        public List<AuditEntry> GetAuditEntries(int take = 80)
        {
            var entries = new List<AuditEntry>();

            // User account events
            foreach (var u in _db.Users.OrderByDescending(x => x.CreatedAt).Take(30))
                entries.Add(new AuditEntry
                {
                    Actor     = u.FullName,
                    Action    = u.IsActive ? "Account Created / Approved" : "Registration Pending",
                    Target    = $"{u.Role} — {u.Email}",
                    Timestamp = u.CreatedAt,
                    Category  = "User"
                });

            // Medical record creation events
            foreach (var r in _db.Records.Include(x => x.Patient)
                                         .OrderByDescending(x => x.CreatedAt).Take(30))
                entries.Add(new AuditEntry
                {
                    Actor     = r.DoctorName ?? "Doctor",
                    Action    = "Medical Record Added",
                    Target    = r.Patient?.FullName ?? $"Patient #{r.PatientId}",
                    Timestamp = r.CreatedAt,
                    Category  = "Record"
                });

            // Complaint lifecycle events
            foreach (var c in _db.Complaints.Include(x => x.Patient)
                                             .OrderByDescending(x => x.SubmittedAt).Take(30))
            {
                entries.Add(new AuditEntry
                {
                    Actor     = c.Patient?.FullName ?? $"Patient #{c.PatientId}",
                    Action    = "Complaint Submitted",
                    Target    = c.Description.Length > 60 ? c.Description[..60] + "…" : c.Description,
                    Timestamp = c.SubmittedAt,
                    Category  = "Complaint"
                });

                // Only add a response event if the complaint was actually responded to
                if (c.RespondedAt.HasValue)
                    entries.Add(new AuditEntry
                    {
                        Actor     = c.RespondedBy ?? "Staff",
                        Action    = "Complaint Responded",
                        Target    = c.Patient?.FullName ?? $"Patient #{c.PatientId}",
                        Timestamp = c.RespondedAt.Value,
                        Category  = "Complaint"
                    });
            }

            // Merge and sort all events by newest first
            return entries.OrderByDescending(e => e.Timestamp).Take(take).ToList();
        }

        // ── Analytics ─────────────────────────────────────────────────────

        /// <summary>
        /// Computes all analytics breakdowns.
        /// Business rules:
        ///   • "Other gender" = anyone not explicitly Male or Female
        ///   • "Total staff" = active users who are not Patients
        ///   • Blood group stats exclude patients with no blood group recorded
        /// </summary>
        public AdminAnalyticsStats GetAnalyticsStats() => new AdminAnalyticsStats
        {
            TotalPatients      = _db.Patients.Count(),
            TotalRecords       = _db.Records.Count(),
            TotalComplaints    = _db.Complaints.Count(),
            ResolvedComplaints = _db.Complaints.Count(c => c.Status == "Reviewed"),
            ActiveRecords      = _db.Records.Count(r => r.Status == "Active"),
            TotalStaff         = _db.Users.Count(u => u.Role != "Patient" && u.IsActive),
            MaleCount          = _db.Patients.Count(p => p.Gender == "Male"),
            FemaleCount        = _db.Patients.Count(p => p.Gender == "Female"),
            // Business rule: "Other" means not explicitly Male or Female (includes null)
            OtherGender        = _db.Patients.Count(p => p.Gender != "Male" && p.Gender != "Female"),
            ActiveComplaints   = _db.Complaints.Count(c => c.Status == "Active"),
            ReviewedComplaints = _db.Complaints.Count(c => c.Status == "Reviewed"),
            ClosedComplaints   = _db.Complaints.Count(c => c.Status == "Closed"),
            DoctorCount        = _db.Users.Count(u => u.Role == "Doctor"       && u.IsActive),
            NurseCount         = _db.Users.Count(u => u.Role == "Nurse"        && u.IsActive),
            ReceptionistCount  = _db.Users.Count(u => u.Role == "Receptionist" && u.IsActive),
            BloodGroups        = _db.Patients
                                    .Where(p => p.BloodGroup != null)
                                    .GroupBy(p => p.BloodGroup!)
                                    .Select(g => new BloodGroupStat { Group = g.Key, Count = g.Count() })
                                    .OrderByDescending(x => x.Count)
                                    .ToList()
        };
    }
}
