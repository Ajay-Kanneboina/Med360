using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.Models;

namespace MediCore.Services
{
    public class DoctorService : IDoctorService
    {
        private readonly AppDbContext _db;

        public DoctorService(AppDbContext db) => _db = db;

        public DoctorDashboardStats GetDashboardStats() => new DoctorDashboardStats
        {
            TotalPatients = _db.Patients.Count(),
            TotalRecords  = _db.Records.Count(),
            ActiveRecords  = _db.Records.Count(r => r.Status == "Active"),
            UnreadComplaints = _db.Complaints.Count(c => !c.IsRead)
        };

        public List<Record> GetRecentRecords(int take = 5) =>
            _db.Records
               .Include(r => r.Patient)
               .OrderByDescending(r => r.CreatedAt)
               .Take(take)
               .ToList();

        public List<Complaint> GetRecentComplaints(int take = 5) =>
            _db.Complaints
               .Include(c => c.Patient)
               .OrderByDescending(c => c.UpdatedAt)
               .Take(take)
               .ToList();

        public List<Patient> GetPriorityPatients(int take = 5) =>
            _db.Patients
               .Include(p => p.Complaints)
               .Where(p => p.Complaints.Any(c => c.Status == "Active"))
               .OrderByDescending(p => p.Complaints.Count(c => c.Status == "Active"))
               .Take(take)
               .ToList();

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

        public List<string> GetBloodGroups() =>
            _db.Patients
               .Where(p => p.BloodGroup != null)
               .Select(p => p.BloodGroup!)
               .Distinct()
               .OrderBy(b => b)
               .ToList();

        public Patient? GetPatientDetail(int id) =>
            _db.Patients
               .Include(p => p.Records)
               .Include(p => p.Complaints)
               .FirstOrDefault(p => p.Id == id);

        public Record AddRecord(Record data, string doctorName)
        {
            var record = new Record
            {
                PatientId  = data.PatientId,
                Diagnosis  = data.Diagnosis,
                Treatment  = data.Treatment,
                Notes  = data.Notes,
                VisitDate  = data.VisitDate,
                Status = string.IsNullOrWhiteSpace(data.Status) ? "Active" : data.Status,
                DoctorName = doctorName,
                CreatedAt = DateTime.Now
            };

            _db.Records.Add(record);
            _db.SaveChanges();
            return record;
        }

        public Record? GetRecord(int id) =>
            _db.Records.Include(r => r.Patient).FirstOrDefault(r => r.Id == id);

        public bool UpdateRecord(int id, Record data)
        {
            var record = _db.Records.Find(id);
            if (record == null) return false;

            record.Diagnosis = data.Diagnosis;
            record.Treatment = data.Treatment;
            record.Notes = data.Notes;
            record.VisitDate = data.VisitDate;
            record.Status = data.Status;
            _db.SaveChanges();
            return true;
        }

        public bool CloseRecord(int id)
        {
            var record = _db.Records.Find(id);
            if (record == null) return false;

            record.Status = "Closed";
            _db.SaveChanges();
            return true;
        }

        public List<Complaint> GetComplaints(string? filter)
        {
            var q = _db.Complaints.Include(c => c.Patient).AsQueryable();

            if (filter == "unread")   q = q.Where(c => !c.IsRead);
            if (filter == "active")   q = q.Where(c => c.Status == "Active");
            if (filter == "reviewed") q = q.Where(c => c.Status == "Reviewed");

            return q.OrderByDescending(c => c.UpdatedAt).ToList();
        }

        public int GetUnreadCount() => _db.Complaints.Count(c => !c.IsRead);

        public Complaint? GetAndMarkRead(int id)
        {
            var complaint = _db.Complaints
                               .Include(c => c.Patient)
                               .FirstOrDefault(c => c.Id == id);

            if (complaint == null) return null;

            complaint.IsRead = true;
            _db.SaveChanges();
            return complaint;
        }

        public bool RespondToComplaint(int id, Complaint data, string doctorName)
        {
            var complaint = _db.Complaints.Find(id);
            if (complaint == null) return false;

            complaint.DoctorResponse = data.DoctorResponse;
            complaint.RespondedBy = doctorName;
            complaint.RespondedAt = DateTime.Now;
            complaint.Status = data.Status;
            complaint.UpdatedAt = DateTime.Now;
            _db.SaveChanges();
            return true;
        }
    }
}
