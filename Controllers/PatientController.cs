using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediCore.Data;
using MediCore.Models;

namespace MediCore.Controllers
{
    public class PatientController : Controller
    {
        private readonly AppDbContext _db;
        public PatientController(AppDbContext db) => _db = db;

        private bool IsPatient() => HttpContext.Session.GetString("UserRole") == "Patient";
        private int  Pid()       => HttpContext.Session.GetInt32("PatientId") ?? 0;

        public IActionResult Dashboard()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            int pid = Pid();
            if (pid == 0) return View("NoRecord");

            var patient = _db.Patients
                .Include(p => p.Complaints)
                .Include(p => p.Records)
                .FirstOrDefault(p => p.Id == pid);

            if (patient == null) return View("NoRecord");

            ViewBag.OpenCount     = patient.Complaints.Count(c => c.Status == "Active");
            ViewBag.ReviewedCount = patient.Complaints.Count(c => c.Status == "Reviewed");
            ViewBag.RecordCount   = patient.Records.Count;
            ViewBag.LastResponse  = patient.Complaints
                .Where(c => c.DoctorResponse != null)
                .OrderByDescending(c => c.RespondedAt)
                .FirstOrDefault();

            return View(patient);
        }

        public IActionResult MyComplaints()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            var list = _db.Complaints
                .Where(c => c.PatientId == Pid())
                .OrderByDescending(c => c.UpdatedAt)
                .ToList();
            return View(list);
        }

        public IActionResult SubmitComplaint()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult SubmitComplaint(string description, string? additionalNotes)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(description))
            {
                ViewBag.Error = "Please describe your problem.";
                return View();
            }

            _db.Complaints.Add(new Complaint
            {
                PatientId       = Pid(),
                Description     = description,
                AdditionalNotes = additionalNotes,
                Status          = "Active",
                IsRead          = false,
                SubmittedAt     = DateTime.Now,
                UpdatedAt       = DateTime.Now
            });
            _db.SaveChanges();

            TempData["Success"] = "Problem submitted. A doctor will respond soon.";
            return RedirectToAction(nameof(MyComplaints));
        }

        public IActionResult EditComplaint(int id)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            var c = _db.Complaints.Find(id);
            if (c == null || c.PatientId != Pid()) return NotFound();
            return View(c);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditComplaint(int id, string description, string? additionalNotes)
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            var c = _db.Complaints.Find(id);
            if (c == null || c.PatientId != Pid()) return NotFound();

            if (string.IsNullOrWhiteSpace(description))
            {
                ViewBag.Error = "Please describe your problem.";
                return View(c);
            }

            c.Description     = description;
            c.AdditionalNotes = additionalNotes;
            c.UpdatedAt       = DateTime.Now;
            c.IsRead          = false;
            _db.SaveChanges();

            TempData["Success"] = "Problem updated. The doctor will be notified.";
            return RedirectToAction(nameof(MyComplaints));
        }

        public IActionResult MyRecords()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            var records = _db.Records
                .Where(r => r.PatientId == Pid())
                .OrderByDescending(r => r.VisitDate)
                .ToList();
            return View(records);
        }

        public IActionResult MyProfile()
        {
            if (!IsPatient()) return RedirectToAction("Login", "Account");
            var p = _db.Patients.Find(Pid());
            if (p == null) return View("NoRecord");
            return View(p);
        }
    }
}
