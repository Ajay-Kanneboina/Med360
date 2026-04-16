using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.Models;

namespace MediCore.Controllers
{
    public class DoctorController : Controller
    {
        private readonly AppDbContext _db;
        public DoctorController(AppDbContext db) => _db = db;

        private bool IsStaff()
        {
            var r = HttpContext.Session.GetString("UserRole");
            return r == "Doctor" || r == "Admin" || r == "Receptionist";
        }

        public IActionResult Dashboard()
        {
            var r = HttpContext.Session.GetString("UserRole");
            if (r == null) return RedirectToAction("Login", "Account");
            if (r != "Doctor" && r != "Admin") return RedirectToAction("Login", "Account");

            ViewBag.NewComplaints = _db.Complaints.Count(c => !c.IsRead);
            ViewBag.ActivePatients = _db.Patients.Count();
            ViewBag.RecentComplaints = _db.Complaints
                .Include(c => c.Patient)
                .OrderByDescending(c => c.UpdatedAt)
                .Take(6)
                .ToList();

            return View();
        }

        public IActionResult Complaints(string? filter)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var q = _db.Complaints.Include(c => c.Patient).AsQueryable();
            if (filter == "unread")   q = q.Where(c => !c.IsRead);
            if (filter == "active")   q = q.Where(c => c.Status == "Active");
            if (filter == "reviewed") q = q.Where(c => c.Status == "Reviewed");

            ViewBag.Filter      = filter ?? "all";
            ViewBag.UnreadCount = _db.Complaints.Count(c => !c.IsRead);

            return View(q.OrderByDescending(c => c.UpdatedAt).ToList());
        }

        public IActionResult ViewComplaint(int id)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var c = _db.Complaints
                .Include(x => x.Patient)
                .FirstOrDefault(x => x.Id == id);

            if (c == null) return NotFound();

            c.IsRead = true;
            _db.SaveChanges();

            return View(c);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Respond(int id, string response, string status)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var c = _db.Complaints.Find(id);
            if (c == null) return NotFound();

            c.DoctorResponse = response;
            c.RespondedBy    = HttpContext.Session.GetString("UserName");
            c.RespondedAt    = DateTime.Now;
            c.Status         = status;
            c.UpdatedAt      = DateTime.Now;
            _db.SaveChanges();

            TempData["Success"] = "Response sent to patient.";
            return RedirectToAction(nameof(Complaints));
        }

        public IActionResult Patients(string? search)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var q = _db.Patients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(p =>
                    p.FullName.ToLower().Contains(s) ||
                    (p.Email != null && p.Email.ToLower().Contains(s)));
            }
            ViewBag.Search = search;
            return View(q.OrderBy(p => p.FullName).ToList());
        }

        public IActionResult PatientDetail(int id)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            var p = _db.Patients
                .Include(x => x.Records)
                .Include(x => x.Complaints)
                .FirstOrDefault(x => x.Id == id);

            if (p == null) return NotFound();
            return View(p);
        }

        public IActionResult AddRecord(int patientId)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");
            var p = _db.Patients.Find(patientId);
            if (p == null) return NotFound();
            ViewBag.PatientName = p.FullName;
            ViewBag.PatientId   = patientId;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AddRecord(int patientId, string diagnosis,
                                       string treatment, string? notes, DateTime visitDate)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(diagnosis) || string.IsNullOrWhiteSpace(treatment))
            {
                var p2 = _db.Patients.Find(patientId);
                ViewBag.PatientName = p2?.FullName;
                ViewBag.PatientId   = patientId;
                ViewBag.Error = "Diagnosis and treatment are required.";
                return View();
            }

            _db.Records.Add(new Record
            {
                PatientId  = patientId,
                Diagnosis  = diagnosis,
                Treatment  = treatment,
                Notes      = notes,
                VisitDate  = visitDate,
                DoctorName = HttpContext.Session.GetString("UserName"),
                Status     = "Active",
                CreatedAt  = DateTime.Now
            });
            _db.SaveChanges();

            TempData["Success"] = "Record added.";
            return RedirectToAction(nameof(PatientDetail), new { id = patientId });
        }
    }
}
