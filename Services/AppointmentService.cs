using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.Models;

namespace MediCore.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly AppDbContext _db;
        public AppointmentService(AppDbContext db) => _db = db;

        private static readonly List<string> AllSlots = new()
        {
            "09:00 AM","09:30 AM","10:00 AM","10:30 AM","11:00 AM","11:30 AM",
            "12:00 PM","12:30 PM","02:00 PM","02:30 PM","03:00 PM","03:30 PM",
            "04:00 PM","04:30 PM","05:00 PM"
        };

        public List<Patient> GetAllPatients() =>
            _db.Patients.OrderBy(p => p.FullName).ToList();

        public List<User> GetAllDoctors() =>
            _db.Users.Where(u => u.Role == "Doctor" && u.IsActive).OrderBy(u => u.FullName).ToList();

        public List<string> GetAvailableSlots(int doctorId, DateTime date)
        {
            var dow = (int)date.DayOfWeek;
            var avail = _db.DoctorAvailabilities
                .FirstOrDefault(a => a.DoctorId == doctorId && a.DayOfWeek == dow && a.IsAvailable);
            if (avail == null) return new List<string>();

            var booked = _db.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == date.Date && a.Status != "Cancelled")
                .Select(a => a.TimeSlot).ToHashSet();

            return AllSlots.Where(s => !booked.Contains(s)).ToList();
        }

        public Appointment? BookAppointment(int patientId, int doctorId, DateTime date, string timeSlot, string? notes)
        {
            bool conflict = _db.Appointments.Any(a =>
                a.DoctorId == doctorId && a.AppointmentDate.Date == date.Date &&
                a.TimeSlot == timeSlot && a.Status != "Cancelled");
            if (conflict) return null;

            var appt = new Appointment
            {
                PatientId = patientId, DoctorId = doctorId,
                AppointmentDate = date.Date, TimeSlot = timeSlot,
                Notes = notes, Status = "Scheduled",
                CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now
            };
            _db.Appointments.Add(appt);
            _db.SaveChanges();
            return appt;
        }

        public List<Appointment> GetAllAppointments(string? status, string? search, DateTime? date)
        {
            var q = _db.Appointments.Include(a => a.Patient).Include(a => a.Doctor).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(a => a.Status == status);
            if (date.HasValue) q = q.Where(a => a.AppointmentDate.Date == date.Value.Date);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(a => (a.Patient != null && a.Patient.FullName.ToLower().Contains(s)) ||
                                  (a.Doctor != null && a.Doctor.FullName.ToLower().Contains(s)));
            }
            return q.OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.TimeSlot).ToList();
        }

        public AppointmentStats GetStats() => new AppointmentStats
        {
            Total     = _db.Appointments.Count(),
            Today     = _db.Appointments.Count(a => a.AppointmentDate.Date == DateTime.Today),
            Scheduled = _db.Appointments.Count(a => a.Status == "Scheduled" || a.Status == "Confirmed"),
            Completed = _db.Appointments.Count(a => a.Status == "Completed"),
            Cancelled = _db.Appointments.Count(a => a.Status == "Cancelled")
        };

        public List<Appointment> GetDoctorAppointments(int doctorUserId, string? status)
        {
            var q = _db.Appointments.Include(a => a.Patient).Where(a => a.DoctorId == doctorUserId).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(a => a.Status == status);
            return q.OrderBy(a => a.AppointmentDate).ThenBy(a => a.TimeSlot).ToList();
        }

        public List<Appointment> GetPatientAppointments(int patientId) =>
            _db.Appointments.Include(a => a.Doctor)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.AppointmentDate).ToList();

        public Appointment? UpdateStatus(int id, string newStatus)
        {
            var a = _db.Appointments.Find(id);
            if (a == null) return null;
            a.Status = newStatus; a.UpdatedAt = DateTime.Now;
            _db.SaveChanges(); return a;
        }

        public Appointment? CancelAppointment(int id, string reason)
        {
            var a = _db.Appointments.Find(id);
            if (a == null) return null;
            a.Status = "Cancelled"; a.CancelReason = reason; a.UpdatedAt = DateTime.Now;
            _db.SaveChanges(); return a;
        }

        public Appointment? Reschedule(int id, DateTime newDate, string newSlot)
        {
            var a = _db.Appointments.Find(id);
            if (a == null) return null;
            bool conflict = _db.Appointments.Any(x =>
                x.Id != id && x.DoctorId == a.DoctorId && x.AppointmentDate.Date == newDate.Date &&
                x.TimeSlot == newSlot && x.Status != "Cancelled");
            if (conflict) return null;
            a.AppointmentDate = newDate.Date; a.TimeSlot = newSlot;
            a.Status = "Rescheduled"; a.UpdatedAt = DateTime.Now;
            _db.SaveChanges(); return a;
        }

        public List<DoctorAvailability> GetDoctorAvailability(int doctorUserId) =>
            _db.DoctorAvailabilities.Where(d => d.DoctorId == doctorUserId).OrderBy(d => d.DayOfWeek).ToList();

        public void SaveAvailability(int doctorUserId, List<DoctorAvailability> slots)
        {
            var existing = _db.DoctorAvailabilities.Where(d => d.DoctorId == doctorUserId);
            _db.DoctorAvailabilities.RemoveRange(existing);
            foreach (var s in slots) { s.DoctorId = doctorUserId; _db.DoctorAvailabilities.Add(s); }
            _db.SaveChanges();
        }
    }
}
