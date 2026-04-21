using Microsoft.EntityFrameworkCore;

using MediCore.Data;
using MediCore.Models;

namespace MediCore.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _db;

        public AdminService(AppDbContext db) => _db = db;

        public AdminDashboardStats GetDashboardStats() => new AdminDashboardStats
        {
            TotalPatients      = _db.Patients.Count(),
            TotalUsers         = _db.Users.Count(),
            TotalRecords       = _db.Records.Count(),
            TotalComplaints    = _db.Complaints.Count(),
            ActiveRecords      = _db.Records.Count(r => r.Status == "Active"),
            ResolvedComplaints = _db.Complaints.Count(c => c.Status == "Reviewed"),
            PendingApprovals   = _db.Users.Count(u => !u.IsActive &&
                                    (u.Role == "Doctor" || u.Role == "Nurse" || u.Role == "Receptionist")),
            NewComplaints      = _db.Complaints.Count(c => !c.IsRead),
            DoctorCount        = _db.Users.Count(u => u.Role == "Doctor"       && u.IsActive),
            NurseCount         = _db.Users.Count(u => u.Role == "Nurse"        && u.IsActive),
            ReceptionistCount  = _db.Users.Count(u => u.Role == "Receptionist" && u.IsActive)
        };

        public List<Patient> GetRecentPatients(int take = 5) =>
            _db.Patients
               .OrderByDescending(p => p.RegisteredOn)
               .Take(take)
               .ToList();

        public List<Complaint> GetRecentComplaints(int take = 5) =>
            _db.Complaints
               .Include(c => c.Patient)
               .OrderByDescending(c => c.UpdatedAt)
               .Take(take)
               .ToList();

        public List<User> GetPendingUsers() =>
            _db.Users
               .Where(u => !u.IsActive &&
                           (u.Role == "Doctor" || u.Role == "Nurse" || u.Role == "Receptionist"))
               .OrderBy(u => u.CreatedAt)
               .ToList();

        public List<User> GetAllStaff(string? role, string? search)
        {
            var q = _db.Users.Where(u => u.Role != "Patient").AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                q = q.Where(u => u.Role == role);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(u => u.FullName.ToLower().Contains(s) || u.Email.ToLower().Contains(s));
            }

            return q.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToList();
        }

        public User? ApproveUser(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return null;

            user.IsActive = true;
            _db.SaveChanges();
            return user;
        }

        public string? RejectUser(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return null;

            _db.Users.Remove(user);
            _db.SaveChanges();
            return user.FullName;
        }

        public User? ToggleUserActive(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return null;

            user.IsActive = !user.IsActive;
            _db.SaveChanges();
            return user;
        }

        public string? DeleteUser(int id)
        {
            var user = _db.Users.Find(id);
            if (user == null) return null;

            _db.Users.Remove(user);
            _db.SaveChanges();
            return user.FullName;
        }

        public List<Patient> GetAllPatients(string? search, string? gender)
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
                    (p.Phone  != null && p.Phone.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(gender))
                q = q.Where(p => p.Gender == gender);

            return q.OrderByDescending(p => p.RegisteredOn).ToList();
        }

        public Patient? GetPatientDetail(int id) =>
            _db.Patients
               .Include(p => p.Records)
               .Include(p => p.Complaints)
               .FirstOrDefault(p => p.Id == id);

        public bool DeletePatient(int id)
        {
            var patient = _db.Patients.Find(id);
            if (patient == null) return false;

            _db.Patients.Remove(patient);
            _db.SaveChanges();
            return true;
        }

        public List<Record> GetAllRecords(string? status, string? search)
        {
            var q = _db.Records.Include(r => r.Patient).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(r => r.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(r =>
                    r.Diagnosis.ToLower().Contains(s) ||
                    r.Treatment.ToLower().Contains(s) ||
                    (r.Patient != null && r.Patient.FullName.ToLower().Contains(s)));
            }

            return q.OrderByDescending(r => r.VisitDate).ToList();
        }

        public List<Complaint> GetAllComplaints(string? status, string? search)
        {
            var q = _db.Complaints.Include(c => c.Patient).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                q = status == "unread"
                    ? q.Where(c => !c.IsRead)
                    : q.Where(c => c.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(c =>
                    c.Description.ToLower().Contains(s) ||
                    (c.Patient != null && c.Patient.FullName.ToLower().Contains(s)));
            }

            return q.OrderByDescending(c => c.UpdatedAt).ToList();
        }

        public int GetUnreadComplaintCount() =>
            _db.Complaints.Count(c => !c.IsRead);

        public List<AuditEntry> GetAuditEntries(int take = 80)
        {
            return _db.AuditLogs
                      .OrderByDescending(a => a.Timestamp)
                      .Take(take)
                      .Select(a => new AuditEntry
                      {
                          Actor     = a.Actor,
                          Action    = a.Action,
                          Target    = a.Target,
                          Category  = a.Category,
                          Timestamp = a.Timestamp
                      })
                      .ToList();
        }

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
